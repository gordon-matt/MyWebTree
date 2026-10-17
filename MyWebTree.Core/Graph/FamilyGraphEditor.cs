using MyWebTree.Models;
using MyWebTree.Services;

namespace MyWebTree.Graph;

/// <summary>
/// The rules for changing a family: at most two parents, spouses go both ways, and nobody can be their own ancestor.
/// The Blazor tree and <c>FamilyTreeService</c> both go through here, so a card and a saved row can't disagree.
/// </summary>
public static class FamilyGraphEditor
{
    public static Result<IReadOnlyList<PersonData>> AddPerson(IReadOnlyList<PersonData> people, PersonWriteModel model)
    {
        var written = Clean(model);
        if (!written.IsSuccess)
        {
            return written.To<IReadOnlyList<PersonData>>();
        }

        var map = Index(people);
        string id = NewId();
        map[id] = FromWrite(id, written.Value);
        return Result<IReadOnlyList<PersonData>>.Success(List(map));
    }

    public static Result<IReadOnlyList<PersonData>> AddRelative(IReadOnlyList<PersonData> people, AddRelativeRequest request)
    {
        var map = Index(people);
        if (!map.TryGetValue(request.RelatesToPersonId, out var anchor))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        var written = Clean(request.Person, GenderFor(request.Kind, request.Person.Gender, anchor.Gender));
        if (!written.IsSuccess)
        {
            return written.To<IReadOnlyList<PersonData>>();
        }

        var created = FromWrite(NewId(), written.Value);
        switch (request.Kind)
        {
            case RelativeKind.Father:
            case RelativeKind.Mother:
                var parent = AddParent(map, anchor, created, request.Kind);
                if (!parent.IsSuccess)
                {
                    return parent.To<IReadOnlyList<PersonData>>();
                }

                break;

            case RelativeKind.Spouse:
                if (anchor.Spouses.Contains(created.Id))
                {
                    return Result<IReadOnlyList<PersonData>>.Conflict("Those two are already spouses.");
                }

                map[created.Id] = created;
                FamilyLinks.Spouse(map, anchor.Id, created.Id);
                break;

            case RelativeKind.Son:
            case RelativeKind.Daughter:
            case RelativeKind.Child:
                var child = AddChild(map, anchor, created, request.OtherParentId);
                if (!child.IsSuccess)
                {
                    return child.To<IReadOnlyList<PersonData>>();
                }

                break;

            default:
                return Result<IReadOnlyList<PersonData>>.Invalid(new ValidationError(nameof(request.Kind), "Choose which relative to add."));
        }

        return Validate(List(map));
    }

    public static Result<IReadOnlyList<PersonData>> Update(IReadOnlyList<PersonData> people, string personId, PersonWriteModel model)
    {
        var map = Index(people);
        if (!map.TryGetValue(personId, out var existing))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        var written = Clean(model);
        if (!written.IsSuccess)
        {
            return written.To<IReadOnlyList<PersonData>>();
        }

        var value = written.Value;
        map[personId] = existing with
        {
            Gender = value.Gender,
            FirstName = value.FirstName,
            LastName = value.LastName,
            BirthDate = value.BirthDate,
            DeathDate = value.DeathDate,
            BirthPlace = value.BirthPlace,
            DeathPlace = value.DeathPlace,
            AvatarUrl = value.AvatarUrl,
            Notes = value.Notes,
        };

        return Validate(List(map));
    }

    public static Result<IReadOnlyList<PersonData>> Remove(IReadOnlyList<PersonData> people, string personId)
    {
        var map = Index(people);
        if (!map.ContainsKey(personId))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        FamilyLinks.Unlink(map, personId);
        return Result<IReadOnlyList<PersonData>>.Success(List(map));
    }

    /// <summary>Drops one spouse or parent link and leaves both people in the tree.</summary>
    public static Result<IReadOnlyList<PersonData>> Unlink(IReadOnlyList<PersonData> people, string personId, string otherId)
    {
        var map = Index(people);
        if (!map.TryGetValue(personId, out var person) || !map.ContainsKey(otherId))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        if (person.Spouses.Contains(otherId))
        {
            FamilyLinks.Unspouse(map, personId, otherId);
        }
        else if (person.Parents.Contains(otherId))
        {
            FamilyLinks.Unparent(map, otherId, personId);
        }
        else if (person.Children.Contains(otherId))
        {
            FamilyLinks.Unparent(map, personId, otherId);
        }
        else
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("Those two are not linked.");
        }

        return Validate(List(map));
    }

    public static Result<IReadOnlyList<PersonData>> Validate(IReadOnlyList<PersonData> people)
    {
        var map = Index(people);
        foreach (var person in map.Values)
        {
            if (person.Parents.Count > 2)
            {
                return Invalid(person.DisplayName + " has more than two parents.");
            }

            if (person.Parents.Contains(person.Id) || person.Spouses.Contains(person.Id) || person.Children.Contains(person.Id))
            {
                return Invalid(person.DisplayName + " can't be related to themself.");
            }

            if (person.Parents.Distinct(StringComparer.Ordinal).Count() != person.Parents.Count)
            {
                return Invalid(person.DisplayName + " lists the same parent twice.");
            }

            foreach (string parentId in person.Parents)
            {
                if (!map.TryGetValue(parentId, out var parent) || !parent.Children.Contains(person.Id))
                {
                    return Invalid("Parent and child links have to point both ways.");
                }
            }

            foreach (string spouseId in person.Spouses)
            {
                if (!map.TryGetValue(spouseId, out var spouse) || !spouse.Spouses.Contains(person.Id))
                {
                    return Invalid("A marriage has to be recorded for both people.");
                }

                if (person.Parents.Contains(spouseId) || person.Children.Contains(spouseId))
                {
                    return Invalid(person.DisplayName + " can't be both married to and descended from the same person.");
                }
            }

            if (IsOwnAncestor(map, person.Id, person.Id, 0))
            {
                return Invalid("That would make " + person.DisplayName + " their own ancestor.");
            }

            if (person.BirthDate is PartialDate born && person.DeathDate is PartialDate died && died.IsBefore(born))
            {
                return Invalid(person.DisplayName + "'s death is dated before their birth.");
            }
        }

        return Result<IReadOnlyList<PersonData>>.Success(people);
    }

    private static Result AddParent(Dictionary<string, PersonData> map, PersonData anchor, PersonData created, RelativeKind kind)
    {
        if (anchor.Parents.Count >= 2)
        {
            return Result.Invalid(new ValidationError(nameof(kind), anchor.DisplayName + " already has two parents."));
        }

        Gender expected = kind == RelativeKind.Father ? Gender.Male : Gender.Female;
        if (anchor.Parents.Any(id => map.TryGetValue(id, out var parent) && parent.Gender == expected))
        {
            string role = expected == Gender.Male ? "a father" : "a mother";
            return Result.Invalid(new ValidationError(nameof(kind), anchor.DisplayName + " already has " + role + "."));
        }

        map[created.Id] = created;
        FamilyLinks.Parent(map, created.Id, anchor.Id);

        string? otherId = map[anchor.Id].Parents.FirstOrDefault(id => id != created.Id);
        if (otherId is not null)
        {
            FamilyLinks.Spouse(map, created.Id, otherId);
        }

        return Result.Success();
    }

    private static Result AddChild(Dictionary<string, PersonData> map, PersonData anchor, PersonData created, string? otherParentId)
    {
        if (otherParentId is not null)
        {
            if (!map.ContainsKey(otherParentId))
            {
                return Result.NotFound("The other parent is no longer in the tree.");
            }

            if (!anchor.Spouses.Contains(otherParentId))
            {
                return Result.Invalid(new ValidationError(nameof(otherParentId), "The other parent needs to be a spouse."));
            }
        }

        map[created.Id] = created;
        FamilyLinks.Parent(map, anchor.Id, created.Id);
        if (otherParentId is not null)
        {
            FamilyLinks.Parent(map, otherParentId, created.Id);
        }
        else if (anchor.Spouses.Count == 1)
        {
            FamilyLinks.Parent(map, anchor.Spouses[0], created.Id);
        }

        return Result.Success();
    }

    private static bool IsOwnAncestor(Dictionary<string, PersonData> map, string start, string target, int depth)
    {
        if (depth > 32 || !map.TryGetValue(start, out var person))
        {
            return depth > 32;
        }

        foreach (string parentId in person.Parents)
        {
            if (parentId == target || IsOwnAncestor(map, parentId, target, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    private static Result<PersonWriteModel> Clean(PersonWriteModel model, Gender? gender = null)
    {
        string first = model.FirstName.Trim();
        string last = model.LastName.Trim();
        if (first.Length == 0 && last.Length == 0)
        {
            return Result<PersonWriteModel>.Invalid(new ValidationError(nameof(model.FirstName), "Give this person a name."));
        }

        if (first.Length > Constants.MaxLengths.PersonName || last.Length > Constants.MaxLengths.PersonName)
        {
            return Result<PersonWriteModel>.Invalid(new ValidationError(
                nameof(model.FirstName),
                $"Names can be at most {Constants.MaxLengths.PersonName} characters."));
        }

        if (model.BirthDate is PartialDate born && model.DeathDate is PartialDate died && died.IsBefore(born))
        {
            return Result<PersonWriteModel>.Invalid(new ValidationError(nameof(model.DeathDate), "Death can't be dated before birth."));
        }

        string? place = NullIfBlank(model.BirthPlace);
        string? deathPlace = NullIfBlank(model.DeathPlace);
        string? avatar = NullIfBlank(model.AvatarUrl);
        string? notes = NullIfBlank(model.Notes);
        if (place is { Length: > Constants.MaxLengths.Place })
        {
            return Result<PersonWriteModel>.Invalid(new ValidationError(nameof(model.BirthPlace), "Birth place is too long."));
        }

        if (deathPlace is { Length: > Constants.MaxLengths.Place })
        {
            return Result<PersonWriteModel>.Invalid(new ValidationError(nameof(model.DeathPlace), "Death place is too long."));
        }

        if (avatar is { Length: > Constants.MaxLengths.AvatarUrl })
        {
            return Result<PersonWriteModel>.Invalid(new ValidationError(nameof(model.AvatarUrl), "The picture address is too long."));
        }

        if (notes is { Length: > Constants.MaxLengths.Notes })
        {
            return Result<PersonWriteModel>.Invalid(new ValidationError(nameof(model.Notes), "Notes are too long."));
        }

        return Result<PersonWriteModel>.Success(model with
        {
            Gender = gender ?? model.Gender,
            FirstName = first,
            LastName = last,
            BirthPlace = place,
            DeathPlace = deathPlace,
            AvatarUrl = avatar,
            Notes = notes,
        });
    }

    private static PersonData FromWrite(string id, PersonWriteModel model) => new()
    {
        Id = id,
        Gender = model.Gender,
        FirstName = model.FirstName,
        LastName = model.LastName,
        BirthDate = model.BirthDate,
        DeathDate = model.DeathDate,
        BirthPlace = model.BirthPlace,
        DeathPlace = model.DeathPlace,
        AvatarUrl = model.AvatarUrl,
        Notes = model.Notes,
    };

    private static Gender GenderFor(RelativeKind kind, Gender requested, Gender anchor) => kind switch
    {
        RelativeKind.Father or RelativeKind.Son => Gender.Male,
        RelativeKind.Mother or RelativeKind.Daughter => Gender.Female,
        RelativeKind.Spouse when requested == Gender.Unknown => anchor switch
        {
            Gender.Male => Gender.Female,
            Gender.Female => Gender.Male,
            _ => Gender.Unknown,
        },
        _ => requested,
    };

    private static Dictionary<string, PersonData> Index(IReadOnlyList<PersonData> people)
    {
        var map = new Dictionary<string, PersonData>(StringComparer.Ordinal);
        foreach (var person in people)
        {
            if (!string.IsNullOrWhiteSpace(person.Id) && !person.IsPlaceholder)
            {
                map[person.Id] = person;
            }
        }

        return map;
    }

    private static List<PersonData> List(Dictionary<string, PersonData> people) => people.Values.ToList();

    private static string NewId() => "n-" + Guid.NewGuid().ToString("N");

    private static string? NullIfBlank(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static Result<IReadOnlyList<PersonData>> Invalid(string message) =>
        Result<IReadOnlyList<PersonData>>.Invalid(new ValidationError(string.Empty, message));
}
