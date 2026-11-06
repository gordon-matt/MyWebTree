namespace MyWebTree;

/// <summary>
/// Provides constants.
/// </summary>
public static class Constants
{
    /// <summary>Database schema for every provider that supports one. SQLite stores the same tables without a schema.</summary>
    public const string Schema = "webtree";

    /// <summary>Kept apart from the host's <c>__EFMigrationsHistory</c> so both can share a database.</summary>
    public const string MigrationsHistoryTable = "__MyWebTreeMigrationsHistory";

    /// <summary>
    /// Provides tables.
    /// </summary>
    public static class Tables
    {
        /// <summary>
        /// The trees constant.
        /// </summary>
        public const string Trees = "Trees";
        /// <summary>
        /// The people constant.
        /// </summary>
        public const string People = "People";
        /// <summary>
        /// The relationships constant.
        /// </summary>
        public const string Relationships = "Relationships";
        /// <summary>
        /// The parent sets constant.
        /// </summary>
        public const string ParentSets = "ParentSets";
        /// <summary>
        /// The parent set members constant.
        /// </summary>
        public const string ParentSetMembers = "ParentSetMembers";
    }

    /// <summary>
    /// Provides max lengths.
    /// </summary>
    public static class MaxLengths
    {
        /// <summary>
        /// The tree name constant.
        /// </summary>
        public const int TreeName = 120;
        /// <summary>
        /// The description constant.
        /// </summary>
        public const int Description = 500;
        /// <summary>
        /// The person name constant.
        /// </summary>
        public const int PersonName = 200;
        /// <summary>
        /// The place constant.
        /// </summary>
        public const int Place = 200;
        /// <summary>
        /// The birth place constant.
        /// </summary>
        public const int BirthPlace = Place;
        /// <summary>
        /// The avatar URL constant.
        /// </summary>
        public const int AvatarUrl = 500;
        /// <summary>
        /// The notes constant.
        /// </summary>
        public const int Notes = 4000;
    }

    /// <summary>
    /// Provides themes.
    /// </summary>
    public static class Themes
    {
        /// <summary>
        /// The light constant.
        /// </summary>
        public const string Light = "light";
        /// <summary>
        /// The dark constant.
        /// </summary>
        public const string Dark = "dark";
        /// <summary>
        /// The auto constant.
        /// </summary>
        public const string Auto = "auto";
        /// <summary>
        /// The bootstrap constant.
        /// </summary>
        public const string Bootstrap = "bootstrap";
    }

    /// <summary>
    /// Provides database providers.
    /// </summary>
    public static class DatabaseProviders
    {
        /// <summary>
        /// The my SQL constant.
        /// </summary>
        public const string MySql = "MySql";
        /// <summary>
        /// The npgsql constant.
        /// </summary>
        public const string Npgsql = "Npgsql";
        /// <summary>
        /// The sqlite constant.
        /// </summary>
        public const string Sqlite = "Sqlite";
        /// <summary>
        /// The SQL server constant.
        /// </summary>
        public const string SqlServer = "SqlServer";
    }
}
