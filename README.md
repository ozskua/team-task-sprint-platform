# Team Task & Sprint Management Platform

A full-stack Kanban platform for teams. Administrators manage users, teams, tasks and sprints; each team owns a board, and members move tasks between workflow columns. Changes appear live on the board, assignees get notifications, and administrators can export sprint reports as Excel or PDF and send them to team webhooks.

## Features

- **Authentication and authorization:** JWT bearer tokens, BCrypt password hashing, `Admin` and `Member` roles, and invitation-only registration.
- **Teams and boards:** every team owns one Kanban board with configurable workflow columns.
- **Tasks and sprints:** task assignment, comments, completion and cancellation, sprint planning, and a board summary with sprint navigation.
- **Notifications:** task events are published to RabbitMQ and stored per recipient by a dedicated service.
- **Real-time updates:** a separate SignalR service pushes board changes and new notifications to connected clients.
- **Reporting:** an admin sprint report exported as Excel or PDF and delivered to per-area webhooks.

## Architecture

Three .NET services and an Angular SPA. The services share no assembly and talk to each other only through RabbitMQ events (contracts in [`docs/EVENT_CONTRACTS.md`](docs/EVENT_CONTRACTS.md)).

```
Angular SPA ──HTTP──► SprintFlow.Web.Server (REST API, SQL Server, Redis, MinIO)
     ▲                        │ publishes events
     │                        ▼
     │                  RabbitMQ
     │          ┌─────────────┴──────────────┐
     │          ▼                            ▼
     │  SprintFlow.Web.Notification   SprintFlow.Web.SignalR
     │  (stores notifications)         (pushes live updates)
     └────────────── WebSocket ──────────────┘
```

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 Web API (C#, EF Core 10) |
| Frontend | Angular 22 (NgModule architecture, Angular Material, TypeScript) |
| Database | Microsoft SQL Server |
| Messaging | RabbitMQ |
| Cache | Redis |
| Object storage | MinIO (S3-compatible) |
| Real-time | SignalR |
| Auth | JWT bearer tokens, BCrypt |

## Getting started

**Prerequisites:** .NET 10 SDK, Node.js with npm, SQL Server, Docker.

1. **Infrastructure.** Copy `.env.example` to `.env` and set `SPRINTFLOW_MINIO_ROOT_USER` and `SPRINTFLOW_MINIO_ROOT_PASSWORD`, then start Redis, RabbitMQ and MinIO:
   ```
   docker compose up -d
   ```
2. **Backend configuration.** Secrets are never committed. Set the JWT signing key with user secrets, and provide the SQL Server and Redis connection strings (`ConnectionStrings:DefaultConnection`, `ConnectionStrings:Redis`) through user secrets or a git-ignored `appsettings.Development.json`:
   ```
   dotnet user-secrets set "Jwt:Key" "<long random value>" --project SprintFlow.Web.Server
   ```
   The API does not use the MinIO root account. In the MinIO console (http://localhost:9001) create a service account limited to the `sprintflow` bucket and store its keys the same way: `Minio:Endpoint` (for example `localhost:9000`), `Minio:AccessKey` and `Minio:SecretKey`.
3. **Database.** Apply the EF Core migrations:
   ```
   dotnet ef database update --project SprintFlow.Web.Server
   ```
4. **Run the services.**
   ```
   dotnet run --project SprintFlow.Web.Server
   dotnet run --project SprintFlow.Web.Notification
   dotnet run --project SprintFlow.Web.SignalR
   ```
5. **Run the frontend.**
   ```
   cd sprintflow.web.client
   npm install
   npm start
   ```

## Documentation

| Document | Purpose |
|---|---|
| [`docs/AUTHENTICATION.md`](docs/AUTHENTICATION.md) | Authentication, roles, invitations, token handling |
| [`docs/DOMAIN_MODEL.md`](docs/DOMAIN_MODEL.md) | Domain model |
| [`docs/EVENT_CONTRACTS.md`](docs/EVENT_CONTRACTS.md) | RabbitMQ topology and event contracts |
