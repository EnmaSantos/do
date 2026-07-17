using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Rules;

public static class InventoryRules
{
    public static bool IsLowStock(int quantity, int reorderThreshold, AssetStatus status) =>
        status != AssetStatus.Retired && quantity <= reorderThreshold;

    public static bool WarrantyExpiresWithin(DateOnly? expiration, int days, DateOnly today) =>
        expiration is not null && expiration >= today && expiration <= today.AddDays(days);

    public static int ApplyTransaction(int current, int requestedChange, InventoryTransactionType type)
    {
        var result = type switch
        {
            InventoryTransactionType.CheckIn => current + Math.Abs(requestedChange),
            InventoryTransactionType.CheckOut => current - Math.Abs(requestedChange),
            InventoryTransactionType.Retirement => current - Math.Abs(requestedChange),
            InventoryTransactionType.Adjustment => current + requestedChange,
            InventoryTransactionType.Transfer => current,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        return result >= 0 ? result : throw new InvalidOperationException("Inventory quantity cannot be negative.");
    }
}

public static class IncidentRules
{
    private static readonly IReadOnlyDictionary<IncidentStatus, IncidentStatus[]> AllowedTransitions =
        new Dictionary<IncidentStatus, IncidentStatus[]>
        {
            [IncidentStatus.New] = [IncidentStatus.Investigating, IncidentStatus.InProgress, IncidentStatus.Closed],
            [IncidentStatus.Investigating] = [IncidentStatus.InProgress, IncidentStatus.Monitoring, IncidentStatus.Resolved],
            [IncidentStatus.InProgress] = [IncidentStatus.Investigating, IncidentStatus.Monitoring, IncidentStatus.Resolved],
            [IncidentStatus.Monitoring] = [IncidentStatus.InProgress, IncidentStatus.Resolved],
            [IncidentStatus.Resolved] = [IncidentStatus.Monitoring, IncidentStatus.Closed],
            [IncidentStatus.Closed] = []
        };

    public static bool CanTransition(IncidentStatus current, IncidentStatus next) =>
        current == next || AllowedTransitions[current].Contains(next);

    public static void ValidateResolution(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary) || summary.Trim().Length < 20)
            throw new InvalidOperationException("A resolution summary of at least 20 characters is required.");
    }

    public static TimeSpan TargetResolutionTime(IncidentSeverity severity) => severity switch
    {
        IncidentSeverity.Sev1 => TimeSpan.FromHours(4),
        IncidentSeverity.Sev2 => TimeSpan.FromHours(8),
        IncidentSeverity.Sev3 => TimeSpan.FromDays(2),
        IncidentSeverity.Sev4 => TimeSpan.FromDays(5),
        _ => throw new ArgumentOutOfRangeException(nameof(severity))
    };
}

public static class ArticleRules
{
    public static void ValidateForPublication(string title, string summary, string symptoms, string rootCause, string steps)
    {
        var fields = new[] { title, summary, symptoms, rootCause, steps };
        if (fields.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Published articles require a title, summary, symptoms, root cause, and resolution steps.");
    }

    public static bool ReviewIsDue(DateOnly? lastReviewed, DateOnly today, int reminderDays = 180) =>
        lastReviewed is null || lastReviewed.Value.AddDays(reminderDays) <= today;
}
