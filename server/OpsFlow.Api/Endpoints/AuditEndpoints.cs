using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit-logs", async (OpsFlowDbContext db, string? entityType, string? user,
            DateTimeOffset? from, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default) =>
        {
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
            var query = db.AuditLogs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
            if (!string.IsNullOrWhiteSpace(user)) query = query.Where(x => x.User.Contains(user));
            if (from is not null) query = query.Where(x => x.Timestamp >= from);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(x => x.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return Results.Ok(new { Items = items, Page = page, PageSize = pageSize, TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
        }).WithTags("Audit").RequireAuthorization("AdminOnly");

        return app;
    }
}
