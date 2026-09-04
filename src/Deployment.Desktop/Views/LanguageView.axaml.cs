using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.CLI.Settings;
using Deployment.Desktop.Services;

namespace Deployment.Desktop.Views;

public partial class LanguageView : UserControl
{
    private readonly AppServices _services;
    private readonly Action _onChanged;

    public LanguageView() : this(null!, null!, () => { }) { }

    public LanguageView(AppServices services, Window owner, Action onChanged)
    {
        InitializeComponent();
        _services = services;
        _onChanged = onChanged;

        Header.Text = L.T("lang.menu.title");
        CurrentText.Text = L.T("lang.menu.current", L.Current.DisplayName());
    }

    private void OnEnglishClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Apply(Language.English);
    private void OnChineseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Apply(Language.TraditionalChinese);

    private void Apply(Language language)
    {
        L.Current = language;
        CliSettingsStore.SaveLanguage(_services.DataDir, language);
        _onChanged();
    }
}
