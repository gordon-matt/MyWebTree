using MyWebTree.Models;

namespace MyWebTree.Services;

public interface IFamilyTreeService
{
    Task<Result<IReadOnlyList<FamilyTreeSummary>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<FamilyTreeDetail>> GetAsync(int treeId, CancellationToken cancellationToken = default);

    Task<Result<FamilyTreeDetail>> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);

    Task<Result<FamilyTreeDetail>> RenameAsync(int treeId, string name, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int treeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the people and relationships of a tree with <paramref name="people"/> and remembers who the tree is centered on.
    /// </summary>
    Task<Result<FamilyTreeDetail>> SaveAsync(
        int treeId,
        IReadOnlyList<PersonData> people,
        string? focusPersonId,
        CancellationToken cancellationToken = default);

    Task<Result<FamilyTreeDetail>> CreateSampleAsync(CancellationToken cancellationToken = default);
}
