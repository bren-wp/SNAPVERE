using System.Security;
using Snapvere.Application.Capture;

namespace Snapvere.UnitTests;

public sealed class CapturePersistenceFailurePolicyTests
{
    [Fact]
    public void TryClassify_DetectsDiskFullWin32Code()
    {
        var exception = new IOException(
            "disk full",
            unchecked((int)0x80070070));

        Assert.True(CapturePersistenceFailurePolicy.TryClassify(exception, out var kind));
        Assert.Equal(CapturePersistenceFailureKind.StorageFull, kind);
    }

    [Fact]
    public void TryClassify_DetectsHandleDiskFullWin32Code()
    {
        var exception = new IOException(
            "handle disk full",
            unchecked((int)0x80070027));

        Assert.True(CapturePersistenceFailurePolicy.TryClassify(exception, out var kind));
        Assert.Equal(CapturePersistenceFailureKind.StorageFull, kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryClassify_DetectsAccessDenied(bool securityException)
    {
        Exception exception = securityException
            ? new SecurityException("blocked by policy")
            : new UnauthorizedAccessException("access denied");

        Assert.True(CapturePersistenceFailurePolicy.TryClassify(exception, out var kind));
        Assert.Equal(CapturePersistenceFailureKind.AccessDenied, kind);
    }

    [Fact]
    public void TryClassify_MapsOtherIoFailureToWriteFailed()
    {
        Assert.True(
            CapturePersistenceFailurePolicy.TryClassify(
                new IOException("write failed"),
                out var kind));
        Assert.Equal(CapturePersistenceFailureKind.WriteFailed, kind);
    }

    [Fact]
    public void TryClassify_DoesNotConsumeUnexpectedProgrammingFailure()
    {
        Assert.False(
            CapturePersistenceFailurePolicy.TryClassify(
                new ArgumentOutOfRangeException("value"),
                out _));
    }

    [Fact]
    public void Wrap_PreservesTechnicalExceptionAsInnerException()
    {
        var inner = new UnauthorizedAccessException(@"C:\Users\private\Pictures");
        var wrapped = CapturePersistenceFailurePolicy.Wrap(inner);

        Assert.Equal(CapturePersistenceFailureKind.AccessDenied, wrapped.Kind);
        Assert.Same(inner, wrapped.InnerException);
        Assert.DoesNotContain("private", wrapped.Message, StringComparison.OrdinalIgnoreCase);
    }
}
