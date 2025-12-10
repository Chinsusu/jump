using System.Text.Json;
using ShadowFox.Core.Common;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Core.Validation;

namespace ShadowFox.Core.Services;

public class ProfileService : IProfileService
{
    private readonly IProfileRepository _profileRepository;
    private readonly FingerprintGenerator _fingerprintGenerator;

    public ProfileService(IProfileRepository profileRepository, FingerprintGenerator fingerprintGenerator)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _fingerprintGenerator = fingerprintGenerator ?? throw new ArgumentNullException(nameof(fingerprintGenerator));
    }

    public async Task<Result<Profile>> CreateAsync(CreateProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            return Result<Profile>.Failure("Create request cannot be null.", ErrorCode.InvalidData);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Profile>.Failure("Profile name is required.", ErrorCode.RequiredFieldMissing);

        // Check if name already exists
        var nameExists = await _profileRepository.ExistsAsync(request.Name, cancellationToken);
        if (nameExists)
            return Result<Profile>.Failure($"Profile with name '{request.Name}' already exists.", ErrorCode.DuplicateEntity);

        try
        {
            // Generate fingerprint
            var fingerprint = _fingerprintGenerator.Generate(request.SpoofLevel);
            var fingerprintJson = JsonSerializer.Serialize(fingerprint);

            // Create profile
            var profile = Profile.CreateNew(
                request.Name,
                fingerprintJson,
                request.GroupId,
                request.Tags,
                request.Notes);

            // Validate profile
            var validationResult = ProfileValidator.ValidateProfile(profile);
            if (!validationResult.IsValid)
                return Result<Profile>.Failure($"Profile validation failed: {string.Join(", ", validationResult.Errors)}", ErrorCode.ValidationFailed);

            // Save to repository
            var savedProfile = await _profileRepository.AddAsync(profile, cancellationToken);
            return Result<Profile>.Success(savedProfile);
        }
        catch (Exception ex)
        {
            return Result<Profile>.Failure($"Failed to create profile: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<Profile>> CloneAsync(int sourceId, string newName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result<Profile>.Failure("New profile name is required.", ErrorCode.RequiredFieldMissing);

        try
        {
            // Get source profile
            var sourceProfile = await _profileRepository.GetByIdAsync(sourceId, cancellationToken);
            if (sourceProfile == null)
                return Result<Profile>.Failure($"Source profile with ID {sourceId} not found.", ErrorCode.NotFound);

            // Generate unique name if needed
            var uniqueName = await GenerateUniqueNameAsync(newName, cancellationToken);

            // Deserialize source fingerprint and clone it
            var sourceFingerprint = JsonSerializer.Deserialize<Fingerprint>(sourceProfile.FingerprintJson);
            if (sourceFingerprint == null)
                return Result<Profile>.Failure("Failed to deserialize source profile fingerprint.", ErrorCode.InvalidFormat);

            var clonedFingerprint = _fingerprintGenerator.Clone(sourceFingerprint);
            var clonedFingerprintJson = JsonSerializer.Serialize(clonedFingerprint);

            // Create cloned profile
            var clonedProfile = Profile.CreateFromClone(sourceProfile, uniqueName, clonedFingerprintJson);

            // Validate cloned profile
            var validationResult = ProfileValidator.ValidateProfile(clonedProfile);
            if (!validationResult.IsValid)
                return Result<Profile>.Failure($"Cloned profile validation failed: {string.Join(", ", validationResult.Errors)}", ErrorCode.ValidationFailed);

            // Save to repository
            var savedProfile = await _profileRepository.AddAsync(clonedProfile, cancellationToken);
            return Result<Profile>.Success(savedProfile);
        }
        catch (Exception ex)
        {
            return Result<Profile>.Failure($"Failed to clone profile: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<Profile>> UpdateAsync(int id, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            return Result<Profile>.Failure("Update request cannot be null.", ErrorCode.InvalidData);

        try
        {
            // Get existing profile
            var existingProfile = await _profileRepository.GetByIdAsync(id, cancellationToken);
            if (existingProfile == null)
                return Result<Profile>.Failure($"Profile with ID {id} not found.", ErrorCode.NotFound);

            // Check name uniqueness if name is being changed
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != existingProfile.Name)
            {
                var nameExists = await _profileRepository.ExistsAsync(request.Name, cancellationToken);
                if (nameExists)
                    return Result<Profile>.Failure($"Profile with name '{request.Name}' already exists.", ErrorCode.DuplicateEntity);
                
                existingProfile.Name = request.Name;
            }

            // Update other properties if provided
            if (!string.IsNullOrWhiteSpace(request.FingerprintJson))
            {
                // Validate fingerprint JSON before updating
                var fingerprintValidation = ProfileValidator.ValidateFingerprintJson(request.FingerprintJson);
                if (!fingerprintValidation.IsValid)
                    return Result<Profile>.Failure($"Invalid fingerprint JSON: {string.Join(", ", fingerprintValidation.Errors)}", ErrorCode.ValidationFailed);
                
                existingProfile.FingerprintJson = request.FingerprintJson;
            }

            if (request.GroupId.HasValue)
                existingProfile.GroupId = request.GroupId.Value == 0 ? null : request.GroupId.Value;

            if (request.Tags != null)
                existingProfile.Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags;

            if (request.Notes != null)
                existingProfile.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes;

            if (request.IsActive.HasValue)
                existingProfile.IsActive = request.IsActive.Value;

            // Update modification timestamp
            existingProfile.UpdateModified();

            // Validate updated profile
            var validationResult = ProfileValidator.ValidateProfile(existingProfile);
            if (!validationResult.IsValid)
                return Result<Profile>.Failure($"Profile validation failed: {string.Join(", ", validationResult.Errors)}", ErrorCode.ValidationFailed);

            // Save changes
            await _profileRepository.UpdateAsync(existingProfile, cancellationToken);
            return Result<Profile>.Success(existingProfile);
        }
        catch (Exception ex)
        {
            return Result<Profile>.Failure($"Failed to update profile: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if profile exists
            var existingProfile = await _profileRepository.GetByIdAsync(id, cancellationToken);
            if (existingProfile == null)
                return Result.Failure($"Profile with ID {id} not found.", ErrorCode.NotFound);

            // Delete profile
            await _profileRepository.DeleteAsync(id, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete profile: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<Profile?>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
            return Result<Profile?>.Success(profile);
        }
        catch (Exception ex)
        {
            return Result<Profile?>.Failure($"Failed to get profile: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<List<Profile>>> GetAllAsync(ProfileFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = filter != null 
                ? await _profileRepository.GetAllAsync(filter, cancellationToken)
                : await _profileRepository.GetAllAsync(cancellationToken);
            
            return Result<List<Profile>>.Success(profiles);
        }
        catch (Exception ex)
        {
            return Result<List<Profile>>.Failure($"Failed to get profiles: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _profileRepository.ExistsAsync(name, cancellationToken);
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check profile existence: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<string>> ExportAsync(int[] profileIds, CancellationToken cancellationToken = default)
    {
        if (profileIds == null || profileIds.Length == 0)
            return Result<string>.Failure("Profile IDs cannot be null or empty.", ErrorCode.InvalidData);

        try
        {
            var profiles = new List<Profile>();
            
            // Get all requested profiles
            foreach (var id in profileIds)
            {
                var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            if (profiles.Count == 0)
                return Result<string>.Failure("No profiles found for the specified IDs.", ErrorCode.NotFound);

            // Create export data structure
            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                Version = "1.0",
                Profiles = profiles.Select(p => new
                {
                    p.Name,
                    p.FingerprintJson,
                    p.GroupId,
                    p.Tags,
                    p.Notes,
                    p.CreatedAt,
                    p.LastModifiedAt,
                    p.IsActive
                }).ToArray()
            };

            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(exportData, options);
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to export profiles: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<ImportResult>> ImportAsync(string jsonData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
            return Result<ImportResult>.Failure("JSON data cannot be null or empty.", ErrorCode.InvalidData);

        try
        {
            var result = new ImportResult();
            
            // Parse JSON
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(jsonData);
            }
            catch (JsonException ex)
            {
                return Result<ImportResult>.Failure($"Invalid JSON format: {ex.Message}", ErrorCode.InvalidFormat);
            }

            // Validate schema
            if (!document.RootElement.TryGetProperty("profiles", out var profilesElement) || 
                profilesElement.ValueKind != JsonValueKind.Array)
            {
                return Result<ImportResult>.Failure("Invalid import format: 'profiles' array not found.", ErrorCode.InvalidFormat);
            }

            // Process each profile
            foreach (var profileElement in profilesElement.EnumerateArray())
            {
                try
                {
                    // Validate required fields
                    if (!profileElement.TryGetProperty("name", out var nameElement) ||
                        !profileElement.TryGetProperty("fingerprintJson", out var fingerprintElement))
                    {
                        result.Errors.Add("Profile missing required fields (name or fingerprintJson)");
                        result.SkippedCount++;
                        continue;
                    }

                    var name = nameElement.GetString();
                    var fingerprintJson = fingerprintElement.GetString();

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fingerprintJson))
                    {
                        result.Errors.Add("Profile has empty name or fingerprint data");
                        result.SkippedCount++;
                        continue;
                    }

                    // Validate fingerprint JSON
                    try
                    {
                        var fingerprint = JsonSerializer.Deserialize<Fingerprint>(fingerprintJson, options);
                        if (fingerprint == null)
                        {
                            result.Errors.Add($"Invalid fingerprint data for profile '{name}'");
                            result.SkippedCount++;
                            continue;
                        }

                        var fingerprintValidation = fingerprint.Validate();
                        if (!fingerprintValidation.IsValid)
                        {
                            result.Errors.Add($"Fingerprint validation failed for profile '{name}': {string.Join(", ", fingerprintValidation.Errors)}");
                            result.SkippedCount++;
                            continue;
                        }
                    }
                    catch (JsonException)
                    {
                        result.Errors.Add($"Invalid fingerprint JSON format for profile '{name}'");
                        result.SkippedCount++;
                        continue;
                    }

                    // Generate unique name if needed
                    var uniqueName = await GenerateUniqueNameAsync(name, cancellationToken);

                    // Extract optional fields
                    int? groupId = null;
                    if (profileElement.TryGetProperty("groupId", out var groupIdElement) && 
                        groupIdElement.ValueKind == JsonValueKind.Number)
                    {
                        groupId = groupIdElement.GetInt32();
                        if (groupId == 0) groupId = null;
                    }

                    string? tags = null;
                    if (profileElement.TryGetProperty("tags", out var tagsElement) && 
                        tagsElement.ValueKind == JsonValueKind.String)
                    {
                        tags = tagsElement.GetString();
                    }

                    string? notes = null;
                    if (profileElement.TryGetProperty("notes", out var notesElement) && 
                        notesElement.ValueKind == JsonValueKind.String)
                    {
                        notes = notesElement.GetString();
                    }

                    // Create profile from import
                    var profile = Profile.CreateFromImport(uniqueName, fingerprintJson, groupId, tags, notes);

                    // Validate profile
                    var profileValidation = ProfileValidator.ValidateProfile(profile);
                    if (!profileValidation.IsValid)
                    {
                        result.Errors.Add($"Profile validation failed for '{name}': {string.Join(", ", profileValidation.Errors)}");
                        result.SkippedCount++;
                        continue;
                    }

                    // Save profile
                    var savedProfile = await _profileRepository.AddAsync(profile, cancellationToken);
                    result.ImportedProfiles.Add(savedProfile);
                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing profile: {ex.Message}");
                    result.SkippedCount++;
                }
            }

            return Result<ImportResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ImportResult>.Failure($"Failed to import profiles: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    private async Task<string> GenerateUniqueNameAsync(string baseName, CancellationToken cancellationToken)
    {
        var uniqueName = baseName;
        var counter = 1;

        while (await _profileRepository.ExistsAsync(uniqueName, cancellationToken))
        {
            uniqueName = $"{baseName} ({counter})";
            counter++;
        }

        return uniqueName;
    }

    public async Task<Result<BulkOperationResult>> BulkDeleteAsync(int[] profileIds, CancellationToken cancellationToken = default)
    {
        if (profileIds == null || profileIds.Length == 0)
            return Result<BulkOperationResult>.Failure("Profile IDs cannot be null or empty.", ErrorCode.InvalidData);

        try
        {
            var (success, processedCount, errors) = await _profileRepository.BulkDeleteAsync(profileIds, cancellationToken);
            
            var result = new BulkOperationResult
            {
                SuccessCount = success ? processedCount : 0,
                FailedCount = success ? 0 : profileIds.Length,
                Errors = errors,
                ProcessedIds = success ? profileIds.ToList() : new List<int>()
            };

            return success 
                ? Result<BulkOperationResult>.Success(result)
                : Result<BulkOperationResult>.Failure($"Bulk delete failed: {string.Join(", ", errors)}", ErrorCode.DatabaseError);
        }
        catch (Exception ex)
        {
            return Result<BulkOperationResult>.Failure($"Failed to perform bulk delete: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<BulkOperationResult>> BulkUpdateTagsAsync(int[] profileIds, string tags, CancellationToken cancellationToken = default)
    {
        if (profileIds == null || profileIds.Length == 0)
            return Result<BulkOperationResult>.Failure("Profile IDs cannot be null or empty.", ErrorCode.InvalidData);

        try
        {
            var (success, processedCount, errors) = await _profileRepository.BulkUpdateTagsAsync(profileIds, tags, cancellationToken);
            
            var result = new BulkOperationResult
            {
                SuccessCount = success ? processedCount : 0,
                FailedCount = success ? 0 : profileIds.Length,
                Errors = errors,
                ProcessedIds = success ? profileIds.ToList() : new List<int>()
            };

            return success 
                ? Result<BulkOperationResult>.Success(result)
                : Result<BulkOperationResult>.Failure($"Bulk tag update failed: {string.Join(", ", errors)}", ErrorCode.DatabaseError);
        }
        catch (Exception ex)
        {
            return Result<BulkOperationResult>.Failure($"Failed to perform bulk tag update: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<BulkOperationResult>> BulkAssignGroupAsync(int[] profileIds, int? groupId, CancellationToken cancellationToken = default)
    {
        if (profileIds == null || profileIds.Length == 0)
            return Result<BulkOperationResult>.Failure("Profile IDs cannot be null or empty.", ErrorCode.InvalidData);

        try
        {
            var (success, processedCount, errors) = await _profileRepository.BulkAssignGroupAsync(profileIds, groupId, cancellationToken);
            
            var result = new BulkOperationResult
            {
                SuccessCount = success ? processedCount : 0,
                FailedCount = success ? 0 : profileIds.Length,
                Errors = errors,
                ProcessedIds = success ? profileIds.ToList() : new List<int>()
            };

            return success 
                ? Result<BulkOperationResult>.Success(result)
                : Result<BulkOperationResult>.Failure($"Bulk group assignment failed: {string.Join(", ", errors)}", ErrorCode.DatabaseError);
        }
        catch (Exception ex)
        {
            return Result<BulkOperationResult>.Failure($"Failed to perform bulk group assignment: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result> RecordProfileAccessAsync(int profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the profile
            var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
            if (profile == null)
                return Result.Failure($"Profile with ID {profileId} not found.", ErrorCode.NotFound);

            // Update profile usage tracking
            profile.UpdateLastOpened();
            await _profileRepository.UpdateAsync(profile, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to record profile access: {ex.Message}", ErrorCode.DatabaseError);
        }
    }
}