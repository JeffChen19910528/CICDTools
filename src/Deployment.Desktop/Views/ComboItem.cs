namespace Deployment.Desktop.Views;

/// <summary>Wraps an entity for display in a ComboBox — Avalonia's default item renderer calls ToString().</summary>
public sealed record ComboItem<T>(string Text, T Value)
{
    public override string ToString() => Text;
}
