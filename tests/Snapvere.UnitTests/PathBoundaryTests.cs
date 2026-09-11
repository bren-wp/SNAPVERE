using Snapvere.Packaging;

namespace Snapvere.UnitTests;

public sealed class PathBoundaryTests
{
    [Fact]
    public void IsSameOrDescendant_AcceptsBoundaryItself()
    {
        var root = Path.Combine(Path.GetTempPath(), "snapvere-boundary");

        Assert.True(PathBoundary.IsSameOrDescendant(root, root));
    }

    [Fact]
    public void IsSameOrDescendant_AcceptsDescendant()
    {
        var root = Path.Combine(Path.GetTempPath(), "snapvere-boundary");
        var child = Path.Combine(root, "nested", "file.exe");

        Assert.True(PathBoundary.IsSameOrDescendant(child, root));
    }

    [Fact]
    public void IsSameOrDescendant_RejectsParentAndSiblingPrefix()
    {
        var parent = Path.Combine(Path.GetTempPath(), "snapvere-boundary");
        var root = Path.Combine(parent, "app");
        var siblingWithSamePrefix = Path.Combine(parent, "app-malicious", "file.exe");

        Assert.False(PathBoundary.IsSameOrDescendant(parent, root));
        Assert.False(PathBoundary.IsSameOrDescendant(siblingWithSamePrefix, root));
    }
}
