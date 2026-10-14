# INSTRUCTIONS.md — Personal working conventions for AI coding sessions

How I like to work with coding agents, distilled across sessions. This file is generic
and travels across repos. A repo's own `AGENTS.md`/`CONTRIBUTING.md` is the base
contract — this file adds my preferences. When the two conflict, ask me.

## 1. Session workflow

- **Inspect before editing.** Read the relevant files, nearby code, tests and package
  references first. Prefer focused reads over repository dumps. Never plan from memory
  of the codebase — verify.
- **Research narrowly.** Use official docs and available tooling for the specific API in
  question, never broad dependency source dives. Never scan package caches or decompile
  dependencies.
- **Ask when it matters.** Ambiguity affecting user-visible behavior, data modeling,
  auth, API contracts, migrations, deployment or scope → ask a concise question.
  For low-risk ambiguity, make the smallest reasonable assumption, state it, proceed.
- **Verify by execution.** Build, run tests, reproduce bugs live before claiming a fix.
  Report only commands actually run and their real results — never invent outcomes.
- **Summarize on completion:** (1) what changed and why, (2) assumptions/decisions I
  need to confirm, (3) build/test commands run with actual results, (4) remaining
  risks or follow-ups. Keep responses short, factual, no fluff, no emojis, no praise.
- **I work in parallel.** I run the app, edit files, and regenerate assets while you
  work. Never kill a process you didn't start (check PIDs first); only clean up your
  own stale processes. Never overwrite or "fix" my uncommitted files (e.g. watcher-
  generated bundles) — leave them alone and mention them.
- **I verify fixes myself** — still do your own verification first; don't rely on mine.
- Commit only when I say so ("commit", "okay commit", "commit everything"). Split into
  small logical commits (see §2), never one giant commit.

## 2. Git & commits

- Conventional commits with scope: `feat(scope): …`, `fix(scope): …`, `chore: …`,
  `style(scope): …`, `refactor(scope): …`, `docs: …`, `test: …`, `chore(release): …`.
- One concern per commit. Generated files (designers, bundles, migrations + snapshots)
  ride along with the commit that caused them.
- After a messy merge, prove the tree state explicitly (e.g. diff staged result against
  the intended branch — must be empty when that was the goal) before committing.
- Tags are `v`-prefixed (`v1.0.0`) unless I say otherwise. Don't push unless asked.
- If the repo uses `git cliff` for changelogs (no config file needed — defaults group
  conventional commits fine), curate the release section by hand afterwards: drop
  entries for superseded/reverted work so the notes match the shipped tree.
- Release commits bundle changelog + `<Version>` bump; move the tag onto the release
  commit only if it's still local-only, and say so.

## 3. Environment & shell (Windows + PowerShell)

- The shell is **PowerShell**, not bash. It ships **aliases** for basic file ops that
  behave like their Unix namesakes for simple usage: `ls`/`dir`, `cat`, `cp`, `mv`,
  `rm`, `echo`, `pwd`, `cd`, `sls` (= `Select-String`), `sort`, `tee`. Aliases do NOT
  accept Unix flags (`ls -la`, `cp -r` fail or misbehave) — use the native cmdlet
  syntax for anything beyond basics.
- These do **not** exist unless Unix tools are on PATH (verify with `Get-Command`,
  don't assume): `grep` → `Select-String`, `head`/`tail` → `Select-Object -First/-Last`,
  `sed`/`awk` → nothing (use file tools or `ForEach-Object`), `wc` → `Measure-Object`,
  `cat` exists but prefer the `read` tool for files. `curl` is aliased to
  `Invoke-WebRequest` — call `curl.exe` explicitly for real curl behavior.
- Redirection differs: `2>$null` (not `2>/dev/null`). `;` chains commands; `&&`/`||`
  exist only on PowerShell 7+.
- Prefer dedicated file tools (`read`/`edit`/`write`/`glob`) over shell for file ops;
  reserve `shell` for build/test commands and real system operations.
- Scratch work (seed scripts, one-off queries, repro harnesses) goes in
  `%TEMP%\opencode\` — **never** in the repo. Confirm no stray files (`git status`)
  when done.
- Never poll background commands; do other useful work and let completion notify.

## 4. Build, run, test

- Know the repo's canonical commands first (`dotnet build {SOLUTION}`, the run/debug
  project, required environment/flavor, ports, local service dependencies) and use
  exactly those. A build must end 0 warnings / 0 errors.
- Tests must run green before merge/release commits: full suite plus targeted
  single-test runs (`--filter <Name>`). Tests should be hermetic where possible
  (isolated databases, stubbed HTTP); document required local services and env
  overrides in the repo docs.
- Flaky tests happen (parallel timing, background-worker races). Re-run in isolation
  to distinguish flakes from real failures; fix the test design, never weaken
  assertions or security checks to make red green.
- Migrations run at startup in dev; review migration conventions before touching the
  schema; no casual destructive changes. Dev connection strings live in untracked or
  environment-specific config — never in shared tracked files.
- Local dev data resets go through the project's own tooling (e.g. ORM drop + migrate),
  seeded from TEMP scripts that are never committed.

## 5. Architecture boundaries (defaults — the repo's own docs override)

- Respect tier roles (API / UI / shared contracts). No CQRS/MediatR/repository
  interfaces/event buses/extra projects without a demonstrated, measured need. Keep
  data access explicit and boring.
- Minimal dependencies; prefer what's already in the stack.
- Server endpoints and typed-client interfaces are added **as a pair**, route prefixes
  and shapes kept in sync. Never reintroduce a router/component the repo deliberately
  replaced — check history before "fixing" unfamiliar wiring.
- Follow the repo's roadmap/phase docs before adding out-of-slice features.

## 6. Server conventions

- Entities: DB-generated or GUIDv7 string ids; date-only semantics for calendar dates
  (never timestamps); `AsNoTracking()` for reads; explicit deletes; clear the change
  tracker after a failed save; catch unique-violations on best-effort writes (cache/
  dedup inserts must never 500 a read).
- Validation-attribute string caps must match DB column max lengths. Prefer
  framework-driven validation (correct attribute targets on records, nullable optional
  query params) so invalid requests become 400s before handlers run — no manual
  validation in endpoints.
- API semantics: missing resources → 404 problem details; creates → 201 with location;
  deletes → 204; bad queries → validation problems. Paged lists: nullable skip/take
  with sane defaults and a max, deterministic ordering with id tiebreak.
- Attribution/identity headers resolve to null on unknown/missing values, never 400.
  Respect the repo's auth model — don't add login, roles, or tenant scoping that the
  design deliberately omits.
- Numeric serialization must be culture-invariant (never string-interpolate doubles
  into stored JSON — some locales emit commas and corrupt it); parse defensively.
- External APIs are called **server-side only**, with timeouts, in-memory caching, and
  graceful degradation (return input/empty/null, never throw to the caller). Only cache
  *successful* translations/lookups; skip translation for already-known terms to
  protect free quotas.
- Realtime payloads stay tiny (id + kind enum with stable wire strings — verify the
  wire format if touched). Background workers are idempotent, per-item try/catch, one
  failure never stops the rest; durable history in-DB, best-effort delivery outside it.
- Secrets (private keys, connection strings) only via user-secrets/env, validated on
  startup, never logged. Production errors must not leak stack traces or internals.

## 7. Client conventions (adjust names to the repo's stack)

- **UI kit first** — check its components/patterns before custom UI; custom components
  only with a stated gap. Never add a second UI library. Icons via the kit's icon set.
- **Razor style: `@code` opening brace on its own line.** Component state goes in a
  `private sealed State` class in a colocated code-behind; colocated JS/CSS modules
  for interop/styles. Respect the repo's component-namespace rules (a wrong namespace
  can hijack type resolution) and its folder conventions (page-only vs shared).
- Mobile-first where the product demands it: persistent nav chrome, bottom-sheet
  forms, native date/select inputs; desktop gets the sidebar layout with proper bottom
  padding, safe-area insets, and viewport-fit cover.
- Modal/sheet discipline: the sheet body region scrolls, the sheet itself never does.
  A sheet body never opens a nested dialog — close with a request object and let the
  opener confirm/execute. Reload lists only when a sheet closes with data, not on
  cancel. Mandatory flows use non-dismissible options.
- Persistent chrome gets exclusion names for view transitions so navigation doesn't
  animate it.
- **Browser-access rules:** touch the browser from post-render/event handlers, never
  from init (no JS runtime during prerender — reads return defaults, void calls
  no-op). Register browser services in every DI container that renders; include
  required script tags before the framework script. Dispose every subscription/handle.
  Denied permission comes back as `false`/`null` — branch and show the user something.
  Element references must exist before attaching streams. Global CSS (not component-
  scoped stylesheets) for any rule targeting markup rendered by *other* components —
  scoping rewrites selectors to the owning component only.
- Data fetching: dispose API responses, check success-with-content, propagate
  cancellation tokens; debounced search; `async void` handlers wrap in try/catch with
  framework dispatch; `IAsyncDisposable` for JS handles.
- UX states everywhere: loading skeletons matching card shapes, empty states with icon
  + action, `role="alert"` error boxes with Retry, localized `aria-label`s, toasts for
  success/failure, focus-visible rings on custom chrome. Realtime: patch loaded rows
  from a single-item fetch (drop/replace/sorted-insert with dedup guards), full reload
  as fallback; dead connection = silent fallback to manual reload.

## 8. Localization

- Resource-file based (`{Strings}.resx` + culture files) consumed via a
  localizer service (`Loc["Key"]`, `Loc["Key", args]` for `{0}` formats). Never register
  the generated class itself in DI (causes runtime constructor-match failures).
- Persist culture client-side; support an explicit culture list with a default; dynamic
  culture switching on WebAssembly needs full globalization data enabled in the client
  project or it crashes on reload.
- Keep the generated `Designer.cs` in sync manually (same placement) even if nothing
  references its static properties at runtime — it's checked in.
- Localize everything user-visible: titles, labels, buttons, toasts, dialogs,
  aria-labels, empty/error states, relative times (with per-language plural/format
  variants). Leave example placeholders and raw values sent to the server as-is.
  Re-fetch/re-render on culture change.

## 9. PWA & static assets (if applicable)

- Manifest: sized `any` icons plus a transparent `maskable` icon (artwork ~66%
  centered) — Android crops non-maskable icons into solid squares. Keep platform
  requirements (e.g. opaque touch icons where required); don't touch vector favicons
  casually. Referenced files must exist with exact filenames.
- Loading screen: centered app icon + progress text, viewport-bounded.
- Service-worker update flow: toast a reload action when a worker waits; re-check on
  tab visibility; reload on controller change with a timed fallback. Keep worker
  source files in sync with each other.

## 10. Tests

- Mirror the repo's test project conventions (framework, runner, coverage collector,
  `InternalsVisibleTo` where needed). New test project = new solution entry.
- Add/update tests for changed behavior (validation, paging/sorting boundaries,
  dedup, delivery, broadcasts, lookup/cache, translation). Prefer pure-logic unit
  tests (extract logic out of components to make it testable) over rendering tests;
  stub HTTP with message-handler fakes; drive internal endpoints directly with fake
  services where no DB is needed; use the full-host factory for integration behavior.
- Beware hosted background services racing tests — drive idempotent paths to
  quiescence instead of asserting exact per-run counts. Capture assertion data
  *before* helper cleanup runs (cleanup in `finally` executes first).
