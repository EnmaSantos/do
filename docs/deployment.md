# Deployment

## Local full stack

```bash
cp .env.example .env
docker compose up --build
```

Compose starts SQL Server, waits for it to accept queries, starts the API, applies migrations and seed data, waits for `/health`, then starts the client. The bundled Microsoft SQL Server Linux image requires an x86-64 host; Microsoft does not support it under Apple Silicon emulation. ARM-based development should use a remote SQL Server instance and override `ConnectionStrings__OpsFlow`.

## Production checklist

1. Build immutable client and API images from a tagged commit.
2. Provision managed SQL Server with encryption, backups, private networking, and a least-privileged application login.
3. Store the connection string and JWT key in the platform secret manager.
4. Apply migrations as a controlled release step before new API replicas receive traffic.
5. Deploy the API and require `/health` to pass.
6. Deploy the client with the public API base URL.
7. Run a login and authenticated-read smoke test.
8. Confirm dashboards, request/error logs, and database alerts.
9. Keep the prior image tag for rollback; do not automatically roll back a non-backward-compatible migration.

## CI gates

Pull requests restore dependencies, lint/build/test the frontend, build/test all .NET projects, and build both container images. Main-branch deployment should add environment approvals, managed secrets, migration execution, rollout, and a post-deployment health check.

## Required secrets

- SQL Server connection string
- JWT signing key of at least 32 random characters
- Container registry credentials
- Deployment-provider credentials

Demo passwords are seed-only credentials and must be replaced or disabled outside a portfolio environment.
