using System.Globalization;

namespace Snapvere.Packaging;

public static class PortableCachePolicy
{
    public static bool IsOwnedVersionCacheDirectoryName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var separator = name.LastIndexOf('-');
        if (separator <= 0 || separator == name.Length - 1)
        {
            return false;
        }

        var architecture = name[(separator + 1)..];
        if (architecture is not ("x86" or "x64" or "arm64"))
        {
            return false;
        }

        var version = name[..separator].Split('.');
        if (version.Length != 3)
        {
            return false;
        }

        foreach (var component in version)
        {
            if (!int.TryParse(
                    component,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var value) ||
                value < 0)
            {
                return false;
            }
        }

        return true;
    }
}
