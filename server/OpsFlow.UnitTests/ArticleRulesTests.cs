using FluentAssertions;
using OpsFlow.Application.Rules;

namespace OpsFlow.UnitTests;

public sealed class ArticleRulesTests
{
    [Fact]
    public void ValidateForPublication_accepts_complete_article() =>
        FluentActions.Invoking(() => ArticleRules.ValidateForPublication("Title", "Summary", "Symptoms", "Root cause", "Steps"))
            .Should().NotThrow();

    [Theory]
    [InlineData("", "Summary", "Symptoms", "Cause", "Steps")]
    [InlineData("Title", "", "Symptoms", "Cause", "Steps")]
    [InlineData("Title", "Summary", "", "Cause", "Steps")]
    [InlineData("Title", "Summary", "Symptoms", "", "Steps")]
    [InlineData("Title", "Summary", "Symptoms", "Cause", "")]
    public void ValidateForPublication_rejects_missing_fields(string title, string summary, string symptoms, string cause, string steps) =>
        FluentActions.Invoking(() => ArticleRules.ValidateForPublication(title, summary, symptoms, cause, steps))
            .Should().Throw<InvalidOperationException>();

    [Theory]
    [InlineData(180, true)]
    [InlineData(181, true)]
    [InlineData(179, false)]
    [InlineData(1, false)]
    public void ReviewIsDue_uses_review_window(int daysAgo, bool expected)
    {
        var today = new DateOnly(2026, 7, 17);
        ArticleRules.ReviewIsDue(today.AddDays(-daysAgo), today).Should().Be(expected);
    }

    [Fact]
    public void ReviewIsDue_returns_true_when_never_reviewed() =>
        ArticleRules.ReviewIsDue(null, new DateOnly(2026, 7, 17)).Should().BeTrue();
}
