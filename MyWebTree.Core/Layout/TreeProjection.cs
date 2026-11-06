using MyWebTree.Graph;
using MyWebTree.Models;

namespace MyWebTree.Layout;

/// <summary>What the projection needs to know about how the chart should be built.</summary>
public sealed record ProjectionOptions
{
    /// <summary>
    /// Hides adopted and foster links, and so everyone who can only be reached through them. Each person is
    /// drawn under a birth set (the active one if it is a birth set, else the first birth set), or under nobody.
    /// </summary>
    public bool BloodRelativesOnly { get; init; }
}

/// <summary>One node of the flat list the layout is drawn from.</summary>
public sealed record ProjectedNode
{
    /// <summary>
    /// Gets or sets the person as the chart sees them. <see cref="PersonData.Parents"/> are the active set's members,
    /// <see cref="PersonData.Children"/> are the children drawn under this person, and for a stub the
    /// <see cref="PersonData.Id"/> is the stub's own id, not the real person's.
    /// </summary>
    public required PersonData Person { get; init; }

    /// <summary>Gets or sets the real person this node stands for. It equals <see cref="PersonData.Id"/> unless this is a stub.</summary>
    public required string PersonId { get; init; }

    /// <summary>Gets or sets a value indicating whether this is a stub: one card, no ancestry or descendants, under parents the person is not currently shown with.</summary>
    public bool IsStub { get; init; }

    /// <summary>Gets or sets the type of the set drawn above this node (the stub's own set for a stub).</summary>
    public ParentageType ParentType { get; init; }

    /// <summary>Gets or sets the id of the set drawn above this node, or null when no parents are drawn.</summary>
    public string? ParentSetId { get; init; }

    /// <summary>Gets or sets how many sets of parents the real person has.</summary>
    public int ParentSetCount { get; init; }
}

/// <summary>
/// Turns the stored graph into the flat list the chart is drawn from. The layout cannot draw one person under two
/// sets of parents at once, so each person is drawn under their active set only. A person who is also a child in
/// another set appears as a stub under those parents.
/// </summary>
public static class TreeProjection
{
    /// <summary>
    /// Builds the nodes. It is pure, and it never walks the graph, so data that loops cannot make it loop.
    /// </summary>
    /// <param name="people">The people. A legacy <see cref="PersonData.Parents"/> list is read as one birth set.</param>
    /// <param name="activeSetByPerson">Person id to the id of the set to draw. A missing or stale entry means the primary set.</param>
    /// <param name="options">Optional switches.</param>
    /// <returns>One node for each person, then one for each stub.</returns>
    public static IReadOnlyList<ProjectedNode> Build(
        IReadOnlyList<PersonData> people,
        IReadOnlyDictionary<string, string>? activeSetByPerson,
        ProjectionOptions? options = null)
    {
        options ??= new ProjectionOptions();
        var real = ParentSetRules.Normalize(people)
            .Where(person => !string.IsNullOrWhiteSpace(person.Id) && !person.IsPlaceholder)
            .GroupBy(person => person.Id, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToList();
        var byId = real.ToDictionary(person => person.Id, StringComparer.Ordinal);

        var active = new Dictionary<string, ParentSetData?>(StringComparer.Ordinal);
        foreach (var person in real)
        {
            active[person.Id] = ChooseDrawn(person, activeSetByPerson, options);
        }

        // A person's own Children order is kept, and a child is listed only under the parents of the set drawn for it.
        var children = real.ToDictionary(
            person => person.Id,
            person => person.Children
                .Where(childId => byId.ContainsKey(childId) && Members(active[childId], byId).Contains(person.Id, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            StringComparer.Ordinal);

        var stubs = new List<ProjectedNode>();
        foreach (var person in real)
        {
            var drawn = active[person.Id];
            var drawnParents = Members(drawn, byId).ToHashSet(StringComparer.Ordinal);
            foreach (var set in person.ParentSets)
            {
                if (ReferenceEquals(set, drawn) || (options.BloodRelativesOnly && set.Type != ParentageType.Birth))
                {
                    continue;
                }

                var members = Members(set, byId).ToList();
                if (members.Count == 0 || members.All(drawnParents.Contains))
                {
                    continue;
                }

                string stubId = StubId(person.Id, set.Id);
                foreach (string memberId in members.Where(id => !drawnParents.Contains(id)))
                {
                    children[memberId].Add(stubId);
                }

                stubs.Add(new ProjectedNode
                {
                    PersonId = person.Id,
                    IsStub = true,
                    ParentType = set.Type,
                    ParentSetId = set.Id,
                    ParentSetCount = person.ParentSets.Count,
                    Person = person with
                    {
                        Id = stubId,
                        Parents = members,
                        ParentSets = [],
                        Spouses = [],
                        Children = [],
                    },
                });
            }
        }

        var nodes = new List<ProjectedNode>(real.Count + stubs.Count);
        foreach (var person in real)
        {
            var drawn = active[person.Id];
            nodes.Add(new ProjectedNode
            {
                PersonId = person.Id,
                ParentType = drawn?.Type ?? ParentageType.Birth,
                ParentSetId = drawn?.Id,
                ParentSetCount = person.ParentSets.Count,
                Person = person with
                {
                    Parents = Members(drawn, byId).ToList(),
                    Spouses = person.Spouses.Where(byId.ContainsKey).ToList(),
                    Children = children[person.Id],
                },
            });
        }

        nodes.AddRange(stubs);
        return nodes;
    }

    /// <summary>The id the stub of <paramref name="childId"/> under the set <paramref name="setId"/> is given.</summary>
    /// <param name="childId">The real person.</param>
    /// <param name="setId">The non-active set.</param>
    /// <returns>An id that cannot clash with a person or a placeholder.</returns>
    public static string StubId(string childId, string setId) => "~stub:" + childId + ":" + setId;

    private static ParentSetData? ChooseDrawn(
        PersonData person,
        IReadOnlyDictionary<string, string>? activeSetByPerson,
        ProjectionOptions options)
    {
        var chosen = ParentSetRules.ActiveOf(person, activeSetByPerson);
        if (!options.BloodRelativesOnly || chosen is null || chosen.Type == ParentageType.Birth)
        {
            return chosen;
        }

        return person.ParentSets.FirstOrDefault(set => set.Type == ParentageType.Birth);
    }

    private static IEnumerable<string> Members(ParentSetData? set, Dictionary<string, PersonData> byId) =>
        set is null
            ? []
            : set.ParentIds.Distinct(StringComparer.Ordinal).Where(byId.ContainsKey);
}
