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

/// <summary>
/// Represents the family tree.
/// </summary>
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
    private const int DefaultDepth = 4;
    private const string NewFamily = "new";

    // The people as the component reasons about them: a legacy Parents list is read as one primary birth set.
    private IReadOnlyList<PersonData> graph = [];
    private Dictionary<string, string> activeSets = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, string>? activeSetsFromHost;
    private bool bloodOnly;
    private bool bloodOnlyFromHost;
    private string? familiesOpenFor;
    private string? removeSetId;
    private ParentChoice choice = new();

    private int ancestry = DefaultDepth;
    private int progeny = DefaultDepth;
    private int ancestryFromHost = DefaultDepth;
    private int progenyFromHost = DefaultDepth;
    private bool siblings;
    private bool siblingsFromHost;
    private LineageScope lineage = LineageScope.Bilateral;
    private LineageScope lineageFromHost = LineageScope.Bilateral;
    private bool dragging;
    private bool saving;
    private bool shouldFit = true;
    private bool userAdjusted;
    private bool fittedOnce;
    private int fitAttempts;
    private bool selectionReady;
    private bool applyingHistory;
    private bool confirmDelete;
    private UnlinkTarget? unlinkTarget;
    private LinkKind? pendingLink;
    private string? linkAnchorId;
    private string? linkTargetId;
    private bool confirmLink;
    private double panX;
    private double panY = 24;
    private double scale = 1;
    private double originX;
    private double originY;
    private TreeOrientation orientation = TreeOrientation.Vertical;
    private TreeOrientation orientationFromHost = TreeOrientation.Vertical;

    /// <summary>
    /// Gets or sets the js.
    /// </summary>
    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Gets or sets the people.
    /// </summary>
    [Parameter]
    public IReadOnlyList<PersonData> People { get; set; } = [];

    /// <summary>
    /// Gets or sets the focus person identifier.
    /// </summary>
    [Parameter]
    public string? FocusPersonId { get; set; }

    /// <summary>
    /// Gets or sets the changed.
    /// </summary>
    [Parameter]
    public EventCallback<FamilyTreeChange> Changed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is editable.
    /// </summary>
    [Parameter]
    public bool Editable { get; set; } = true;

    /// <summary>
    /// Gets or sets the theme.
    /// </summary>
    [Parameter]
    public string Theme { get; set; } = "auto";

    /// <summary>Both sides of the family, or only the father's line. The toolbar can change it afterwards.</summary>
    [Parameter]
    public LineageScope Lineage { get; set; } = LineageScope.Bilateral;

    /// <summary>Draw brothers and sisters beside the focus person. The toolbar can change it afterwards.</summary>
    [Parameter]
    public bool ShowSiblings { get; set; }

    /// <summary>Down the page, or across it. The toolbar can change it afterwards.</summary>
    [Parameter]
    public TreeOrientation Orientation { get; set; } = TreeOrientation.Vertical;

    /// <summary>
    /// Generations of ancestors to show. The toolbar can change it afterwards, and <see cref="DepthsChanged"/> reports
    /// that, so a host can remember the choice and pass it back here next time.
    /// </summary>
    [Parameter]
    public int AncestryDepth { get; set; } = DefaultDepth;

    /// <summary>Generations of descendants to show. See <see cref="AncestryDepth"/>.</summary>
    [Parameter]
    public int ProgenyDepth { get; set; } = DefaultDepth;

    /// <summary>The most generations the toolbar allows in either direction.</summary>
    [Parameter]
    public int MaxGenerations { get; set; } = 8;

    /// <summary>Raised when the user steps either generation count up or down.</summary>
    [Parameter]
    public EventCallback<TreeDepths> DepthsChanged { get; set; }

    /// <summary>
    /// Raised when the user changes generations, siblings, lineage or orientation.
    /// Pass the value back through the matching parameters next time.
    /// </summary>
    [Parameter]
    public EventCallback<TreeView> ViewChanged { get; set; }

    /// <summary>
    /// The set of parents drawn for each person, as person id to set id. A person with no entry, or a stale one,
    /// is drawn under their primary set. Choosing another set on the chart changes only this view, never
    /// <see cref="People"/>, and is reported through <see cref="ActiveParentSetsChanged"/>.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, string>? ActiveParentSets { get; set; }

    /// <summary>
    /// Raised with the whole person id to set id map when the user picks another family for someone.
    /// <see cref="Changed"/> is not raised for this, because nobody's data changed.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyDictionary<string, string>> ActiveParentSetsChanged { get; set; }

    /// <summary>
    /// Hide adopted and foster links, and everyone who can only be reached through them. The toolbar offers
    /// the same switch once the tree has such a link, and <see cref="ViewChanged"/> reports it.
    /// </summary>
    [Parameter]
    public bool BloodRelativesOnly { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    [Parameter]
    public string Height { get; set; } = "720px";

    /// <summary>
    /// Gets or sets the class.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    [Parameter]
    public string? Error { get; set; }

    /// <summary>Drawn with a second outline, for example the person who belongs to the signed-in user.</summary>
    [Parameter]
    public string? HighlightedPersonId { get; set; }

    /// <summary>Host navigation. The button is hidden when nothing is listening.</summary>
    [Parameter]
    public EventCallback<PersonData> OnOpenPerson { get; set; }

    /// <summary>
    /// Gets or sets the open person text.
    /// </summary>
    [Parameter]
    public string OpenPersonText { get; set; } = "Open";

    /// <summary>
    /// When set, the editor shows "Choose picture" and stores whatever address the host returns.
    /// Return null to leave the current address alone.
    /// </summary>
    [Parameter]
    public Func<AvatarRequest, Task<string?>>? ChooseAvatar { get; set; }

    /// <summary>
    /// Gets or sets the additional attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private PersonData? Selected => graph.FirstOrDefault(person => person.Id == selectedId);

    private List<PersonData> Matches => string.IsNullOrWhiteSpace(query)
        ? []
        : graph.Where(person =>
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

            var anchor = graph.FirstOrDefault(person => person.Id == editor.RelatesToId);
            return anchor is null
                ? []
                : anchor.Spouses.Select(id => graph.FirstOrDefault(person => person.Id == id)).OfType<PersonData>().ToList();
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

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        graph = ParentSetRules.Normalize(People);

        // Same rule as the toolbar choices below: a map from the host replaces ours only when the host changes it.
        if (!ReferenceEquals(ActiveParentSets, activeSetsFromHost))
        {
            activeSetsFromHost = ActiveParentSets;
            activeSets = ActiveParentSets is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(ActiveParentSets, StringComparer.Ordinal);
        }

        if (BloodRelativesOnly != bloodOnlyFromHost)
        {
            bloodOnlyFromHost = BloodRelativesOnly;
            bloodOnly = BloodRelativesOnly;
        }

        if (Lineage != lineageFromHost)
        {
            lineageFromHost = Lineage;
            lineage = Lineage;
        }

        if (ShowSiblings != siblingsFromHost)
        {
            siblingsFromHost = ShowSiblings;
            siblings = ShowSiblings;
        }

        if (Orientation != orientationFromHost)
        {
            orientationFromHost = Orientation;
            orientation = Orientation;
        }

        // Same rule as Lineage: a value from the host replaces the toolbar's only when the host changes it.
        if (AncestryDepth != ancestryFromHost)
        {
            ancestryFromHost = AncestryDepth;
            ancestry = AncestryDepth;
        }

        if (ProgenyDepth != progenyFromHost)
        {
            progenyFromHost = ProgenyDepth;
            progeny = ProgenyDepth;
        }

        // The limit can be lowered after the tree is up, so the current values are checked every time.
        ancestry = Math.Clamp(ancestry, 0, MaxDepth);
        progeny = Math.Clamp(progeny, 0, MaxDepth);

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

    /// <inheritdoc/>
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

        var box = await module.InvokeAsync<ViewportSize?>("size", viewport) ?? new ViewportSize();
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

    /// <summary>
    /// Handles the viewport resize event.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
        layout = TreeLayoutCalculator.Calculate(graph, new TreeLayoutOptions
        {
            FocusId = FocusPersonId,
            AncestryDepth = ancestry,
            ProgenyDepth = progeny,
            ShowSiblings = siblings,
            Lineage = lineage,
            ShowPlaceholders = Editable,
            Orientation = orientation,
            ActiveParentSets = activeSets,
            BloodRelativesOnly = bloodOnly,
        });
    }

    private void TrackSelection()
    {
        var incoming = graph.Select(person => person.Id).ToHashSet(StringComparer.Ordinal);
        if (!selectionReady)
        {
            selectedId ??= FocusPersonId is not null && incoming.Contains(FocusPersonId)
                ? FocusPersonId
                : graph.FirstOrDefault()?.Id;
            knownIds.Clear();
            knownIds.UnionWith(incoming);
            selectionReady = graph.Count > 0;
            return;
        }

        if (selectedId is not null && !incoming.Contains(selectedId) && !selectedId.StartsWith('+'))
        {
            selectedId = graph.FirstOrDefault(person => !knownIds.Contains(person.Id))?.Id
                ?? (FocusPersonId is not null && incoming.Contains(FocusPersonId) ? FocusPersonId : null);
        }

        knownIds.Clear();
        knownIds.UnionWith(incoming);

        if (linkAnchorId is not null && !incoming.Contains(linkAnchorId))
        {
            CancelLink();
        }

        if (linkTargetId is not null && !incoming.Contains(linkTargetId))
        {
            CancelLinkTarget();
        }

        if (unlinkTarget is not null && (selectedId is null || !incoming.Contains(unlinkTarget.OtherId)))
        {
            unlinkTarget = null;
        }

        if (removeSetId is not null && Selected?.ParentSets.All(set => set.Id != removeSetId) != false)
        {
            removeSetId = null;
        }

        if (familiesOpenFor is not null && !incoming.Contains(familiesOpenFor))
        {
            familiesOpenFor = null;
        }
    }

    private PersonData? Person(string id) => graph.FirstOrDefault(person => person.Id == id);

    private string? Kinship(string id) => layout.Kinship.TryGetValue(id, out string? label) ? label : null;

    /// <summary>The person a card stands for. A stub's own id is not a person, so it answers with the real one.</summary>
    private static string RealId(TreeCard card) => card.PersonId ?? card.Id;

    private string CardClass(TreeCard card)
    {
        var gender = Person(RealId(card))?.Gender ?? (card.Placeholder switch
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

        // A stub is a second drawing of someone, so only the real card carries the focus and selection marks.
        string focus = !card.IsStub && card.Id == layout.FocusId ? " mwt-focus" : "";
        string selected = !card.IsStub && card.Id == selectedId ? " mwt-selected" : "";
        string marked = !card.IsPlaceholder && !card.IsStub && card.Id == HighlightedPersonId ? " mwt-marked" : "";
        string placeholder = card.IsPlaceholder ? " mwt-placeholder" : "";
        string stub = card.IsStub ? " mwt-stub" : "";
        string anchor = pendingLink is not null && RealId(card) == linkAnchorId ? " mwt-link-anchor" : "";
        string target = confirmLink && RealId(card) == linkTargetId ? " mwt-link-target" : "";
        return "mwt-card " + tone + focus + selected + marked + placeholder + stub + anchor + target;
    }

    /// <summary>The badge text for a link that is not a birth link, or null when there is nothing to show.</summary>
    private static string? BadgeLetter(TreeCard card) =>
        card.ParentSetId is null ? null : card.ParentType switch
        {
            ParentageType.Adopted => "A",
            ParentageType.Foster => "F",
            _ => null,
        };

    private static string TypeWord(ParentageType type) => type switch
    {
        ParentageType.Adopted => "Adopted",
        ParentageType.Foster => "Foster",
        _ => "Birth",
    };

    private static string LinkClass(TreeLink link)
    {
        if (link.Dashed)
        {
            return "mwt-ghost";
        }

        return link.Type switch
        {
            ParentageType.Adopted => "mwt-adopted",
            ParentageType.Foster => "mwt-foster",
            _ => "",
        };
    }

    private string CardTitle(TreeCard card, PersonData? person)
    {
        if (card.IsStub && person is not null && pendingLink is null)
        {
            return person.DisplayName + " (" + TypeWord(card.ParentType).ToLowerInvariant() + " child here" + (card.SeeAlso ? ", also shown elsewhere" : "") + ")";
        }

        if (pendingLink is not null && !card.IsPlaceholder && RealId(card) != linkAnchorId && person is not null)
        {
            string name = Anchor?.DisplayName ?? "the selected person";
            return pendingLink switch
            {
                LinkKind.Parent => "Parent of " + name,
                LinkKind.Spouse => "Spouse of " + name,
                LinkKind.Child => "Child of " + name,
                _ => person.DisplayName,
            };
        }

        return card.IsPlaceholder ? PlaceholderLabel(card.Placeholder) : person?.DisplayName ?? "Person";
    }

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

    private Task SelectCard(TreeCard card)
    {
        familiesOpenFor = null;
        if (pendingLink is not null)
        {
            ChooseLinkTarget(RealId(card), card.IsPlaceholder);
            return Task.CompletedTask;
        }

        selectedId = RealId(card);
        confirmDelete = false;
        unlinkTarget = null;
        removeSetId = null;
        formError = null;
        if (card.IsPlaceholder)
        {
            OpenAdd(card.Placeholder ?? PlaceholderKind.Child, card.RelatesToId);
            return Task.CompletedTask;
        }

        editor = null;

        // A stub only selects: the panel then shows the person's families, and the card's own buttons do the rest.
        return card.IsStub ? Task.CompletedTask : FocusAsync(card.Id);
    }

    private Task FocusCardAsync(TreeCard card) =>
        pendingLink is not null || card.IsPlaceholder ? Task.CompletedTask : FocusAsync(RealId(card));

    private void AddFirst() => editor = new EditorState();

    private void EditSelected()
    {
        if (Selected is not { } person)
        {
            return;
        }

        CancelLink();
        confirmDelete = false;
        unlinkTarget = null;
        editor = EditorState.From(person);
    }

    private void OpenAdd(PlaceholderKind kind, string? relatesTo, Gender? childGender = null)
    {
        CancelLink();
        unlinkTarget = null;
        var anchor = relatesTo is null ? null : graph.FirstOrDefault(person => person.Id == relatesTo);
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
                ? anchor?.Spouses.FirstOrDefault(id => graph.Any(person => person.Id == id))
                : null,
        };
        formError = null;
        confirmDelete = false;
        removeSetId = null;
        if (anchor is not null && relative is RelativeKind.Father or RelativeKind.Mother)
        {
            choice = DefaultChoice(anchor, relative == RelativeKind.Father ? Gender.Male : Gender.Female, null);
        }
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

    // A person who already has two parents can still be given a father or mother in a new family, so no kind is ruled out.
    private bool CanAdd(RelativeKind kind) => Editable && Selected is not null;

    private bool CanLink(LinkKind kind) => Editable && Selected is not null && graph.Count >= 2;

    private string LinkButtonTitle(LinkKind kind) => kind switch
    {
        LinkKind.Parent => "Link an existing person as a parent.",
        LinkKind.Spouse => "Link an existing person as a spouse.",
        LinkKind.Child => "Link an existing person as a child.",
        _ => "Link",
    };

    private PersonData? Anchor => linkAnchorId is null ? null : Person(linkAnchorId);

    private PersonData? LinkTarget => linkTargetId is null ? null : Person(linkTargetId);

    private string LinkPrompt
    {
        get
        {
            string name = Anchor?.DisplayName ?? "the selected person";
            return pendingLink switch
            {
                LinkKind.Parent => "Click a parent of " + name + " in the tree, or search for them.",
                LinkKind.Spouse => "Click a spouse of " + name + " in the tree, or search for them.",
                LinkKind.Child => "Click a child of " + name + " in the tree, or search for them.",
                _ => "Click the other person.",
            };
        }
    }

    private string LinkConfirmText
    {
        get
        {
            string anchor = Anchor?.DisplayName ?? "the selected person";
            string other = LinkTarget?.DisplayName ?? "them";
            string question = pendingLink switch
            {
                LinkKind.Parent => "Make " + other + " a parent of " + anchor + "?",
                LinkKind.Spouse => "Link " + other + " as a spouse of " + anchor + "?",
                LinkKind.Child => "Make " + other + " a child of " + anchor + "?",
                _ => "Link these two?",
            };

            string? extra = InducedSpouseNote();
            return extra is null ? question : question + " " + extra;
        }
    }

    private string? InducedSpouseNote()
    {
        if (pendingLink is not (LinkKind.Parent or LinkKind.Child) || Anchor is not { } anchor || LinkTarget is not { } other)
        {
            return null;
        }

        var child = pendingLink == LinkKind.Child ? other : anchor;
        var parent = pendingLink == LinkKind.Child ? anchor : other;

        // The new parent joins the chosen family, and only the other member of that family becomes a spouse.
        var joined = choice.Value == NewFamily ? null : child.ParentSets.FirstOrDefault(set => set.Id == choice.Value);
        string? existingId = joined?.ParentIds.FirstOrDefault(id => id != parent.Id);
        if (existingId is null || parent.Spouses.Contains(existingId))
        {
            return null;
        }

        string existingName = Person(existingId)?.DisplayName ?? "the other parent";
        return "This also records " + parent.DisplayName + " and " + existingName + " as spouses.";
    }

    private string LinkBar =>
        confirmLink && LinkTarget is { } target
            ? "Confirm the link to " + target.DisplayName + " in the side panel."
            : LinkPrompt;

    private void BeginLink(LinkKind kind)
    {
        if (Selected is null)
        {
            return;
        }

        if (pendingLink == kind)
        {
            CancelLink();
            return;
        }

        if (!CanLink(kind))
        {
            return;
        }

        editor = null;
        confirmDelete = false;
        unlinkTarget = null;
        formError = null;
        pendingLink = kind;
        linkAnchorId = Selected.Id;
        linkTargetId = null;
        confirmLink = false;
    }

    private void ChooseLinkTarget(string id, bool placeholder)
    {
        if (placeholder || id == linkAnchorId || graph.All(person => person.Id != id))
        {
            return;
        }

        linkTargetId = id;
        confirmLink = true;
        confirmDelete = false;
        unlinkTarget = null;
        formError = null;

        // A parent link asks which family the parent joins, so the default is worked out once the other end is known.
        if (pendingLink is LinkKind.Parent or LinkKind.Child && Anchor is { } anchor && Person(id) is { } other)
        {
            var (child, parent) = pendingLink == LinkKind.Parent ? (anchor, other) : (other, anchor);
            choice = DefaultChoice(child, Gender.Unknown, parent.Id);
        }
    }

    /// <summary>The child and the parent a pending parent or child link would join, or null for any other link.</summary>
    private (PersonData Child, PersonData Parent)? PendingParentLink =>
        pendingLink is LinkKind.Parent or LinkKind.Child && Anchor is { } anchor && LinkTarget is { } other
            ? pendingLink == LinkKind.Parent ? (anchor, other) : (other, anchor)
            : null;

    private void CancelLink()
    {
        pendingLink = null;
        linkAnchorId = null;
        linkTargetId = null;
        confirmLink = false;
    }

    private void CancelLinkTarget()
    {
        linkTargetId = null;
        confirmLink = false;
    }

    private void AskDelete()
    {
        CancelLink();
        unlinkTarget = null;
        confirmDelete = true;
    }

    private void AskUnlink(string otherId, string? setId = null)
    {
        CancelLink();
        confirmDelete = false;
        removeSetId = null;
        unlinkTarget = new UnlinkTarget(otherId, setId);
    }

    /// <summary>
    /// The side panel's list of spouses and children. Parents are listed by family instead. A child the person
    /// parents in more than one family gets one row for each, so an unlink says which link it means.
    /// </summary>
    private IEnumerable<RelativeRow> Relatives(PersonData person)
    {
        foreach (string id in person.Spouses)
        {
            yield return new RelativeRow("Spouse", id, null);
        }

        foreach (string id in person.Children)
        {
            var holding = Person(id)?.ParentSets.Where(set => set.ParentIds.Contains(person.Id)).ToList() ?? [];
            if (holding.Count == 0)
            {
                yield return new RelativeRow("Child", id, null);
                continue;
            }

            foreach (var set in holding)
            {
                string label = Person(id)!.ParentSets.Count > 1 ? "Child (" + TypeWord(set.Type).ToLowerInvariant() + ")" : "Child";
                yield return new RelativeRow(label, id, set.Id);
            }
        }
    }

    private string SetNames(ParentSetData set) =>
        string.Join(" & ", set.ParentIds.Select(id => Person(id)?.DisplayName).Where(name => name is not null));

    /// <summary>One line describing a family: type, who is in it, and the date when there is one.</summary>
    private string SetSummary(ParentSetData set) =>
        TypeWord(set.Type) + " · " + SetNames(set) + (set.Type != ParentageType.Birth && set.Date is { } date ? " · " + date : "");

    private static bool HasRoom(ParentSetData set, Gender role, Func<string, Gender?> genderOf, string? excludeParentId) =>
        set.ParentIds.Count < 2
        && (excludeParentId is null || !set.ParentIds.Contains(excludeParentId))
        && (role == Gender.Unknown || set.ParentIds.All(id => genderOf(id) != role));

    /// <summary>The families of <paramref name="child"/> a new parent could join.</summary>
    private List<ParentSetData> OpenSets(PersonData child, Gender role, string? excludeParentId) =>
        child.ParentSets.Where(set => HasRoom(set, role, id => Person(id)?.Gender, excludeParentId)).ToList();

    /// <summary>
    /// Joins the family the chart is showing when it has room, then any other with room, and otherwise starts a new
    /// one. A child with no family yet starts a birth family.
    /// </summary>
    private ParentChoice DefaultChoice(PersonData child, Gender role, string? excludeParentId)
    {
        var open = OpenSets(child, role, excludeParentId);
        string? active = ParentSetRules.ActiveOf(child, activeSets)?.Id;
        var pick = open.FirstOrDefault(set => set.Id == active) ?? open.FirstOrDefault();
        bool hasBirth = child.ParentSets.Any(set => set.Type == ParentageType.Birth);
        return new ParentChoice { Value = pick?.Id ?? NewFamily, Type = hasBirth ? ParentageType.Adopted : ParentageType.Birth };
    }

    /// <summary>
    /// A new family defaults to birth parents, unless the child already has them. Birth parents are unique, so the
    /// type moves to adopted when the user asks for another family.
    /// </summary>
    private void CoerceNewFamilyType(PersonData child)
    {
        if (choice.Value == NewFamily
            && choice.Type == ParentageType.Birth
            && child.ParentSets.Any(set => set.Type == ParentageType.Birth))
        {
            choice.Type = ParentageType.Adopted;
        }
    }

    private ParentSetTarget ToTarget(ParentChoice pick) => pick.Value == NewFamily
        ? new ParentSetTarget { CreateNew = true, Type = pick.Type }
        : new ParentSetTarget { ParentSetId = pick.Value };

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
            result = FamilyGraphEditor.Update(graph, editor.PersonId, model);
        }
        else if (editor.Kind is null || editor.RelatesToId is null)
        {
            result = FamilyGraphEditor.AddPerson(graph, model);
            selectedId = result.IsSuccess ? result.Value.LastOrDefault()?.Id : selectedId;
        }
        else
        {
            result = FamilyGraphEditor.AddRelative(graph, new AddRelativeRequest
            {
                RelatesToPersonId = editor.RelatesToId,
                Kind = editor.Kind.Value,
                OtherParentId = string.IsNullOrWhiteSpace(editor.OtherParentId) ? null : editor.OtherParentId,
                Person = model,
                ParentSet = editor.Kind is RelativeKind.Father or RelativeKind.Mother ? ToTarget(choice) : null,
            });
            if (result.IsSuccess)
            {
                selectedId = result.Value.Select(person => person.Id).Except(graph.Select(person => person.Id)).FirstOrDefault() ?? selectedId;
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

        var result = FamilyGraphEditor.Remove(graph, person.Id);
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

    private async Task ConfirmLinkAsync()
    {
        if (pendingLink is not LinkKind kind || linkAnchorId is null || linkTargetId is null || saving)
        {
            return;
        }

        var parentLink = PendingParentLink;
        var target = parentLink is null ? null : ToTarget(choice);
        var result = FamilyGraphEditor.Link(graph, linkAnchorId, linkTargetId, kind, target);
        if (!result.IsSuccess)
        {
            formError = result.GetDisplayMessage();
            CancelLinkTarget();
            return;
        }

        string personId = linkAnchorId;
        string otherId = linkTargetId;
        CancelLink();
        formError = null;

        if (parentLink is null)
        {
            await PublishAsync(result.Value, FocusPersonId, FamilyTreeChangeKind.Linked, personId, otherId, kind);
            return;
        }

        var (before, parent) = parentLink.Value;

        // The family the parent joined: the one that now holds them and did not before.
        var after = result.Value.First(person => person.Id == before.Id);
        var joined = after.ParentSets.FirstOrDefault(set =>
            set.ParentIds.Contains(parent.Id)
            && before.ParentSets.FirstOrDefault(old => old.Id == set.Id)?.ParentIds.Contains(parent.Id) != true);
        bool started = joined is not null && before.ParentSets.All(old => old.Id != joined.Id);

        if (started)
        {
            // A new family is a change to the child, so it is reported as one rather than as a link.
            await PublishAsync(result.Value, FocusPersonId, FamilyTreeChangeKind.ParentSetAdded, before.Id, parent.Id, null, joined!.Id, joined.Type);
            return;
        }

        await PublishAsync(result.Value, FocusPersonId, FamilyTreeChangeKind.Linked, personId, otherId, kind, joined?.Id, joined?.Type);
    }

    private async Task UnlinkAsync(string personId, string otherId, string? setId = null)
    {
        if (saving)
        {
            return;
        }

        var person = Person(personId);
        var other = Person(otherId);
        var result = FamilyGraphEditor.Unlink(graph, personId, otherId, setId);
        if (!result.IsSuccess)
        {
            formError = result.GetDisplayMessage();
            return;
        }

        // How the other person related to this one, and which family the link was in, read before it went away.
        LinkKind? how = null;
        ParentSetData? family = null;
        if (person is not null && other is not null)
        {
            if (person.Spouses.Contains(otherId))
            {
                how = LinkKind.Spouse;
            }
            else if (ParentSetRules.AllParentIds(person).Contains(otherId))
            {
                how = LinkKind.Parent;
                family = HoldingSet(person, otherId, setId);
            }
            else if (person.Children.Contains(otherId))
            {
                how = LinkKind.Child;
                family = HoldingSet(other, personId, setId);
            }
        }

        formError = null;
        unlinkTarget = null;
        await PublishAsync(result.Value, FocusPersonId, FamilyTreeChangeKind.Unlinked, personId, otherId, how, family?.Id, family?.Type);
    }

    private static ParentSetData? HoldingSet(PersonData child, string parentId, string? setId)
    {
        var holding = child.ParentSets.Where(set => set.ParentIds.Contains(parentId)).ToList();
        return setId is null ? holding.Count == 1 ? holding[0] : null : holding.FirstOrDefault(set => set.Id == setId);
    }

    private async Task ChangeSetTypeAsync(string personId, string setId, ChangeEventArgs e)
    {
        if (saving || !Enum.TryParse<ParentageType>(e.Value?.ToString(), out var type))
        {
            return;
        }

        await UpdateSetAsync(personId, setId, FamilyGraphEditor.SetParentSetType(graph, personId, setId, type));
    }

    private async Task ChangeSetDateAsync(string personId, string setId, ChangeEventArgs e)
    {
        if (saving)
        {
            return;
        }

        string? text = e.Value?.ToString();
        var date = PartialDate.FromStored(text);
        if (!string.IsNullOrWhiteSpace(text) && date is null)
        {
            formError = "Use a date like 1987, 1987-06 or 1987-06-14.";
            return;
        }

        await UpdateSetAsync(personId, setId, FamilyGraphEditor.SetParentSetDate(graph, personId, setId, date));
    }

    private async Task MakePrimaryAsync(string personId, string setId)
    {
        if (!saving)
        {
            await UpdateSetAsync(personId, setId, FamilyGraphEditor.SetPrimaryParentSet(graph, personId, setId));
        }
    }

    private async Task UpdateSetAsync(string personId, string setId, Result<IReadOnlyList<PersonData>> result)
    {
        if (!result.IsSuccess)
        {
            formError = result.GetDisplayMessage();
            return;
        }

        formError = null;
        var set = result.Value.FirstOrDefault(person => person.Id == personId)?.ParentSets.FirstOrDefault(item => item.Id == setId);
        await PublishAsync(result.Value, FocusPersonId, FamilyTreeChangeKind.ParentSetUpdated, personId, null, null, setId, set?.Type);
    }

    private void AskRemoveSet(string setId)
    {
        CancelLink();
        confirmDelete = false;
        unlinkTarget = null;
        removeSetId = setId;
    }

    private async Task RemoveSetAsync(string personId, string setId)
    {
        if (saving)
        {
            return;
        }

        var removed = Person(personId)?.ParentSets.FirstOrDefault(set => set.Id == setId);
        var result = FamilyGraphEditor.RemoveParentSet(graph, personId, setId);
        if (!result.IsSuccess)
        {
            formError = result.GetDisplayMessage();
            return;
        }

        formError = null;
        removeSetId = null;
        await PublishAsync(result.Value, FocusPersonId, FamilyTreeChangeKind.ParentSetRemoved, personId, null, null, setId, removed?.Type);
    }

    /// <summary>
    /// Draws a person under another of their families. This is a view choice: <see cref="Changed"/> is not raised,
    /// only <see cref="ActiveParentSetsChanged"/>, and the next render rebuilds the branch above them.
    /// </summary>
    private async Task SwitchSetAsync(string personId, string setId)
    {
        familiesOpenFor = null;
        var person = Person(personId);
        if (person is null || person.ParentSets.All(set => set.Id != setId))
        {
            return;
        }

        // The primary set is the default, so choosing it just forgets the override.
        if (ParentSetRules.PrimaryOf(person)?.Id == setId)
        {
            activeSets.Remove(personId);
        }
        else
        {
            activeSets[personId] = setId;
        }

        Rebuild();
        await ActiveParentSetsChanged.InvokeAsync(new Dictionary<string, string>(activeSets, StringComparer.Ordinal));
    }

    private void ToggleFamilies(string personId) =>
        familiesOpenFor = familiesOpenFor == personId ? null : personId;

    private async Task SeeAlsoAsync(TreeCard card)
    {
        selectedId = RealId(card);
        familiesOpenFor = null;
        editor = null;
        await FocusAsync(RealId(card));
    }

    private bool HasNonBirthSets => graph.Any(person => person.ParentSets.Any(set => set.Type != ParentageType.Birth));

    private async Task ToggleBloodOnly()
    {
        bloodOnly = !bloodOnly;
        familiesOpenFor = null;
        Rebuild();
        shouldFit = true;
        userAdjusted = false;
        await NotifyViewAsync();
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

    /// <summary>
    /// Performs the center on operation.
    /// </summary>
    /// <param name="personId">The person identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CenterOnAsync(string personId)
    {
        if (string.IsNullOrWhiteSpace(personId) || graph.All(person => person.Id != personId))
        {
            return Task.CompletedTask;
        }

        CancelLink();
        unlinkTarget = null;
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
        if (pendingLink is not null)
        {
            ChooseLinkTarget(person.Id, placeholder: false);
            query = string.Empty;
            return Task.CompletedTask;
        }

        selectedId = person.Id;
        query = string.Empty;
        editor = null;
        unlinkTarget = null;
        return FocusAsync(person.Id);
    }

    private async Task UndoAsync()
    {
        if (undo.Count == 0 || saving)
        {
            return;
        }

        redo.Add(new Snapshot(graph.ToList(), FocusPersonId));
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

        undo.Add(new Snapshot(graph.ToList(), FocusPersonId));
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
        string? otherPersonId = null,
        LinkKind? link = null,
        string? parentSetId = null,
        ParentageType? type = null)
    {
        if (!applyingHistory && kind != FamilyTreeChangeKind.FocusChanged)
        {
            undo.Add(new Snapshot(graph.ToList(), FocusPersonId));
            if (undo.Count > 40)
            {
                undo.RemoveAt(0);
            }

            redo.Clear();
        }

        saving = true;
        try
        {
            await Changed.InvokeAsync(new FamilyTreeChange(next, focus, kind, personId, otherPersonId, link, parentSetId, type));
        }
        finally
        {
            saving = false;
        }
    }

    private int MaxDepth => Math.Max(0, MaxGenerations);

    private async Task SetDepthAsync(bool descendants, int delta)
    {
        if (descendants)
        {
            progeny = Math.Clamp(progeny + delta, 0, MaxDepth);
        }
        else
        {
            ancestry = Math.Clamp(ancestry + delta, 0, MaxDepth);
        }

        Rebuild();
        shouldFit = true;
        userAdjusted = false;
        await NotifyViewAsync();
    }

    private async Task ToggleSiblings()
    {
        siblings = !siblings;
        Rebuild();
        shouldFit = true;
        userAdjusted = false;
        await NotifyViewAsync();
    }

    private async Task SetLineage(LineageScope scope)
    {
        if (lineage == scope)
        {
            return;
        }

        lineage = scope;
        Rebuild();
        shouldFit = true;
        userAdjusted = false;
        await NotifyViewAsync();
    }

    private async Task ToggleOrientation()
    {
        orientation = orientation == TreeOrientation.Vertical ? TreeOrientation.Horizontal : TreeOrientation.Vertical;
        Rebuild();
        shouldFit = true;
        userAdjusted = false;
        await NotifyViewAsync();
    }

    private async Task NotifyViewAsync()
    {
        await DepthsChanged.InvokeAsync(new TreeDepths(ancestry, progeny));
        await ViewChanged.InvokeAsync(new TreeView(ancestry, progeny, siblings, lineage, orientation, bloodOnly));
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
        familiesOpenFor = null;
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

    /// <summary>
    /// Applies the view.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <param name="nextScale">The next scale.</param>
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

        return await module.InvokeAsync<ViewportSize?>("size", viewport) ?? new ViewportSize();
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

    /// <summary>A link the user asked to remove: the other person, and the family when the link is a parent link.</summary>
    private sealed record UnlinkTarget(string OtherId, string? SetId);

    private sealed record RelativeRow(string Label, string OtherId, string? SetId);

    /// <summary>Which family a new parent joins: a family's id, or <see cref="NewFamily"/> with the type to start it with.</summary>
    private sealed class ParentChoice
    {
        public string Value { get; set; } = NewFamily;

        public ParentageType Type { get; set; } = ParentageType.Birth;
    }

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
