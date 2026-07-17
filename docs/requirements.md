# Requirements and acceptance criteria

## Authentication

- Seed Administrator, Support Engineer, Inventory Manager, and Viewer accounts.
- Issue signed JWTs after valid credentials.
- Reject unauthenticated requests and role-inappropriate writes.
- Allow self-registration only into the Viewer role.

## Inventory

- Search, filter, sort, and paginate active assets.
- Create and update validated asset records.
- Record check-in, check-out, transfer, adjustment, and retirement atomically.
- Prevent negative quantities.
- Flag quantity at or below its reorder threshold.
- Flag warranties inside a 30/60-day window.
- Archive operational records instead of deleting them.

## Incidents

- Create, assign, update, comment on, resolve, and filter incidents.
- Enforce allowed status transitions.
- Require a meaningful resolution summary.
- Link incidents to affected services and assets.
- Preserve a chronological response timeline and status history.
- Identify unresolved and overdue incidents.

## Service registry

- Track business/technical owners, environment, status, contacts, URLs, and dependencies.
- Show related open-incident counts.
- Restrict service administration to administrators.

## Knowledge

- Search/filter articles by text, service, tag, and publication state.
- Create drafts from scratch or a resolved incident.
- Require complete problem, cause, and resolution fields before publication.
- Record helpful/not-helpful feedback and review dates.

## Dashboard and audit

- Summarize open incidents, mean resolution time, asset status, stock/warranty alerts, recent resolutions/articles, and audit activity.
- Restrict the full audit endpoint to administrators.
- Record important writes with actor, action, entity, timestamp, and before/after context.

## Quality attributes

- One-command local stack through Docker Compose.
- Swagger/OpenAPI and SQL-aware health check.
- Structured logging and Problem Details errors with trace IDs.
- Automated frontend, domain, authentication, authorization, and workflow tests in CI.
- Responsive, keyboard-focusable UI with loading/empty/error-ready patterns.
