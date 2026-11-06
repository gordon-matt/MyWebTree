# Design check: parent sets, stub nodes and per-edge styling

**Question.** Can the chart draw stub nodes (a child shown under a non-active set of parents) and per-edge styling (solid, dashed, dotted) without rewriting the layout?

**Finding: yes, with no layout rewrite.** The README describes the component as derived from family-chart, but the layout is not JavaScript.

- `MyWebTree.Core/Layout/TreeLayoutCalculator.cs` is a pure C# calculator. It takes a flat `PersonData` list and returns `TreeCard` (id and position) and `TreeLink` (a line segment) records.
- `MyWebTree.Components/wwwroot/mywebtree.js` only measures the viewport, watches for resizes and handles wheel zoom. It never sees a person, a card or a link.
- `FamilyTree.razor` renders every `TreeLink` as an SVG `<line>` and every `TreeCard` as an absolutely positioned `<button>`.

So both needs are met by data, not by new geometry.

## Stub nodes

A stub is one more node in the flat list. A new pure step, `TreeProjection.Build`, runs before the layout:

1. Each person's projected `Parents` come from their active set only.
2. For each non-active set of a child, the projection adds one node (id `~stub:<childId>:<setId>`) whose `Parents` are that set's members and which has no children or spouses. The members get the stub in their projected `Children`.
3. The layout already places a child under its parents and does not descend further when the node has no children, so a stub needs no special placement.

Cards carry the extra facts the UI needs (`IsStub`, `PersonId`, `ParentType`, `ParentSetId`, `ParentSetCount`, `SeeAlso`). The layout is told nothing new except the node metadata.

Because each person has exactly one active set in the projected graph, the layout's existing `placed` and `claimed` sets still guarantee that nobody is drawn twice with full expansion, and no traversal can loop.

## Per-edge styling

`TreeLink` gets a `ParentageType`. `BuildLinks` already groups children by their parent set to draw one bus per family. The group key now also includes the type, so a couple with a birth child and an adopted child get two buses at slightly different heights, each in its own style. The component maps the type to a CSS class (`mwt-edge-adopted`, `mwt-edge-foster`).

The existing dashed placeholder links are different in kind (a missing relative, not a kind of parentage). They become faint solid lines so they cannot be confused with an adopted (dashed) or foster (dotted) edge. Placeholder *cards* stay dashed.

## What was not needed

No change to the JavaScript, no change to card geometry, and no second layout pass.
