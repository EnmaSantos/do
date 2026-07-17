using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Security;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Identity;

namespace OpsFlow.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OpsFlowDbContext db, UserManager<OpsUser> users, RoleManager<IdentityRole<Guid>> roles)
    {
        foreach (var roleName in Roles.All)
            if (!await roles.RoleExistsAsync(roleName))
                await roles.CreateAsync(new IdentityRole<Guid>(roleName));

        await CreateUser(users, "admin@opsflow.demo", "Priya Shah", Roles.Administrator);
        await CreateUser(users, "support@opsflow.demo", "Maya Chen", Roles.SupportEngineer);
        await CreateUser(users, "inventory@opsflow.demo", "Jordan Lee", Roles.InventoryManager);
        await CreateUser(users, "viewer@opsflow.demo", "Alex Kim", Roles.Viewer);

        if (await db.Assets.AnyAsync()) return;

        var engineering = new Department { Name = "Engineering", Description = "Product and platform engineering" };
        var operations = new Department { Name = "Operations", Description = "Business operations" };
        var infrastructure = new Department { Name = "Infrastructure", Description = "IT infrastructure" };
        var sales = new Department { Name = "Sales", Description = "Regional sales" };
        var boise = new Location { Name = "Boise HQ", City = "Boise", State = "ID" };
        var idahoFalls = new Location { Name = "Idaho Falls", City = "Idaho Falls", State = "ID" };
        var twinFalls = new Location { Name = "Twin Falls", City = "Twin Falls", State = "ID" };
        var dataCenter = new Location { Name = "Boise Data Center", City = "Boise", State = "ID" };
        var laptop = new AssetCategory { Name = "Laptop" };
        var server = new AssetCategory { Name = "Server" };
        var network = new AssetCategory { Name = "Network" };
        var mobile = new AssetCategory { Name = "Mobile" };
        var monitor = new AssetCategory { Name = "Monitor" };

        db.AddRange(engineering, operations, infrastructure, sales, boise, idahoFalls, twinFalls, dataCenter,
            laptop, server, network, mobile, monitor);

        var employeePortal = new ServiceRecord
        {
            Name = "Employee Portal", Description = "Internal employee workspace and identity gateway",
            BusinessOwner = "People Operations", TechnicalOwner = "Workplace Systems",
            Environment = EnvironmentType.Production, Status = ServiceStatus.MajorIncident,
            SupportContact = "workplace-oncall@opsflow.demo", Dependencies = "Identity Provider, Notification Service"
        };
        var inventorySync = new ServiceRecord
        {
            Name = "Inventory Synchronization", Description = "Synchronizes stock changes across warehouses",
            BusinessOwner = "Supply Chain", TechnicalOwner = "Supply Chain Engineering",
            Environment = EnvironmentType.Production, Status = ServiceStatus.Degraded,
            SupportContact = "supplychain-oncall@opsflow.demo", Dependencies = "SQL Server, Service Bus"
        };
        var notification = new ServiceRecord
        {
            Name = "Notification Service", Description = "Transactional email and notification delivery",
            BusinessOwner = "Core Services", TechnicalOwner = "Platform Engineering",
            Environment = EnvironmentType.Production, Status = ServiceStatus.Degraded,
            SupportContact = "platform-oncall@opsflow.demo", Dependencies = "Email provider, Template service"
        };
        var ordering = new ServiceRecord
        {
            Name = "Customer Ordering API", Description = "Customer order placement and validation",
            BusinessOwner = "Commerce", TechnicalOwner = "Commerce Platform",
            Environment = EnvironmentType.Production, Status = ServiceStatus.Operational,
            SupportContact = "commerce-oncall@opsflow.demo", Dependencies = "Payment Processing, Inventory Synchronization"
        };
        var payment = new ServiceRecord
        {
            Name = "Payment Processing", Description = "Payment authorization and settlement",
            BusinessOwner = "Finance", TechnicalOwner = "Finance Technology",
            Environment = EnvironmentType.Production, Status = ServiceStatus.Operational,
            SupportContact = "payments-oncall@opsflow.demo", Dependencies = "Payment gateway"
        };
        var corporateNetwork = new ServiceRecord
        {
            Name = "Corporate Network", Description = "Office connectivity and secure access",
            BusinessOwner = "Information Technology", TechnicalOwner = "Infrastructure",
            Environment = EnvironmentType.Production, Status = ServiceStatus.Operational,
            SupportContact = "network-oncall@opsflow.demo", Dependencies = "ISP, VPN concentrator"
        };
        db.AddRange(employeePortal, inventorySync, notification, ordering, payment, corporateNetwork);

        var thinkSystem = new Asset
        {
            AssetTag = "AST-0981", Name = "ThinkSystem SR650", Manufacturer = "Lenovo", Model = "SR650",
            SerialNumber = "SR650-DEMO-981", Category = server, Location = dataCenter, Department = infrastructure,
            Quantity = 1, ReorderThreshold = 1, PurchaseDate = new DateOnly(2023, 7, 29),
            WarrantyExpiration = new DateOnly(2026, 7, 29), Status = AssetStatus.Maintenance,
            Notes = "Employee Portal application host"
        };
        db.AddRange(
            new Asset { AssetTag = "AST-1048", Name = "MacBook Pro 14-inch", Manufacturer = "Apple", Model = "M4 Pro", Category = laptop, Location = boise, Department = engineering, Quantity = 12, ReorderThreshold = 5, WarrantyExpiration = new DateOnly(2027, 11, 18), Status = AssetStatus.InStock },
            new Asset { AssetTag = "AST-1042", Name = "Dell Latitude 7440", Manufacturer = "Dell", Model = "7440", Category = laptop, Location = idahoFalls, Department = operations, Quantity = 3, ReorderThreshold = 5, WarrantyExpiration = new DateOnly(2026, 8, 2), Status = AssetStatus.InStock },
            thinkSystem,
            new Asset { AssetTag = "AST-0874", Name = "iPhone 15", Manufacturer = "Apple", Model = "iPhone 15", Category = mobile, Location = twinFalls, Department = sales, Quantity = 8, ReorderThreshold = 4, WarrantyExpiration = new DateOnly(2027, 1, 14), Status = AssetStatus.Assigned, AssignedUser = "Regional sales team" },
            new Asset { AssetTag = "AST-0733", Name = "Cisco Catalyst 9300", Manufacturer = "Cisco", Model = "C9300", Category = network, Location = boise, Department = infrastructure, Quantity = 2, ReorderThreshold = 2, WarrantyExpiration = new DateOnly(2026, 9, 4), Status = AssetStatus.Assigned, AssignedUser = "Network closet B" },
            new Asset { AssetTag = "AST-0521", Name = "HP EliteDisplay E243", Manufacturer = "HP", Model = "E243", Category = monitor, Location = idahoFalls, Department = operations, Quantity = 0, ReorderThreshold = 4, Status = AssetStatus.Retired, IsArchived = true });

        var incident = new Incident
        {
            IncidentNumber = "INC-2026-0147", Title = "Employee Portal authentication failures",
            Description = "Employees are receiving intermittent 401 responses after the identity certificate rotation.",
            Severity = IncidentSeverity.Sev1, Priority = IncidentPriority.Urgent, Status = IncidentStatus.Investigating,
            Reporter = "Priya Shah", AssignedEngineer = "Maya Chen", DateReported = DateTimeOffset.UtcNow.AddMinutes(-43),
            DueAt = DateTimeOffset.UtcNow.AddHours(3),
            IncidentServices = [new IncidentService { Service = employeePortal }],
            IncidentAssets = [new IncidentAsset { Asset = thinkSystem }],
            Comments =
            [
                new IncidentComment { Author = "Priya Shah", Body = "Incident declared after failed-login rate crossed 35%." },
                new IncidentComment { Author = "Maya Chen", Body = "Rollback staged; validating token signatures in canary." }
            ]
        };
        db.Incidents.Add(incident);
        db.Incidents.AddRange(
            new Incident { IncidentNumber = "INC-2026-0146", Title = "Inventory sync queue is delayed", Description = "Warehouse updates are 18 minutes behind real time.", Severity = IncidentSeverity.Sev2, Priority = IncidentPriority.High, Status = IncidentStatus.Monitoring, Reporter = "Jordan Lee", AssignedEngineer = "Noah Williams", DateReported = DateTimeOffset.UtcNow.AddHours(-2), IncidentServices = [new IncidentService { Service = inventorySync }] },
            new Incident { IncidentNumber = "INC-2026-0144", Title = "Payment receipt emails missing", Description = "Completed orders are not consistently generating receipt emails.", Severity = IncidentSeverity.Sev3, Priority = IncidentPriority.Normal, Status = IncidentStatus.InProgress, Reporter = "Finance Support", AssignedEngineer = "Elena Ruiz", DateReported = DateTimeOffset.UtcNow.AddHours(-5), IncidentServices = [new IncidentService { Service = notification }] },
            new Incident { IncidentNumber = "INC-2026-0141", Title = "Office Wi-Fi roaming instability", Description = "Third-floor users briefly disconnect between access points.", Severity = IncidentSeverity.Sev4, Priority = IncidentPriority.Low, Status = IncidentStatus.New, Reporter = "Alex Kim", DateReported = DateTimeOffset.UtcNow.AddDays(-1), IncidentServices = [new IncidentService { Service = corporateNetwork }] });

        db.KnowledgeArticles.AddRange(
            new KnowledgeArticle { ArticleNumber = "KB-0087", Title = "Recovering from stale identity certificates", Summary = "Identify certificate mismatch errors, roll back safely, and validate token signatures.", ProblemSymptoms = "Intermittent 401 responses after certificate rotation.", RootCause = "Application nodes retained the previous signing certificate.", ResolutionSteps = "Rollback the certificate, recycle affected nodes, then validate signatures in a canary.", Author = "Maya Chen", Tags = "identity,certificates,runbook", Status = ArticleStatus.Published, LastReviewedDate = new DateOnly(2026, 7, 11), Service = employeePortal },
            new KnowledgeArticle { ArticleNumber = "KB-0084", Title = "Clearing inventory consumer backlogs", Summary = "Safe scaling and validation steps for the synchronization queue.", ProblemSymptoms = "Warehouse updates lag behind real time.", RootCause = "Consumer throughput falls below message arrival rate.", ResolutionSteps = "Measure backlog, scale consumers, and monitor message age until clear.", Author = "Noah Williams", Tags = "queue,scaling", Status = ArticleStatus.Published, LastReviewedDate = new DateOnly(2026, 6, 28), Service = inventorySync });

        db.AuditLogs.AddRange(
            new AuditLog { User = "Maya Chen", Action = "Changed status to Investigating", EntityType = "Incident", EntityIdentifier = "INC-2026-0147" },
            new AuditLog { User = "Priya Shah", Action = "Created critical incident", EntityType = "Incident", EntityIdentifier = "INC-2026-0147" },
            new AuditLog { User = "Jordan Lee", Action = "Transferred asset to Idaho Falls", EntityType = "Asset", EntityIdentifier = "AST-1042" });

        await db.SaveChangesAsync();
    }

    private static async Task CreateUser(UserManager<OpsUser> manager, string email, string name, string role)
    {
        if (await manager.FindByEmailAsync(email) is not null) return;
        var user = new OpsUser { Id = Guid.NewGuid(), UserName = email, Email = email, EmailConfirmed = true, DisplayName = name };
        var result = await manager.CreateAsync(user, "Demo123!");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        await manager.AddToRoleAsync(user, role);
    }
}
