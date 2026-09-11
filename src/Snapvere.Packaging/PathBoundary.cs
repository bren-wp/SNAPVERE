namespace Snapvere.Packaging;

/// <summary>
/// Canonical path-boundary helpers shared by packaging and lifecycle code.
/// String-prefix checks alone are insufficient because the protected directory
/// itself must also count as inside the boundary and sibling prefixes must not.
/// </summary>
public static class PathBoundary
{
    public static bool IsSameOrDescendant(string candidatePath, string parentDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentDirectory);

        var candidate = Path.GetFullPath(candidatePath);
        var parent = Path.GetFullPath(parentDirectory);
        var relative = Path.GetRelativePath(parent, candidate);

        if (string.Equals(relative, ".", StringComparison.Ordinal))
        {
            return true;
        }

        if (Path.IsPathRooted(relative) ||
            string.Equals(relative, "..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}
