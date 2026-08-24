namespace FirebirdAdmin.Application.Diagnostics;

public interface IAlertCorrelator
{
    Alert Correlate(DiagnosticResult result, Alert? existing);
}
