using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using Snapvere.Application.Capture;

namespace Snapvere.App.Services;

/// <summary>
/// Lets the user choose the final destination of a completed PNG capture.
/// Cancelling the picker keeps the capture in the normal Pictures\SNAPVERE
/// folder so no capture is lost.
/// </summary>
public sealed class CaptureSaveLocationService
{
    private const string PickerSettingsIdentifier = "SNAPVERE-Capture-Save";

    private readonly CapturePathProvider _pathProvider;
    private readonly CaptureRelocationService _relocationService;

    public CaptureSaveLocationService(CapturePathProvider pathProvider)
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _relocationService = new CaptureRelocationService();
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

        return _relocationService.Relocate(capture, selection.Path);
    }
}
