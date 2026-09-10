namespace Snapvere.Application.Capture;

/// <summary>
/// Moves a completed capture to a user-selected PNG destination without ever
/// exposing a partially copied destination file. The destination is populated
/// through a sibling temporary file and then atomically replaced. If Windows
/// cannot remove the original after the destination is complete, the original
/// is intentionally retained as a recovery copy.
/// </summary>
public sealed class CaptureRelocationService
{
    public CaptureSaveResult Relocate(CaptureSaveResult capture, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var sourcePath = Path.GetFullPath(capture.FilePath);
        var finalPath = Path.GetFullPath(destinationPath);

        if (!string.Equals(Path.GetExtension(finalPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("SNAPVERE capture destinations must use the .png extension.", nameof(destinationPath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The completed SNAPVERE capture could not be found.", sourcePath);
        }

        if (string.Equals(sourcePath, finalPath, StringComparison.OrdinalIgnoreCase))
        {
            return capture with { FilePath = finalPath };
        }

        var destinationDirectory = Path.GetDirectoryName(finalPath)
            ?? throw new ArgumentException("The capture destination must include a parent directory.", nameof(destinationPath));
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".snapvere-{Guid.NewGuid():N}.tmp");

        try
        {
            File.Copy(sourcePath, temporaryPath, overwrite: false);
            File.Move(temporaryPath, finalPath, overwrite: true);

            try
            {
                File.Delete(sourcePath);
            }
            catch (IOException)
            {
                // Destination is already complete. Keep the source as a safe
                // recovery copy rather than risking loss of the capture.
            }
            catch (UnauthorizedAccessException)
            {
                // Destination is already complete. Keep the source as a safe
                // recovery copy rather than risking loss of the capture.
            }
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }

        return capture with { FilePath = finalPath };
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
