using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Application.Diagnostics.Rules;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class DiagnosticRulesTests
{
    [Fact]
    public void LongTransactionRule_ShouldReturnEvidence()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = new MonitoringSnapshot(
            Guid.NewGuid(),
            now,
            [],
            [new TransactionSnapshot(10, 1, "1", now.AddMinutes(-20), 1, 2, 3, 4)],
            []);

        var result = new LongTransactionRule().Evaluate(new DiagnosticContext(null, snapshot, Now: now), DiagnosticRuleOptions.Normal);

        result.Should().ContainSingle();
        result[0].Evidence.Should().Contain(evidence => evidence.Key == "AgeSeconds");
    }

    [Fact]
    public void Presets_ShouldChangeThresholds()
    {
        DiagnosticRuleOptions.Normal.EffectiveLongTransactionThreshold.Should().Be(TimeSpan.FromMinutes(15));
        new DiagnosticRuleOptions(DiagnosticPreset.Aggressive).EffectiveLongTransactionThreshold.Should().Be(TimeSpan.FromMinutes(5));
        new DiagnosticRuleOptions(DiagnosticPreset.Conservative).EffectiveLongTransactionThreshold.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void SlowStatementRule_ShouldReturnEvidence()
    {
        var profilerEvent = new ProfilerEvent(1, DateTimeOffset.UtcNow, TraceEventType.StatementFinished, TimeSpan.FromSeconds(5), "SYSDBA", 1, 2, "select 1", new ProfilerMetrics(), null, "raw");

        var result = new SlowStatementRule().Evaluate(new DiagnosticContext(null, ProfilerEvent: profilerEvent), DiagnosticRuleOptions.Normal);

        result.Should().ContainSingle();
        result[0].RuleId.Should().Be("TRACE_SLOW_STATEMENT");
    }

    [Fact]
    public void TraceTechnicalRule_ShouldCatchUnparsed()
    {
        var profilerEvent = new ProfilerEvent(1, DateTimeOffset.UtcNow, TraceEventType.Unparsed, null, null, null, null, null, new ProfilerMetrics(), null, "broken");

        var result = new TraceTechnicalErrorRule().Evaluate(new DiagnosticContext(null, ProfilerEvent: profilerEvent), DiagnosticRuleOptions.Normal);

        result.Should().ContainSingle();
    }

    [Fact]
    public void Correlator_ShouldDeduplicateAndReopenResolvedAlert()
    {
        var correlator = new AlertCorrelator();
        var result = CreateResult();

        var first = correlator.Correlate(result, null);
        var acknowledged = first with { Status = AlertStatus.Acknowledged };
        var second = correlator.Correlate(result with { ObservedAt = result.ObservedAt.AddSeconds(1) }, acknowledged);
        var third = correlator.Correlate(result with { ObservedAt = result.ObservedAt.AddSeconds(2) }, second with { Status = AlertStatus.Resolved });

        second.Status.Should().Be(AlertStatus.Acknowledged);
        second.Occurrences.Should().Be(2);
        third.Status.Should().Be(AlertStatus.Active);
    }

    private static DiagnosticResult CreateResult()
    {
        return new DiagnosticResult(
            "RULE",
            DiagnosticSeverity.Medium,
            "msg",
            new DiagnosticTarget("Transaction", "1"),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new DiagnosticEvidence("x", 1)]);
    }
}
