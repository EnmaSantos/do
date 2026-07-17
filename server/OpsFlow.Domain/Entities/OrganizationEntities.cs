using OpsFlow.Domain.Common;

namespace OpsFlow.Domain.Entities;

public sealed class Department : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Asset> Assets { get; set; } = [];
}

public sealed class Location : AuditableEntity
{
    public required string Name { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public string? Address { get; set; }
    public ICollection<Asset> Assets { get; set; } = [];
}

public sealed class AssetCategory : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Asset> Assets { get; set; } = [];
}
