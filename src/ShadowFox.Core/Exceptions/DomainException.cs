using ShadowFox.Core.Common;

namespace ShadowFox.Core.Exceptions;

/// <summary>
/// Base class for all domain-specific exceptions.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message, ErrorCode errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected DomainException(string message, ErrorCode errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public ErrorCode ErrorCode { get; }
}

/// <summary>
/// Exception thrown when a requested profile is not found.
/// </summary>
public class ProfileNotFoundException : DomainException
{
    public ProfileNotFoundException(int profileId) 
        : base($"Profile with ID {profileId} was not found.", ErrorCode.NotFound)
    {
        ProfileId = profileId;
    }

    public ProfileNotFoundException(string profileName) 
        : base($"Profile with name '{profileName}' was not found.", ErrorCode.NotFound)
    {
        ProfileName = profileName;
    }

    /// <summary>
    /// Gets the ID of the profile that was not found, if applicable.
    /// </summary>
    public int? ProfileId { get; }

    /// <summary>
    /// Gets the name of the profile that was not found, if applicable.
    /// </summary>
    public string? ProfileName { get; }
}

/// <summary>
/// Exception thrown when attempting to create a profile with a name that already exists.
/// </summary>
public class DuplicateNameException : DomainException
{
    public DuplicateNameException(string name) 
        : base($"A profile with the name '{name}' already exists.", ErrorCode.DuplicateEntity)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the duplicate name that caused the exception.
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// Exception thrown when a requested group is not found.
/// </summary>
public class GroupNotFoundException : DomainException
{
    public GroupNotFoundException(int groupId) 
        : base($"Group with ID {groupId} was not found.", ErrorCode.NotFound)
    {
        GroupId = groupId;
    }

    public GroupNotFoundException(string groupName) 
        : base($"Group with name '{groupName}' was not found.", ErrorCode.NotFound)
    {
        GroupName = groupName;
    }

    /// <summary>
    /// Gets the ID of the group that was not found, if applicable.
    /// </summary>
    public int? GroupId { get; }

    /// <summary>
    /// Gets the name of the group that was not found, if applicable.
    /// </summary>
    public string? GroupName { get; }
}

/// <summary>
/// Exception thrown when profile validation fails.
/// </summary>
public class ProfileValidationException : DomainException
{
    public ProfileValidationException(string message) 
        : base(message, ErrorCode.ValidationFailed)
    {
    }

    public ProfileValidationException(string message, Exception innerException) 
        : base(message, ErrorCode.ValidationFailed, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when fingerprint validation fails.
/// </summary>
public class FingerprintValidationException : DomainException
{
    public FingerprintValidationException(string message) 
        : base(message, ErrorCode.ValidationFailed)
    {
    }

    public FingerprintValidationException(string message, Exception innerException) 
        : base(message, ErrorCode.ValidationFailed, innerException)
    {
    }
}