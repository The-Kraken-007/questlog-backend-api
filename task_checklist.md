# QuestLog — Backend Task Checklist

Track what's done, what's in progress, and what's next.

---

## Phase 1 — Foundation ✅ COMPLETE

- [x] Create solution (`QuestLog.slnx`)
- [x] Create 4 projects: Domain, Application, Infrastructure, API
- [x] Set up project references (clean architecture dependency chain)
- [x] Add NuGet packages (MediatR, EF Core, SQLite, FluentValidation, Swagger)
- [x] Define domain entities: `Habit`, `HabitEntry`, `Goal`, `Milestone`, `DailyLog`
- [x] Define `GoalStatus` enum
- [x] Create `AppDbContext` with entity configurations and indexes
- [x] Add `AssemblyMarker` for MediatR handler scanning
- [x] Configure `Program.cs` (EF Core, MediatR, Swagger, CORS)
- [x] Add connection string to `appsettings.json`
- [x] Create first migration: `InitialCreate`
- [x] Verify: API runs, Swagger opens, DB file created ✅

---

## Phase 2 — Habits API ✅ COMPLETE

- [x] `CreateHabitCommand` + Handler
- [x] `GetAllHabitsQuery` + Handler
- [x] `ToggleHabitEntryCommand` + Handler (upsert — flip IsCompleted)
- [x] `GetHabitEntriesQuery` + Handler (for calendar heatmap, with optional date range)
- [x] `UpdateHabitCommand` + Handler (partial update: name, emoji, sortOrder, isArchived)
- [x] Streak calculation logic (`StreakCalculator` — pure static function)
- [x] `IAppDbContext` interface in Application layer (clean architecture)
- [x] `ValidationPipelineBehavior` — MediatR pipeline runs FluentValidation before every handler
- [x] `GlobalExceptionMiddleware` — converts ValidationException→400, KeyNotFoundException→404, else→500
- [x] `CreateHabitCommandValidator`, `UpdateHabitCommandValidator`, `ToggleHabitEntryCommandValidator`
- [x] `HabitsController` — 5 endpoints wired
- [x] **Checkpoint:** 15/15 E2E tests passed ✅

---

## Phase 3 — Goals API ✅ COMPLETE

- [x] `CreateGoalCommand` + Handler
- [x] `UpdateGoalCommand` + Handler
- [x] `GetAllGoalsQuery` + Handler (includes milestones + progress %)
- [x] `GetGoalByIdQuery` + Handler
- [x] `AddMilestoneCommand` + Handler
- [x] `ToggleMilestoneCommand` + Handler (auto-completes goal when all milestones done)
- [x] Progress calculation logic (`completed / total * 100`)
- [x] `GoalsController` with all endpoints wired
- [x] **Checkpoint:** Create goal → add 4 milestones → complete 2 → progress = 50% ✅

---

## Phase 4 — Daily Log API ✅ COMPLETE

- [x] `CreateOrUpdateLogCommand` + Handler (upsert by date)
- [x] `GetLogByDateQuery` + Handler (returns 404 if no log for that date)
- [x] `GetLogsInRangeQuery` + Handler
- [x] `DailyLogsController` with all endpoints wired
- [x] **Checkpoint:** Write log → retrieve → update → retrieve again ✅

---

## Phase 5 — Dashboard API ✅ COMPLETE

- [x] `GetDashboardDataQuery` + Handler
  - [x] Today's habits with completion status per habit
  - [x] Top 3 active streaks
  - [x] All active goals with progress %
  - [x] Today's log content (or null if not written)
- [x] `DashboardController`
- [x] **Checkpoint:** `/api/dashboard` returns complete, correct JSON ✅

---

## Phase 5.5 — Hardening ✅ COMPLETE

- [x] Global exception middleware (catches unhandled errors, returns ProblemDetails)
- [x] MediatR validation pipeline behavior (FluentValidation runs before handlers)
- [x] Consistent error response format (RFC 7807 ProblemDetails)
- [x] Add `.gitignore` (exclude `questlog.db`, `bin/`, `obj/`)
- [x] Initialize git repo in `/backend`
- [x] `SQLitePCLRaw.lib.e_sqlite3` pinned to `3.50.3` (CVE fix)
- [x] **Checkpoint:** Invalid requests return 400. Crashes return 500. CVE mitigated. ✅

---

## Future Phases (Post-MVP)

- [ ] **Docker** — Dockerfile + docker-compose.yml
- [ ] **CI/CD** — GitHub Actions pipeline (build, test, push image)
- [ ] **Deploy** — VPS or Azure
- [ ] **Logging** — Serilog + Seq
- [ ] **Monitoring** — Health checks, Prometheus + Grafana
- [ ] **Caching** — Redis for dashboard/streak data
- [ ] **Background Jobs** — Hangfire for reminders
- [ ] **Auth** — JWT when going multi-user
