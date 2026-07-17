using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Models;
using OpsFlow.Application.Rules;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Auditing;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Api.Endpoints;

public static class IncidentEndpoints
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/incidents").WithTags("Incidents").RequireAuthorization();

        group.MapGet("/", async (OpsFlowDbContext db, string? search, IncidentStatus? status,
            IncidentSeverity? severity, Guid? serviceId, bool unresolvedOnly = false, bool overdueOnly = false,
            int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        {
            page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
            var query = db.Incidents.AsNoTracking().Where(x => !x.IsArchived);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.IncidentNumber.Contains(search) || x.Title.Contains(search));
            if (status is not null) query = query.Where(x => x.Status == status);
            if (severity is not null) query = query.Where(x => x.Severity == severity);
            if (serviceId is not null) query = query.Where(x => x.IncidentServices.Any(link => link.ServiceId == serviceId));
            if (unresolvedOnly) query = query.Where(x => x.Status != IncidentStatus.Resolved && x.Status != IncidentStatus.Closed);
            if (overdueOnly) query = query.Where(x => x.DueAt < DateTimeOffset.UtcNow && x.Status != IncidentStatus.Resolved && x.Status != IncidentStatus.Closed);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(x => x.Status == IncidentStatus.Resolved || x.Status == IncidentStatus.Closed)
                .ThenBy(x => x.Severity).ThenByDescending(x => x.DateReported)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.IncidentNumber, x.Title, x.Description, x.Severity, x.Priority, x.Status,
                    x.Reporter, x.AssignedEngineer, x.DateReported, x.ResolutionDate, x.DueAt,
                    Services = x.IncidentServices.Select(link => new { link.ServiceId, link.Service.Name }),
                    Assets = x.IncidentAssets.Select(link => new { link.AssetId, link.Asset.AssetTag, link.Asset.Name }) })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { Items = items, Page = page, PageSize = pageSize, TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
        });

        group.MapGet("/{id:guid}", async (Guid id, OpsFlowDbContext db, CancellationToken cancellationToken) =>
        {
            var incident = await db.Incidents.AsNoTracking()
                .Include(x => x.Comments.OrderBy(comment => comment.CreatedAt))
                .Include(x => x.StatusHistory.OrderBy(history => history.CreatedAt))
                .Include(x => x.IncidentAssets).ThenInclude(x => x.Asset)
                .Include(x => x.IncidentServices).ThenInclude(x => x.Service)
                .SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            return incident is null ? Results.NotFound() : Results.Ok(incident);
        });

        group.MapPost("/", async (IncidentRequest request, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["incident"] = ["Title and description are required."] });
            var nextNumber = (await db.Incidents.CountAsync(cancellationToken)) + 144;
            var incident = new Incident
            {
                IncidentNumber = $"INC-{DateTimeOffset.UtcNow.Year}-{nextNumber:0000}", Title = request.Title,
                Description = request.Description, Severity = request.Severity, Priority = request.Priority,
                Reporter = request.Reporter, AssignedEngineer = request.AssignedEngineer,
                DueAt = DateTimeOffset.UtcNow.Add(IncidentRules.TargetResolutionTime(request.Severity)),
                StatusHistory = [new IncidentStatusHistory { PreviousStatus = null, NewStatus = IncidentStatus.New,
                    ChangedBy = context.User.Identity?.Name ?? request.Reporter, Note = "Incident created" }]
            };
            if (request.ServiceId is not null) incident.IncidentServices.Add(new IncidentService { ServiceId = request.ServiceId.Value });
            if (request.AssetId is not null) incident.IncidentAssets.Add(new IncidentAsset { AssetId = request.AssetId.Value });
            db.Incidents.Add(incident);
            audit.Record(context.User, "Created incident", "Incident", incident.IncidentNumber, current: request);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/incidents/{incident.Id}", new { incident.Id, incident.IncidentNumber });
        }).RequireAuthorization("SupportWrite");

        group.MapPut("/{id:guid}", async (Guid id, IncidentUpdateRequest request, HttpContext context,
            OpsFlowDbContext db, AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (incident is null) return Results.NotFound();
            if (!IncidentRules.CanTransition(incident.Status, request.Status))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = [$"Cannot transition from {incident.Status} to {request.Status}."] });
            var previous = new { incident.Title, incident.Severity, incident.Priority, incident.Status };
            if (incident.Status != request.Status)
                db.IncidentStatusHistory.Add(new IncidentStatusHistory { IncidentId = id, PreviousStatus = incident.Status,
                    NewStatus = request.Status, ChangedBy = context.User.Identity?.Name ?? "Unknown" });
            incident.Title = request.Title; incident.Description = request.Description; incident.Severity = request.Severity;
            incident.Priority = request.Priority; incident.Status = request.Status; incident.UpdatedAt = DateTimeOffset.UtcNow;
            audit.Record(context.User, "Updated incident", "Incident", incident.IncidentNumber, previous, request);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(incident);
        }).RequireAuthorization("SupportWrite");

        group.MapPost("/{id:guid}/assign", async (Guid id, AssignIncidentRequest request, HttpContext context,
            OpsFlowDbContext db, AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (incident is null) return Results.NotFound();
            var previous = incident.AssignedEngineer; incident.AssignedEngineer = request.AssignedEngineer;
            incident.UpdatedAt = DateTimeOffset.UtcNow;
            audit.Record(context.User, "Assigned incident", "Incident", incident.IncidentNumber, previous, request.AssignedEngineer);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { incident.Id, incident.AssignedEngineer });
        }).RequireAuthorization("SupportWrite");

        group.MapPost("/{id:guid}/comments", async (Guid id, IncidentCommentRequest request, HttpContext context,
            OpsFlowDbContext db, AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Body)) return Results.BadRequest();
            var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (incident is null) return Results.NotFound();
            var comment = new IncidentComment { IncidentId = id, Author = context.User.Identity?.Name ?? "Unknown",
                Body = request.Body, IsInternal = request.IsInternal };
            db.IncidentComments.Add(comment);
            audit.Record(context.User, "Added incident update", "Incident", incident.IncidentNumber);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/incidents/{id}/comments/{comment.Id}", comment);
        }).RequireAuthorization("SupportWrite");

        group.MapPost("/{id:guid}/resolve", async (Guid id, ResolveIncidentRequest request, HttpContext context,
            OpsFlowDbContext db, AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            IncidentRules.ValidateResolution(request.ResolutionSummary);
            var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (incident is null) return Results.NotFound();
            if (!IncidentRules.CanTransition(incident.Status, IncidentStatus.Resolved))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["This incident cannot be resolved from its current status."] });
            var previousStatus = incident.Status;
            incident.Status = IncidentStatus.Resolved; incident.ResolutionDate = DateTimeOffset.UtcNow;
            incident.ResolutionSummary = request.ResolutionSummary; incident.UpdatedAt = DateTimeOffset.UtcNow;
            db.IncidentStatusHistory.Add(new IncidentStatusHistory { IncidentId = id, PreviousStatus = previousStatus,
                NewStatus = IncidentStatus.Resolved, ChangedBy = context.User.Identity?.Name ?? "Unknown", Note = request.ResolutionSummary });
            audit.Record(context.User, "Resolved incident", "Incident", incident.IncidentNumber, previousStatus, IncidentStatus.Resolved);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(incident);
        }).RequireAuthorization("SupportWrite");

        return app;
    }
}
