using OpsFlow.Domain.Common;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;

public sealed class ServiceRecord : AuditableEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string BusinessOwner { get; set; }
    public required string TechnicalOwner { get; set; }
    public EnvironmentType Environment { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Operational;
    public string? RepositoryUrl { get; set; }
    public string? DocumentationUrl { get; set; }
    public required string SupportContact { get; set; }
    public string? Dependencies { get; set; }
    public ICollection<IncidentService> IncidentServices { get; set; } = [];
    public ICollection<KnowledgeArticle> KnowledgeArticles { get; set; } = [];
}

public sealed class Incident : AuditableEntity
{
    public required string IncidentNumber { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentPriority Priority { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.New;
    public required string Reporter { get; set; }
    public string? AssignedEngineer { get; set; }
    public DateTimeOffset DateReported { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolutionDate { get; set; }
    public string? ResolutionSummary { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public ICollection<IncidentComment> Comments { get; set; } = [];
    public ICollection<IncidentStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<IncidentAsset> IncidentAssets { get; set; } = [];
    public ICollection<IncidentService> IncidentServices { get; set; } = [];
    public ICollection<KnowledgeArticle> KnowledgeArticles { get; set; } = [];
}

public sealed class IncidentComment : AuditableEntity
{
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public required string Author { get; set; }
    public required string Body { get; set; }
    public bool IsInternal { get; set; } = true;
}

public sealed class IncidentStatusHistory : AuditableEntity
{
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public IncidentStatus? PreviousStatus { get; set; }
    public IncidentStatus NewStatus { get; set; }
    public required string ChangedBy { get; set; }
    public string? Note { get; set; }
}

public sealed class IncidentAsset
{
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
}

public sealed class IncidentService
{
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public ServiceRecord Service { get; set; } = null!;
}
