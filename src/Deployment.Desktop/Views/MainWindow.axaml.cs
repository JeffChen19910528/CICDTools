using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;

namespace Deployment.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly AppServices _services;
    private readonly List<NavItem> _navItems;

    public MainWindow() : this(null!) { }

    public MainWindow(AppServices services)
    {
        InitializeComponent();
        _services = services;

        _navItems =
        [
            new NavItem("menu.apps", () => new AppsView(_services, this)),
            new NavItem("menu.releases", () => new ReleasesView(_services, this)),
            new NavItem("menu.deploy", () => new DeployView(_services, this)),
            new NavItem("menu.backups", () => new BackupsView(_services, this)),
            new NavItem("menu.history", () => new HistoryView(_services, this)),
            new NavItem("menu.recovery", () => new RecoveryView(_services, this)),
            new NavItem("menu.language", () => new LanguageView(_services, this, RefreshLanguage)),
        ];

        RefreshLanguage();
    }

    /// <summary>Re-reads all nav labels and the current window title/content after a language switch.</summary>
    public void RefreshLanguage()
    {
        TitleText.Text = L.T("app.title");

        var selectedIndex = NavList.SelectedIndex;
        NavList.SelectionChanged -= OnNavSelectionChanged;
        NavList.ItemsSource = _navItems.Select(n => L.T(n.LabelKey)).ToList();
        NavList.SelectionChanged += OnNavSelectionChanged;

        NavList.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        ContentHost.Content = _navItems[NavList.SelectedIndex].Factory();
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedIndex < 0) return;
        ContentHost.Content = _navItems[NavList.SelectedIndex].Factory();
    }

    private sealed record NavItem(string LabelKey, Func<Control> Factory);
}
