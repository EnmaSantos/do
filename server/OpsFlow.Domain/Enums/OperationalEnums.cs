namespace OpsFlow.Domain.Enums;

public enum AssetStatus { InStock, Assigned, Maintenance, Retired }
public enum InventoryTransactionType { CheckIn, CheckOut, Transfer, Adjustment, Retirement }
public enum IncidentSeverity { Sev1, Sev2, Sev3, Sev4 }
public enum IncidentPriority { Urgent, High, Normal, Low }
public enum IncidentStatus { New, Investigating, InProgress, Monitoring, Resolved, Closed }
public enum ServiceStatus { Operational, Degraded, MajorIncident, Maintenance }
public enum EnvironmentType { Production, Staging, Development }
public enum ArticleStatus { Draft, Published }
