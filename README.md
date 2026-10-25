A Blazor family tree for .NET 10. Hand it a list of people and it draws the chart, edits relatives, and hands the change back. Storage is optional: the included service keeps trees in SQLite, SQL Server, PostgreSQL, or MySQL, or the host can keep people in its own database and only use the component.

## Screenshots
<img src="https://github.com/gordon-matt/MyWebTree/blob/master/_Misc/Screenshots/Light.png" alt="Light Theme" width="100%" />

<img src="https://github.com/gordon-matt/MyWebTree/blob/master/_Misc/Screenshots/Dark.png" alt="Dark Theme" width="100%" />

Features in this version:

- Pan, zoom, and fit, with ancestry and descendant depth, a vertical or horizontal layout, and a sibling toggle that starts off
- Kinship labels from the person the tree is centered on
- Add a father, mother, spouse, son, or daughter, edit a person, or remove them without removing their relatives
- Unlink a spouse or a parent and leave both people in the tree
- Undo and redo
- Partial dates: a year, a year and month, or a full day
- Light, dark, auto, and Bootstrap-friendly themes

MyWebTree does not authenticate anyone and does not know about profile pages, uploads, or genealogy file formats. The host decides who may edit, where a person opens, and how a picture is stored.

## Projects

| Project | Purpose |
| --- | --- |
| `MyWebTree.Core` | `PersonData`, `PartialDate`, `FamilyGraphEditor`, and the layout calculator. Depends on Ardalis.Result. |
| `MyWebTree.Data` | Entities and the EF Core model. Tables live in the `webtree` schema. |
| `MyWebTree.Data.Sqlite` | SQLite. Register it with `AddMyWebTreeSqlite()`. |
| `MyWebTree.Data.Sql` | SQL Server. Register it with `AddMyWebTreeSqlServer()`. |
| `MyWebTree.Data.Npgsql` | PostgreSQL. Register it with `AddMyWebTreeNpgsql()`. |
| `MyWebTree.Data.MySql` | MySQL. Register it with `AddMyWebTreeMySql()`. |
| `MyWebTree.Services` | `IFamilyTreeService` and the Extenso repositories. |
| `MyWebTree.Components` | The `FamilyTree` Razor component, its JavaScript and CSS. |
| `MyWebTree.Tests` | xUnit tests for the graph rules and a SQLite round trip. |
| `MyWebTree.Demo.Blazor` | Blazor Server demo. |

## How it fits together

`FamilyTree` is given `People` and raises `Changed` with a `FamilyTreeChange`. The graph rules live in `FamilyGraphEditor`, so a card and a saved row cannot disagree. `IFamilyTreeService` is one way to persist that change. A host that already has people uses the component and the editor, and does not have to use the service.

Repositories are Extenso `IRepository<T>` instances bound to `IMyWebTreeDbContextFactory`, not the host's own factory. A host can call `AddEntityFrameworkRepository()` for its own database without MyWebTree reading the wrong one. Services return `Ardalis.Result`.

## Getting started

Reference `MyWebTree.Components` for the UI. Reference `MyWebTree.Services` and one data project when MyWebTree should own the database. Register exactly one of `AddMyWebTreeSqlite()`, `AddMyWebTreeSqlServer()`, `AddMyWebTreeNpgsql()`, or `AddMyWebTreeMySql()`.

```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services
    .AddMyWebTreeServices()
    .AddMyWebTreeSqlite(builder.Configuration.GetConnectionString("MyWebTree"));

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

SQL Server, PostgreSQL, and MySQL use the same call with their own method. Each needs `ConnectionStrings:MyWebTree`. SQLite does not: omit the connection string and it stores `MyWebTree.db` under the user's local application data folder.

```csharp
builder.Services.AddMyWebTreeSqlServer(connectionString);
builder.Services.AddMyWebTreeNpgsql(connectionString);
builder.Services.AddMyWebTreeMySql(connectionString);
```

Add the stylesheet to `App.razor`, and `@using MyWebTree.Components` to `_Imports.razor`:

```html
<link rel="stylesheet" href="@Assets["_content/MyWebTree.Components/mywebtree.css"]" />
```

```razor
<FamilyTree People="detail.People"
            FocusPersonId="@detail.FocusPersonId"
            Theme="auto"
            Changed="OnChanged" />
```

The component must render interactively. `AutoMigrate` is on by default. Set `SeedSample` to create the Ward family when the database has no trees yet.

`Database:Provider` is `Sqlite`, `SqlServer`, `Npgsql`, or `MySql`. The demo reads that value and calls the matching registration method. An unknown name fails at startup.

```json
{
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "MyWebTree": "Data Source=App_Data/MyWebTree.db"
  },
  "MyWebTree": {
    "AutoMigrate": true,
    "SeedSample": false
  }
}
```

```text
Server=localhost;Database=MyWebTree;Trusted_Connection=True;TrustServerCertificate=True
Host=localhost;Database=MyWebTree;Username=postgres;Password=postgres
Server=localhost;Database=MyWebTree;User=root;Password=root
```

Those three strings are SQL Server, PostgreSQL, and MySQL. Each of those providers has its own migrations. SQL Server, PostgreSQL, and MySQL create the tables in the `webtree` schema. SQLite has no schemas, so that provider ignores the warning and creates the same tables without one. The migrations history table is `__MyWebTreeMigrationsHistory`, in `webtree` where the provider has schemas, so MyWebTree can share a database with the host.

Parameters: `People`, `FocusPersonId`, `Changed`, `Editable` (default `true`), `Theme` (`light`, `dark`, `auto`, `bootstrap`), `Lineage` (`Bilateral` by default, or `Patrilineal`), `ShowSiblings` (default `false`), `Orientation` (`Vertical` by default, or `Horizontal`), `AncestryDepth` and `ProgenyDepth` (generations shown above and below the focus person; `4` by default), `MaxGenerations` (the most the toolbar steppers allow; `8` by default), `DepthsChanged`, `ViewChanged`, `Height` (default `720px`), `Class`, `Error`, `HighlightedPersonId`, `OnOpenPerson`, `OpenPersonText`, and `ChooseAvatar`. The toolbar can change any of those after the tree opens. `ViewChanged` receives a `TreeView` (generations, siblings, lineage and orientation) each time, so the host can remember it (for example in local storage) and pass it back through the matching parameters next time. `DepthsChanged` still receives just the two generation counts. Other attributes are passed to the outer element.

## Integrating with your own people

Use this path when people already live in the host: posts, photos, accounts, or anything else that must not be deleted just because they disappeared from a save payload. Reference `MyWebTree.Components` and `MyWebTree.Core` only. Do not call `IFamilyTreeService.SaveAsync`. That method replaces every relationship in the tree and deletes people who are missing from the list.

Map the host's rows to `PersonData` on the way in. Ids are strings, so a placeholder card that has never been saved can sit beside a real person. `Changed` is the way out.

```razor
<FamilyTree People="people"
            FocusPersonId="@focusId"
            Editable="@canEdit"
            HighlightedPersonId="@currentPersonId"
            OnOpenPerson="OpenPerson"
            OpenPersonText="View profile"
            ChooseAvatar="ChooseAvatar"
            Changed="OnChanged" />
```

### What a change means

`FamilyTreeChange.Kind` says which write to make. `People` is always the whole graph after the edit. `PersonId` is who it happened to.

| Kind | What the host should do |
| --- | --- |
| `Added` | Insert `PersonId` and the links on that card. |
| `Updated` | Update names, dates, places, picture, and notes on `PersonId`. Links did not change. |
| `Removed` | Delete `PersonId` and their links. Relatives stay. |
| `Unlinked` | Remove the link between `PersonId` and `OtherPersonId`. Delete neither person. |
| `FocusChanged` | Remember the centered person. Nobody's data changed. |
| `Snapshot` | Undo, redo, or any other full replace. Reconcile the host to `People`. |

`FocusChanged` still raises `Changed`, so the host can store the centered person wherever it likes. The component does not use browser storage.

```csharp
private async Task OnChanged(FamilyTreeChange change)
{
    switch (change.Kind)
    {
        case FamilyTreeChangeKind.Added:
            await people.InsertAsync(change.People.Single(person => person.Id == change.PersonId));
            break;
        case FamilyTreeChangeKind.Updated:
            await people.UpdateAsync(change.People.Single(person => person.Id == change.PersonId));
            break;
        case FamilyTreeChangeKind.Removed:
            await people.DeleteAsync(change.PersonId!);
            break;
        case FamilyTreeChangeKind.Unlinked:
            await people.UnlinkAsync(change.PersonId!, change.OtherPersonId!);
            break;
        case FamilyTreeChangeKind.FocusChanged:
            focusId = change.FocusPersonId;
            break;
        case FamilyTreeChangeKind.Snapshot:
            await people.ReplaceAsync(change.People);
            focusId = change.FocusPersonId;
            break;
    }
}
```

After a successful save, pass the stored people back in through `People`. Ids assigned by the host on `Added` have to be the ids on the next render, or the next edit will not find the person.

`Editable` is one switch for the whole tree. The host sets it from its own roles. There is no per-person permission inside the component.

### Dates

`PartialDate` is a year, an optional month, and an optional day. `new PartialDate(1842)` is a year only. `new PartialDate(1842, 6)` is June 1842. `new PartialDate(1842, 6, 1)` is a full day. `ToString()` returns `1842`, `1842-06`, or `1842-06-01`.

A death is rejected only when the parts both dates actually know say it is earlier than the birth. A year-only death in the same year as a full birth is kept, because the day is unknown.

The editor asks for year, month, and day separately. Month and day can be left blank. A day without a month, or a month without a year, is not saved.

On a card, the lifespan is the years. The side panel shows the full partial date plus birth place and death place.

### Open a person

`OnOpenPerson` shows a button on the selected person. The button is hidden when nothing is listening. `OpenPersonText` defaults to `Open`. The callback receives the `PersonData`. The host navigates to a profile, an editor, or anywhere else.

`HighlightedPersonId` draws a second outline on that card. Use it for the person who belongs to the signed-in user. It does not change who the tree is centered on. `FocusPersonId` does that.

### Pictures

`AvatarUrl` is an address the component puts in an `<img>`. The editor also has a text field for pasting one.

`ChooseAvatar` adds a Choose picture button. The host uploads, or picks from its own store, and returns the address to store. Return null to leave the current address alone. Return an empty string to clear it.

```csharp
private async Task<string?> ChooseAvatar(AvatarRequest request)
{
    string? path = await files.UploadAsync(request.PersonId);
    return path is null ? null : "/uploads/" + path;
}
```

The component does not read the file itself.

### Fields the component stores

`PersonData` carries `Id`, `Gender` (`Unknown`, `Male`, `Female`), `FirstName`, `LastName`, `BirthDate`, `DeathDate`, `BirthPlace`, `DeathPlace`, `AvatarUrl`, `Notes`, and the `Parents`, `Spouses`, and `Children` id lists.

Names can be 200 characters. Places can be 200. The picture address can be 500. Notes can be 4000. Anything the host keeps beyond that (biography, occupation, account id, map coordinates) stays on the host's own record. Copy it back onto the person when you handle `Updated`, because the component will not round-trip fields it does not know.

### Rules

- A person has at most two parents, and at most one father and one mother.
- A spouse link and a parent link are stored in both directions.
- Adding a second parent also links those two parents as spouses.
- Adding a child of someone with exactly one spouse also links that spouse as the other parent.
- Nobody can be their own ancestor, or both married to and descended from the same person.
- Delete removes the person from every list and leaves the relatives.
- Unlink removes one spouse or parent link. From the selected person, the side panel lists spouses and parents.

## Behaviour

- Click a card to select it. Double-click, or Center, to rebuild the tree around them.
- Search matches name, lifespan, birth place, and death place.
- Placeholder cards are dashed. Choosing one opens the add form for that relative.
- Undo keeps the last 40 edits. Undo and redo raise `Changed` with kind `Snapshot`.
- Depth steppers run from 0 to 8 generations. They only change the drawing, not the data, so they do not raise `Changed`.
- `IFamilyTreeService.SaveAsync` writes the whole graph: it replaces relationships and deletes stored people who are not in the payload. That is the right save for a tree MyWebTree owns. It is the wrong save for a host whose people have other rows pointing at them.

## Demo

`MyWebTree.Demo.Blazor` runs at https://localhost:7427. It reads `Database:Provider`, uses SQLite when that setting is missing, seeds the Ward family, and saves through `IFamilyTreeService`.

## Credits

MyWebTree is derived from [family-chart](https://github.com/donatso/family-chart) by [@donatso](https://github.com/donatso), as such it inherits the MIT license.

## Support

If you find this project useful, consider supporting its development.

<p align="center">
  <a href="https://buymeacoffee.com/vnmatt">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/BuyMeACoffee_Logo.png"
         alt="Buy Me a Coffee"
         height="40">
  </a>&nbsp;&nbsp;&nbsp;
  <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=gordon_matt%40live%2ecom&lc=AU&currency_code=AUD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/PayPal_Logo.png"
         alt="Donate with PayPal"
         height="40">
  </a>
</p>

<p align="center">
  Prefer using your phone? Scan the QR code:
</p>

<p align="center">
  <a href="https://buymeacoffee.com/vnmatt">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/BuyMeACoffee_QR.png"
         alt="Buy Me a Coffee QR Code"
         width="180">
  </a>
</p>

<img src="https://komarev.com/ghpvc/?username=gordon-matt&label=GitHub%20Hits%20Since%202025-06-01%3A%20&color=ff0000&style=flat" alt="gordon-matt" />
