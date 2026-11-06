namespace MyWebTree.Services;

/// <summary>
/// Represents the family tree service.
/// </summary>
public sealed class FamilyTreeService(
    ILogger<FamilyTreeService> logger,
    IRepository<FamilyTree> trees,
    IRepository<Person> people,
    IRepository<PersonRelationship> relationships,
    IRepository<ParentSet> parentSets,
    IRepository<ParentSetMember> parentSetMembers) : IFamilyTreeService
{
    /// <summary>
    /// Lists the value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree summaries.</returns>
    public async Task<Result<IReadOnlyList<FamilyTreeSummary>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var found = (await trees.FindAsync(new SearchOptions<FamilyTree>
        {
            OrderBy = query => query.OrderBy(tree => tree.Name).ThenBy(tree => tree.Id),
            CancellationToken = cancellationToken,
        })).ToList();

        var everyone = await people.FindAsync(new SearchOptions<Person> { CancellationToken = cancellationToken });
        var counts = everyone.GroupBy(person => person.TreeId).ToDictionary(group => group.Key, group => group.Count());

        IReadOnlyList<FamilyTreeSummary> summaries = found
            .Select(tree => new FamilyTreeSummary(
                tree.Id,
                tree.Name,
                tree.Description,
                counts.GetValueOrDefault(tree.Id),
                tree.UpdatedAtUtc))
            .ToList();

        return Result<IReadOnlyList<FamilyTreeSummary>>.Success(summaries);
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="treeId">The tree identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    public async Task<Result<FamilyTreeDetail>> GetAsync(int treeId, CancellationToken cancellationToken = default)
    {
        var tree = await trees.FindOneAsync(treeId);
        if (tree is null)
        {
            return Result<FamilyTreeDetail>.NotFound("That family tree no longer exists.");
        }

        var members = (await people.FindAsync(new SearchOptions<Person>
        {
            Query = person => person.TreeId == treeId,
            OrderBy = query => query.OrderBy(person => person.BirthDate).ThenBy(person => person.LastName).ThenBy(person => person.FirstName),
            CancellationToken = cancellationToken,
        })).ToList();

        var links = (await relationships.FindAsync(new SearchOptions<PersonRelationship>
        {
            Query = link => link.TreeId == treeId,
            CancellationToken = cancellationToken,
        })).ToList();

        var sets = (await parentSets.FindAsync(new SearchOptions<ParentSet>
        {
            Query = set => set.TreeId == treeId,
            OrderBy = query => query.OrderBy(set => set.Id),
            CancellationToken = cancellationToken,
        })).ToList();

        var setMembers = (await parentSetMembers.FindAsync(new SearchOptions<ParentSetMember>
        {
            Query = member => member.TreeId == treeId,
            OrderBy = query => query.OrderBy(member => member.Id),
            CancellationToken = cancellationToken,
        })).ToList();

        return Result<FamilyTreeDetail>.Success(ToDetail(tree, members, links, sets, setMembers));
    }

    /// <summary>
    /// Creates the value.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    public async Task<Result<FamilyTreeDetail>> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        var validated = CleanName(name);
        if (!validated.IsSuccess)
        {
            return validated.To<FamilyTreeDetail>();
        }

        string? about = NullIfBlank(description);
        if (about is { Length: > Constants.MaxLengths.Description })
        {
            return Result<FamilyTreeDetail>.Invalid(new ValidationError(nameof(description), "The description is too long."));
        }

        var now = DateTime.UtcNow;
        var saved = await trees.InsertAsync(new FamilyTree
        {
            Name = validated.Value,
            Description = about,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        }, ContextOptions.ForCancellationToken(cancellationToken));

        logger.LogInformation("Created family tree {TreeId} '{Name}'", saved.Id, saved.Name);
        return await GetAsync(saved.Id, cancellationToken);
    }

    /// <summary>
    /// Renames the value.
    /// </summary>
    /// <param name="treeId">The tree identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    public async Task<Result<FamilyTreeDetail>> RenameAsync(int treeId, string name, CancellationToken cancellationToken = default)
    {
        var validated = CleanName(name);
        if (!validated.IsSuccess)
        {
            return validated.To<FamilyTreeDetail>();
        }

        var tree = await trees.FindOneAsync(treeId);
        if (tree is null)
        {
            return Result<FamilyTreeDetail>.NotFound("That family tree no longer exists.");
        }

        tree.Name = validated.Value;
        tree.UpdatedAtUtc = DateTime.UtcNow;
        await trees.UpdateAsync(tree, ContextOptions.ForCancellationToken(cancellationToken));
        return await GetAsync(treeId, cancellationToken);
    }

    /// <summary>
    /// Deletes the value.
    /// </summary>
    /// <param name="treeId">The tree identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<Result> DeleteAsync(int treeId, CancellationToken cancellationToken = default)
    {
        var tree = await trees.FindOneAsync(treeId);
        if (tree is null)
        {
            return Result.NotFound("That family tree no longer exists.");
        }

        var token = ContextOptions.ForCancellationToken(cancellationToken);
        tree.FocusPersonId = null;
        await trees.UpdateAsync(tree, token);
        await parentSetMembers.DeleteAsync(member => member.TreeId == treeId, token);
        await parentSets.DeleteAsync(set => set.TreeId == treeId, token);
        await relationships.DeleteAsync(link => link.TreeId == treeId, token);
        await people.DeleteAsync(person => person.TreeId == treeId, token);
        await trees.DeleteAsync(tree, token);

        logger.LogInformation("Deleted family tree {TreeId}", treeId);
        return Result.Success();
    }

    /// <summary>
    /// Saves the value.
    /// </summary>
    /// <param name="treeId">The tree identifier.</param>
    /// <param name="peopleGraph">The people graph.</param>
    /// <param name="focusPersonId">The focus person identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    public async Task<Result<FamilyTreeDetail>> SaveAsync(
        int treeId,
        IReadOnlyList<PersonData> peopleGraph,
        string? focusPersonId,
        CancellationToken cancellationToken = default)
    {
        var incoming = peopleGraph.Where(person => !person.IsPlaceholder).ToList();
        if (incoming.Select(person => person.Id).Distinct(StringComparer.Ordinal).Count() != incoming.Count)
        {
            return Result<FamilyTreeDetail>.Invalid(new ValidationError(nameof(peopleGraph), "Two people were given the same id."));
        }

        var validated = FamilyGraphEditor.Validate(incoming);
        if (!validated.IsSuccess)
        {
            return validated.To<FamilyTreeDetail>();
        }

        // The normalized graph: every child has explicit sets and exactly one is primary.
        incoming = validated.Value.ToList();

        var tree = await trees.FindOneAsync(treeId);
        if (tree is null)
        {
            return Result<FamilyTreeDetail>.NotFound("That family tree no longer exists.");
        }

        var token = ContextOptions.ForCancellationToken(cancellationToken);
        var existing = (await people.FindAsync(new SearchOptions<Person>
        {
            Query = person => person.TreeId == treeId,
            CancellationToken = cancellationToken,
        })).ToDictionary(person => person.Id);

        var existingSets = (await parentSets.FindAsync(new SearchOptions<ParentSet>
        {
            Query = set => set.TreeId == treeId,
            CancellationToken = cancellationToken,
        })).ToDictionary(set => set.Id);

        // Spouse links and set members are rewritten wholesale; nothing else points at them.
        await relationships.DeleteAsync(link => link.TreeId == treeId, token);
        await parentSetMembers.DeleteAsync(member => member.TreeId == treeId, token);

        var keep = incoming
            .Select(person => int.TryParse(person.Id, out int id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        // A set keeps its row (and so its id) only while it still belongs to the same stored child.
        var keptSets = new Dictionary<string, ParentSet>(StringComparer.Ordinal);
        foreach (var person in incoming)
        {
            if (!int.TryParse(person.Id, out int childId) || !existing.ContainsKey(childId))
            {
                continue;
            }

            foreach (var set in person.ParentSets)
            {
                if (int.TryParse(set.Id, out int setId)
                    && existingSets.TryGetValue(setId, out var storedSet)
                    && storedSet.ChildPersonId == childId)
                {
                    keptSets[set.Id] = storedSet;
                }
            }
        }

        var droppedSets = existingSets.Values.Where(set => !keptSets.ContainsValue(set)).ToList();
        if (droppedSets.Count > 0)
        {
            await parentSets.DeleteAsync(droppedSets, token);
        }

        var removed = existing.Values.Where(person => !keep.Contains(person.Id)).ToList();
        if (tree.FocusPersonId is int focused && removed.Any(person => person.Id == focused))
        {
            tree.FocusPersonId = null;
            await trees.UpdateAsync(tree, token);
        }

        if (removed.Count > 0)
        {
            await people.DeleteAsync(removed, token);
            foreach (var person in removed)
            {
                existing.Remove(person.Id);
            }
        }

        var idMap = new Dictionary<string, int>(StringComparer.Ordinal);
        var updates = new List<Person>();
        var inserts = new List<(string ClientId, Person Entity)>();

        foreach (var person in incoming)
        {
            if (int.TryParse(person.Id, out int id) && existing.TryGetValue(id, out var stored))
            {
                Apply(person, stored);
                updates.Add(stored);
                idMap[person.Id] = id;
            }
            else
            {
                var created = new Person { TreeId = treeId };
                Apply(person, created);
                inserts.Add((person.Id, created));
            }
        }

        if (updates.Count > 0)
        {
            await people.UpdateAsync(updates, token);
        }

        if (inserts.Count > 0)
        {
            await people.InsertAsync(inserts.Select(item => item.Entity), token);
            foreach (var (clientId, entity) in inserts)
            {
                idMap[clientId] = entity.Id;
            }
        }

        var links = BuildLinks(treeId, incoming, idMap);
        if (links.Count > 0)
        {
            await relationships.InsertAsync(links, token);
        }

        await SaveParentSetsAsync(treeId, incoming, idMap, keptSets, token);

        tree.FocusPersonId = focusPersonId is not null && idMap.TryGetValue(focusPersonId, out int mapped) ? mapped : null;
        tree.UpdatedAtUtc = DateTime.UtcNow;
        await trees.UpdateAsync(tree, token);

        logger.LogInformation("Saved family tree {TreeId} ({Count} people)", treeId, incoming.Count);
        return await GetAsync(treeId, cancellationToken);
    }

    /// <summary>
    /// Creates the sample.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    public async Task<Result<FamilyTreeDetail>> CreateSampleAsync(CancellationToken cancellationToken = default)
    {
        var created = await CreateAsync(SampleFamily.Name, "Four generations of a fictional family.", cancellationToken);
        if (!created.IsSuccess)
        {
            return created;
        }

        return await SaveAsync(created.Value.Id, SampleFamily.Create(), SampleFamily.FocusId, cancellationToken);
    }

    private static List<PersonRelationship> BuildLinks(int treeId, IReadOnlyList<PersonData> incoming, Dictionary<string, int> idMap)
    {
        var links = new List<PersonRelationship>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var person in incoming)
        {
            int personId = idMap[person.Id];
            foreach (string spouseId in person.Spouses)
            {
                int spouse = idMap[spouseId];
                if (spouse == personId)
                {
                    continue;
                }

                int low = Math.Min(personId, spouse);
                int high = Math.Max(personId, spouse);
                if (seen.Add($"{low}:{high}"))
                {
                    links.Add(new PersonRelationship
                    {
                        TreeId = treeId,
                        FromPersonId = low,
                        ToPersonId = high,
                        Kind = RelationshipKind.Spouse,
                    });
                }
            }
        }

        return links;
    }

    /// <summary>
    /// Writes every child's parent sets. A set that was already stored is updated in place so its id survives;
    /// any other set is inserted. Members are always inserted fresh, in the order the set lists its parents.
    /// </summary>
    private async Task SaveParentSetsAsync(
        int treeId,
        IReadOnlyList<PersonData> incoming,
        Dictionary<string, int> idMap,
        Dictionary<string, ParentSet> keptSets,
        ContextOptions token)
    {
        var updates = new List<ParentSet>();
        var inserts = new List<(ParentSetData Set, ParentSet Entity)>();

        foreach (var person in incoming)
        {
            foreach (var set in person.ParentSets)
            {
                if (keptSets.TryGetValue(set.Id, out var stored))
                {
                    stored.Type = set.Type;
                    stored.IsPrimary = set.IsPrimary;
                    stored.Date = set.Date;
                    updates.Add(stored);
                    inserts.Add((set, stored));
                }
                else
                {
                    var created = new ParentSet
                    {
                        TreeId = treeId,
                        ChildPersonId = idMap[person.Id],
                        Type = set.Type,
                        IsPrimary = set.IsPrimary,
                        Date = set.Date,
                    };
                    inserts.Add((set, created));
                }
            }
        }

        if (updates.Count > 0)
        {
            await parentSets.UpdateAsync(updates, token);
        }

        var added = inserts.Where(item => !keptSets.ContainsKey(item.Set.Id)).Select(item => item.Entity).ToList();
        if (added.Count > 0)
        {
            await parentSets.InsertAsync(added, token);
        }

        var members = inserts
            .SelectMany(item => item.Set.ParentIds.Select(parentId => new ParentSetMember
            {
                TreeId = treeId,
                ParentSetId = item.Entity.Id,
                ParentPersonId = idMap[parentId],
            }))
            .ToList();

        if (members.Count > 0)
        {
            await parentSetMembers.InsertAsync(members, token);
        }
    }

    private static FamilyTreeDetail ToDetail(
        FamilyTree tree,
        List<Person> members,
        List<PersonRelationship> links,
        List<ParentSet> sets,
        List<ParentSetMember> setMembers)
    {
        var parents = members.ToDictionary(person => person.Id, _ => new List<string>());
        var spouses = members.ToDictionary(person => person.Id, _ => new List<string>());
        var children = members.ToDictionary(person => person.Id, _ => new List<string>());
        var childSets = members.ToDictionary(person => person.Id, _ => new List<ParentSetData>());
        var known = members.Select(person => person.Id).ToHashSet();

        foreach (var link in links)
        {
            if (!known.Contains(link.FromPersonId) || !known.Contains(link.ToPersonId))
            {
                continue;
            }

            if (link.Kind == RelationshipKind.Parent)
            {
                // Only a database that was never migrated has these; they read as one implicit birth set.
                Add(children, link.FromPersonId, link.ToPersonId);
                Add(parents, link.ToPersonId, link.FromPersonId);
            }
            else
            {
                Add(spouses, link.FromPersonId, link.ToPersonId);
                Add(spouses, link.ToPersonId, link.FromPersonId);
            }
        }

        var membersBySet = setMembers
            .Where(member => known.Contains(member.ParentPersonId))
            .GroupBy(member => member.ParentSetId)
            .ToDictionary(group => group.Key, group => group.OrderBy(member => member.Id).Select(member => member.ParentPersonId).ToList());

        foreach (var set in sets.OrderBy(set => set.Id))
        {
            if (!known.Contains(set.ChildPersonId) || !membersBySet.TryGetValue(set.Id, out var parentIds) || parentIds.Count == 0)
            {
                continue;
            }

            childSets[set.ChildPersonId].Add(new ParentSetData
            {
                Id = set.Id.ToString(),
                Type = set.Type,
                IsPrimary = set.IsPrimary,
                ParentIds = parentIds.Select(id => id.ToString()).ToList(),
                Date = set.Date,
            });

            foreach (int parentId in parentIds)
            {
                Add(children, parentId, set.ChildPersonId);
            }
        }

        var people = members.Select(person => ParentSetRules.NormalizePerson(new PersonData
        {
            Id = person.Id.ToString(),
            Gender = person.Gender,
            FirstName = person.FirstName,
            LastName = person.LastName,
            BirthDate = person.BirthDate,
            DeathDate = person.DeathDate,
            BirthPlace = person.BirthPlace,
            DeathPlace = person.DeathPlace,
            AvatarUrl = person.AvatarUrl,
            Notes = person.Notes,
            Parents = parents[person.Id],
            ParentSets = childSets[person.Id],
            Spouses = spouses[person.Id],
            Children = children[person.Id],
        })).ToList();

        return new FamilyTreeDetail(
            tree.Id,
            tree.Name,
            tree.Description,
            tree.FocusPersonId?.ToString(),
            people);
    }
    private static void Add(Dictionary<int, List<string>> lists, int ownerId, int otherId)
    {
        string id = otherId.ToString();
        if (!lists[ownerId].Contains(id))
        {
            lists[ownerId].Add(id);
        }
    }

    private static void Apply(PersonData person, Person entity)
    {
        entity.Gender = person.Gender;
        entity.FirstName = person.FirstName;
        entity.LastName = person.LastName;
        entity.BirthDate = person.BirthDate;
        entity.DeathDate = person.DeathDate;
        entity.BirthPlace = person.BirthPlace;
        entity.DeathPlace = person.DeathPlace;
        entity.AvatarUrl = person.AvatarUrl;
        entity.Notes = person.Notes;
    }

    private static Result<string> CleanName(string name)
    {
        string trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return Result<string>.Invalid(new ValidationError(nameof(name), "Give the tree a name."));
        }

        if (trimmed.Length > Constants.MaxLengths.TreeName)
        {
            return Result<string>.Invalid(new ValidationError(nameof(name), "That name is too long."));
        }

        return Result<string>.Success(trimmed);
    }

    private static string? NullIfBlank(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length == 0 ? null : trimmed;
    }
}
