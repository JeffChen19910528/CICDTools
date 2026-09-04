using Avalonia.Controls;
using Deployment.CLI.Localization;

namespace Deployment.Desktop.Views;

public partial class MessageDialog : Window
{
    public MessageDialog() => InitializeComponent();

    public MessageDialog(string title, string message, bool isError) : this()
    {
        Title = title;
        MessageText.Text = message;
        if (isError) MessageText.Foreground = Avalonia.Media.Brushes.OrangeRed;
        OkButton.Content = L.T("common.ok");
    }

    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
