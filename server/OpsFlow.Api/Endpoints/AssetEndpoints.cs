using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Models;
using OpsFlow.Application.Rules;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Auditing;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Api.Endpoints;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assets").WithTags("Assets").RequireAuthorization();

        group.MapGet("/", async (OpsFlowDbContext db, string? search, string? category, string? location,
            string? department, AssetStatus? status, bool includeArchived = false, int page = 1, int pageSize = 20,
            string sort = "assetTag", CancellationToken cancellationToken = default) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = db.Assets.AsNoTracking()
                .Include(x => x.Category).Include(x => x.Location).Include(x => x.Department)
                .Where(x => includeArchived || !x.IsArchived);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.AssetTag.Contains(search) || x.Name.Contains(search) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(search)));
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category.Name == category);
            if (!string.IsNullOrWhiteSpace(location)) query = query.Where(x => x.Location.Name == location);
            if (!string.IsNullOrWhiteSpace(department)) query = query.Where(x => x.Department.Name == department);
            if (status is not null) query = query.Where(x => x.Status == status);
            query = sort.ToLowerInvariant() switch
            {
                "name" => query.OrderBy(x => x.Name),
                "quantity" => query.OrderBy(x => x.Quantity),
                "warranty" => query.OrderBy(x => x.WarrantyExpiration),
                "-created" => query.OrderByDescending(x => x.CreatedAt),
                _ => query.OrderBy(x => x.AssetTag)
            };
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.AssetTag, x.Name, Category = x.Category.Name, x.Manufacturer, x.Model, x.SerialNumber,
                    Location = x.Location.Name, Department = x.Department.Name, x.Quantity, x.ReorderThreshold,
                    x.PurchaseDate, x.WarrantyExpiration, x.Status, x.AssignedUser, x.Notes, x.IsArchived,
                    LowStock = x.Status != AssetStatus.Retired && x.Quantity <= x.ReorderThreshold
                }).ToListAsync(cancellationToken);
            return Results.Ok(new { Items = items, Page = page, PageSize = pageSize, TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
        });

        group.MapGet("/{id:guid}", async (Guid id, OpsFlowDbContext db, CancellationToken cancellationToken) =>
        {
            var asset = await db.Assets.AsNoTracking().Include(x => x.Category).Include(x => x.Location)
                .Include(x => x.Department).Include(x => x.Transactions.OrderByDescending(t => t.CreatedAt))
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            return asset is null ? Results.NotFound() : Results.Ok(asset);
        });

        group.MapPost("/", async (AssetRequest request, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            if (request.Quantity < 0 || request.ReorderThreshold < 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Quantities cannot be negative."] });
            var category = await db.AssetCategories.SingleOrDefaultAsync(x => x.Name == request.Category, cancellationToken);
            var location = await db.Locations.SingleOrDefaultAsync(x => x.Name == request.Location, cancellationToken);
            var department = await db.Departments.SingleOrDefaultAsync(x => x.Name == request.Department, cancellationToken);
            if (category is null || location is null || department is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["organization"] = ["Category, location, and department must exist."] });

            var nextNumber = (await db.Assets.CountAsync(cancellationToken)) + 1050;
            var asset = new Asset
            {
                AssetTag = $"AST-{nextNumber:0000}", Name = request.Name, Category = category,
                Manufacturer = request.Manufacturer, Model = request.Model, SerialNumber = request.SerialNumber,
                Location = location, Department = department, Quantity = request.Quantity,
                ReorderThreshold = request.ReorderThreshold, PurchaseDate = request.PurchaseDate,
                WarrantyExpiration = request.WarrantyExpiration, Status = request.Status,
                AssignedUser = request.AssignedUser, Notes = request.Notes
            };
            db.Assets.Add(asset);
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                Asset = asset, Type = InventoryTransactionType.CheckIn, QuantityChange = request.Quantity,
                QuantityAfter = request.Quantity, ToLocationId = location.Id, Reason = "Initial stock"
            });
            audit.Record(context.User, "Created asset", "Asset", asset.AssetTag, current: request);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/assets/{asset.Id}", new { asset.Id, asset.AssetTag });
        }).RequireAuthorization("InventoryWrite");

        group.MapPut("/{id:guid}", async (Guid id, AssetRequest request, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var asset = await db.Assets.Include(x => x.Category).Include(x => x.Location).Include(x => x.Department)
                .SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (asset is null) return Results.NotFound();
            var previous = new { asset.Name, asset.Quantity, asset.Status, asset.LocationId, asset.DepartmentId };
            var category = await db.AssetCategories.SingleOrDefaultAsync(x => x.Name == request.Category, cancellationToken);
            var location = await db.Locations.SingleOrDefaultAsync(x => x.Name == request.Location, cancellationToken);
            var department = await db.Departments.SingleOrDefaultAsync(x => x.Name == request.Department, cancellationToken);
            if (category is null || location is null || department is null) return Results.BadRequest();
            asset.Name = request.Name; asset.Category = category; asset.Manufacturer = request.Manufacturer;
            asset.Model = request.Model; asset.SerialNumber = request.SerialNumber; asset.Location = location;
            asset.Department = department; asset.ReorderThreshold = request.ReorderThreshold;
            asset.PurchaseDate = request.PurchaseDate; asset.WarrantyExpiration = request.WarrantyExpiration;
            asset.Status = request.Status; asset.AssignedUser = request.AssignedUser; asset.Notes = request.Notes;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            audit.Record(context.User, "Updated asset", "Asset", asset.AssetTag, previous, request);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(asset);
        }).RequireAuthorization("InventoryWrite");

        group.MapPost("/{id:guid}/transactions", async (Guid id, AssetTransactionRequest request,
            HttpContext context, OpsFlowDbContext db, AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["reason"] = ["A reason is required."] });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (asset is null) return Results.NotFound();
            var previousQuantity = asset.Quantity;
            var previousLocation = asset.LocationId;
            asset.Quantity = InventoryRules.ApplyTransaction(asset.Quantity, request.QuantityChange, request.Type);
            if (request.Type == InventoryTransactionType.Transfer && request.DestinationLocationId is not null)
                asset.LocationId = request.DestinationLocationId.Value;
            if (request.Type == InventoryTransactionType.CheckOut)
            {
                asset.Status = AssetStatus.Assigned;
                asset.AssignedUser = request.AssignedUser;
            }
            if (request.Type == InventoryTransactionType.CheckIn)
            {
                asset.Status = AssetStatus.InStock;
                asset.AssignedUser = null;
            }
            if (request.Type == InventoryTransactionType.Retirement) asset.Status = AssetStatus.Retired;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                AssetId = asset.Id, Type = request.Type, QuantityChange = asset.Quantity - previousQuantity,
                QuantityAfter = asset.Quantity, FromLocationId = previousLocation, ToLocationId = asset.LocationId,
                Reason = request.Reason, AssignedUser = request.AssignedUser
            });
            audit.Record(context.User, $"Recorded {request.Type}", "Asset", asset.AssetTag,
                new { Quantity = previousQuantity, LocationId = previousLocation },
                new { asset.Quantity, asset.LocationId });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.AssetTag, asset.Quantity, asset.Status, asset.LocationId });
        }).RequireAuthorization("InventoryWrite");

        group.MapPost("/{id:guid}/transfer", async (Guid id, AssetTransactionRequest request,
            HttpContext context, OpsFlowDbContext db, AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            if (request.DestinationLocationId is null || string.IsNullOrWhiteSpace(request.Reason))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["destination"] = ["Destination and reason are required."] });
            var asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (asset is null) return Results.NotFound();
            var previous = asset.LocationId;
            asset.LocationId = request.DestinationLocationId.Value;
            db.InventoryTransactions.Add(new InventoryTransaction { AssetId = id, Type = InventoryTransactionType.Transfer,
                QuantityChange = 0, QuantityAfter = asset.Quantity, FromLocationId = previous, ToLocationId = asset.LocationId,
                Reason = request.Reason });
            audit.Record(context.User, "Transferred asset", "Asset", asset.AssetTag, previous, asset.LocationId);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { asset.Id, asset.LocationId });
        }).RequireAuthorization("InventoryWrite");

        group.MapPost("/{id:guid}/archive", async (Guid id, HttpContext context, OpsFlowDbContext db,
            AuditRecorder audit, CancellationToken cancellationToken) =>
        {
            var asset = await db.Assets.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);
            if (asset is null) return Results.NotFound();
            asset.IsArchived = true; asset.Status = AssetStatus.Retired; asset.UpdatedAt = DateTimeOffset.UtcNow;
            audit.Record(context.User, "Archived asset", "Asset", asset.AssetTag);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("InventoryWrite");

        return app;
    }
}
