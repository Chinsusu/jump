namespace ShadowFox.Core.Common;

/// <summary>
/// Defines error codes for different types of failures in the system.
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// No error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// The requested entity was not found.
    /// </summary>
    NotFound = 100,

    /// <summary>
    /// A duplicate entity already exists.
    /// </summary>
    DuplicateEntity = 101,

    /// <summary>
    /// The provided data is invalid.
    /// </summary>
    InvalidData = 200,

    /// <summary>
    /// A validation rule was violated.
    /// </summary>
    ValidationFailed = 201,

    /// <summary>
    /// A required field is missing.
    /// </summary>
    RequiredFieldMissing = 202,

    /// <summary>
    /// A field value is out of the acceptable range.
    /// </summary>
    ValueOutOfRange = 203,

    /// <summary>
    /// The format of the provided data is invalid.
    /// </summary>
    InvalidFormat = 204,

    /// <summary>
    /// A database operation failed.
    /// </summary>
    DatabaseError = 300,

    /// <summary>
    /// A concurrency conflict occurred.
    /// </summary>
    ConcurrencyConflict = 301,

    /// <summary>
    /// A file system operation failed.
    /// </summary>
    FileSystemError = 400,

    /// <summary>
    /// Access to a resource was denied.
    /// </summary>
    AccessDenied = 500,

    /// <summary>
    /// An encryption or decryption operation failed.
    /// </summary>
    EncryptionError = 501
}