using Avalonia.Controls;
using Deployment.CLI;
using Deployment.CLI.Localization;
using Deployment.Domain.Entities;

namespace Deployment.Desktop.Views;

public partial class ReleaseDetailDialog : Window
{
    public ReleaseDetailDialog() : this(new Release()) { }

    public ReleaseDetailDialog(Release release)
    {
        InitializeComponent();
        Title = release.ReleaseId;
        Header.Text = release.ReleaseId;
        MetaText.Text = $"{L.T("release.list.colCreated")}: {release.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} ({release.CreatedBy})";
        NotesText.Text = release.Notes ?? string.Empty;
        NotesText.IsVisible = !string.IsNullOrWhiteSpace(release.Notes);
        FilesHeader.Text = $"{L.T("release.show.files")} ({release.Files.Count})";
        CloseButton.Content = L.T("common.ok");

        FilesGrid.Columns[0].Header = L.T("release.show.colPath");
        FilesGrid.Columns[1].Header = L.T("release.show.colSize");
        FilesGrid.Columns[2].Header = L.T("release.show.colSha");

        FilesGrid.ItemsSource = release.Files.OrderBy(f => f.RelativePath)
            .Select(f => new FileRow(f.RelativePath, ByteFormatter.Format(f.Size), f.Sha256))
            .ToList();
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private sealed record FileRow(string Path, string Size, string Sha);
}
