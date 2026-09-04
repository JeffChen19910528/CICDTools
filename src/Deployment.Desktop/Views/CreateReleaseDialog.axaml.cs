using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;

namespace Deployment.Desktop.Views;

public partial class CreateReleaseDialog : Window
{
    public CreateReleaseDialog()
    {
        InitializeComponent();
        Header.Text = L.T("release.menu.create");
        Title = L.T("release.menu.create");
        VersionLabel.Text = L.T("release.create.versionPrompt");
        SourceLabel.Text = L.T("release.create.sourcePrompt");
        NotesLabel.Text = L.T("release.create.notesPrompt");
        BrowseButton.Content = L.T("common.browse");
        OkButton.Content = L.T("common.ok");
        CancelButton.Content = L.T("common.cancel");
        Opened += (_, _) => VersionBox.Focus();
    }

    private async void OnBrowseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var path = await DialogService.PickFolderAsync(this, L.T("release.create.sourcePrompt"), SourceBox.Text);
        if (path != null) SourceBox.Text = path;
    }

    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(VersionBox.Text) || string.IsNullOrWhiteSpace(SourceBox.Text))
        {
            ErrorText.Text = L.T("release.create.versionPrompt");
            ErrorText.IsVisible = true;
            return;
        }

        if (!Directory.Exists(SourceBox.Text))
        {
            ErrorText.Text = L.T("release.create.sourceNotFound", SourceBox.Text);
            ErrorText.IsVisible = true;
            return;
        }

        Close((VersionBox.Text.Trim(), SourceBox.Text.Trim(), NotesBox.Text?.Trim() ?? string.Empty));
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);
}
