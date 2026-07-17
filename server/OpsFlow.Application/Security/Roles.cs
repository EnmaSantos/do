namespace OpsFlow.Application.Security;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string SupportEngineer = "Support Engineer";
    public const string InventoryManager = "Inventory Manager";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Administrator, SupportEngineer, InventoryManager, Viewer];
    public const string InventoryWriters = Administrator + "," + InventoryManager;
    public const string SupportWriters = Administrator + "," + SupportEngineer;
    public const string KnowledgeWriters = Administrator + "," + SupportEngineer;
}
