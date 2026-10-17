namespace MyWebTree.Services;

/// <summary>
/// Converts a failed result between value types while preserving why it failed.
/// </summary>
public static class ResultExtensions
{
    public static bool IsSuccessStatus(this IResult result) =>
        result.Status is ResultStatus.Ok or ResultStatus.Created or ResultStatus.NoContent;

    /// <summary>
    /// Re-shapes a failed <paramref name="result"/> as a <see cref="Result{TTo}"/>, carrying the
    /// status, error messages and validation errors across. Throws on a success, since there
    /// would be no value to carry.
    /// </summary>
    public static Result<TTo> To<TTo>(this IResult result)
    {
        if (result.IsSuccessStatus())
        {
            throw new InvalidOperationException(
                $"{nameof(To)} propagates failures; a successful result cannot be converted to " +
                $"Result<{typeof(TTo).Name}> without a projection.");
        }

        string[] errors = result.Errors?.ToArray() ?? [];

        return result.Status switch
        {
            ResultStatus.NotFound => Result<TTo>.NotFound(errors),
            ResultStatus.Forbidden => Result<TTo>.Forbidden(errors),
            ResultStatus.Unauthorized => Result<TTo>.Unauthorized(errors),
            ResultStatus.Invalid => Result<TTo>.Invalid([.. result.ValidationErrors ?? []]),
            ResultStatus.Conflict => Result<TTo>.Conflict(errors),
            ResultStatus.Unavailable => Result<TTo>.Unavailable(errors),
            ResultStatus.CriticalError => Result<TTo>.CriticalError(errors),
            _ => Result<TTo>.Error(new ErrorList(errors)),
        };
    }

    public static string GetDisplayMessage(this IResult result)
    {
        string? first = result.Errors?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        var validation = result.ValidationErrors?.FirstOrDefault();
        return validation is not null && !string.IsNullOrWhiteSpace(validation.ErrorMessage)
            ? validation.ErrorMessage
            : result.Status switch
            {
                ResultStatus.NotFound => "That no longer exists. It may have been deleted.",
                ResultStatus.Forbidden => "You don't have permission to do that.",
                ResultStatus.Unauthorized => "Please sign in to continue.",
                ResultStatus.Conflict => "That conflicts with something that already exists.",
                ResultStatus.Invalid => "Some of the values supplied were not valid.",
                ResultStatus.Unavailable => "The family tree is temporarily unavailable.",
                _ => "Something went wrong. Please try again.",
            };
    }
}
