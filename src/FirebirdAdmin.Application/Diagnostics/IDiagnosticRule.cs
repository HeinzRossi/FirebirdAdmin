namespace FirebirdAdmin.Application.Diagnostics;

public interface IDiagnosticRule
{
    string RuleId { get; }
    IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions options);
}
