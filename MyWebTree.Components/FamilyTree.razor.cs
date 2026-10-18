using System.Globalization;
using Ardalis.Result;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MyWebTree.Graph;
using MyWebTree.Layout;
using MyWebTree.Models;
using MyWebTree.Services;

namespace MyWebTree.Components;

public partial class FamilyTree : ComponentBase, IAsyncDisposable
{
    private readonly List<Snapshot> undo = [];
    private readonly List<Snapshot> redo = [];
    private readonly HashSet<string> knownIds = new(StringComparer.Ordinal);

    private TreeLayoutResult layout = TreeLayoutResult.Empty;
    private EditorState? editor;
    private IJSObjectReference? module;
    private DotNetObjectReference<FamilyTree>? self;
    private ElementReference viewport;
    private string? selectedId;
    private string? lastFocus;
    private string? formError;
    private string query = "";
    private int ancestry = 4;
    private int progeny = 4;
    private bool siblings = true;
    private bool dragging;
    private bool saving;
    private bool shouldFit = true;
    private bool userAdjusted;
    private bool fittedOnce;
    private int fitAttempts;
    private bool selectionReady;
    private bool applyingHistory;
    private bool confirmDelete;
    private double panX;
    private double panY = 24;
    private double scale = 1;
    private double originX;
    private double originY;
    private TreeOrientation orientation = TreeOrientation.Vertical;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<PersonData> People { get; set; } = [];

    [Parameter]
    public string? FocusPersonId { get; set; }

    [Parameter]
    public EventCallback<FamilyTreeChange> Changed { get; set; }

    [Parameter]
    public bool Editable { get; set; } = true;

    [Parameter]
    public string Theme { get; set; } = "auto";

    [Parameter]
    public string Height { get; set; } = "720px";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Error { get; set; }

    /// <summary>Drawn with a second outline, for example the person who belongs to the signed-in user.</summary>
    [Parameter]
    public string? HighlightedPersonId { get; set; }

    /// <summary>Host navigation. The button is hidden when nothing is listening.</summary>
    [Parameter]
    public EventCallback<PersonData> OnOpenPerson { get; set; }

    [Parameter]
    public string OpenPersonText { get; set; } = "Open";

    /// <summary>
    /// When set, the editor shows "Choose picture" and stores whatever address the host returns.
    /// Return null to leave the current address alone.
    /// </summary>
    [Parameter]
    public Func<AvatarRequest, Task<string?>>? ChooseAvatar { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private PersonData? Selected => People.FirstOrDefault(person => person.Id == selectedId);

    private List<PersonData> Matches => string.IsNullOrWhiteSpace(query)
        ? []
        : People.Where(person =>
        {
            string term = query.Trim();
            return
            person.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || (person.BirthPlace?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || (person.DeathPlace?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || person.Lifespan.Contains(term);
        })
            .Take(8)
            .ToList();

    private List<PersonData> OtherParents
    {
        get
        {
            if (editor?.RelatesToId is null)
            {
                return [];
            }

            var anchor = People.FirstOrDefault(person => person.Id == editor.RelatesToId);
            return anchor is null
                ? []
                : anchor.Spouses.Select(id => People.FirstOrDefault(person => person.Id == id)).OfType<PersonData>().ToList();
        }
    }

    private bool GenderLocked => editor?.Kind is RelativeKind.Father or RelativeKind.Mother or RelativeKind.Son or RelativeKind.Daughter;

    private bool AskOtherParent =>
        editor?.Kind is RelativeKind.Child or RelativeKind.Son or RelativeKind.Daughter && OtherParents.Count > 1;

    private string EditorTitle => editor?.PersonId is not null
        ? "Edit"
        : editor?.Kind switch
        {
            RelativeKind.Father => "Add father",
            RelativeKind.Mother => "Add mother",
            RelativeKind.Spouse => "Add spouse",
            RelativeKind.Son => "Add son",
            RelativeKind.Daughter => "Add daughter",
            RelativeKind.Child => "Add child",
            _ => "Add person",
        };

    private string RootStyle => string.Create(
        CultureInfo.InvariantCulture,
        $"height:{Height};--mwt-card-w:{TreeLayoutCalculator.CardWidth}px;--mwt-card-h:{TreeLayoutCalculator.CardHeight}px");

    private string WorldStyle => string.Create(
        CultureInfo.InvariantCulture,
        $"width:{layout.Width:0.###}px;height:{layout.Height:0.###}px;transform:translate({panX:0.###}px,{panY:0.###}px) scale({scale:0.###})");

    private int ZoomPercent => (int)Math.Round(scale * 100);

    protected override void OnParametersSet()
    {
        Rebuild();
        if (!fittedOnce || FocusPersonId != lastFocus)
        {
            shouldFit = true;
            userAdjusted = false;
            fittedOnce = true;
            lastFocus = FocusPersonId;
        }

        TrackSelection();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/MyWebTree.Components/mywebtree.js");
            self = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("watch", viewport, self);
            await module.InvokeVoidAsync("bindWheel", viewport, self);
        }

        if (!shouldFit || module is null)
        {
            return;
        }

        var box = await module.InvokeAsync<ViewportSize>("size", viewport);
        if (box.Width < 10 || box.Height < 10)
        {
            if (fitAttempts++ < 10)
            {
                await Task.Delay(50);
                StateHasChanged();
            }

            return;
        }

        fitAttempts = 0;
        shouldFit = false;
        Fit(box.Width, box.Height);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnViewportResize()
    {
        if (userAdjusted || dragging || module is null)
        {
            return;
        }

        var box = await MeasureAsync();
        if (box.Width < 10 || box.Height < 10)
        {
            return;
        }

        Fit(box.Width, box.Height);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("unwatch", viewport);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        self?.Dispose();
    }

    private void Rebuild()
    {
        layout = TreeLayoutCalculator.Calculate(People, new TreeLayoutOptions
        {
            FocusId = FocusPersonId,
            AncestryDepth = ancestry,
            ProgenyDepth = progeny,
            ShowSiblings = siblings,
            ShowPlaceholders = Editable,
            Orientation = orientation,
        });
    }

    private void TrackSelection()
    {
        var incoming = People.Select(person => person.Id).ToHashSet(StringComparer.Ordinal);
        if (!selectionReady)
        {
            selectedId ??= FocusPersonId is not null && incoming.Contains(FocusPersonId)
                ? FocusPersonId
                : People.FirstOrDefault()?.Id;
            knownIds.Clear();
            knownIds.UnionWith(incoming);
            selectionReady = People.Count > 0;
            return;
        }

        if (selectedId is not null && !incoming.Contains(selectedId) && !selectedId.StartsWith('+'))
        {
            selectedId = People.FirstOrDefault(person => !knownIds.Contains(person.Id))?.Id
                ?? (FocusPersonId is not null && incoming.Contains(FocusPersonId) ? FocusPersonId : null);
        }

        knownIds.Clear();
        knownIds.UnionWith(incoming);
    }

    private PersonData? Person(string id) => People.FirstOrDefault(person => person.Id == id);

    private string? Kinship(string id) => layout.Kinship.TryGetValue(id, out string? label) ? label : null;

    private string CardClass(TreeCard card)
    {
        var gender = Person(card.Id)?.Gender ?? (card.Placeholder switch
        {
            PlaceholderKind.Father => Gender.Male,
            PlaceholderKind.Mother => Gender.Female,
            _ => Gender.Unknown,
        });

        string tone = gender switch
        {
            Gender.Male => "mwt-male",
            Gender.Female => "mwt-female",
            _ => "mwt-unknown",
        };

        string focus = card.Id == layout.FocusId ? " mwt-focus" : "";
        string selected = card.Id == selectedId ? " mwt-selected" : "";
        string marked = !card.IsPlaceholder && card.Id == HighlightedPersonId ? " mwt-marked" : "";
        string placeholder = card.IsPlaceholder ? " mwt-placeholder" : "";
        return "mwt-card " + tone + focus + selected + marked + placeholder;
    }

    private static string CardTitle(TreeCard card, PersonData? person) =>
        card.IsPlaceholder ? PlaceholderLabel(card.Placeholder) : person?.DisplayName ?? "Person";

    private static string PlaceholderLabel(PlaceholderKind? kind) => kind switch
    {
        PlaceholderKind.Father => "Add father",
        PlaceholderKind.Mother => "Add mother",
        PlaceholderKind.Spouse => "Add spouse",
        PlaceholderKind.Child => "Add child",
        _ => "Add relative",
    };

    private static string GenderWord(Gender gender) => gender switch
    {
        Gender.Male => "Male",
        Gender.Female => "Female",
        _ => "Unknown",
    };

    private static string Place(TreeCard card) =>
        string.Create(CultureInfo.InvariantCulture, $"left:{card.X:0.###}px;top:{card.Y:0.###}px");

    private static string Px(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Part(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";

    private void SelectCard(TreeCard card)
    {
        selectedId = card.Id;
        confirmDelete = false;
        formError = null;
        if (card.IsPlaceholder)
        {
            OpenAdd(card.Placeholder ?? PlaceholderKind.Child, card.RelatesToId);
        }
        else
        {
            editor = null;
        }
    }

    private Task FocusCardAsync(TreeCard card) =>
        card.IsPlaceholder ? Task.CompletedTask : FocusAsync(card.Id);

    private void AddFirst() => editor = new EditorState();

    private void EditSelected()
    {
        if (Selected is not { } person)
        {
            return;
        }

        confirmDelete = false;
        editor = EditorState.From(person);
    }

    private void OpenAdd(PlaceholderKind kind, string? relatesTo, Gender? childGender = null)
    {
        var anchor = relatesTo is null ? null : People.FirstOrDefault(person => person.Id == relatesTo);
        var relative = kind switch
        {
            PlaceholderKind.Father => RelativeKind.Father,
            PlaceholderKind.Mother => RelativeKind.Mother,
            PlaceholderKind.Spouse => RelativeKind.Spouse,
            _ => childGender switch
            {
                Gender.Male => RelativeKind.Son,
                Gender.Female => RelativeKind.Daughter,
                _ => RelativeKind.Child,
            },
        };

        editor = new EditorState
        {
            Kind = relative,
            RelatesToId = relatesTo,
            Gender = relative switch
            {
                RelativeKind.Father or RelativeKind.Son => Gender.Male,
                RelativeKind.Mother or RelativeKind.Daughter => Gender.Female,
                RelativeKind.Spouse when anchor?.Gender == Gender.Male => Gender.Female,
                RelativeKind.Spouse when anchor?.Gender == Gender.Female => Gender.Male,
                _ => Gender.Unknown,
            },
            OtherParentId = kind == PlaceholderKind.Child
                ? anchor?.Spouses.FirstOrDefault(id => People.Any(person => person.Id == id))
                : null,
        };
        formError = null;
        confirmDelete = false;
    }

    private void CloseEditor()
    {
        editor = null;
        formError = null;
        if (selectedId?.StartsWith('+') == true)
        {
            selectedId = FocusPersonId;
        }
    }

    private bool CanAdd(RelativeKind kind)
    {
        if (!Editable || Selected is not { } person)
        {
            return false;
        }

        return kind switch
        {
            RelativeKind.Father => person.Parents.Count < 2 && !HasParent(person, Gender.Male),
            RelativeKind.Mother => person.Parents.Count < 2 && !HasParent(person, Gender.Female),
            _ => true,
        };
    }

    private bool HasParent(PersonData person, Gender gender) =>
        person.Parents.Any(id => People.FirstOrDefault(parent => parent.Id == id)?.Gender == gender);

    private async Task SaveEditorAsync()
    {
        if (editor is null || saving)
        {
            return;
        }

        if (!editor.TryToModel(out var model, out string? dateError))
        {
            formError = dateError;
            return;
        }

        Result<IReadOnlyList<PersonData>> result;
        if (editor.PersonId is not null)
        {
            result = FamilyGraphEditor.Update(People, editor.PersonId, model);
        }
        else if (editor.Kind is null || editor.RelatesToId is null)
        {
            result = FamilyGraphEditor.AddPerson(People, model);
            selectedId = result.IsSuccess ? result.Value.LastOrDefault()?.Id : selectedId;
        }
        else
        {
            result = FamilyGraphEditor.AddRelative(People, new AddRelativeRequest
            {
                RelatesToPersonId = editor.RelatesToId,
                Kind = editor.Kind.Value,
                OtherParentId = string.IsNullOrWhiteSpace(editor.OtherParentId) ? null : editor.OtherParentId,
                Person = model,
            });
            if (result.IsSuccess)
            {
                selectedId = result.Value.Select(person => person.Id).Except(People.Select(person => person.Id)).FirstOrDefault() ?? selectedId;
            }
        }

        if (!result.IsSuccess)
        {
            formError = result.GetDisplayMessage();
            return;
        }

        string? changedId = editor.PersonId ?? selectedId;
        var kind = editor.PersonId is null ? FamilyTreeChangeKind.Added : FamilyTreeChangeKind.Updated;
        editor = null;
        formError = null;
        await PublishAsync(result.Value, FocusPersonId, kind, changedId);
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } person || saving)
        {
            return;
        }

        var result = FamilyGraphEditor.Remove(People, person.Id);
        if (!result.IsSuccess)
        {
            formError = result.GetDisplayMessage();
            return;
        }

        string? focus = FocusPersonId == person.Id ? result.Value.FirstOrDefault()?.Id : FocusPersonId;
        selectedId = focus;
        confirmDelete = false;
        await PublishAsync(result.Value, focus, FamilyTreeChangeKind.Removed, person.Id);
    }

    private async Task UnlinkAsync(string personId, string otherId)
    {
        if (saving)
        {
            return;
        }

        var result = FamilyGraphEditor.Unlink(People, personId, otherId);
        if (!result.IsSuccess)
        {
            formError = result.GetDisplayMessage();
            return;
        }

        formError = null;
        await PublishAsync(result.Value, FocusPersonId, FamilyTreeChangeKind.Unlinked, personId, otherId);
    }

    private Task OpenSelectedAsync(PersonData person) => OnOpenPerson.InvokeAsync(person);

    private async Task ChooseAvatarAsync()
    {
        if (editor is null || ChooseAvatar is null)
        {
            return;
        }

        string? url = await ChooseAvatar(new AvatarRequest(editor.PersonId, editor.AvatarUrl));
        if (url is not null)
        {
            editor.AvatarUrl = url;
        }
    }

    public Task CenterOnAsync(string personId)
    {
        if (string.IsNullOrWhiteSpace(personId) || People.All(person => person.Id != personId))
        {
            return Task.CompletedTask;
        }

        selectedId = personId;
        editor = null;
        query = string.Empty;
        confirmDelete = false;
        if (personId == FocusPersonId)
        {
            shouldFit = true;
            userAdjusted = false;
            StateHasChanged();
            return Task.CompletedTask;
        }

        return FocusAsync(personId);
    }

    private Task FocusAsync(string personId) =>
        personId == FocusPersonId
            ? Task.CompletedTask
            : PublishAsync(People, personId, FamilyTreeChangeKind.FocusChanged, personId);

    private Task JumpToAsync(PersonData person)
    {
        selectedId = person.Id;
        query = string.Empty;
        editor = null;
        return FocusAsync(person.Id);
    }

    private async Task UndoAsync()
    {
        if (undo.Count == 0 || saving)
        {
            return;
        }

        redo.Add(new Snapshot(People.ToList(), FocusPersonId));
        var snapshot = undo[^1];
        undo.RemoveAt(undo.Count - 1);
        await RestoreAsync(snapshot);
    }

    private async Task RedoAsync()
    {
        if (redo.Count == 0 || saving)
        {
            return;
        }

        undo.Add(new Snapshot(People.ToList(), FocusPersonId));
        var snapshot = redo[^1];
        redo.RemoveAt(redo.Count - 1);
        await RestoreAsync(snapshot);
    }

    private async Task RestoreAsync(Snapshot snapshot)
    {
        applyingHistory = true;
        selectedId = snapshot.FocusId;
        try
        {
            await PublishAsync(snapshot.People, snapshot.FocusId);
        }
        finally
        {
            applyingHistory = false;
        }
    }

    private async Task PublishAsync(
        IReadOnlyList<PersonData> next,
        string? focus,
        FamilyTreeChangeKind kind = FamilyTreeChangeKind.Snapshot,
        string? personId = null,
        string? otherPersonId = null)
    {
        if (!applyingHistory)
        {
            undo.Add(new Snapshot(People.ToList(), FocusPersonId));
            if (undo.Count > 40)
            {
                undo.RemoveAt(0);
            }

            redo.Clear();
        }

        saving = true;
        try
        {
            await Changed.InvokeAsync(new FamilyTreeChange(next, focus, kind, personId, otherPersonId));
        }
        finally
        {
            saving = false;
        }
    }

    private void SetDepth(bool descendants, int delta)
    {
        if (descendants)
        {
            progeny = Math.Clamp(progeny + delta, 0, 8);
        }
        else
        {
            ancestry = Math.Clamp(ancestry + delta, 0, 8);
        }

        Rebuild();
        shouldFit = true;
        userAdjusted = false;
    }

    private void ToggleSiblings()
    {
        siblings = !siblings;
        Rebuild();
        shouldFit = true;
        userAdjusted = false;
    }

    private void ToggleOrientation()
    {
        orientation = orientation == TreeOrientation.Vertical ? TreeOrientation.Horizontal : TreeOrientation.Vertical;
        Rebuild();
        shouldFit = true;
        userAdjusted = false;
    }

    private void SetBirthYear(ChangeEventArgs e) => SetYear(true, e);

    private void SetBirthMonth(ChangeEventArgs e) => SetMonth(true, e);

    private void SetBirthDay(ChangeEventArgs e) => SetDay(true, e);

    private void SetDeathYear(ChangeEventArgs e) => SetYear(false, e);

    private void SetDeathMonth(ChangeEventArgs e) => SetMonth(false, e);

    private void SetDeathDay(ChangeEventArgs e) => SetDay(false, e);

    private void SetYear(bool birth, ChangeEventArgs e)
    {
        if (editor is null)
        {
            return;
        }

        int? year = ParsePart(e);
        if (birth)
        {
            editor.BirthYear = year;
            if (year is null)
            {
                editor.BirthMonth = null;
                editor.BirthDay = null;
            }
        }
        else
        {
            editor.DeathYear = year;
            if (year is null)
            {
                editor.DeathMonth = null;
                editor.DeathDay = null;
            }
        }
    }

    private void SetMonth(bool birth, ChangeEventArgs e)
    {
        if (editor is null)
        {
            return;
        }

        if (birth)
        {
            editor.BirthMonth = ParsePart(e);
        }
        else
        {
            editor.DeathMonth = ParsePart(e);
        }
    }

    private void SetDay(bool birth, ChangeEventArgs e)
    {
        if (editor is null)
        {
            return;
        }

        if (birth)
        {
            editor.BirthDay = ParsePart(e);
        }
        else
        {
            editor.DeathDay = ParsePart(e);
        }
    }

    private static int? ParsePart(ChangeEventArgs e)
    {
        string? text = e.Value?.ToString();
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : null;
    }

    private void PanStart(PointerEventArgs e)
    {
        if (e.Button != 0)
        {
            return;
        }

        dragging = true;
        originX = e.ClientX;
        originY = e.ClientY;
    }

    private void PanMove(PointerEventArgs e)
    {
        if (!dragging)
        {
            return;
        }

        panX += e.ClientX - originX;
        panY += e.ClientY - originY;
        originX = e.ClientX;
        originY = e.ClientY;
        userAdjusted = true;
    }

    private void PanEnd(PointerEventArgs e) => dragging = false;

    [JSInvokable]
    public void ApplyView(double x, double y, double nextScale)
    {
        panX = x;
        panY = y;
        scale = nextScale;
        userAdjusted = true;
        StateHasChanged();
    }

    private async Task ZoomByAsync(double factor)
    {
        userAdjusted = true;
        var box = await MeasureAsync();
        double cx = box.Width / 2;
        double cy = box.Height / 2;
        double next = Math.Clamp(scale * factor, 0.12, 2.2);
        double worldX = (cx - panX) / scale;
        double worldY = (cy - panY) / scale;
        scale = next;
        panX = cx - (worldX * scale);
        panY = cy - (worldY * scale);
    }

    private async Task FitAsync()
    {
        userAdjusted = false;
        var box = await MeasureAsync();
        Fit(box.Width, box.Height);
    }

    private async Task<ViewportSize> MeasureAsync()
    {
        if (module is null)
        {
            return new ViewportSize();
        }

        return await module.InvokeAsync<ViewportSize>("size", viewport);
    }

    private void Fit(double viewWidth, double viewHeight)
    {
        if (layout.Width <= 1 || layout.Height <= 1 || viewWidth <= 1 || viewHeight <= 1)
        {
            scale = 1;
            return;
        }

        const double pad = 48;
        scale = Math.Clamp(Math.Min((viewWidth - pad) / layout.Width, (viewHeight - pad) / layout.Height), 0.12, 1.15);
        panX = (viewWidth - (layout.Width * scale)) / 2;
        panY = (viewHeight - (layout.Height * scale)) / 2;
    }

    private sealed class ViewportSize
    {
        public double Width { get; set; }

        public double Height { get; set; }
    }

    private sealed record Snapshot(IReadOnlyList<PersonData> People, string? FocusId);

    private sealed class EditorState
    {
        public string? PersonId { get; set; }

        public RelativeKind? Kind { get; set; }

        public string? RelatesToId { get; set; }

        public string? OtherParentId { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public Gender Gender { get; set; }

        public int? BirthYear { get; set; }

        public int? BirthMonth { get; set; }

        public int? BirthDay { get; set; }

        public int? DeathYear { get; set; }

        public int? DeathMonth { get; set; }

        public int? DeathDay { get; set; }

        public string? BirthPlace { get; set; }

        public string? DeathPlace { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Notes { get; set; }

        public bool TryToModel(out PersonWriteModel model, out string? error)
        {
            if (!TryDate(BirthYear, BirthMonth, BirthDay, "Birth", out var birth, out error)
                || !TryDate(DeathYear, DeathMonth, DeathDay, "Death", out var death, out error))
            {
                model = new PersonWriteModel();
                return false;
            }

            model = new PersonWriteModel
            {
                Gender = Gender,
                FirstName = FirstName,
                LastName = LastName,
                BirthDate = birth,
                DeathDate = death,
                BirthPlace = BirthPlace,
                DeathPlace = DeathPlace,
                AvatarUrl = AvatarUrl,
                Notes = Notes,
            };
            return true;
        }

        public static EditorState From(PersonData person) => new()
        {
            PersonId = person.Id,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Gender = person.Gender,
            BirthYear = person.BirthDate?.Year,
            BirthMonth = person.BirthDate?.Month,
            BirthDay = person.BirthDate?.Day,
            DeathYear = person.DeathDate?.Year,
            DeathMonth = person.DeathDate?.Month,
            DeathDay = person.DeathDate?.Day,
            BirthPlace = person.BirthPlace,
            DeathPlace = person.DeathPlace,
            AvatarUrl = person.AvatarUrl,
            Notes = person.Notes,
        };

        private static bool TryDate(int? year, int? month, int? day, string label, out PartialDate? date, out string? error)
        {
            date = null;
            error = null;
            if (year is null && month is null && day is null)
            {
                return true;
            }

            if (year is null)
            {
                error = label + " needs a year.";
                return false;
            }

            if (!PartialDate.TryCreate(year.Value, month, day, out var parsed))
            {
                error = label + " is not a real date.";
                return false;
            }

            date = parsed;
            return true;
        }
    }
}
