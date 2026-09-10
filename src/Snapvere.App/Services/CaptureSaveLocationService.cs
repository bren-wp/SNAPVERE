using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using Snapvere.Application.Capture;

namespace Snapvere.App.Services;

/// <summary>
/// Lets the user choose the final destination of a completed PNG capture.
/// The capture pipeline writes a complete file first; this service then moves
/// that known-good PNG to the user-selected path using a destination-local
/// temporary copy and atomic replace. Cancelling the picker keeps the capture
/// in the normal Pictures\SNAPVERE folder so no capture is lost.
/// </summary>
public sealed class CaptureSaveLocationService
{
    private const string PickerSettingsIdentifier = "SNAPVERE-Capture-Save";

    private readonly CapturePathProvider _pathProvider;

    public CaptureSaveLocationService(CapturePathProvider pathProvider)
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
    }

    public async Task<CaptureSaveResult> ChooseFinalLocationAsync(
        Window owner,
        CaptureSaveResult capture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(capture);
        cancellationToken.ThrowIfCancellationRequested();

        var sourcePath = Path.GetFullPath(capture.FilePath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The completed SNAPVERE capture could not be found.", sourcePath);
        }

        var defaultDirectory = _pathProvider.GetDefaultCaptureDirectory();
        Directory.CreateDirectory(defaultDirectory);

        var picker = new FileSavePicker(owner.AppWindow.Id)
        {
            Title = "SNAPVERE",
            SuggestedFolder = Path.GetDirectoryName(sourcePath) ?? defaultDirectory,
            SuggestedFileName = Path.GetFileNameWithoutExtension(sourcePath),
            DefaultFileExtension = ".png",
            ShowOverwritePrompt = true,
            SettingsIdentifier = PickerSettingsIdentifier
        };
        picker.FileTypeChoices.Add("PNG", new List<string> { ".png" });

        var selection = await picker.PickSaveFileAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (selection is null || string.IsNullOrWhiteSpace(selection.Path))
        {
            return capture;
        }

        var destinationPath = Path.GetFullPath(selection.Path);
        if (!string.Equals(Path.GetExtension(destinationPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SNAPVERE captures can only be saved as PNG files.");
        }

        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            return capture with { FilePath = destinationPath };
        }

        RelocateCompletedCapture(sourcePath, destinationPath);
        return capture with { FilePath = destinationPath };
    }

    private static void RelocateCompletedCapture(string sourcePath, string destinationPath)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("The selected capture destination has no parent directory.");
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.Copy(sourcePath, temporaryPath, overwrite: false);
            File.Move(temporaryPath, destinationPath, overwrite: true);

            try
            {
                File.Delete(sourcePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The requested destination is already safe and complete. Keep
                // the original as a recovery copy if Windows cannot remove it.
                StartupDiagnostics.Record("Remove original capture after Save As", exception);
            }
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }
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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StartupDiagnostics.Record("Clean Save As temporary file", exception);
        }
    }
}
