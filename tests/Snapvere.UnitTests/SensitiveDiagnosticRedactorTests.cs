using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class SensitiveDiagnosticRedactorTests
{
    [Fact]
    public void Redact_RemovesPathsUrisEmailsAndSecrets()
    {
        var input =
            "Failed at C:\\Users\\private-user\\Secret Project\\capture.png " +
            "from https://example.test/private?id=7 " +
            "for private@example.test token=abc123 Authorization: Bearer header.secret.value";

        var actual = SensitiveDiagnosticRedactor.Redact(input);

        Assert.DoesNotContain("private-user", actual, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret Project", actual, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("example.test/private", actual, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private@example.test", actual, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("abc123", actual, StringComparison.Ordinal);
        Assert.DoesNotContain("header.secret.value", actual, StringComparison.Ordinal);
        Assert.Contains("[path-redacted]", actual, StringComparison.Ordinal);
        Assert.Contains("[uri-redacted]", actual, StringComparison.Ordinal);
        Assert.Contains("[email-redacted]", actual, StringComparison.Ordinal);
        Assert.Contains("token=[redacted]", actual, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Bearer [redacted]", actual, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Redact_RemovesUncPathButPreservesDiagnosticShape()
    {
        var input = "IOException while reading \\\\server\\private-share\\user\\capture.png";

        var actual = SensitiveDiagnosticRedactor.Redact(input);

        Assert.StartsWith("IOException while reading ", actual, StringComparison.Ordinal);
        Assert.Contains("[path-redacted]", actual, StringComparison.Ordinal);
        Assert.DoesNotContain("private-share", actual, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Redact_TruncatesOversizedDiagnosticBeforePersistence()
    {
        var input = new string('x', 70 * 1024);

        var actual = SensitiveDiagnosticRedactor.Redact(input);

        Assert.True(actual.Length < input.Length);
        Assert.EndsWith("[diagnostic truncated]", actual, StringComparison.Ordinal);
    }
}
