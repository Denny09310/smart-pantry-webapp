# INSTRUCTIONS.md — Personal working conventions for AI sessions

This file records how I (the repo owner) like to work with coding agents on Smart Pantry.
`AGENTS.md` is the base contract — this file adds personal preferences and hard-won
lessons discovered across sessions. When the two conflict, ask me.

## 1. Session workflow

- **Inspect before editing.** Read the relevant files, nearby code, tests and package
  references first. Prefer focused reads over repository dumps. Never present a plan
  based on memory of the codebase — verify.
- **Research narrowly.** Use official docs and MCP tools for the specific API in question,
  never broad dependency source dives. Never scan NuGet caches or decompile packages.
- **Ask when it matters.** Ambiguity affecting user-visible behavior, data modeling, auth,
  API contracts, migrations, deployment or scope → ask a concise question. For low-risk
  ambiguity, make the smallest reasonable assumption, state it, and proceed.
- **Verify by execution.** Build, run tests, reproduce bugs live (curl a running server,
  query Postgres directly) before claiming a fix. Report only commands actually run and
  their real results — never invent outcomes.
- **Summarize on completion:** (1) what changed and why, (2) assumptions/decisions I need
  to confirm, (3) build/test commands run with actual results, (4) remaining risks or
  follow-ups. Keep responses short, factual, no fluff, no emojis, no praise.
- **I test the app myself, in parallel.** I run the server, edit files, and regenerate
  CSS while you work. Never kill a process you didn't start (check PIDs first); only
  clean up your own stale processes. Never overwrite or "fix" my uncommitted files
  (notably `tailwind.min.css` watch output) — leave them alone and mention them.
- **I verify fixes myself** ("I've tried them, it works") — still do your own verification
  first; don't rely on mine.
- Commit only when I say so ("commit", "okay commit", "commit everything"). When I do,
  split into small logical commits (see §2), don't lump everything into one.

## 2. Git & commits

- Conventional commits with scope: `feat(client): …`, `fix(server): …`, `chore: …`,
  `style(client): …`, `refactor(client): …`, `docs: …`, `chore(release): …`.
- One concern per commit; never one giant commit. Separate: features, fixes, chores,
  style, test-only changes, generated files.
- Generated files (`Strings.Designer.cs`, `tailwind.min.css` rebuilds, migrations +
  snapshot) ride along with the commit that caused them.
- Merges: I decide the strategy per case (e.g. "overhaul wins everywhere"). After a
  messy merge, prove the tree state explicitly (`git diff --cached <branch> --name-only`
  must be empty when that was the goal) before committing.
- Tags are `v`-prefixed (`v1.0.0`) unless I say otherwise. Don't push unless asked.
- Branch flow used here: feature branches → `develop` → `master` (+ tag). Release
  commits (`chore(release)`) bundle changelog + `<Version>` bump.
- `git cliff` generates CHANGELOG.md (no `cliff.toml`, defaults are fine). Curate the
  release section by hand afterwards — drop entries for superseded/reverted work so the
  notes match the shipped tree.

## 3. Environment & shell (Windows + PowerShell)

- The shell is **PowerShell**: `tail`, `head`, `grep`, `sed`, `wc`, `cat`, `2>/dev/null`
  do NOT exist. Use `Select-Object -First/-Last N`, `Select-String`, `Get-Content`,
  `Get-ChildItem` instead.
- Prefer dedicated file tools (`read`/`edit`/`write`) over shell for file ops; reserve
  `shell` for `dotnet` commands and real system operations.
- Scratch work (seed scripts, one-off queries, repro harnesses) goes in
  `C:\Users\dkoja\AppData\Local\Temp\opencode\` — **never** in the repo. Delete or leave
  it outside git; confirm the repo has no stray files (`git status`) when done.
- Never poll for background commands; do other useful work and let completion notify.

## 4. Build, run, test

- Build: `dotnet build smart-pantry-webapp.slnx` (must end 0 warnings / 0 errors).
- Run/debug via the **Server** project only: `http://localhost:5125`
  (`ASPNETCORE_ENVIRONMENT=Development` or a launch profile — Production has no
  connection string and fails at `MigrateAsync`). Local Postgres via docker on :5432.
- Tests: `dotnet test smart-pantry-webapp.slnx`; single test:
  `dotnet test tests/Server.Tests --filter <Name>`. Tests boot the real host with
  isolated auto-created Postgres DBs (dropped afterwards); override with
  `SMARTPANTRY_TEST_CONNECTION`. A green full run is required before merge/release
  commits (currently ~87 tests).
- Flaky tests happen (parallel timing, background worker races). Re-run in isolation
  to distinguish flakes from real failures; fix the test design, never weaken assertions
  or security checks.
- Migrations: `dotnet ef migrations add <Name> --project Server`. `Program.cs` runs
  `MigrateAsync()` at startup. Review `Server/Data/Migrations` conventions; no casual
  destructive changes. Connection string lives **only** in
  `Server/appsettings.Development.json` (local Postgres, `smart_pantry`).
- To wipe + reseed local dev data: `dotnet ef database drop --force --project src/Server`
  then `database update`, then seed via a TEMP script (never committed).

## 5. Architecture boundaries (hard rules)

- `Server`: Minimal APIs + application behavior + EF Core + background work.
- `Client`: Blazor WebAssembly standalone UI. `Shared`: transport contracts only.
- No CQRS/MediatR/repository interfaces/event buses/extra projects without a demonstrated,
  measured need. Use `AppDbContext` directly; keep data access explicit.
- Minimal dependencies; prefer what's already in the stack.
- Server uses `GeneratedEndpoints` source gen (`[MapGroup]`/`[MapGet]`/`[MapPost]` handler
  classes + `AddEndpointHandlers()`/`MapEndpointHandlers()`). Client uses hand-written
  Refit interfaces in `Client/Services/Api/` on the `"Server.API"` HttpClient — **every
  server endpoint gets its matching Refit method**, route prefixes and shapes in sync.
- Bit.Brouter replaces `<Router>` in `App.razor` (view transitions, scroll-to-top,
  `h1` focus). Never add the built-in `<Router>` back.
- Follow `ROADMAP.md` phase order before adding out-of-slice features.

## 6. Server conventions

- Entities: GUIDv7 string `Id` generated in the default; `DateOnly` expiries (never
  timestamps); snake_case Postgres via `UseSnakeCaseNamingConvention`; `AsNoTracking()`
  for reads; `ExecuteDeleteAsync()` for deletes; `ChangeTracker.Clear()` after a failed
  save; catch `UniqueViolation` for insert races (cache/dedup writes are best-effort and
  must never 500 a lookup).
- String length caps in DataAnnotations must match DB max lengths (name 200, notes 2000,
  unit/barcode 32, location 64, member 100, color 16, image URL 2000, NutriScore 2).
- Validation is framework-driven: `[property: …]` targets on positional records (bare
  attributes land on the ctor parameter and are ignored), `AddValidation()` +
  `AddSharedValidation()` (`[ValidatableType]` roots), nullable `[AsParameters]` query
  props. Invalid requests become 400 ProblemDetails before handlers run — no manual
  validation in endpoints.
- API semantics: missing items → 404 ProblemDetails; creates → 201 with location;
  deletes → 204; bad queries → ValidationProblem. List paging: nullable `Skip`/`Take`
  defaulting to `0`/`20`, max `Take` 100, deterministic ordering (`ThenBy Id`).
  `"all"` means unfiltered (location exact-match case-insensitive via `ILike`, name
  substring via `ILike`).
- Member attribution via `X-Member-Id` header, stamped at creation only; unknown/missing
  members resolve to null, never 400. No auth, no `HouseholdId` scoping — single
  household per instance by design.
- NutriScore stored uppercase-invariant. `NutritionFacts` (per 100g, all-optional) is
  serialized with `JsonSerializer` (culture-invariant!) — never string-interpolate doubles
  into JSON (IT locales emit commas and corrupt it); parse defensively.
- External APIs (OpenFoodFacts, TheMealDB, MyMemory) are called **server-side only**,
  with 15s HttpClient timeouts, in-memory caching (translations 30d, ingredient list 24h),
  and graceful degradation (return input/empty/null, never throw to the caller). Only
  cache *successful* translations (check `responseStatus == 200`); skip translation for
  terms already known-English to protect free quotas.
- Product lookup order: local cache → OpenFoodFacts → persist → pantry-items fallback → 404.
- VAPID: `PublicKey` may be in source; `PrivateKey` only via user-secrets/env, validated
  on startup, never logged. Never log connection strings or secrets. Production errors
  must not leak stack traces or internals.
- `PantryChangedEvent` payloads stay tiny (item id + `PantryChangeKind` enum serialized
  as `"created"`/`"updated"`/`"removed"` via `JsonStringEnumConverter` — wire format is
  contractual, verify it if touched).
- ReminderWorker runs at startup + daily 8am server-local; idempotent per
  item/expiry/type; per-item try/catch with tracker reset; one failure never stops the
  rest. In-app notifications are the durable history; web push is best-effort (invalid
  subscriptions are deleted).

## 7. Client / Blazor conventions

- **BlazorBlueprint first** (`Bb*`, Lucide icons) — check its components/patterns before
  custom UI; custom components only with a stated gap. Never add a second UI library.
- **Razor style: `@code` opening brace on its own line.** Component state goes in a
  `private sealed State` class in a colocated `.razor.cs` code-behind (`namespace
  Client.Components[.Pages]`); colocated `.razor.js`/`.razor.css` for interop/styles.
- Never create a `Client.Components.Shared.*` namespace — it hijacks `@using
  Shared.Models`. Files under `Shared/` subfolders declare `@namespace Client.Components`.
  Page-only components live in `Shared/<Page>/`, cross-page ones at `Shared/` root.
- Mobile-first: phone UI is the target — floating `BottomBar` (mobile-only, square
  icon buttons, backdrop blur, `title` tooltips) + bottom-sheet forms + native
  date/select inputs. Desktop gets `BbSidebar` + `BbSidebarInset` (`md:` layout,
  `pb-28` mobile bottom padding, safe-area insets, `viewport-fit=cover`).
- Bottom sheets are custom (`BottomSheetService` + `BottomSheetProvider`; handle, snap
  points, pan-to-close, colocated JS/CSS). The sheet body region scrolls, the sheet
  itself never does. A sheet body never opens a dialog — close with a request object
  and let the opener confirm/execute (`ItemDeletion` pattern). Reload lists only when a
  sheet closes with data (`!result.Cancelled`). Mandatory flows use non-dismissible
  `SheetOptions`. Keep `service-worker.shared.js` in sync across both worker files.
- Persistent chrome gets `view-transition-name` (`bottom-nav`, sidebar) so page
  transitions don't animate it.
- **Browser access rules (every Butil call):** touch the browser from
  `OnAfterRenderAsync`/event handlers, never `OnInitializedAsync` (no JS runtime during
  prerender — reads return defaults, void calls no-op). `AddBitButilServices()` in every
  DI container; `bit-butil.js` before the Blazor script in `index.html`. Dispose every
  `ButilSubscription`/handle. Denied permission = `false`/`null` branch with user
  feedback, never an exception path. `ElementReference` must exist before `AttachTo`.
  No hand-written JS for barcode scanning — `BarcodeDetector` + `MediaDevices` only
  (custom colocated JS modules are fine for sheet gestures/pull-to-refresh).
- Data fetching: `using var response = await Api.…`, check `IsSuccessfulWithContent`,
  propagate `CancellationToken`s; debounced search (300ms + `SearchCts`); `async void`
  event handlers wrap in try/catch + `InvokeAsync`/`DispatchExceptionAsync`;
  `IAsyncDisposable` for JS handles/subscriptions.
- UX states everywhere: loading skeletons matching card shapes, `BbEmpty` with icon +
  action, `role="alert"` error boxes with Retry, localized `aria-label`s on icon buttons,
  toasts for success/failure (`ProductFound`, `CouldNotSave`, …). Keyboard/focus-visible
  rings on interactive custom chrome.
- Realtime: `PantryRealtimeService` with auto-reconnect; pages **patch** loaded rows from
  a single-item fetch (drop/replace/sorted-insert, `RemoveByKeyAsync`/`ReplaceAsync`/
  `InsertSortedAsync` on the list, dedup guard on page append) and full-`ReloadAsync()`
  on anything uncertain. Dead connection = silent fallback to manual reload.
- Conventions by example: FAB removed in favor of header icon-button; add-item is an
  icon button in the header; expiry shortcuts +3d/+1wk/+1mo/+6mo; `DateFormat dd/MM/yyyy`;
  quantity stepper 0–9999 default `pcs`/`pantry`; `bit-butil.js` required for
  `InfiniteScrollList`'s IntersectionObserver sentinel.

## 8. Localization (EN + IT)

- `Strings.resx` (en) + `Strings.it.resx`, consumed via `LocalizationService` wrapping
  `IStringLocalizer<Strings>` (`Loc["Key"]`, `Loc["Key", args]` for `{0}` formats).
  Never register `Strings` itself in DI (causes `NoConstructorMatch` at runtime).
- Culture persisted in localStorage (`smart-pantry:culture`); `en-US` default, `it-IT`
  supported; `SupportedCultures` + `BlazorWebAssemblyLoadAllGlobalizationData=true` in
  the client project (dynamic culture switch crashes without it).
- Keep `Strings.Designer.cs` in sync manually (same alphabetical placement) even though
  nothing references its static properties at runtime — it's checked in.
- Localize everything user-visible: titles, labels, buttons, toasts, dialogs, aria-labels,
  empty/error states, relative times (`JustNow`, `{0}m/h/d` variants — IT uses
  `adesso`, `{0} min/h/g fa`). Example `e.g.` placeholders and raw values sent to the
  server stay as-is. API-facing language uses two-letter codes
  (`TwoLetterISOLanguageName` → `lang` query param).
- Re-search/re-render on `CultureChanged`; Settings uses a `BbSelect` language picker
  and reuses the `MemberSwitcher` sheet instead of duplicating member UI.

## 9. PWA & static assets

- `manifest.webmanifest`: 192 + 512 `any` icons plus a transparent `maskable` 512 icon
  (`"purpose": "maskable"`, artwork ~66% centered) — Android splash/launcher crops
  non-maskable icons into white squares. `theme_color`/`background_color` black,
  `display: standalone`. Icons referenced by the manifest must exist with exact filenames.
- `apple-touch-icon.png` stays opaque (iOS requirement); don't touch `favicon.svg`.
- `index.html` loading screen: centered app icon + progress text, `max-h-dvh`.
- `UpdateNotifier` (Butil `ServiceWorker`): toasts a reload action when a worker waits;
  re-checks on tab visibility; controller-change reloads with a 4s fallback.

## 10. Tests (xUnit, `tests/Server.Tests`)

- Add/update tests for changed behavior (validation, paging/sorting boundaries,
  reminder dedup, push delivery, hub broadcasts, lookup/cache, translation).
- Stub HTTP with `HttpMessageHandler` fakes; drive internal endpoints directly
  (`InternalsVisibleTo`) with fake services where no DB is needed; use
  `WebApplicationFactory` (`PantryApiFactory`) for full-host behavior.
- Beware background-worker races in tests: the hosted `ReminderWorker` runs at startup —
  drive idempotent paths to quiescence instead of asserting exact per-run counts.
- Hub tests: capture assertions' data *before* helper cleanup runs (cleanup in `finally`
  executes before the caller resumes).
