using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Identity;

namespace OpsFlow.Infrastructure.Persistence;

public sealed class OpsFlowDbContext(DbContextOptions<OpsFlowDbContext> options)
    : IdentityDbContext<OpsUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<ServiceRecord> Services => Set<ServiceRecord>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
    public DbSet<IncidentStatusHistory> IncidentStatusHistory => Set<IncidentStatusHistory>();
    public DbSet<IncidentAsset> IncidentAssets => Set<IncidentAsset>();
    public DbSet<IncidentService> IncidentServices => Set<IncidentService>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<ArticleFeedback> ArticleFeedback => Set<ArticleFeedback>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Asset>().HasIndex(x => x.AssetTag).IsUnique();
        builder.Entity<Incident>().HasIndex(x => x.IncidentNumber).IsUnique();
        builder.Entity<KnowledgeArticle>().HasIndex(x => x.ArticleNumber).IsUnique();
        builder.Entity<ServiceRecord>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<Department>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<Location>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<AssetCategory>().HasIndex(x => x.Name).IsUnique();

        builder.Entity<Asset>().Property(x => x.AssetTag).HasMaxLength(32);
        builder.Entity<Asset>().Property(x => x.Name).HasMaxLength(160);
        builder.Entity<Incident>().Property(x => x.IncidentNumber).HasMaxLength(32);
        builder.Entity<KnowledgeArticle>().Property(x => x.ArticleNumber).HasMaxLength(32);
        builder.Entity<AuditLog>().Property(x => x.EntityIdentifier).HasMaxLength(80);

        builder.Entity<Asset>()
            .HasOne(x => x.Category).WithMany(x => x.Assets).HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Asset>()
            .HasOne(x => x.Location).WithMany(x => x.Assets).HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Asset>()
            .HasOne(x => x.Department).WithMany(x => x.Assets).HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IncidentAsset>().HasKey(x => new { x.IncidentId, x.AssetId });
        builder.Entity<IncidentService>().HasKey(x => new { x.IncidentId, x.ServiceId });
        builder.Entity<IncidentAsset>().HasOne(x => x.Incident).WithMany(x => x.IncidentAssets).HasForeignKey(x => x.IncidentId);
        builder.Entity<IncidentAsset>().HasOne(x => x.Asset).WithMany(x => x.IncidentAssets).HasForeignKey(x => x.AssetId);
        builder.Entity<IncidentService>().HasOne(x => x.Incident).WithMany(x => x.IncidentServices).HasForeignKey(x => x.IncidentId);
        builder.Entity<IncidentService>().HasOne(x => x.Service).WithMany(x => x.IncidentServices).HasForeignKey(x => x.ServiceId);

        builder.Entity<KnowledgeArticle>()
            .HasOne(x => x.Incident).WithMany(x => x.KnowledgeArticles).HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<KnowledgeArticle>()
            .HasOne(x => x.Service).WithMany(x => x.KnowledgeArticles).HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(x => x.ClrType == typeof(string)))
                property.SetMaxLength(property.GetMaxLength() ?? 2000);
        }
    }
}
