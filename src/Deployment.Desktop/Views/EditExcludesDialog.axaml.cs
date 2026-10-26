using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;

namespace Deployment.Desktop.Views;

public partial class EditExcludesDialog : Window
{
    public EditExcludesDialog() : this(null!) { }

    public EditExcludesDialog(AppServices services)
    {
        InitializeComponent();
        Header.Text = L.T("app.menu.editExcludes");
        Title = L.T("app.menu.editExcludes");
        ExcludeLabel.Text = L.T("app.target.excludePrompt");
        OkButton.Content = L.T("common.ok");
        CancelButton.Content = L.T("common.cancel");

        if (services is null) return;
        Selector.Configure(services, showRelease: false);
        Selector.SelectionChanged += () => ExcludeBox.Text = Selector.SelectedTarget?.BackupExcludes ?? string.Empty;
        Loaded += async (_, _) => await Selector.LoadAsync();
    }

    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Selector.SelectedTarget is null) return;
        Close((Selector.SelectedTarget, ExcludeBox.Text?.Trim() ?? string.Empty));
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);
}
