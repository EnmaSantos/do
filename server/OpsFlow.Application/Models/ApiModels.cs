using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserProfile User);
public sealed record UserProfile(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

public sealed record AssetRequest(string Name, string Category, string Manufacturer, string Model, string? SerialNumber,
    string Location, string Department, int Quantity, int ReorderThreshold, DateOnly? PurchaseDate,
    DateOnly? WarrantyExpiration, AssetStatus Status, string? AssignedUser, string? Notes);
public sealed record AssetTransactionRequest(InventoryTransactionType Type, int QuantityChange, string Reason,
    Guid? DestinationLocationId, string? AssignedUser);

public sealed record IncidentRequest(string Title, string Description, IncidentSeverity Severity,
    IncidentPriority Priority, string Reporter, string? AssignedEngineer, Guid? ServiceId, Guid? AssetId);
public sealed record IncidentUpdateRequest(string Title, string Description, IncidentSeverity Severity,
    IncidentPriority Priority, IncidentStatus Status);
public sealed record AssignIncidentRequest(string AssignedEngineer);
public sealed record IncidentCommentRequest(string Body, bool IsInternal = true);
public sealed record ResolveIncidentRequest(string ResolutionSummary);

public sealed record ServiceRequest(string Name, string Description, string BusinessOwner, string TechnicalOwner,
    EnvironmentType Environment, ServiceStatus Status, string? RepositoryUrl, string? DocumentationUrl,
    string SupportContact, string? Dependencies);

public sealed record ArticleRequest(string Title, string Summary, string ProblemSymptoms, string RootCause,
    string ResolutionSteps, string Author, string Tags, Guid? ServiceId, Guid? IncidentId);
public sealed record ArticleFeedbackRequest(bool WasHelpful, string? Comment);
