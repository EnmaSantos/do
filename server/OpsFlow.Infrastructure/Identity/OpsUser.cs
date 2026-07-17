using Microsoft.AspNetCore.Identity;

namespace OpsFlow.Infrastructure.Identity;

public sealed class OpsUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
