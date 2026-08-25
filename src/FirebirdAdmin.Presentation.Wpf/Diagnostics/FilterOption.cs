namespace FirebirdAdmin.Presentation.Wpf.Diagnostics;

public sealed record FilterOption(string Label, string Value)
{
    public override string ToString() => Label;
}
