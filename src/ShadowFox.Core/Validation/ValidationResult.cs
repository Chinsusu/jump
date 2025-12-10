namespace ShadowFox.Core.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly List<string> _errors = new();

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Adds an error message to the validation result.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _errors.Add(error);
        }
    }

    /// <summary>
    /// Adds multiple error messages to the validation result.
    /// </summary>
    /// <param name="errors">The error messages to add.</param>
    public void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            _errors.Add(error);
        }
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result with no errors.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with the specified error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A validation result with the specified error.</returns>
    public static ValidationResult Failure(string error)
    {
        var result = new ValidationResult();
        result.AddError(error);
        return result;
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A validation result with the specified errors.</returns>
    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        var result = new ValidationResult();
        result.AddErrors(errors);
        return result;
    }
}