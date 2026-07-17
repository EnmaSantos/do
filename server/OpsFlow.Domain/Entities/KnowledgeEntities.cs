using OpsFlow.Domain.Common;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;

public sealed class KnowledgeArticle : AuditableEntity
{
    public required string ArticleNumber { get; set; }
    public required string Title { get; set; }
    public required string Summary { get; set; }
    public required string ProblemSymptoms { get; set; }
    public required string RootCause { get; set; }
    public required string ResolutionSteps { get; set; }
    public required string Author { get; set; }
    public string Tags { get; set; } = string.Empty;
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public DateOnly? LastReviewedDate { get; set; }
    public Guid? ServiceId { get; set; }
    public ServiceRecord? Service { get; set; }
    public Guid? IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public ICollection<ArticleFeedback> Feedback { get; set; } = [];
}

public sealed class ArticleFeedback : AuditableEntity
{
    public Guid ArticleId { get; set; }
    public KnowledgeArticle Article { get; set; } = null!;
    public bool WasHelpful { get; set; }
    public string? Comment { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string User { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public required string EntityIdentifier { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}
