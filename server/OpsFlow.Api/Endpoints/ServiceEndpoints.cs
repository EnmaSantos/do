using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Models;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Auditing;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Api.Endpoints;

public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/services").WithTags("Services").RequireAuthorization();

        group.MapGet("/", async (OpsFlowDbContext db, string? search, CancellationToken cancellationToken) =>
        {
            var query = db.Services.AsNoTracking().Where(x => !x.IsArchived);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Name.Contains(search) || x.Description.Contains(search) || x.TechnicalOwner.Contains(search));
            return Results.Ok(await query.OrderBy(x => x.Name).Select(x => new
            {
                x.Id, x.Name, x.Description, x.BusinessOwner, x.TechnicalOwner, x.Environment, x.Status,
                x.RepositoryUrl, x.DocumentationUrl, x.SupportContact, x.Dependencies,
                OpenIncidents = x.IncidentServices.Count(link =>
                    link.Incident.Status != IncidentStatus.Resolved && link.Incident.Status != IncidentStatus.Closed)
            }).ToListAsync(cancellationToken));
        });

        group.MapGet("/{id:guid}", async (Guid id, OpsFlowDbContext db, CancellationToken cancellationToken) =>
        {
            var service = await db.Services.AsNoTracking().Include(x => x.IncidentServices).ThenInclude(x => x.Incident)
                .SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            return service is null ? Results.NotFound() : Results.Ok(service);
        });

        group.MapPost("/", async (ServiceRequest request, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            if (await db.Services.AnyAsync(x => x.Name == request.Name, cancellationToken))
                return Results.Conflict(new { message = "A service with this name already exists." });
            var service = Map(request);
            db.Services.Add(service);
            audit.Record(context.User, "Created service", "Service", service.Name, current: request);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/services/{service.Id}", service);
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (Guid id, ServiceRequest request, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var service = await db.Services.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (service is null) return Results.NotFound();
            var previous = new { service.Name, service.Status, service.TechnicalOwner };
            service.Name = request.Name; service.Description = request.Description;
            service.BusinessOwner = request.BusinessOwner; service.TechnicalOwner = request.TechnicalOwner;
            service.Environment = request.Environment; service.Status = request.Status;
            service.RepositoryUrl = request.RepositoryUrl; service.DocumentationUrl = request.DocumentationUrl;
            service.SupportContact = request.SupportContact; service.Dependencies = request.Dependencies;
            service.UpdatedAt = DateTimeOffset.UtcNow;
            audit.Record(context.User, "Updated service", "Service", service.Name, previous, request);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(service);
        }).RequireAuthorization("AdminOnly");

        return app;
    }

    private static ServiceRecord Map(ServiceRequest request) => new()
    {
        Name = request.Name, Description = request.Description, BusinessOwner = request.BusinessOwner,
        TechnicalOwner = request.TechnicalOwner, Environment = request.Environment, Status = request.Status,
        RepositoryUrl = request.RepositoryUrl, DocumentationUrl = request.DocumentationUrl,
        SupportContact = request.SupportContact, Dependencies = request.Dependencies
    };
}
