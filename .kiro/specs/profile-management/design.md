# Profile Management System Design Document

## Overview

The Profile Management System serves as the core foundation of ShadowFox antidetect browser, providing comprehensive profile lifecycle management with advanced fingerprinting capabilities. The system follows Clean Architecture principles with clear separation between domain logic, application services, and infrastructure concerns. It supports thousands of profiles with efficient storage, retrieval, and manipulation operations while maintaining high performance and security standards.

## Architecture

The system adopts a layered architecture pattern with the following components:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer (WPF)                 │
│  ProfileListView │ ProfileDetailView │ BulkOperationsView   │
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│     ProfileService │ FingerprintService │ GroupService      │
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│                     Domain Layer                            │
│   Profile │ Fingerprint │ Group │ ProfileValidator          │
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                        │
│  ProfileRepository │ EF Core │ SQLite │ Encryption          │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### Core Domain Models

**Profile Entity**
- Represents a complete browser identity with metadata
- Contains fingerprint data, organizational information, and usage statistics
- Supports validation rules for data integrity

**Fingerprint Value Object**
- Immutable representation of browser characteristics
- Encapsulates all spoofing parameters (UA, screen, hardware, etc.)
- Provides factory methods for different spoof levels

**Group Entity**
- Organizational container for related profiles
- Supports hierarchical grouping and bulk operations
- Maintains referential integrity with profiles

### Application Services

**IProfileService Interface**
```csharp
public interface IProfileService
{
    Task<Result<Profile>> CreateAsync(CreateProfileRequest request);
    Task<Result<Profile>> CloneAsync(int sourceId, string newName);
    Task<Result<Profile>> UpdateAsync(int id, UpdateProfileRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> BulkDeleteAsync(int[] ids);
    Task<Result<Profile[]>> GetAllAsync(ProfileFilter filter);
    Task<Result<Profile>> GetByIdAsync(int id);
    Task<Result> ExportAsync(int[] ids, string filePath);
    Task<Result<ImportResult>> ImportAsync(string filePath);
}
```

**IFingerprintService Interface**
```csharp
public interface IFingerprintService
{
    Fingerprint Generate(SpoofLevel level);
    Fingerprint Clone(Fingerprint source);
    ValidationResult Validate(Fingerprint fingerprint);
    string Serialize(Fingerprint fingerprint);
    Fingerprint Deserialize(string json);
}
```

**IGroupService Interface**
```csharp
public interface IGroupService
{
    Task<Result<Group>> CreateAsync(string name);
    Task<Result<Group[]>> GetAllAsync();
    Task<Result> DeleteAsync(int id);
    Task<Result> AssignProfilesAsync(int groupId, int[] profileIds);
}
```

### Repository Interfaces

**IProfileRepository Interface**
```csharp
public interface IProfileRepository
{
    Task<Profile> AddAsync(Profile profile);
    Task<Profile> UpdateAsync(Profile profile);
    Task DeleteAsync(int id);
    Task<Profile[]> GetAllAsync(ProfileFilter filter);
    Task<Profile> GetByIdAsync(int id);
    Task<bool> ExistsAsync(string name);
    Task BulkUpdateAsync(int[] ids, ProfileUpdate update);
}
```

## Data Models

### Profile Database Schema
```sql
CREATE TABLE Profiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name NVARCHAR(200) NOT NULL UNIQUE,
    Tags NVARCHAR(500),
    GroupId INTEGER,
    Notes NVARCHAR(1000),
    FingerprintJson NVARCHAR(4000) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    LastOpenedAt DATETIME,
    LastModifiedAt DATETIME NOT NULL,
    UsageCount INTEGER DEFAULT 0,
    IsActive BOOLEAN DEFAULT 1,
    FOREIGN KEY (GroupId) REFERENCES Groups(Id)
);

CREATE TABLE Groups (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name NVARCHAR(200) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    CreatedAt DATETIME NOT NULL,
    ProfileCount INTEGER DEFAULT 0
);

CREATE INDEX IX_Profiles_Name ON Profiles(Name);
CREATE INDEX IX_Profiles_Group ON Profiles(GroupId);
CREATE INDEX IX_Profiles_CreatedAt ON Profiles(CreatedAt);
CREATE INDEX IX_Profiles_LastOpened ON Profiles(LastOpenedAt);
```

### Fingerprint JSON Structure
```json
{
  "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...",
  "platform": "Win64",
  "hardwareConcurrency": 8,
  "deviceMemory": 16,
  "screenWidth": 1920,
  "screenHeight": 1080,
  "devicePixelRatio": 1.0,
  "timezone": "America/New_York",
  "locale": "en-US",
  "languages": ["en-US", "en"],
  "webGlUnmaskedVendor": "Intel Inc.",
  "webGlUnmaskedRenderer": "Intel(R) Iris(R) Xe Graphics",
  "canvasNoiseLevel": 0.05,
  "audioNoiseLevel": 0.001,
  "fontList": ["Arial", "Helvetica", "Times New Roman", ...],
  "spoofLevel": "Ultra"
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After analyzing all acceptance criteria, several properties can be consolidated to eliminate redundancy:

- Properties 1.1, 1.3, and 2.2 all test unique identifier generation and can be combined into a single comprehensive property
- Properties 2.1 and 2.4 both test fingerprint copying behavior and can be merged
- Properties 3.1 and 5.4 both test group existence validation and can be consolidated
- Properties 4.1 and 4.4 both test JSON serialization and can be combined
- Properties 6.1, 6.2, 6.3, and 6.4 all test validation rules and can be merged into a comprehensive validation property

### Core Properties

**Property 1: Profile creation generates unique identifiers**
*For any* profile creation request, the system should assign a unique identifier and valid timestamp that differs from all existing profiles
**Validates: Requirements 1.1, 1.3, 2.2**

**Property 2: Spoof level determines fingerprint characteristics**
*For any* profile creation with a specified SpoofLevel, the generated fingerprint should contain characteristics appropriate to that level (Basic/Advanced/Ultra)
**Validates: Requirements 1.2**

**Property 3: Profile names must be unique**
*For any* profile creation or update operation, the system should reject attempts to use names that already exist in the database
**Validates: Requirements 1.4**

**Property 4: Profile persistence is immediate**
*For any* successful profile creation, the profile data should be immediately queryable from the database
**Validates: Requirements 1.5**

**Property 5: Profile cloning preserves fingerprint data**
*For any* profile cloning operation, the cloned profile should contain identical fingerprint characteristics except for randomized noise values
**Validates: Requirements 2.1, 2.4**

**Property 6: Clone names are automatically modified**
*For any* profile cloning operation, the cloned profile name should have a suffix appended to ensure uniqueness
**Validates: Requirements 2.3**

**Property 7: Clone timestamps are updated**
*For any* profile cloning operation, the cloned profile should have a creation timestamp set to the current time
**Validates: Requirements 2.5**

**Property 8: Group assignment validates existence**
*For any* profile group assignment operation, the system should reject assignments to non-existent groups
**Validates: Requirements 3.1, 5.4**

**Property 9: Tags are stored as comma-separated strings**
*For any* profile with tags, the tags should be stored in the database as a comma-separated string format
**Validates: Requirements 3.2**

**Property 10: Filtering works across multiple fields**
*For any* profile filter operation, the results should include only profiles matching the specified group and tag criteria
**Validates: Requirements 3.3**

**Property 11: Group deletion updates profile references**
*For any* group deletion operation, all profiles previously assigned to that group should have their group reference removed
**Validates: Requirements 3.4**

**Property 12: Search matches multiple fields**
*For any* search query, the results should include profiles where the query matches the name, group, or tag fields
**Validates: Requirements 3.5**

**Property 13: Export serialization is complete**
*For any* profile export operation, the resulting JSON should contain all fingerprint properties and profile metadata
**Validates: Requirements 4.1, 4.4**

**Property 14: Import validation enforces schema**
*For any* profile import operation, invalid JSON structures should be rejected with appropriate error messages
**Validates: Requirements 4.2**

**Property 15: Import regenerates identifiers**
*For any* profile import operation, new unique identifiers should be assigned to prevent conflicts with existing profiles
**Validates: Requirements 4.5**

**Property 16: Bulk operations are atomic**
*For any* bulk update operation, either all selected profiles should be updated successfully or none should be modified
**Validates: Requirements 5.3, 5.5**

**Property 17: Profile validation enforces constraints**
*For any* profile editing operation, invalid fingerprint values should be rejected according to defined validation rules
**Validates: Requirements 6.1, 6.2, 6.3, 6.4**

**Property 18: Modifications update timestamps**
*For any* profile modification operation, the last modified timestamp should be updated to the current time
**Validates: Requirements 6.5**

**Property 19: Usage tracking records access**
*For any* profile browser launch operation, the last opened timestamp should be updated to the current time
**Validates: Requirements 7.1**

**Property 20: Usage calculations are accurate**
*For any* profile with recorded usage events, the calculated total usage time should equal the sum of all session durations
**Validates: Requirements 7.3**

**Property 21: Sorting respects specified criteria**
*For any* profile list with sorting applied, the results should be ordered according to the specified field (creation date, last opened, usage frequency)
**Validates: Requirements 7.4**

**Property 22: Sensitive data is encrypted**
*For any* profile stored in the database, sensitive fingerprint properties should be encrypted using AES-256
**Validates: Requirements 8.1**

**Property 23: Database queries are parameterized**
*For any* database operation, user input should be handled through parameterized queries to prevent SQL injection
**Validates: Requirements 8.2**

**Property 24: Export maintains encryption**
*For any* profile export operation, sensitive data should remain encrypted in the exported file
**Validates: Requirements 8.3**

**Property 25: Startup integrity checks pass**
*For any* application startup, database integrity verification should complete successfully before allowing profile operations
**Validates: Requirements 8.4**

**Property 26: Error logging protects sensitive data**
*For any* error condition, logged information should not contain unencrypted sensitive profile data
**Validates: Requirements 8.5**

## Error Handling

The system implements comprehensive error handling with the following strategies:

### Validation Errors
- **Input Validation**: All user inputs are validated at the service layer before processing
- **Business Rule Validation**: Domain-specific rules are enforced through validators
- **Data Integrity**: Foreign key constraints and unique constraints are validated

### Operational Errors
- **Database Connectivity**: Connection failures are handled with retry logic and graceful degradation
- **File System Operations**: Import/export operations include proper error handling and cleanup
- **Concurrency**: Optimistic concurrency control prevents data corruption in multi-user scenarios

### Security Errors
- **Authentication Failures**: Invalid access attempts are logged and blocked
- **Encryption Errors**: Cryptographic operations are wrapped with proper exception handling
- **SQL Injection Attempts**: Parameterized queries prevent injection attacks

### Error Response Strategy
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string ErrorMessage { get; }
    public ErrorCode ErrorCode { get; }
    
    public static Result<T> Success(T value) => new(true, value, null, ErrorCode.None);
    public static Result<T> Failure(string message, ErrorCode code) => new(false, default, message, code);
}
```

## Testing Strategy

The Profile Management System employs a dual testing approach combining unit tests and property-based tests to ensure comprehensive coverage and correctness verification.

### Unit Testing Approach
Unit tests focus on specific examples, edge cases, and integration points:
- **Service Layer Tests**: Verify business logic implementation with concrete examples
- **Repository Tests**: Validate data access patterns with known datasets
- **Validation Tests**: Check specific validation rules with boundary values
- **Error Handling Tests**: Verify exception scenarios and error responses

### Property-Based Testing Approach
Property-based tests verify universal properties across all valid inputs using **FsCheck** for C#:
- **Configuration**: Each property test runs a minimum of 100 iterations with random inputs
- **Generators**: Smart generators create realistic test data within valid domains
- **Properties**: Each correctness property is implemented as a single property-based test
- **Tagging**: Tests are tagged with format: **Feature: profile-management, Property {number}: {property_text}**

### Test Organization
- Property-based tests validate the 26 correctness properties defined above
- Unit tests complement property tests by covering specific scenarios and edge cases
- Integration tests verify end-to-end workflows with real database operations
- Performance tests ensure the system handles thousands of profiles efficiently

### Testing Requirements
- All new functionality must include both unit tests and property-based tests
- Property tests must reference their corresponding design document property
- Test coverage should exceed 90% for critical business logic
- Performance tests must validate sub-second response times for common operations