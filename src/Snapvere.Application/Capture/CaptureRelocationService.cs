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
    public async Task<CaptureSaveResult> RelocateAsync(
        CaptureSaveResult capture,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

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
            await CopyToTemporaryFileAsync(
                sourcePath,
                temporaryPath,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
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

    private static async Task CopyToTemporaryFileAsync(
        string sourcePath,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
        await using var destination = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);

        await source.CopyToAsync(destination, 64 * 1024, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
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
