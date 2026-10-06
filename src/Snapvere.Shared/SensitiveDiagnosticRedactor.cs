using System.Text.RegularExpressions;

namespace Snapvere.Shared;

/// <summary>
/// Removes common user-identifying paths, addresses and credential-like values
/// from local diagnostics before they are persisted.
/// </summary>
public static class SensitiveDiagnosticRedactor
{
    private const int MaximumInputCharacters = 64 * 1024;
    private const string RedactedDiagnostic = "[diagnostic-redacted]";
    private const string RedactedPath = "[path-redacted]";
    private const string RedactedUri = "[uri-redacted]";
    private const string RedactedEmail = "[email-redacted]";

    private static readonly Regex UriPattern = new(
        @"\b(?:https?|file)://[^\s<>'""]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex WindowsPathPattern = new(
        @"(?<![A-Za-z0-9])(?:[A-Za-z]:\\|\\\\)(?:(?!\s+\|\s+|\s+(?:from|for)\s+(?=(?:https?|file)://|[A-Z0-9._%+-]+@)|:line\s+\d+|[\r\n<>""']).)+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex EmailPattern = new(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex AuthorizationHeaderPattern = new(
        @"\bAuthorization\s*:\s*[^\r\n|]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex BearerPattern = new(
        @"\bBearer\s+[A-Z0-9._~+/=-]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SecretAssignmentPattern = new(
        @"\b(password|passwd|pwd|token|secret|api[-_]?key|access[-_]?key|client[-_]?secret|cookie|session)\b\s*[:=]\s*[^\r\n|;,]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var bounded = value.Length <= MaximumInputCharacters
            ? value
            : value[..MaximumInputCharacters] + Environment.NewLine + "[diagnostic truncated]";

        try
        {
            var redacted = WindowsPathPattern.Replace(bounded, RedactedPath);
            redacted = UriPattern.Replace(redacted, RedactedUri);
            redacted = EmailPattern.Replace(redacted, RedactedEmail);
            redacted = AuthorizationHeaderPattern.Replace(redacted, "Authorization: [redacted]");
            redacted = BearerPattern.Replace(redacted, "Bearer [redacted]");
            redacted = SecretAssignmentPattern.Replace(
                redacted,
                match => $"{match.Groups[1].Value}=[redacted]");
            return redacted;
        }
        catch (RegexMatchTimeoutException)
        {
            return RedactedDiagnostic;
        }
    }
}
