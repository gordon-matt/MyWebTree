namespace MyWebTree;

public static class Constants
{
    /// <summary>Database schema for every provider that supports one. SQLite stores the same tables without a schema.</summary>
    public const string Schema = "webtree";

    /// <summary>Kept apart from the host's <c>__EFMigrationsHistory</c> so both can share a database.</summary>
    public const string MigrationsHistoryTable = "__MyWebTreeMigrationsHistory";

    public static class Tables
    {
        public const string Trees = "Trees";
        public const string People = "People";
        public const string Relationships = "Relationships";
    }

    public static class MaxLengths
    {
        public const int TreeName = 120;
        public const int Description = 500;
        public const int PersonName = 200;
        public const int Place = 200;
        public const int BirthPlace = Place;
        public const int AvatarUrl = 500;
        public const int Notes = 4000;
    }

    public static class Themes
    {
        public const string Light = "light";
        public const string Dark = "dark";
        public const string Auto = "auto";
        public const string Bootstrap = "bootstrap";
    }

    public static class DatabaseProviders
    {
        public const string Sqlite = "Sqlite";
    }
}
