using FsCheck;
using FsCheck.Xunit;
using ShadowFox.Infrastructure.Services;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class SecureErrorLoggingPropertyTests
{
    /// <summary>
    /// **Feature: profile-management, Property 26: Error logging protects sensitive data**
    /// **Validates: Requirements 8.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ErrorLoggingProtectsSensitiveData_FingerprintData()
    {
        return Prop.ForAll(
            GenerateSensitiveFingerprintData(),
            sensitiveData =>
            {
                var loggingService = new SecureLoggingService();
                
                // Log an error message containing sensitive fingerprint data
                loggingService.LogError($"Profile creation failed with fingerprint: {sensitiveData}");
                
                var logEntries = loggingService.GetLogEntries();
                var logEntry = logEntries.FirstOrDefault();
                
                if (logEntry == null)
                    return false.ToProperty();
                
                // Verify sensitive data is not present in the log
                var containsSensitiveUserAgent = (logEntry.Contains("Mozilla/5.0") || 
                                                logEntry.Contains("Chrome/")) && 
                                               !logEntry.Contains("[REDACTED]");
                
                var containsSensitiveWebGL = (logEntry.Contains("Intel") || 
                                            logEntry.Contains("Graphics")) && 
                                           !logEntry.Contains("[REDACTED]");
                
                var containsSensitiveFonts = (logEntry.Contains("Arial") || 
                                            logEntry.Contains("Helvetica")) && 
                                           !logEntry.Contains("[REDACTED]");
                
                // Log should not contain unredacted sensitive data
                var isSecure = !containsSensitiveUserAgent && !containsSensitiveWebGL && !containsSensitiveFonts;
                
                // Should contain redaction markers if sensitive data was present in input
                var inputHasSensitiveData = sensitiveData.Contains("Mozilla") || 
                                          sensitiveData.Contains("Chrome") ||
                                          sensitiveData.Contains("Intel") ||
                                          sensitiveData.Contains("Arial");
                
                var hasRedactionMarkers = logEntry.Contains("[REDACTED]") || 
                                        logEntry.Contains("[IP_REDACTED]");
                
                // If input had sensitive data, log should have redaction markers
                var redactionWorking = !inputHasSensitiveData || hasRedactionMarkers;
                
                return (isSecure && redactionWorking).ToProperty();
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 26: Error logging protects sensitive data**
    /// **Validates: Requirements 8.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ErrorLoggingProtectsSensitiveData_EncryptionKeys()
    {
        return Prop.ForAll(
            GenerateEncryptionKeyData(),
            keyData =>
            {
                var loggingService = new SecureLoggingService();
                
                // Log an error message containing encryption key data
                loggingService.LogError($"Encryption failed with configuration: {keyData}");
                
                var logEntries = loggingService.GetLogEntries();
                var logEntry = logEntries.FirstOrDefault();
                
                if (logEntry == null)
                    return false.ToProperty();
                
                // Verify encryption keys are not present in the log
                var containsActualKey = logEntry.Contains("AES256") || 
                                      logEntry.Contains("secretkey123") ||
                                      logEntry.Contains("password123");
                
                // Should contain redaction markers instead
                var hasRedactionMarkers = logEntry.Contains("[REDACTED]");
                
                return (!containsActualKey && hasRedactionMarkers).ToProperty();
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 26: Error logging protects sensitive data**
    /// **Validates: Requirements 8.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ErrorLoggingProtectsSensitiveData_PersonalInfo()
    {
        return Prop.ForAll(
            GeneratePersonalInfoData(),
            personalData =>
            {
                var loggingService = new SecureLoggingService();
                
                // Log an error message containing personal information
                loggingService.LogError($"User operation failed: {personalData}");
                
                var logEntries = loggingService.GetLogEntries();
                var logEntry = logEntries.FirstOrDefault();
                
                if (logEntry == null)
                    return false.ToProperty();
                
                // Verify personal information is not present in the log
                var containsEmail = logEntry.Contains("@example.com") || 
                                  logEntry.Contains("@test.com");
                
                var containsIpAddress = logEntry.Contains("192.168.1.1") || 
                                      logEntry.Contains("10.0.0.1");
                
                // Should contain redaction markers instead
                var hasEmailRedaction = logEntry.Contains("[EMAIL_REDACTED]");
                var hasIpRedaction = logEntry.Contains("[IP_REDACTED]");
                
                return (!containsEmail && !containsIpAddress && 
                       (hasEmailRedaction || hasIpRedaction)).ToProperty();
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 26: Error logging protects sensitive data**
    /// **Validates: Requirements 8.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ErrorLoggingPreservesNonSensitiveData()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && 
                                          !s.Contains("@") && 
                                          !s.Contains("Mozilla") &&
                                          !s.Contains("key") &&
                                          !s.Contains("password")),
            nonSensitiveData =>
            {
                var loggingService = new SecureLoggingService();
                
                // Log an error message with non-sensitive data
                loggingService.LogError($"Operation failed: {nonSensitiveData}");
                
                var logEntries = loggingService.GetLogEntries();
                var logEntry = logEntries.FirstOrDefault();
                
                if (logEntry == null)
                    return false.ToProperty();
                
                // Non-sensitive data should be preserved in the log
                var containsOriginalData = logEntry.Contains(nonSensitiveData);
                
                return containsOriginalData.ToProperty();
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 26: Error logging protects sensitive data**
    /// **Validates: Requirements 8.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ErrorLoggingHandlesExceptions()
    {
        try
        {
            var loggingService = new SecureLoggingService();
            
            // Create an exception with potentially sensitive data
            var sensitiveException = new InvalidOperationException(
                "Database connection failed with key=AES256SecretKey and user=admin@company.com");
            
            // Log the exception
            loggingService.LogError("Critical error occurred", sensitiveException);
            
            var logEntries = loggingService.GetLogEntries();
            var logEntry = logEntries.FirstOrDefault();
            
            if (logEntry == null)
                return false;
            
            // Verify sensitive data in exception is redacted
            var containsSensitiveKey = logEntry.Contains("AES256SecretKey");
            var containsSensitiveEmail = logEntry.Contains("admin@company.com");
            
            // Should contain redaction markers
            var hasRedactionMarkers = logEntry.Contains("[REDACTED]") || 
                                    logEntry.Contains("[EMAIL_REDACTED]");
            
            return !containsSensitiveKey && !containsSensitiveEmail && hasRedactionMarkers;
        }
        catch
        {
            return false;
        }
    }

    private static Arbitrary<string> GenerateSensitiveFingerprintData()
    {
        var fingerprintData = new[]
        {
            """{"userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36"}""",
            """{"webGlUnmaskedVendor": "Intel Inc.", "webGlUnmaskedRenderer": "Intel(R) Iris(R) Xe Graphics"}""",
            """{"fontList": ["Arial", "Helvetica", "Times New Roman", "Segoe UI"]}""",
            "userAgent: Mozilla/5.0 Chrome/120.0.0.0",
            "webGlUnmaskedVendor: Intel Inc.",
            "fontList: Arial,Helvetica,Times New Roman"
        };
        
        return Arb.From(Gen.Elements(fingerprintData));
    }

    private static Arbitrary<string> GenerateEncryptionKeyData()
    {
        var keyData = new[]
        {
            "key=AES256SecretKey123",
            "password=secretkey123",
            "token=jwt_secret_token_here",
            "secret: my_encryption_secret",
            "encryptionKey: super_secret_key_value"
        };
        
        return Arb.From(Gen.Elements(keyData));
    }

    private static Arbitrary<string> GeneratePersonalInfoData()
    {
        var personalData = new[]
        {
            "user@example.com failed to login",
            "IP address 192.168.1.1 blocked",
            "Contact admin@test.com for support",
            "Request from 10.0.0.1 denied",
            "Email notification sent to user@company.org"
        };
        
        return Arb.From(Gen.Elements(personalData));
    }
}