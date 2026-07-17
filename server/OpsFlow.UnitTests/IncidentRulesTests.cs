using FluentAssertions;
using OpsFlow.Application.Rules;
using OpsFlow.Domain.Enums;

namespace OpsFlow.UnitTests;

public sealed class IncidentRulesTests
{
    [Theory]
    [InlineData(IncidentStatus.New, IncidentStatus.Investigating, true)]
    [InlineData(IncidentStatus.New, IncidentStatus.Resolved, false)]
    [InlineData(IncidentStatus.Investigating, IncidentStatus.Resolved, true)]
    [InlineData(IncidentStatus.InProgress, IncidentStatus.Monitoring, true)]
    [InlineData(IncidentStatus.Monitoring, IncidentStatus.New, false)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Closed, true)]
    [InlineData(IncidentStatus.Closed, IncidentStatus.New, false)]
    [InlineData(IncidentStatus.InProgress, IncidentStatus.InProgress, true)]
    public void CanTransition_enforces_workflow(IncidentStatus current, IncidentStatus next, bool expected) =>
        IncidentRules.CanTransition(current, next).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too short")]
    public void ValidateResolution_rejects_incomplete_summary(string? summary) =>
        FluentActions.Invoking(() => IncidentRules.ValidateResolution(summary))
            .Should().Throw<InvalidOperationException>().WithMessage("*at least 20 characters*");

    [Fact]
    public void ValidateResolution_accepts_actionable_summary() =>
        FluentActions.Invoking(() => IncidentRules.ValidateResolution("Rolled back the stale certificate and recycled all nodes."))
            .Should().NotThrow();

    [Theory]
    [InlineData(IncidentSeverity.Sev1, 4)]
    [InlineData(IncidentSeverity.Sev2, 8)]
    [InlineData(IncidentSeverity.Sev3, 48)]
    [InlineData(IncidentSeverity.Sev4, 120)]
    public void TargetResolutionTime_matches_severity(IncidentSeverity severity, int hours) =>
        IncidentRules.TargetResolutionTime(severity).Should().Be(TimeSpan.FromHours(hours));
}
