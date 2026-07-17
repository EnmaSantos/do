using OpsFlow.Domain.Common;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;

public sealed class Asset : AuditableEntity
{
    public required string AssetTag { get; set; }
    public required string Name { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int Quantity { get; set; }
    public int ReorderThreshold { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiration { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.InStock;
    public string? AssignedUser { get; set; }
    public string? Notes { get; set; }
    public Guid CategoryId { get; set; }
    public AssetCategory Category { get; set; } = null!;
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<InventoryTransaction> Transactions { get; set; } = [];
    public ICollection<IncidentAsset> IncidentAssets { get; set; } = [];
}

public sealed class InventoryTransaction : AuditableEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public InventoryTransactionType Type { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public required string Reason { get; set; }
    public string? AssignedUser { get; set; }
}
