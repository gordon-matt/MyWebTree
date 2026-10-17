namespace MyWebTree;

public sealed class MyWebTreeOptions
{
    public const string SectionName = "MyWebTree";

    /// <summary>Applies migrations when the host starts.</summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>Creates the sample Ward family when the database has no trees yet.</summary>
    public bool SeedSample { get; set; }
}
