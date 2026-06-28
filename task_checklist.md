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

## Phase 3 — Goals API 🔲 TODO

- [ ] `CreateGoalCommand` + Handler
- [ ] `UpdateGoalCommand` + Handler
- [ ] `GetAllGoalsQuery` + Handler
- [ ] `GetGoalByIdQuery` + Handler
- [ ] `AddMilestoneCommand` + Handler
- [ ] `ToggleMilestoneCommand` + Handler
- [ ] Progress calculation logic (`completed / total * 100`)
- [ ] `GoalsController` (wire up all endpoints)
- [ ] **Checkpoint:** Create goal → add milestones → complete some → progress = correct % ✅

---

## Phase 4 — Daily Log API 🔲 TODO

- [ ] `CreateOrUpdateLogCommand` + Handler (upsert by date)
- [ ] `GetLogByDateQuery` + Handler
- [ ] `GetLogsInRangeQuery` + Handler
- [ ] `DailyLogsController` (wire up all endpoints)
- [ ] **Checkpoint:** Write log → retrieve → update → retrieve again ✅

---

## Phase 5 — Dashboard API 🔲 TODO

- [ ] `GetDashboardDataQuery` + Handler
  - [ ] Today's habits with completion status
  - [ ] Top 3 streaks
  - [ ] Active goals with progress %
  - [ ] Today's log preview
- [ ] `DashboardController`
- [ ] **Checkpoint:** Hit `/api/dashboard` → get complete JSON with all data ✅

---

## Phase 6 — Polish & Hardening 🔲 TODO

- [ ] Add global exception handling middleware
- [ ] Add request validation pipeline (FluentValidation + MediatR pipeline behavior)
- [ ] Add `ProblemDetails` responses for consistent error format
- [ ] Add `.gitignore` (exclude `questlog.db`, `bin/`, `obj/`)
- [ ] Initialize git repo in `/backend`

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
