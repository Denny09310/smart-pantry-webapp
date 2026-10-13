# AGENTS.md — Coding Agent Instructions

These instructions apply across the Smart Pantry repository.

## 1. Understand the repository first

- Inspect the relevant project files, nearby implementation, tests, package references, and existing conventions before editing.
- Prefer focused inspection over broad repository dumps.
- **Do not scan NuGet package source code, global package caches, or decompiled dependency internals** to understand a library. Use official documentation, package READMEs/release notes, public API metadata, and small targeted examples instead.
- Verify the repository's actual .NET version, database provider, authentication flow, package APIs, and hosting setup. Do not guess.

## 2. Use tools and documentation

- **Use MCP tools when available and relevant** for repository context, connected documentation, search, or supported actions. Prefer them when they provide a reliable source of truth.
- Search online documentation when it can resolve framework, package, browser, security, or deployment questions. Prefer official project/vendor documentation matching the versions in this repository.
- For APIs or behavior that can change between releases, verify current docs and check release notes or migration guides when upgrading.
- Keep research narrow: investigate the specific API or issue, not an entire dependency's source tree.
- If a relevant MCP tool is unavailable, use the repository and official documentation. Never claim a tool was used when it was not.

## 3. Preserve the existing architecture

- `Server`: ASP.NET Core Minimal APIs, application behavior, EF Core persistence, and background work.
- `Client`: Blazor WebAssembly Standalone UI.
- `Shared`: transport contracts shared between client and server.
- Use `AppDbContext` directly where appropriate. Keep data access explicit and easy to follow.
- Do not introduce CQRS, DDD aggregate layers, use-case/handler classes, repository interfaces, MediatR, event buses, extra projects, or similar abstractions unless a specific demonstrated requirement justifies them.
- Keep `Shared` focused on transport contracts; do not put server implementation or persistence concerns there.
- Minimize dependencies and prefer capabilities already in the stack.

## 4. UI: prioritize BlazorBlueprint

- **Check BlazorBlueprint components and patterns before creating custom UI components.**
- Verify the installed version and consult its official docs for available components, APIs, variants, examples, and accessibility guidance.
- Reuse existing components and styling conventions for a consistent UI.
- Create a custom component only when BlazorBlueprint and existing app components cannot reasonably meet the requirement. Briefly explain the gap in the change or final summary.
- Do not add another UI library for a component the current stack can handle.

## 5. Ask when requirements are unclear

- Ask a concise question when ambiguity would materially affect user-visible behavior, data modeling, authorization, API contracts, migrations, deployment, or scope.
- Do not ask about details that can be verified from the repository or official documentation.
- For low-risk ambiguity, make the smallest reasonable assumption, state it, and proceed.
- Do not silently invent requirements or expand the task.

## 6. Security: no login by design

- Per `ROADMAP.md`, there is **no authentication**: one server instance and database per household, member profiles are display names only. Do not add login, roles, `HouseholdId` scoping, or invitation flows.
- Restrict access at deployment (private network, VPN, firewall), not in app code.
- Validate input at the API boundary. Do not expose stack traces, secrets, or internal details in production errors.
- Never log connection strings, private VAPID keys, or other secrets. Keep VAPID private keys server-side only.

## 7. Data and background work

- Review schema and migration conventions before changing the database. Do not make destructive migration changes casually.
- Do not treat calendar expiry dates as timestamps by default; preserve the app's intended date/time-zone semantics and test boundaries.
- The reminder worker (`ReminderWorker`) runs at startup and daily at 8am server-local; reminders are idempotent per item/expiry/type, so extra runs are free. One failed item never stops the rest (per-item try/catch with tracker reset).
- Background reminder work should be resilient and idempotent. A failed push should not block delivery attempts to other recipients.
- Keep in-app notifications as the durable notification history and web push as best-effort.
- Deduplicate reminders using the established recipient/item/expiry/reminder identity, and handle invalid subscriptions safely.
- Do not introduce a job platform, queue, or distributed infrastructure unless a measured requirement calls for it.

## 8. Code quality and verification

- Follow local naming, formatting, API, component, and error-handling conventions.
- Razor style: `@code` opening brace on its own line.
- Keep changes focused; avoid unrelated refactoring.
- Add or update tests for changed behavior, especially authorization, household isolation, invitations, expiry boundaries, and reminder deduplication.
- Run relevant tests first, then build/test the solution when practical. Report only commands actually run and their real results.
- For UI changes, consider responsive layouts, keyboard accessibility, loading/empty/error states, and existing styles.
- Never fix a failing test by weakening a security check or removing meaningful assertions.

## 9. Completion summary

When finished, report briefly:

1. What changed and why.
2. Important assumptions or decisions needed from the user.
3. Build/test commands run and actual results.
4. Remaining risks, blockers, or migration/configuration steps.

Do not claim deployment, external actions, or successful verification unless they actually occurred.


## Stack

- .NET 10 solution (`smart-pantry-webapp.slnx`): `src/Client` (Blazor WebAssembly) + `src/Server` (ASP.NET Core) + `src/Shared` (transport contracts) + `tests/Server.Tests` (xUnit). No CI.
- Routing: Bit.Brouter 10.6.2 replaces `<Router>` in `App.razor` (view transitions on, scroll-to-top, focus `h1`); pages are discovered via `@page` + `AppAssembly` scan. Do not add the built-in `<Router>` back.
- Build: `dotnet build smart-pantry-webapp.slnx`. Run/debug via the **Server** project (`http://localhost:5125`, `https://localhost:7139`); it serves the Client (`MapStaticAssets` + `MapFallbackToFile("index.html")` in `src/Server/Program.cs`). Run with `ASPNETCORE_ENVIRONMENT=Development` (or a launch profile) — `appsettings.json` has no connection string, so Production startup fails at `MigrateAsync`.
- Tests: `dotnet test smart-pantry-webapp.slnx` (single test: `dotnet test tests/Server.Tests --filter <Name>`). `PantryApiTests` boot the real host via `WebApplicationFactory` and need local Postgres; each test class gets its own auto-created database, dropped afterwards (base connection override via `SMARTPANTRY_TEST_CONNECTION`).
- Product plan and phase order live in `ROADMAP.md`; check it before adding features outside the current slice.

## Endpoints: update both sides

- Server uses `GeneratedEndpoints` source gen: handler classes with `[MapGroup]` / `[MapGet]` / `[MapPost]`, registered via `AddEndpointHandlers()` / `MapEndpointHandlers()`.
- Client uses Refit: hand-written endpoint interfaces in `Client/Services/Api/` (`IPantryEndpoints`, `IMemberEndpoints` with `[PathPrefix]` + verb attributes), registered via `AddRefitGeneratedClient` on the `"Server.API"` `HttpClient`.
- When adding an endpoint, add the server handler **and** the matching Refit interface method, keeping route prefixes and shapes in sync.
- Full pantry CRUD in `src/Server/Endpoints/PantryEndpoints.cs`: `GET /api/pantry` (filters: `Location` — `"all"` means unfiltered — `Name` via `ILike`, `Status` (`expired`/`soon`/`fresh`), `Sort` (`expiry-asc`/`expiry-desc`/`name`, default `expiry-asc` with deterministic ordering); paging `Skip`/`Take` are nullable, defaulting to `0`/`20` in the handler, max `Take` 100), `POST /api/pantry`, `GET /api/pantry/{id}`, `PUT /api/pantry/{id}`, `DELETE /api/pantry/{id}` (204). Missing items return 404 ProblemDetails. Shared literals and the 7-day window live in `Shared.Models.PantryRules` — both tiers reference it.
- Member profiles in `src/Server/Endpoints/MemberEndpoints.cs`: `GET /api/members`, `POST /api/members` (201), `PUT /api/members/{id}` (rename/color/archive; no delete). Attribution flows via the `X-Member-Id` request header, stamped at item creation only; unknown/missing members resolve to null, never 400.
- Notifications in `src/Server/Endpoints/NotificationEndpoints.cs`: `GET /api/notifications` (newest first + unread count + total; `Skip`/`Take` paging), `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`.
- Push in `src/Server/Endpoints/PushEndpoints.cs`: `GET /api/push/public-key` (404 when unconfigured), `POST /api/push/subscriptions` (upsert by endpoint), `DELETE /api/push/subscriptions?endpoint=`. The reminder worker sends one push per new reminder to every subscription (best-effort; rejected subscriptions are deleted). VAPID keys live in `VapidOptions` (`PublicKey` may be in source; `PrivateKey` only via user-secrets/env — validated on startup, never logged).
- Realtime in `src/Server/Hubs/PantryHub.cs` (typed `Hub<IPantryHubClient>`, mapped at `/hubs/pantry`): pantry create/update/delete broadcast `PantryChangedEvent` (item id + created/updated/removed). Client `PantryRealtimeService` connects with automatic reconnect; pages re-query on broadcast and treat a dead connection as silent fallback to manual reload. Contract in `Shared.Models` (`PantryHubModels.cs`).
- Paged lists use the generic `InfiniteScrollList<T>` (`Shared/`, Bit.Butil IntersectionObserver sentinel with bottom pre-load margin; `ReloadAsync()` after filter changes; dispose the subscription). It needs the `bit-butil.js` script tag in `index.html` — without it browser reads silently return defaults.
- Validation is framework-driven: DataAnnotations on Shared DTOs (use `[property: ...]` targets on positional records — bare attributes land on the ctor parameter and are ignored), enforced automatically via `AddValidation()` + `AddSharedValidation()` (source-gen metadata is per-assembly, so Shared registers itself and marks roots `[ValidatableType]`). Invalid requests get a 400 ProblemDetails before the handler runs — no manual validation in endpoints. Keep `[AsParameters]` query properties nullable (non-nullable value types bind as required and break optional paging).

## Database

- PostgreSQL via Npgsql with snake_case naming (`UseSnakeCaseNamingConvention`). `AppDbContext` has `DbSet<PantryItem> Items`.
- `Program.cs` runs `MigrateAsync()` at startup. Connection string `Default` exists **only** in `Server/appsettings.Development.json` (local Postgres); `appsettings.json` has none — configure per-environment, keep secrets out of source.
- Migrations: `dotnet ef migrations add <Name> --project Server` (EF Design package is on Server). Review `Server/Data/Migrations` conventions first; avoid destructive changes.

## Domain quirks

- `PantryItem.Id` is a GUIDv7 string generated in the entity default; `ExpirationDate` is `DateOnly` — keep date-only semantics and test boundary dates. Do not store derived expiry status; compute it.
- **No auth, no multi-tenant isolation.** Per `ROADMAP.md`, one server instance + database per household, member profiles without login; restrict access at deployment, not in app code. Do not add auth roles, `HouseholdId` scoping, invitations, or push infrastructure unless the roadmap phase is explicitly in scope.

## UI

- BlazorBlueprint 4.1.0 (`Bb*` components, Lucide icons) — check its components/patterns before building custom UI. `_Layout.razor` already hosts `BbPortalHost`, `BbToastProvider`, `BbDialogProvider` (required for portal/toast/dialog components).
- Mobile-first: phone UI is the current target (custom floating `BottomBar` + bottom-sheet forms, native date/select inputs for OS pickers). Desktop gets a `BbSidebar` + `BbSidebarInset` layout (`_Layout.razor`); the bar and bottom padding are mobile-only. Never create a `Client.Components.Shared` namespace — it hijacks `@using Shared.Models` resolution; shared components live in `Client.Components` via `@namespace`.
- Bottom sheets are custom, not `BbSheet`: global `BottomSheetService` + `BottomSheetProvider` under `Shared/BottomSheet/` (handle, snap points, pan-to-close, colocated JS, isolated CSS; the body region scrolls, the sheet itself never does). A sheet body never opens a dialog (it renders beneath the sheet) — close with a request object and let the opener confirm and execute. Page-only components go in `Shared/<Page>/`, cross-page ones at `Shared/` root. Files under `Shared/` subfolders must still declare `@namespace Client.Components` — a `Client.Components.Shared.*` namespace hijacks `@using Shared.Models` resolution.
- PWA shell (manifest, icons, service worker) already exists in `Client/wwwroot`; offline sync stays out of scope per roadmap.
- App updates: `UpdateNotifier` (Bit.Butil `ServiceWorker`) toasts a reload action when a new worker is waiting. Shared worker logic lives in `service-worker.shared.js`, imported by both worker files — keep the two in sync.
