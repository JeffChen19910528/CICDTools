using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Deployment.Desktop.Views;

namespace Deployment.Desktop.Services;

public static class DialogService
{
    /// <summary>Shows the shared confirm dialog. Mirrors InteractiveMenu's AnsiConsole.Confirm(prompt, defaultYes).</summary>
    public static Task<bool> ConfirmAsync(Window owner, string title, string message, bool defaultYes = false)
    {
        var dialog = new ConfirmDialog(title, message, defaultYes);
        return dialog.ShowDialog<bool>(owner);
    }

    /// <summary>Opens a native folder picker. Falls back to leaving the current text box value untouched if the user cancels.</summary>
    public static async Task<string?> PickFolderAsync(Window owner, string title, string? suggestedPath = null)
    {
        var storage = owner.StorageProvider;
        if (!storage.CanPickFolder) return null;

        IStorageFolder? startFolder = null;
        if (!string.IsNullOrWhiteSpace(suggestedPath) && Directory.Exists(suggestedPath))
        {
            startFolder = await storage.TryGetFolderFromPathAsync(suggestedPath);
        }

        var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = startFolder
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public static async Task ShowMessageAsync(Window owner, string title, string message, bool isError = false)
    {
        var dialog = new MessageDialog(title, message, isError);
        await dialog.ShowDialog(owner);
    }
}
