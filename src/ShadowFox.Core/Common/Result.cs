namespace ShadowFox.Core.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T>
{
    private Result(bool isSuccess, T? value, string? errorMessage, ErrorCode errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value returned by the operation if successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new(true, value, null, ErrorCode.None);

    /// <summary>
    /// Creates a failed result with the specified error message and code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The error code.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string message, ErrorCode code) => new(false, default, message, code);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string message) => new(false, default, message, ErrorCode.Unknown);
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail without returning a value.
/// </summary>
public class Result
{
    private Result(bool isSuccess, string? errorMessage, ErrorCode errorCode)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true, null, ErrorCode.None);

    /// <summary>
    /// Creates a failed result with the specified error message and code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The error code.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string message, ErrorCode code) => new(false, message, code);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string message) => new(false, message, ErrorCode.Unknown);
}