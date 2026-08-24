using FirebirdAdmin.Application.Diagnostics;

namespace FirebirdAdmin.Presentation.Wpf.Diagnostics;

public sealed class AlertRowViewModel(Alert alert)
{
    public Alert Alert { get; } = alert;
    public Guid Id => Alert.Id;
    public string Severity => Alert.Severity.ToString();
    public string Status => Alert.Status.ToString();
    public string RuleId => Alert.RuleId;
    public string Target => Alert.Target.DisplayName ?? $"{Alert.Target.Type}:{Alert.Target.Id}";
    public string Message => Alert.Message;
    public string LastSeen => Alert.LastSeen.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public int Occurrences => Alert.Occurrences;
    public string Evidence => string.Join(Environment.NewLine, Alert.Evidence.Select(evidence => $"{evidence.Key}: {evidence.Value} {evidence.Unit}".Trim()));
    public string Timeline => $"FirstSeen: {Alert.FirstSeen:O}{Environment.NewLine}LastSeen: {Alert.LastSeen:O}{Environment.NewLine}Occurrences: {Alert.Occurrences}";
}
