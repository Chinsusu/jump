using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShadowFox.Infrastructure.Services;

/// <summary>
/// Provides secure logging functionality that protects sensitive data from being exposed in logs.
/// </summary>
public interface ISecureLoggingService
{
    void LogError(string message, Exception? exception = null);
    void LogWarning(string message);
    void LogInformation(string message);
    string SanitizeMessage(string message);
}

/// <summary>
/// Implementation of secure logging that sanitizes sensitive data before logging.
/// </summary>
public class SecureLoggingService : ISecureLoggingService
{
    private static readonly Regex FingerprintJsonPattern = new(
        @"""(userAgent|webGlUnmaskedVendor|webGlUnmaskedRenderer|fontList)""\s*:\s*(?:""[^""]*""|(?:\[[^\]]*\]))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FingerprintSimplePattern = new(
        @"(userAgent|webGlUnmaskedVendor|webGlUnmaskedRenderer|fontList)\s*:\s*[^\r\n,]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EncryptionKeyPattern = new(
        @"(key|password|secret|token)\s*[:=]\s*[^\s,}]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EmailPattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled);

    private static readonly Regex IpAddressPattern = new(
        @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b",
        RegexOptions.Compiled);

    private readonly List<string> _logEntries = new();

    public void LogError(string message, Exception? exception = null)
    {
        var sanitizedMessage = SanitizeMessage(message);
        var sanitizedException = exception != null ? SanitizeMessage(exception.ToString()) : null;
        
        var logEntry = $"ERROR: {sanitizedMessage}";
        if (sanitizedException != null)
        {
            logEntry += $" Exception: {sanitizedException}";
        }
        
        _logEntries.Add(logEntry);
        
        // In a real implementation, this would write to actual logging infrastructure
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {logEntry}");
    }

    public void LogWarning(string message)
    {
        var sanitizedMessage = SanitizeMessage(message);
        var logEntry = $"WARNING: {sanitizedMessage}";
        
        _logEntries.Add(logEntry);
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {logEntry}");
    }

    public void LogInformation(string message)
    {
        var sanitizedMessage = SanitizeMessage(message);
        var logEntry = $"INFO: {sanitizedMessage}";
        
        _logEntries.Add(logEntry);
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {logEntry}");
    }

    public string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var sanitized = message;

        // Remove sensitive fingerprint data (JSON format)
        sanitized = FingerprintJsonPattern.Replace(sanitized, match =>
        {
            var property = match.Groups[1].Value;
            return $"\"{property}\": \"[REDACTED]\"";
        });

        // Remove sensitive fingerprint data (simple format)
        sanitized = FingerprintSimplePattern.Replace(sanitized, match =>
        {
            var property = match.Groups[1].Value;
            return $"{property}: [REDACTED]";
        });

        // Remove encryption keys and secrets
        sanitized = EncryptionKeyPattern.Replace(sanitized, match =>
        {
            var keyName = match.Value.Split(new[] { ':', '=' }, 2)[0];
            return $"{keyName}: [REDACTED]";
        });

        // Remove email addresses
        sanitized = EmailPattern.Replace(sanitized, "[EMAIL_REDACTED]");

        // Remove IP addresses
        sanitized = IpAddressPattern.Replace(sanitized, "[IP_REDACTED]");

        // Remove potential JSON with sensitive data
        if (IsJsonString(sanitized))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(sanitized);
                sanitized = SanitizeJsonElement(jsonDoc.RootElement).ToString();
            }
            catch
            {
                // If JSON parsing fails, treat as regular string
            }
        }

        return sanitized;
    }

    private static bool IsJsonString(string str)
    {
        str = str.Trim();
        return (str.StartsWith("{") && str.EndsWith("}")) || 
               (str.StartsWith("[") && str.EndsWith("]"));
    }

    private static JsonElement SanitizeJsonElement(JsonElement element)
    {
        // This is a simplified implementation
        // In a real scenario, you'd need to reconstruct the JSON with sanitized values
        return element;
    }

    // For testing purposes - get logged entries
    public IReadOnlyList<string> GetLogEntries() => _logEntries.AsReadOnly();

    public void ClearLogs() => _logEntries.Clear();
}