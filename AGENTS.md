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

- `SmartPantry.Server`: ASP.NET Core Minimal APIs, application behavior, EF Core persistence, and background work.
- `SmartPantry.Client`: Blazor WebAssembly Standalone UI.
- `SmartPantry.Shared`: transport contracts shared between client and server.
- Use `AppDbContext` directly where appropriate. Keep data access explicit and easy to follow.
- Do not introduce CQRS, DDD aggregate layers, use-case/handler classes, repository interfaces, MediatR, event buses, extra projects, or similar abstractions unless a specific demonstrated requirement justifies them.
- Keep `SmartPantry.Shared` focused on transport contracts; do not put server implementation or persistence concerns there.
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

## 6. Security and household isolation

- The server is the security boundary; client-side checks are not authorization.
- Enforce authentication and household membership on every protected endpoint, including notifications, invitations, and push subscriptions.
- Never trust client-supplied `HouseholdId`, `UserId`, role, or ownership claims without server-side validation against the authenticated principal and database.
- Scope reads, updates, and deletes to the caller's authorized household.
- Validate input at the API boundary. Do not expose stack traces, secrets, or internal details in production errors.
- Never log passwords, access tokens, invitation tokens, private VAPID keys, or other secrets.
- Preserve invitation protections: secure tokens, hashed storage where appropriate, expiry, and one-time use. Ensure membership removal revokes access and stops future push delivery.

## 7. Data and background work

- Review schema and migration conventions before changing the database. Do not make destructive migration changes casually.
- Do not treat calendar expiry dates as timestamps by default; preserve the app's intended date/time-zone semantics and test boundaries.
- Background reminder work should be resilient and idempotent. A failed push should not block delivery attempts to other recipients.
- Keep in-app notifications as the durable notification history and web push as best-effort.
- Deduplicate reminders using the established recipient/item/expiry/reminder identity, and handle invalid subscriptions safely.
- Do not introduce a job platform, queue, or distributed infrastructure unless a measured requirement calls for it.

## 8. Code quality and verification

- Follow local naming, formatting, API, component, and error-handling conventions.
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

- .NET 10 solution (`smart-pantry-webapp.slnx`): `Client` (Blazor WebAssembly) + `Server` (ASP.NET Core). No test project, no CI.
- Build: `dotnet build smart-pantry-webapp.slnx`. Run/debug via the **Server** project (`http://localhost:5125`, `https://localhost:7139`); it serves the Client (`MapStaticAssets` + `MapFallbackToFile("index.html")` in `Server/Program.cs`).
- Product plan and phase order live in `ROADMAP.md`; check it before adding features outside the current slice.

## Contracts: no Shared project

- `Shared.Models` is a **namespace, not a project**. DTOs live in `Client/Models/DTOs`; `Server` consumes them through its `ProjectReference` to `Client` (`Server/Server.csproj`).
- Keep DTOs in `Client/Models/DTOs` with namespace `Shared.Models`. Do not create a Shared project unless a concrete need arises.

## Endpoints: update both sides

- Server uses `GeneratedEndpoints` source gen: handler classes with `[MapGroup]` / `[MapGet]` / `[MapPost]`, registered via `AddEndpointHandlers()` / `MapEndpointHandlers()`.
- Client uses Refit: hand-written `IPantryEndpoints` in `Client/Services/Api/ServerApi.cs` (`[PathPrefix]` + verb attributes), registered via `AddRefitGeneratedClient` on the `"Server.API"` `HttpClient`.
- When adding an endpoint, add the server handler **and** the matching Refit interface method, keeping route prefixes and shapes in sync.
- Current surface is only `GET /api/pantry` (filters: `Location` — `"all"` means unfiltered — and `Name` via `ILike`; paging `Skip`/`Take`, default `0`/`20`) and `POST /api/pantry`, in `Server/Endpoints/PantryEndpoints.cs`.

## Database

- PostgreSQL via Npgsql with snake_case naming (`UseSnakeCaseNamingConvention`). `AppDbContext` has `DbSet<PantryItem> Items`.
- `Program.cs` runs `MigrateAsync()` at startup. Connection string `Default` exists **only** in `Server/appsettings.Development.json` (local Postgres); `appsettings.json` has none — configure per-environment, keep secrets out of source.
- Migrations: `dotnet ef migrations add <Name> --project Server` (EF Design package is on Server). Review `Server/Data/Migrations` conventions first; avoid destructive changes.

## Domain quirks

- `PantryItem.Id` is a GUIDv7 string generated in the entity default; `ExpirationDate` is `DateOnly` — keep date-only semantics and test boundary dates. Do not store derived expiry status; compute it.
- **No auth, no multi-tenant isolation.** Per `ROADMAP.md`, one server instance + database per household, member profiles without login; restrict access at deployment, not in app code. Do not add auth roles, `HouseholdId` scoping, invitations, or push infrastructure unless the roadmap phase is explicitly in scope.

## UI

- BlazorBlueprint 4.1.0 (`Bb*` components, Lucide icons) — check its components/patterns before building custom UI. `_Layout.razor` already hosts `BbPortalHost`, `BbToastProvider`, `BbDialogProvider` (required for portal/toast/dialog components).
- PWA shell (manifest, icons, service worker) already exists in `Client/wwwroot`; offline sync stays out of scope per roadmap.
