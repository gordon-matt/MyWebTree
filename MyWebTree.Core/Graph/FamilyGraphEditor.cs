using MyWebTree.Models;
using MyWebTree.Services;

namespace MyWebTree.Graph;

/// <summary>
/// The rules for changing a family: at most two parents in a set, spouses go both ways, and nobody can be their own ancestor.
/// A person may have several sets of parents (birth, adopted, foster), and every rule applies across all of them.
/// The Blazor tree and <c>FamilyTreeService</c> both go through here, so a card and a saved row can't disagree.
/// </summary>
public static class FamilyGraphEditor
{
    /// <summary>
    /// Adds the person.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="model">The model.</param>
    /// <returns>The outcome for the person datas.</returns>
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

    /// <summary>
    /// Adds the relative.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="request">The request.</param>
    /// <returns>The outcome for the person datas.</returns>
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
                var parent = AddParent(map, anchor, created, request.Kind, request.ParentSet);
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

    /// <summary>
    /// Updates the value.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The person identifier.</param>
    /// <param name="model">The model.</param>
    /// <returns>The outcome for the person datas.</returns>
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

    /// <summary>
    /// Removes the value.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The person identifier.</param>
    /// <returns>The outcome for the person datas.</returns>
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

    /// <summary>
    /// Links two people already in the tree. <paramref name="kind"/> is how <paramref name="otherId"/>
    /// relates to <paramref name="personId"/>. A second parent in a set is also recorded as the spouse of the parent
    /// already there. The selected person's own spouse is not added as the other parent.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The selected person.</param>
    /// <param name="otherId">The person who was clicked.</param>
    /// <param name="kind">How <paramref name="otherId"/> relates to <paramref name="personId"/>.</param>
    /// <param name="parentSet">
    /// For a parent link, which set of the child the new parent joins: the child's primary set when null, or the
    /// one named. The component passes the set it is showing.
    /// </param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> Link(
        IReadOnlyList<PersonData> people,
        string personId,
        string otherId,
        LinkKind kind,
        ParentSetTarget? parentSet = null)
    {
        if (string.Equals(personId, otherId, StringComparison.Ordinal))
        {
            return Invalid("A person can't be linked to themself.");
        }

        var map = Index(people);
        if (!map.ContainsKey(personId) || !map.ContainsKey(otherId))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        return kind switch
        {
            LinkKind.Parent => LinkParent(map, map[personId], map[otherId], parentSet),
            LinkKind.Child => LinkParent(map, map[otherId], map[personId], parentSet),
            LinkKind.Spouse => LinkSpouse(map, map[personId], map[otherId]),
            _ => Invalid("Choose which link to make."),
        };
    }

    /// <summary>
    /// Drops one spouse, parent, or child link and leaves both people in the tree. A parent link is one person's
    /// membership of one set. When the parent is in more than one set of the child, <paramref name="parentSetId"/>
    /// says which. A set left with nobody in it is removed, and another set becomes primary if it was the primary one.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The selected person.</param>
    /// <param name="otherId">The other end of the link.</param>
    /// <param name="parentSetId">The child's set to take the parent out of, for a parent or child link.</param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> Unlink(
        IReadOnlyList<PersonData> people,
        string personId,
        string otherId,
        string? parentSetId = null)
    {
        var map = Index(people);
        if (!map.TryGetValue(personId, out var person) || !map.TryGetValue(otherId, out var other))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        if (person.Spouses.Contains(otherId))
        {
            FamilyLinks.Unspouse(map, personId, otherId);
        }
        else if (person.Parents.Contains(otherId) || ParentSetRules.AllParentIds(person).Contains(otherId))
        {
            var removed = TakeOutOfSet(map, person, other, parentSetId);
            if (!removed.IsSuccess)
            {
                return removed;
            }
        }
        else if (person.Children.Contains(otherId))
        {
            var removed = TakeOutOfSet(map, other, person, parentSetId);
            if (!removed.IsSuccess)
            {
                return removed;
            }
        }
        else
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("Those two are not linked.");
        }

        return Validate(List(map));
    }

    /// <summary>
    /// Gives <paramref name="personId"/> (the child) another set of parents. A set is never empty, so the parents
    /// are named here. Two parents are recorded as spouses. The first set a child gets becomes the primary one.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The child.</param>
    /// <param name="type">Birth, adopted, or foster.</param>
    /// <param name="parentIds">One or two parents already in the tree.</param>
    /// <param name="date">The adoption or placement date, if known.</param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> AddParentSet(
        IReadOnlyList<PersonData> people,
        string personId,
        ParentageType type,
        IReadOnlyList<string> parentIds,
        PartialDate? date = null)
    {
        var map = Index(people);
        if (!map.TryGetValue(personId, out var child))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        if (parentIds.Count is < 1 or > 2)
        {
            return Invalid("A set of parents needs one or two people.");
        }

        if (parentIds.Distinct(StringComparer.Ordinal).Count() != parentIds.Count)
        {
            return Invalid(child.DisplayName + " lists the same parent twice.");
        }

        foreach (string parentId in parentIds)
        {
            if (!map.ContainsKey(parentId))
            {
                return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
            }

            if (parentId == personId)
            {
                return Invalid("A person can't be linked to themself.");
            }
        }

        var added = new ParentSetData
        {
            Id = ParentSetRules.NewSetId(),
            Type = type,
            IsPrimary = child.ParentSets.Count == 0,
            ParentIds = parentIds.ToList(),
            Date = date,
        };

        if (child.ParentSets.Any(set => ParentSetRules.SameFamily(set, added)))
        {
            return Result<IReadOnlyList<PersonData>>.Conflict(child.DisplayName + " already has that set of parents.");
        }

        FamilyLinks.SetParentSets(map, personId, [.. child.ParentSets, added]);
        if (parentIds.Count == 2)
        {
            string? marriage = MarriageProblem(map[parentIds[0]], map[parentIds[1]]);
            if (marriage is not null)
            {
                return Invalid(marriage);
            }

            FamilyLinks.Spouse(map, parentIds[0], parentIds[1]);
        }

        return Validate(List(map));
    }

    /// <summary>Changes a set to birth, adopted, or foster. A person never has two sets with the same members and type.</summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The child.</param>
    /// <param name="setId">The set to change.</param>
    /// <param name="type">The new type.</param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> SetParentSetType(
        IReadOnlyList<PersonData> people,
        string personId,
        string setId,
        ParentageType type) =>
        ChangeSet(people, personId, setId, (child, set) =>
        {
            var changed = set with { Type = type };
            return child.ParentSets.Any(other => other.Id != set.Id && ParentSetRules.SameFamily(other, changed))
                ? Result<ParentSetData>.Conflict(child.DisplayName + " already has that set of parents.")
                : Result<ParentSetData>.Success(changed);
        });

    /// <summary>Makes a set the one drawn by default. The set that was primary stops being primary.</summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The child.</param>
    /// <param name="setId">The set to promote.</param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> SetPrimaryParentSet(
        IReadOnlyList<PersonData> people,
        string personId,
        string setId)
    {
        var map = Index(people);
        if (!map.TryGetValue(personId, out var child))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        if (child.ParentSets.All(set => set.Id != setId))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That set of parents is no longer there.");
        }

        FamilyLinks.SetParentSets(map, personId, child.ParentSets.Select(set => set with { IsPrimary = set.Id == setId }).ToList());
        return Validate(List(map));
    }

    /// <summary>Sets or clears the adoption or placement date of a set.</summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The child.</param>
    /// <param name="setId">The set to change.</param>
    /// <param name="date">The date, or null to clear it.</param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> SetParentSetDate(
        IReadOnlyList<PersonData> people,
        string personId,
        string setId,
        PartialDate? date) =>
        ChangeSet(people, personId, setId, (_, set) => Result<ParentSetData>.Success(set with { Date = date }));

    /// <summary>
    /// Removes a whole set of parents from a child. The parents stay in the tree, and stay listed as a parent of
    /// the child if they are in another of the child's sets. If it was the primary set, another one takes over.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <param name="personId">The child.</param>
    /// <param name="setId">The set to remove.</param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> RemoveParentSet(
        IReadOnlyList<PersonData> people,
        string personId,
        string setId)
    {
        var map = Index(people);
        if (!map.TryGetValue(personId, out var child))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        if (child.ParentSets.All(set => set.Id != setId))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That set of parents is no longer there.");
        }

        FamilyLinks.SetParentSets(map, personId, child.ParentSets.Where(set => set.Id != setId).ToList());
        return Validate(List(map));
    }

    /// <summary>
    /// Checks the whole graph and returns it in the normalized shape: every person's sets are explicit, one is
    /// primary, and <see cref="PersonData.Parents"/> equals the primary set's members.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <returns>The outcome for the person datas.</returns>
    public static Result<IReadOnlyList<PersonData>> Validate(IReadOnlyList<PersonData> people)
    {
        var normalized = ParentSetRules.Normalize(people);
        var map = Index(normalized);
        foreach (var person in map.Values)
        {
            if (person.Parents.Contains(person.Id)
                || person.Spouses.Contains(person.Id)
                || person.Children.Contains(person.Id)
                || person.ParentSets.Any(set => set.ParentIds.Contains(person.Id)))
            {
                return Invalid(person.DisplayName + " can't be related to themself.");
            }

            if (person.ParentSets.Select(set => set.Id).Distinct(StringComparer.Ordinal).Count() != person.ParentSets.Count)
            {
                return Invalid(person.DisplayName + " has two sets of parents with the same id.");
            }

            for (int i = 0; i < person.ParentSets.Count; i++)
            {
                var set = person.ParentSets[i];
                if (set.ParentIds.Count > 2)
                {
                    return Invalid(person.DisplayName + " has more than two parents.");
                }

                if (set.ParentIds.Distinct(StringComparer.Ordinal).Count() != set.ParentIds.Count)
                {
                    return Invalid(person.DisplayName + " lists the same parent twice.");
                }

                if (person.ParentSets.Skip(i + 1).Any(other => ParentSetRules.SameFamily(set, other)))
                {
                    return Invalid(person.DisplayName + " has the same set of parents more than once.");
                }

                if (set.Date is PartialDate placed && person.BirthDate is PartialDate born && placed.IsBefore(born))
                {
                    return Invalid(person.DisplayName + "'s parents are dated before they were born.");
                }

                foreach (string parentId in set.ParentIds)
                {
                    if (!map.TryGetValue(parentId, out var parent) || !parent.Children.Contains(person.Id))
                    {
                        return Invalid("Parent and child links have to point both ways.");
                    }

                    string? dated = ParentageProblem(parent, person);
                    if (dated is not null)
                    {
                        return Invalid(dated);
                    }
                }
            }

            foreach (string spouseId in person.Spouses)
            {
                if (!map.TryGetValue(spouseId, out var spouse) || !spouse.Spouses.Contains(person.Id))
                {
                    return Invalid("A marriage has to be recorded for both people.");
                }

                if (IsAncestor(map, person.Id, spouseId) || IsAncestor(map, spouseId, person.Id))
                {
                    return Invalid(person.DisplayName + " can't be both married to and descended from the same person.");
                }
            }

            if (IsAncestor(map, person.Id, person.Id))
            {
                return Invalid("That would make " + person.DisplayName + " their own ancestor.");
            }

            if (person.BirthDate is PartialDate birth && person.DeathDate is PartialDate died && died.IsBefore(birth))
            {
                return Invalid(person.DisplayName + "'s death is dated before their birth.");
            }
        }

        return Result<IReadOnlyList<PersonData>>.Success(normalized);
    }

    private static Result<IReadOnlyList<PersonData>> ChangeSet(
        IReadOnlyList<PersonData> people,
        string personId,
        string setId,
        Func<PersonData, ParentSetData, Result<ParentSetData>> change)
    {
        var map = Index(people);
        if (!map.TryGetValue(personId, out var child))
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That person is no longer in the tree.");
        }

        var set = child.ParentSets.FirstOrDefault(item => item.Id == setId);
        if (set is null)
        {
            return Result<IReadOnlyList<PersonData>>.NotFound("That set of parents is no longer there.");
        }

        var changed = change(child, set);
        if (!changed.IsSuccess)
        {
            return changed.To<IReadOnlyList<PersonData>>();
        }

        FamilyLinks.SetParentSets(map, personId, child.ParentSets.Select(item => item.Id == setId ? changed.Value : item).ToList());
        return Validate(List(map));
    }

    /// <summary>Takes one parent out of one of the child's sets.</summary>
    private static Result<IReadOnlyList<PersonData>> TakeOutOfSet(
        Dictionary<string, PersonData> map,
        PersonData child,
        PersonData parent,
        string? parentSetId)
    {
        var holding = child.ParentSets.Where(set => set.ParentIds.Contains(parent.Id)).ToList();
        if (holding.Count == 0)
        {
            // The two lists disagreed. Drop the stray entries and keep going.
            FamilyLinks.Unparent(map, parent.Id, child.Id);
            return Result<IReadOnlyList<PersonData>>.Success(List(map));
        }

        ParentSetData target;
        if (parentSetId is not null)
        {
            var named = holding.FirstOrDefault(set => set.Id == parentSetId);
            if (named is null)
            {
                return Result<IReadOnlyList<PersonData>>.NotFound("Those two are not linked in that set of parents.");
            }

            target = named;
        }
        else if (holding.Count == 1)
        {
            target = holding[0];
        }
        else
        {
            return Invalid(parent.DisplayName + " is a parent of " + child.DisplayName + " in more than one set. Choose which one to unlink.");
        }

        var next = child.ParentSets
            .Select(set => set.Id == target.Id ? set with { ParentIds = set.ParentIds.Where(id => id != parent.Id).ToArray() } : set)
            .Where(set => set.ParentIds.Count > 0)
            .ToList();
        FamilyLinks.SetParentSets(map, child.Id, next);
        return Result<IReadOnlyList<PersonData>>.Success(List(map));
    }

    private static Result<IReadOnlyList<PersonData>> LinkSpouse(
        Dictionary<string, PersonData> map,
        PersonData person,
        PersonData other)
    {
        if (person.Spouses.Contains(other.Id))
        {
            return Result<IReadOnlyList<PersonData>>.Conflict("Those two are already spouses.");
        }

        FamilyLinks.Spouse(map, person.Id, other.Id);
        string? marriage = MarriageProblem(person, other);
        return marriage is null ? Validate(List(map)) : Invalid(marriage);
    }

    private static Result<IReadOnlyList<PersonData>> LinkParent(
        Dictionary<string, PersonData> map,
        PersonData child,
        PersonData parent,
        ParentSetTarget? target)
    {
        var placed = PlaceParent(map, child, parent, target, checkMarriage: true, enforceRole: false, Gender.Unknown);
        return placed.IsSuccess ? Validate(List(map)) : placed.To<IReadOnlyList<PersonData>>();
    }

    /// <summary>
    /// Puts <paramref name="parent"/> into one of the child's sets, or into a new one. This is the single place a
    /// parent link is written, so a card, a link, and a saved row follow the same limits.
    /// </summary>
    private static Result<ParentSetData> PlaceParent(
        Dictionary<string, PersonData> map,
        PersonData child,
        PersonData parent,
        ParentSetTarget? target,
        bool checkMarriage,
        bool enforceRole,
        Gender role)
    {
        var sets = child.ParentSets.ToList();
        ParentSetData placed;
        if (target?.CreateNew == true || sets.Count == 0)
        {
            placed = new ParentSetData
            {
                Id = ParentSetRules.NewSetId(),
                Type = target?.Type ?? ParentageType.Birth,
                IsPrimary = sets.Count == 0,
                ParentIds = [parent.Id],
            };
            if (sets.Any(set => ParentSetRules.SameFamily(set, placed)))
            {
                return Result<ParentSetData>.Conflict(child.DisplayName + " already has that set of parents.");
            }

            sets.Add(placed);
        }
        else
        {
            var existing = target?.ParentSetId is null
                ? ParentSetRules.ChoosePrimary(sets)
                : sets.FirstOrDefault(set => set.Id == target.ParentSetId);
            if (existing is null)
            {
                return Result<ParentSetData>.NotFound("That set of parents is no longer there.");
            }

            if (existing.ParentIds.Contains(parent.Id))
            {
                return Result<ParentSetData>.Conflict("Those two are already parent and child.");
            }

            if (existing.ParentIds.Count >= 2)
            {
                string where = sets.Count > 1 ? " in that family." : ".";
                return Result<ParentSetData>.Invalid(new ValidationError(string.Empty, child.DisplayName + " already has two parents" + where));
            }

            if (enforceRole && existing.ParentIds.Any(id => map.TryGetValue(id, out var member) && member.Gender == role))
            {
                string word = role == Gender.Male ? "a father" : "a mother";
                return Result<ParentSetData>.Invalid(new ValidationError(string.Empty, child.DisplayName + " already has " + word + "."));
            }

            placed = existing with { ParentIds = [.. existing.ParentIds, parent.Id] };
            if (sets.Any(set => set.Id != existing.Id && ParentSetRules.SameFamily(set, placed)))
            {
                return Result<ParentSetData>.Invalid(new ValidationError(string.Empty, child.DisplayName + " already has that set of parents."));
            }

            sets[sets.IndexOf(existing)] = placed;
        }

        FamilyLinks.SetParentSets(map, child.Id, sets);
        string? otherId = placed.ParentIds.FirstOrDefault(id => id != parent.Id);
        if (otherId is not null && map.TryGetValue(otherId, out var otherParent))
        {
            if (checkMarriage)
            {
                string? marriage = MarriageProblem(parent, otherParent);
                if (marriage is not null)
                {
                    return Result<ParentSetData>.Invalid(new ValidationError(string.Empty, marriage));
                }
            }

            FamilyLinks.Spouse(map, parent.Id, otherId);
        }

        return Result<ParentSetData>.Success(placed);
    }

    private static string? ParentageProblem(PersonData parent, PersonData child)
    {
        if (parent.BirthDate is PartialDate parentBorn
            && child.BirthDate is PartialDate childBorn
            && childBorn.IsBefore(parentBorn))
        {
            return child.DisplayName + " was born before " + parent.DisplayName + ".";
        }

        return null;
    }

    private static string? MarriageProblem(PersonData left, PersonData right)
    {
        if (left.DeathDate is PartialDate leftDied
            && right.BirthDate is PartialDate rightBorn
            && leftDied.IsBefore(rightBorn))
        {
            return left.DisplayName + " died before " + right.DisplayName + " was born.";
        }

        if (right.DeathDate is PartialDate rightDied
            && left.BirthDate is PartialDate leftBorn
            && rightDied.IsBefore(leftBorn))
        {
            return right.DisplayName + " died before " + left.DisplayName + " was born.";
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="target"/> can be reached from <paramref name="start"/> by following parent links
    /// through every set. The visited set keeps a cycle in bad data from looping.
    /// </summary>
    private static bool IsAncestor(Dictionary<string, PersonData> map, string start, string target)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(start);
        while (pending.Count > 0)
        {
            if (!map.TryGetValue(pending.Pop(), out var person))
            {
                continue;
            }

            foreach (var set in person.ParentSets)
            {
                foreach (string parentId in set.ParentIds)
                {
                    if (parentId == target)
                    {
                        return true;
                    }

                    if (visited.Add(parentId))
                    {
                        pending.Push(parentId);
                    }
                }
            }
        }

        return false;
    }

    private static Result AddParent(
        Dictionary<string, PersonData> map,
        PersonData anchor,
        PersonData created,
        RelativeKind kind,
        ParentSetTarget? target)
    {
        Gender expected = kind == RelativeKind.Father ? Gender.Male : Gender.Female;
        map[created.Id] = created;
        var placed = PlaceParent(map, anchor, created, target, checkMarriage: false, enforceRole: true, expected);
        return placed.IsSuccess ? Result.Success() : ToPlain(placed);
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
        string? second = otherParentId ?? (anchor.Spouses.Count == 1 ? anchor.Spouses[0] : null);
        FamilyLinks.SetParentSets(map, created.Id, [NewBirthSet(anchor.Id, second is not null && map.ContainsKey(second) ? second : null)]);
        return Result.Success();
    }

    private static ParentSetData NewBirthSet(string parentId, string? otherParentId) => new()
    {
        Id = ParentSetRules.NewSetId(),
        Type = ParentageType.Birth,
        IsPrimary = true,
        ParentIds = otherParentId is null ? [parentId] : [parentId, otherParentId],
    };

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
                map[person.Id] = ParentSetRules.NormalizePerson(person);
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

    private static Result ToPlain(Result<ParentSetData> failed) => failed.Status switch
    {
        ResultStatus.NotFound => Result.NotFound([.. failed.Errors]),
        ResultStatus.Conflict => Result.Conflict([.. failed.Errors]),
        ResultStatus.Invalid => Result.Invalid([.. failed.ValidationErrors]),
        _ => Result.Error(new ErrorList(failed.Errors)),
    };

    private static Result<IReadOnlyList<PersonData>> Invalid(string message) =>
        Result<IReadOnlyList<PersonData>>.Invalid(new ValidationError(string.Empty, message));
}
