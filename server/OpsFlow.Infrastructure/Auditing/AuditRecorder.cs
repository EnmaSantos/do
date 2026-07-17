using System.Security.Claims;
using System.Text.Json;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Infrastructure.Auditing;

public sealed class AuditRecorder(OpsFlowDbContext db)
{
    public void Record(ClaimsPrincipal principal, string action, string entityType, string identifier,
        object? previous = null, object? current = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            User = principal.FindFirstValue(ClaimTypes.Name) ?? "System",
            Action = action,
            EntityType = entityType,
            EntityIdentifier = identifier,
            PreviousValue = previous is null ? null : JsonSerializer.Serialize(previous),
            NewValue = current is null ? null : JsonSerializer.Serialize(current)
        });
    }
}
