# Database design

## Entity relationships

```mermaid
erDiagram
    DEPARTMENT ||--o{ ASSET : owns
    LOCATION ||--o{ ASSET : contains
    ASSET_CATEGORY ||--o{ ASSET : categorizes
    ASSET ||--o{ INVENTORY_TRANSACTION : records
    INCIDENT ||--o{ INCIDENT_COMMENT : contains
    INCIDENT ||--o{ INCIDENT_STATUS_HISTORY : tracks
    INCIDENT ||--o{ INCIDENT_ASSET : affects
    ASSET ||--o{ INCIDENT_ASSET : linked_to
    INCIDENT ||--o{ INCIDENT_SERVICE : affects
    SERVICE ||--o{ INCIDENT_SERVICE : linked_to
    INCIDENT o|--o{ KNOWLEDGE_ARTICLE : produces
    SERVICE o|--o{ KNOWLEDGE_ARTICLE : documents
    KNOWLEDGE_ARTICLE ||--o{ ARTICLE_FEEDBACK : receives
```

ASP.NET Core Identity contributes users, roles, user roles, claims, logins, and tokens.

## Integrity choices

- Asset tags, incident numbers, article numbers, service names, department names, location names, and category names are unique.
- Organization records use restricted deletes while operational records point to them.
- Incident-to-asset and incident-to-service links use composite primary keys.
- Incident and service references on knowledge articles become null rather than deleting the article.
- Operational entities carry created/updated metadata and an archive flag.
- Audit logs are append-only application records.

## Lifecycle behavior

Assets and incidents are archived rather than deleted. Inventory transactions, incident comments, and status history remain attached so a later investigation can reconstruct what happened.

## Migration

`InitialCreate` under `OpsFlow.Infrastructure/Persistence/Migrations` creates the full schema, including Identity tables and operational indexes. The API applies pending migrations before seeding demo data.
