using MyWebTree.Models;

namespace MyWebTree.Services;

/// <summary>
/// Defines the contract for the family tree service.
/// </summary>
public interface IFamilyTreeService
{
    /// <summary>
    /// Lists the value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree summaries.</returns>
    Task<Result<IReadOnlyList<FamilyTreeSummary>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="treeId">The tree identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    Task<Result<FamilyTreeDetail>> GetAsync(int treeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the value.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    Task<Result<FamilyTreeDetail>> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames the value.
    /// </summary>
    /// <param name="treeId">The tree identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    Task<Result<FamilyTreeDetail>> RenameAsync(int treeId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the value.
    /// </summary>
    /// <param name="treeId">The tree identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    Task<Result> DeleteAsync(int treeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the people and relationships of a tree with <paramref name="people"/> and remembers who the tree is centered on.
    /// </summary>
    Task<Result<FamilyTreeDetail>> SaveAsync(
        int treeId,
        IReadOnlyList<PersonData> people,
        string? focusPersonId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the sample.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the family tree detail.</returns>
    Task<Result<FamilyTreeDetail>> CreateSampleAsync(CancellationToken cancellationToken = default);
}
