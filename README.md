# QuestLog — Backend

.NET 10 Web API following Clean Architecture with MediatR CQRS pattern.

---


## Architecture

```
QuestLog.Domain          ← Entities, Enums (no dependencies)
     ↑
QuestLog.Application     ← MediatR Commands/Queries, Interfaces, DTOs
     ↑
QuestLog.Infrastructure  ← EF Core, SQLite, Repository implementations
     ↑
QuestLog.API             ← Controllers, Program.cs, entry point
```

**Dependency Rule:** Outer layers depend on inner layers. Domain knows nothing about anyone.

---

## Project Layout

```
backend/
├── src/
│   ├── QuestLog.API/
│   │   ├── Controllers/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── QuestLog.Application/
│   │   ├── Habits/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Goals/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── DailyLogs/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Dashboard/
│   │   │   └── Queries/
│   │   └── Common/Interfaces/
│   ├── QuestLog.Domain/
│   │   ├── Entities/
│   │   │   ├── Habit.cs
│   │   │   ├── HabitEntry.cs
│   │   │   ├── Goal.cs
│   │   │   ├── Milestone.cs
│   │   │   └── DailyLog.cs
│   │   └── Enums/
│   │       └── GoalStatus.cs
│   └── QuestLog.Infrastructure/
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   └── Migrations/
│       └── Repositories/
└── QuestLog.slnx
```

---

## Running Locally

```bash
dotnet run --project src/QuestLog.API --urls "http://localhost:5000"
```

Swagger UI: **http://localhost:5000**

---

## Key Packages

| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 14.1.0 | CQRS command/query handling |
| EF Core | 10.0.9 | ORM |
| EF Core SQLite | 10.0.9 | SQLite provider |
| FluentValidation | 12.1.1 | Request validation |
| Swashbuckle | 10.2.3 | Swagger / OpenAPI |

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/habits` | Get all active habits |
| POST | `/api/habits` | Create a habit |
| PUT | `/api/habits/{id}` | Update a habit |
| POST | `/api/habits/{id}/toggle/{date}` | Toggle habit completion for a date |
| GET | `/api/habits/{id}/entries` | Get entries (for heatmap) |
| GET | `/api/goals` | Get all goals |
| POST | `/api/goals` | Create a goal |
| PUT | `/api/goals/{id}` | Update a goal |
| POST | `/api/goals/{id}/milestones` | Add milestone to goal |
| PUT | `/api/milestones/{id}/toggle` | Toggle milestone completion |
| GET | `/api/logs/{date}` | Get log for a date (yyyy-MM-dd) |
| POST | `/api/logs` | Create or update today's log |
| GET | `/api/logs?from={date}&to={date}` | Get logs in a date range |
| GET | `/api/dashboard` | Aggregated dashboard data |
