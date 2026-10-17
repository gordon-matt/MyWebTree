namespace MyWebTree.Data;

/// <summary>
/// MyWebTree's own context factory. It deliberately extends rather than replaces Extenso's
/// <see cref="IDbContextFactory"/>: host applications built on Extenso register that interface for
/// their own context, and MyWebTree must not take it over. Repositories are constructed against
/// this type explicitly instead.
/// </summary>
public interface IMyWebTreeDbContextFactory : IDbContextFactory
{
    /// <summary>One of <see cref="Constants.DatabaseProviders"/>.</summary>
    string ProviderName { get; }
}
