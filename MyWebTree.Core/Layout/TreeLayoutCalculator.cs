using MyWebTree.Graph;
using MyWebTree.Models;

namespace MyWebTree.Layout;

/// <summary>
/// Places a family the way family-chart does, without D3: the focus person's ancestors sit above,
/// descendants sit below, and spouses stand beside each other. Horizontal mode turns that on its side.
/// </summary>
public static class TreeLayoutCalculator
{
    public const double CardWidth = 232;
    public const double CardHeight = 84;
    public const double SpouseGap = 28;
    public const double SiblingGap = 40;
    public const double LevelGap = 78;

    private const double Padding = 48;

    public static TreeLayoutResult Calculate(IReadOnlyList<PersonData> people, TreeLayoutOptions? options = null)
    {
        options ??= new TreeLayoutOptions();
        var map = new Dictionary<string, PersonData>(StringComparer.Ordinal);
        foreach (var person in people)
        {
            if (!string.IsNullOrWhiteSpace(person.Id) && !person.IsPlaceholder)
            {
                map[person.Id] = person;
            }
        }

        if (map.Count == 0)
        {
            return TreeLayoutResult.Empty;
        }

        string focusId = options.FocusId is not null && map.ContainsKey(options.FocusId)
            ? options.FocusId
            : map.Keys.First();

        if (options.ShowPlaceholders)
        {
            AddPlaceholders(map, focusId);
        }

        bool horizontal = options.Orientation == TreeOrientation.Horizontal;
        var metrics = new Metrics(
            horizontal ? CardHeight : CardWidth,
            horizontal ? CardWidth : CardHeight,
            SpouseGap,
            SiblingGap,
            LevelGap);

        int ancestry = Math.Max(0, options.AncestryDepth);
        int progeny = Math.Max(0, options.ProgenyDepth);
        bool hasRealParent = map[focusId].Parents.Any(id => map.TryGetValue(id, out var parent) && !parent.IsPlaceholder);

        Block root;
        string? ancestorOf = null;
        int ancestorDepth = 0;
        if (options.ShowSiblings && hasRealParent && ancestry > 0)
        {
            string parentId = PreferredParent(map[focusId], map);
            var placed = new HashSet<string>(StringComparer.Ordinal);
            root = LayoutDown(parentId, progeny + 1, map, placed, metrics);
            ancestorOf = parentId;
            ancestorDepth = ancestry - 1;
        }
        else if (options.ShowSiblings && hasRealParent)
        {
            root = LayoutSiblings(focusId, progeny, map, metrics);
        }
        else
        {
            var placed = new HashSet<string>(StringComparer.Ordinal);
            root = LayoutDown(focusId, progeny, map, placed, metrics);
            ancestorOf = focusId;
            ancestorDepth = ancestry;
        }

        var cards = root.Cards.ToList();
        if (ancestorOf is not null && ancestorDepth > 0)
        {
            if (options.Lineage == LineageScope.Patrilineal)
            {
                AttachAncestors(cards, map, ancestorOf, ancestorDepth, metrics);
            }
            else if (ancestorOf == focusId)
            {
                var claimed = cards.Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
                PlaceAbove(cards, focusId, BuildAncestorColumn(focusId, ancestorDepth, map, claimed, metrics), metrics);
            }
            else
            {
                var parents = map[focusId].Parents
                    .Where(id => cards.Any(card => card.Id == id))
                    .Distinct(StringComparer.Ordinal)
                    .Take(2)
                    .ToList();
                AttachBothSides(cards, map, parents, ancestorDepth, metrics);
            }
        }

        if (horizontal)
        {
            cards = cards.Select(card => card with { X = card.Y, Y = card.X }).ToList();
        }

        Normalize(cards, out double width, out double height);
        var links = BuildLinks(cards, map, horizontal);
        var kinship = KinshipLabeler.Label(map, focusId);
        return new TreeLayoutResult(focusId, cards, links, kinship, width, height);
    }

    private static void AddPlaceholders(Dictionary<string, PersonData> map, string focusId)
    {
        if (!map.ContainsKey(focusId))
        {
            return;
        }

        PersonData Focus() => map[focusId];

        if (Focus().Parents.Count < 2 && !Focus().Parents.Any(id => map.TryGetValue(id, out var parent) && parent.Gender == Gender.Male))
        {
            string id = "+father:" + focusId;
            map[id] = Placeholder(id, Gender.Male, PlaceholderKind.Father, focusId);
            FamilyLinks.Parent(map, id, focusId);
        }

        if (Focus().Parents.Count < 2 && !Focus().Parents.Any(id => map.TryGetValue(id, out var parent) && parent.Gender == Gender.Female))
        {
            string id = "+mother:" + focusId;
            map[id] = Placeholder(id, Gender.Female, PlaceholderKind.Mother, focusId);
            FamilyLinks.Parent(map, id, focusId);
        }

        string? father = Focus().Parents.FirstOrDefault(id => map.TryGetValue(id, out var parent) && parent.Gender == Gender.Male);
        string? mother = Focus().Parents.FirstOrDefault(id => map.TryGetValue(id, out var parent) && parent.Gender == Gender.Female);
        if (father is not null && mother is not null)
        {
            FamilyLinks.Spouse(map, father, mother);
        }

        string spouseId = "+spouse:" + focusId;
        Gender spouseGender = Focus().Gender switch
        {
            Gender.Male => Gender.Female,
            Gender.Female => Gender.Male,
            _ => Gender.Unknown,
        };
        map[spouseId] = Placeholder(spouseId, spouseGender, PlaceholderKind.Spouse, focusId);
        FamilyLinks.Spouse(map, focusId, spouseId);

        string? partner = Focus().Spouses.FirstOrDefault(id => map.TryGetValue(id, out var spouse) && !spouse.IsPlaceholder) ?? spouseId;
        string childId = "+child:" + focusId;
        map[childId] = Placeholder(childId, Gender.Unknown, PlaceholderKind.Child, focusId);
        FamilyLinks.Parent(map, focusId, childId);
        FamilyLinks.Parent(map, partner, childId);
    }

    private static PersonData Placeholder(string id, Gender gender, PlaceholderKind kind, string relatesTo) => new()
    {
        Id = id,
        Gender = gender,
        IsPlaceholder = true,
        Placeholder = kind,
        PlaceholderForId = relatesTo,
    };

    private static string PreferredParent(PersonData person, Dictionary<string, PersonData> map)
    {
        var parents = person.Parents.Where(map.ContainsKey).Select(id => map[id]).ToList();
        var realMale = parents.FirstOrDefault(parent => parent.Gender == Gender.Male && !parent.IsPlaceholder);
        if (realMale is not null)
        {
            return realMale.Id;
        }

        return (parents.FirstOrDefault(parent => !parent.IsPlaceholder) ?? parents[0]).Id;
    }

    private static Block LayoutDown(
        string id,
        int depthRemaining,
        Dictionary<string, PersonData> map,
        HashSet<string> placed,
        Metrics metrics)
    {
        if (!map.TryGetValue(id, out var person) || !placed.Add(id))
        {
            return new Block();
        }

        var spouses = new List<string>();
        foreach (string spouseId in person.Spouses)
        {
            if (map.ContainsKey(spouseId) && placed.Add(spouseId))
            {
                spouses.Add(spouseId);
            }
        }

        foreach (string childId in person.Children)
        {
            if (!map.TryGetValue(childId, out var child))
            {
                continue;
            }

            foreach (string parentId in child.Parents)
            {
                if (parentId != id && map.ContainsKey(parentId) && placed.Add(parentId))
                {
                    spouses.Add(parentId);
                }
            }
        }

        spouses.Sort((a, b) => ComparePeople(map[a], map[b]));

        var childIds = new List<string>();
        if (depthRemaining > 0)
        {
            ConsiderChildren(person, id, spouses, map, placed, childIds);
            foreach (string spouseId in spouses)
            {
                ConsiderChildren(map[spouseId], id, spouses, map, placed, childIds);
            }

            childIds.Sort((a, b) => ComparePeople(map[a], map[b]));
        }

        var childBlocks = new List<Block>();
        foreach (string childId in childIds)
        {
            var child = LayoutDown(childId, depthRemaining - 1, map, placed, metrics);
            if (child.Cards.Count > 0)
            {
                childBlocks.Add(child);
            }
        }

        int partnerCount = 1 + spouses.Count;
        double partnersWidth = partnerCount * metrics.Across + Math.Max(0, partnerCount - 1) * metrics.SpouseGap;
        double childrenWidth = childBlocks.Count == 0
            ? 0
            : childBlocks.Sum(block => block.Width) + (childBlocks.Count - 1) * metrics.SiblingGap;
        double width = Math.Max(partnersWidth, Math.Max(childrenWidth, metrics.Across));

        var block = new Block { Width = width };
        double x = (width - partnersWidth) / 2;
        block.Cards.Add(Card(person, x, 0));
        double left = x;
        x += metrics.Across + metrics.SpouseGap;
        foreach (string spouseId in spouses)
        {
            block.Cards.Add(Card(map[spouseId], x, 0));
            x += metrics.Across + metrics.SpouseGap;
        }

        block.StemX = (left + left + partnersWidth) / 2;
        double height = metrics.Down;
        if (childBlocks.Count > 0)
        {
            double childX = (width - childrenWidth) / 2;
            double childY = metrics.Down + metrics.LevelGap;
            foreach (var child in childBlocks)
            {
                foreach (var card in child.Cards)
                {
                    block.Cards.Add(card with { X = card.X + childX, Y = card.Y + childY });
                }

                childX += child.Width + metrics.SiblingGap;
                height = Math.Max(height, childY + child.Height);
            }
        }

        block.Height = height;
        return block;
    }

    private static void ConsiderChildren(
        PersonData owner,
        string primaryId,
        List<string> spouses,
        Dictionary<string, PersonData> map,
        HashSet<string> placed,
        List<string> childIds)
    {
        foreach (string childId in owner.Children)
        {
            if (!map.TryGetValue(childId, out var child) || placed.Contains(childId) || childIds.Contains(childId))
            {
                continue;
            }

            bool belongs = child.Parents.Contains(primaryId) || child.Parents.Any(spouses.Contains);
            if (belongs)
            {
                childIds.Add(childId);
            }
        }
    }

    private static Block LayoutSiblings(string focusId, int progenyDepth, Dictionary<string, PersonData> map, Metrics metrics)
    {
        var ids = new List<string> { focusId };
        foreach (string parentId in map[focusId].Parents)
        {
            if (!map.TryGetValue(parentId, out var parent))
            {
                continue;
            }

            foreach (string childId in parent.Children)
            {
                if (map.ContainsKey(childId) && !ids.Contains(childId))
                {
                    ids.Add(childId);
                }
            }
        }

        ids.Sort((a, b) => ComparePeople(map[a], map[b]));
        var placed = new HashSet<string>(StringComparer.Ordinal);
        var blocks = ids.Select(id => LayoutDown(id, progenyDepth, map, placed, metrics)).Where(block => block.Cards.Count > 0).ToList();
        return Join(blocks, metrics);
    }

    private static Block Join(List<Block> blocks, Metrics metrics)
    {
        if (blocks.Count == 0)
        {
            return new Block();
        }

        double width = blocks.Sum(block => block.Width) + (blocks.Count - 1) * metrics.SiblingGap;
        var joined = new Block { Width = width, StemX = width / 2 };
        double x = 0;
        double height = 0;
        foreach (var block in blocks)
        {
            foreach (var card in block.Cards)
            {
                joined.Cards.Add(card with { X = card.X + x });
            }

            x += block.Width + metrics.SiblingGap;
            height = Math.Max(height, block.Height);
        }

        joined.Height = height;
        return joined;
    }

    private static void AttachBothSides(
        List<TreeCard> cards,
        Dictionary<string, PersonData> map,
        List<string> parentIds,
        int depth,
        Metrics metrics)
    {
        var claimed = cards.Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
        var sides = parentIds
            .Select(id => (Id: id, Block: BuildAncestorColumn(id, depth, map, claimed, metrics)))
            .Where(side => side.Block.Cards.Count > 0)
            .OrderBy(side => cards.First(card => card.Id == side.Id).X)
            .ToList();
        if (sides.Count == 0)
        {
            return;
        }

        if (sides.Count >= 2)
        {
            var left = sides[0];
            var right = sides[1];
            var leftCard = cards.First(card => card.Id == left.Id);
            var rightCard = cards.First(card => card.Id == right.Id);
            double leftMid = leftCard.X + (metrics.Across / 2);
            double rightMid = rightCard.X + (metrics.Across / 2);
            double leftEnd = leftMid - left.Block.StemX + left.Block.Width;
            double rightStart = rightMid - right.Block.StemX;
            double gap = leftEnd + metrics.SiblingGap - rightStart;
            if (gap > 0)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i].X + (metrics.Across / 2) >= rightMid - 0.5)
                    {
                        cards[i] = cards[i] with { X = cards[i].X + gap };
                    }
                }
            }
        }

        foreach (var side in sides)
        {
            PlaceAbove(cards, side.Id, side.Block, metrics);
        }
    }

    private static Block BuildAncestorColumn(
        string personId,
        int depth,
        Dictionary<string, PersonData> map,
        HashSet<string> claimed,
        Metrics metrics)
    {
        if (depth <= 0 || !map.TryGetValue(personId, out var person))
        {
            return new Block();
        }

        var parents = person.Parents
            .Where(id => map.ContainsKey(id) && !claimed.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .Select(id => map[id])
            .OrderBy(parent => parent.Gender == Gender.Male ? 0 : parent.Gender == Gender.Female ? 1 : 2)
            .ThenBy(parent => parent.Id, StringComparer.Ordinal)
            .Take(2)
            .ToList();
        foreach (var parent in parents)
        {
            claimed.Add(parent.Id);
        }

        if (parents.Count == 0)
        {
            return new Block();
        }

        var columns = new List<Block>();
        foreach (var parent in parents)
        {
            columns.Add(AnchorBelow(BuildAncestorColumn(parent.Id, depth - 1, map, claimed, metrics), parent, metrics));
        }

        return columns.Count == 1 ? columns[0] : JoinCouple(columns[0], columns[1], metrics);
    }

    /// <summary>
    /// Puts this person on the bottom of the block, centered under their own ancestors.
    /// A wife with no ancestors of her own therefore stays on her husband's row.
    /// </summary>
    private static Block AnchorBelow(Block above, PersonData person, Metrics metrics)
    {
        double parentCenter = above.Cards.Count == 0 ? metrics.Across / 2 : above.StemX;
        double parentX = parentCenter - (metrics.Across / 2);
        double parentY = above.Cards.Count == 0 ? 0 : above.Height + metrics.LevelGap;

        double minX = parentX;
        double maxX = parentX + metrics.Across;
        foreach (var card in above.Cards)
        {
            minX = Math.Min(minX, card.X);
            maxX = Math.Max(maxX, card.X + metrics.Across);
        }

        double shift = -minX;
        var block = new Block
        {
            Width = maxX - minX,
            Height = parentY + metrics.Down,
            StemX = parentCenter + shift,
        };
        foreach (var card in above.Cards)
        {
            block.Cards.Add(card with { X = card.X + shift });
        }

        block.Cards.Add(Card(person, parentX + shift, parentY));
        return block;
    }

    /// <summary>
    /// Sits the two parents a spouse-gap apart and bottom-aligns them.
    /// Ancestor cards on the same row are pushed apart; cards on a higher row may extend over a spouse who has none.
    /// </summary>
    private static Block JoinCouple(Block left, Block right, Metrics metrics)
    {
        double maxHeight = Math.Max(left.Height, right.Height);
        double leftDy = maxHeight - left.Height;
        double rightDy = maxHeight - right.Height;
        double rightX = left.StemX - right.StemX + metrics.Across + metrics.SpouseGap;

        double push = 0;
        foreach (var leftCard in left.Cards)
        {
            double leftTop = leftCard.Y + leftDy;
            double leftBottom = leftTop + metrics.Down;
            foreach (var rightCard in right.Cards)
            {
                double rightTop = rightCard.Y + rightDy;
                double rightBottom = rightTop + metrics.Down;
                if (leftTop >= rightBottom - 0.5 || rightTop >= leftBottom - 0.5)
                {
                    continue;
                }

                double need = leftCard.X + metrics.Across + metrics.SpouseGap - (rightX + rightCard.X);
                if (need > push)
                {
                    push = need;
                }
            }
        }

        rightX += push;
        double origin = Math.Min(0, rightX);
        double leftCenter = left.StemX - origin;
        double rightCenter = rightX + right.StemX - origin;
        var joined = new Block
        {
            Width = Math.Max(left.Width, rightX + right.Width) - origin,
            Height = maxHeight,
            StemX = (leftCenter + rightCenter) / 2,
        };
        foreach (var card in left.Cards)
        {
            joined.Cards.Add(card with { X = card.X - origin, Y = card.Y + leftDy });
        }

        foreach (var card in right.Cards)
        {
            joined.Cards.Add(card with { X = card.X + rightX - origin, Y = card.Y + rightDy });
        }

        return joined;
    }

    private static void PlaceAbove(List<TreeCard> cards, string anchorId, Block block, Metrics metrics)
    {
        if (block.Cards.Count == 0)
        {
            return;
        }

        var anchor = cards.FirstOrDefault(card => card.Id == anchorId);
        if (anchor is null)
        {
            return;
        }

        double dx = anchor.X + (metrics.Across / 2) - block.StemX;
        double dy = anchor.Y - metrics.LevelGap - block.Height;
        foreach (var card in block.Cards)
        {
            cards.Add(card with { X = card.X + dx, Y = card.Y + dy });
        }
    }

    private static void AttachAncestors(
        List<TreeCard> cards,
        Dictionary<string, PersonData> map,
        string personId,
        int depth,
        Metrics metrics)
    {
        if (depth <= 0 || !map.TryGetValue(personId, out var person))
        {
            return;
        }

        var parents = person.Parents.Where(map.ContainsKey).Distinct(StringComparer.Ordinal).Take(2).ToList();
        if (parents.Count == 0)
        {
            return;
        }

        var existing = cards.Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
        if (parents.All(existing.Contains))
        {
            foreach (string parentId in parents)
            {
                AttachAncestors(cards, map, parentId, depth - 1, metrics);
            }

            return;
        }

        string start = parents.First(parentId => !existing.Contains(parentId));
        var placed = new HashSet<string>(existing, StringComparer.Ordinal);
        var block = LayoutDown(start, 0, map, placed, metrics);
        string? other = parents.FirstOrDefault(parentId => parentId != start && block.Cards.All(card => card.Id != parentId));
        if (other is not null && !existing.Contains(other))
        {
            var second = LayoutDown(other, 0, map, placed, metrics);
            if (second.Cards.Count > 0)
            {
                double shift = block.Width + metrics.SpouseGap;
                foreach (var card in second.Cards)
                {
                    block.Cards.Add(card with { X = card.X + shift });
                }

                block.Width += metrics.SpouseGap + second.Width;
                block.Height = Math.Max(block.Height, second.Height);
            }
        }

        if (block.Cards.Count == 0)
        {
            return;
        }

        var parentCards = block.Cards.Where(card => parents.Contains(card.Id)).ToList();
        block.StemX = parentCards.Count == 0
            ? block.Width / 2
            : (parentCards.Min(card => card.X) + parentCards.Max(card => card.X + metrics.Across)) / 2;

        var anchor = cards.FirstOrDefault(card => card.Id == personId);
        if (anchor is null)
        {
            return;
        }

        var row = cards.Where(card =>
                Math.Abs(card.Y - anchor.Y) < 1
                && (card.Id == personId || person.Spouses.Contains(card.Id)))
            .ToList();
        if (row.Count == 0)
        {
            row.Add(anchor);
        }

        double mid = (row.Min(card => card.X) + row.Max(card => card.X + metrics.Across)) / 2;
        double top = row.Min(card => card.Y);
        double dx = mid - block.StemX;
        double dy = top - metrics.LevelGap - block.Height;
        foreach (var card in block.Cards)
        {
            cards.Add(card with { X = card.X + dx, Y = card.Y + dy });
        }

        foreach (string parentId in parents)
        {
            AttachAncestors(cards, map, parentId, depth - 1, metrics);
        }
    }

    private static void Normalize(List<TreeCard> cards, out double width, out double height)
    {
        if (cards.Count == 0)
        {
            width = 0;
            height = 0;
            return;
        }

        double minX = cards.Min(card => card.X);
        double minY = cards.Min(card => card.Y);
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i] = cards[i] with
            {
                X = cards[i].X - minX + Padding,
                Y = cards[i].Y - minY + Padding,
            };
        }

        width = cards.Max(card => card.X) + CardWidth + Padding;
        height = cards.Max(card => card.Y) + CardHeight + Padding;
    }

    private static List<TreeLink> BuildLinks(List<TreeCard> cards, Dictionary<string, PersonData> map, bool horizontal)
    {
        var links = new List<TreeLink>();
        var cardOf = new Dictionary<string, TreeCard>(StringComparer.Ordinal);
        foreach (var card in cards)
        {
            cardOf[card.Id] = card;
        }

        var seenSpouses = new HashSet<string>(StringComparer.Ordinal);
        foreach (var card in cards)
        {
            if (!map.TryGetValue(card.Id, out var person))
            {
                continue;
            }

            foreach (string spouseId in person.Spouses)
            {
                if (!cardOf.TryGetValue(spouseId, out var other))
                {
                    continue;
                }

                string key = string.CompareOrdinal(card.Id, spouseId) < 0 ? card.Id + "|" + spouseId : spouseId + "|" + card.Id;
                if (!seenSpouses.Add(key) || !AreAdjacent(card, other))
                {
                    continue;
                }

                bool dashed = person.IsPlaceholder || map[spouseId].IsPlaceholder;
                links.Add(SpouseLink(card, other, dashed));
            }
        }

        var groups = new Dictionary<string, List<TreeCard>>(StringComparer.Ordinal);
        foreach (var card in cards)
        {
            if (!map.TryGetValue(card.Id, out var person))
            {
                continue;
            }

            var parentIds = person.Parents.Where(cardOf.ContainsKey).Order(StringComparer.Ordinal).ToList();
            if (parentIds.Count == 0)
            {
                continue;
            }

            string key = string.Join('|', parentIds);
            if (!groups.TryGetValue(key, out var children))
            {
                groups[key] = children = [];
            }

            children.Add(card);
        }

        foreach (var (key, children) in groups)
        {
            var parents = key.Split('|').Select(id => cardOf[id]).ToList();
            bool forward = horizontal
                ? children.All(child => child.X >= parents.Max(parent => parent.X) - 1)
                : children.All(child => child.Y >= parents.Max(parent => parent.Y) - 1);
            if (!forward)
            {
                continue;
            }

            bool dashed = parents.Concat(children).Any(card => map.TryGetValue(card.Id, out var person) && person.IsPlaceholder);
            links.AddRange(ParentLinks(parents, children, horizontal, dashed));
        }

        return links;
    }

    private static bool AreAdjacent(TreeCard a, TreeCard b)
    {
        if (Math.Abs(a.Y - b.Y) < 2)
        {
            double gap = Math.Abs(a.X - b.X) - CardWidth;
            return gap >= -1 && gap < CardWidth;
        }

        if (Math.Abs(a.X - b.X) < 2)
        {
            double gap = Math.Abs(a.Y - b.Y) - CardHeight;
            return gap >= -1 && gap < CardHeight;
        }

        return false;
    }

    private static TreeLink SpouseLink(TreeCard a, TreeCard b, bool dashed)
    {
        if (Math.Abs(a.Y - b.Y) < 2)
        {
            var left = a.X <= b.X ? a : b;
            var right = a.X <= b.X ? b : a;
            return new TreeLink(left.X + CardWidth, left.Y + (CardHeight / 2), right.X, right.Y + (CardHeight / 2), dashed);
        }

        var top = a.Y <= b.Y ? a : b;
        var bottom = a.Y <= b.Y ? b : a;
        return new TreeLink(top.X + (CardWidth / 2), top.Y + CardHeight, bottom.X + (CardWidth / 2), bottom.Y, dashed);
    }

    private static IEnumerable<TreeLink> ParentLinks(List<TreeCard> parents, List<TreeCard> children, bool horizontal, bool dashed)
    {
        if (horizontal)
        {
            double parentRight = parents.Max(parent => parent.X + CardWidth);
            double childLeft = children.Min(child => child.X);
            double busX = parentRight + ((childLeft - parentRight) * 0.45);
            double top = parents.Min(parent => parent.Y + (CardHeight / 2));
            double bottom = parents.Max(parent => parent.Y + (CardHeight / 2));
            foreach (var child in children)
            {
                top = Math.Min(top, child.Y + (CardHeight / 2));
                bottom = Math.Max(bottom, child.Y + (CardHeight / 2));
            }

            foreach (var parent in parents)
            {
                yield return new TreeLink(parent.X + CardWidth, parent.Y + (CardHeight / 2), busX, parent.Y + (CardHeight / 2), dashed);
            }

            if (bottom - top > 1)
            {
                yield return new TreeLink(busX, top, busX, bottom, dashed);
            }

            foreach (var child in children)
            {
                yield return new TreeLink(busX, child.Y + (CardHeight / 2), child.X, child.Y + (CardHeight / 2), dashed);
            }

            yield break;
        }

        double parentBottom = parents.Max(parent => parent.Y + CardHeight);
        double childTop = children.Min(child => child.Y);
        double busY = parentBottom + ((childTop - parentBottom) * 0.45);
        double left = parents.Min(parent => parent.X + (CardWidth / 2));
        double right = parents.Max(parent => parent.X + (CardWidth / 2));
        foreach (var child in children)
        {
            left = Math.Min(left, child.X + (CardWidth / 2));
            right = Math.Max(right, child.X + (CardWidth / 2));
        }

        foreach (var parent in parents)
        {
            yield return new TreeLink(parent.X + (CardWidth / 2), parent.Y + CardHeight, parent.X + (CardWidth / 2), busY, dashed);
        }

        if (right - left > 1)
        {
            yield return new TreeLink(left, busY, right, busY, dashed);
        }

        foreach (var child in children)
        {
            yield return new TreeLink(child.X + (CardWidth / 2), busY, child.X + (CardWidth / 2), child.Y, dashed);
        }
    }

    private static TreeCard Card(PersonData person, double x, double y) => new(
        person.Id,
        x,
        y,
        person.IsPlaceholder,
        person.Placeholder,
        person.PlaceholderForId);

    private static int ComparePeople(PersonData a, PersonData b)
    {
        if (a.IsPlaceholder != b.IsPlaceholder)
        {
            return a.IsPlaceholder ? 1 : -1;
        }

        int date = Nullable.Compare(a.BirthDate, b.BirthDate);
        return date != 0 ? date : string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Block
    {
        public double Width { get; set; }

        public double Height { get; set; }

        public double StemX { get; set; }

        public List<TreeCard> Cards { get; } = [];
    }

    private readonly record struct Metrics(double Across, double Down, double SpouseGap, double SiblingGap, double LevelGap);
}
