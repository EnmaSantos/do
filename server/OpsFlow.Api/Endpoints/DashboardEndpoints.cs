using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Rules;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard/summary", async (OpsFlowDbContext db, CancellationToken cancellationToken) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var openIncidents = db.Incidents.AsNoTracking().Where(x => !x.IsArchived && x.Status != IncidentStatus.Resolved && x.Status != IncidentStatus.Closed);
            var resolved = await db.Incidents.AsNoTracking().Where(x => x.ResolutionDate != null).ToListAsync(cancellationToken);
            var assets = await db.Assets.AsNoTracking().Where(x => !x.IsArchived).ToListAsync(cancellationToken);
            var recentIncidents = await db.Incidents.AsNoTracking().Where(x => x.ResolutionDate != null)
                .OrderByDescending(x => x.ResolutionDate).Take(5).Select(x => new { x.Id, x.IncidentNumber, x.Title, x.ResolutionDate }).ToListAsync(cancellationToken);
            var recentArticles = await db.KnowledgeArticles.AsNoTracking().Where(x => x.Status == ArticleStatus.Published && !x.IsArchived)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(5).Select(x => new { x.Id, x.ArticleNumber, x.Title, x.LastReviewedDate }).ToListAsync(cancellationToken);
            var recentAudit = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(8).ToListAsync(cancellationToken);
            var averageResolutionMinutes = resolved.Count == 0 ? 0 : resolved.Average(x => (x.ResolutionDate!.Value - x.DateReported).TotalMinutes);

            return Results.Ok(new
            {
                OpenIncidents = await openIncidents.CountAsync(cancellationToken),
                OpenBySeverity = await openIncidents.GroupBy(x => x.Severity).Select(x => new { Severity = x.Key, Count = x.Count() }).ToListAsync(cancellationToken),
                OpenByStatus = await openIncidents.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync(cancellationToken),
                AverageTimeToResolutionMinutes = Math.Round(averageResolutionMinutes, 1),
                AssetsByStatus = assets.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Sum(asset => asset.Quantity) }),
                LowStock = assets.Where(x => InventoryRules.IsLowStock(x.Quantity, x.ReorderThreshold, x.Status))
                    .Select(x => new { x.Id, x.AssetTag, x.Name, x.Quantity, x.ReorderThreshold }),
                ExpiringWarranties = assets.Where(x => InventoryRules.WarrantyExpiresWithin(x.WarrantyExpiration, 60, today))
                    .OrderBy(x => x.WarrantyExpiration).Select(x => new { x.Id, x.AssetTag, x.Name, x.WarrantyExpiration }),
                RecentResolvedIncidents = recentIncidents,
                RecentPublishedArticles = recentArticles,
                RecentAuditActivity = recentAudit
            });
        }).WithTags("Dashboard").RequireAuthorization();

        return app;
    }
}
