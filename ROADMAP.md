# Smart Pantry — Development Roadmap

## Goal

A local-first pantry app for a single household. One install serves one
household on its trusted local network: everyone shares the same pantry,
picks their name from a member list (no passwords, no sign-in), and gets
expiry reminders in-app and via push notifications on opted-in devices.

There is intentionally **no authentication or authorization**. Anyone who
can reach the server on the network can read and change everything. This
is acceptable on a trusted LAN, but the app must never be exposed to the
internet as-is — that would require real auth first.

The roadmap preserves the current solution shape:

```text
src/
├── Server/
│   ├── Data/          # ApplicationDbContext, entities, migrations
│   ├── Endpoints/     # Minimal API endpoint groups (GeneratedEndpoints)
│   ├── Services/      # NotificationService, PushService
│   ├── Workers/       # ExpirationCheckWorker
│   └── Extensions/
├── Client/
│   ├── Components/    # Pages, shared components
│   ├── Services/      # Refit API clients
│   └── wwwroot/       # PWA assets, service workers, css
├── Shared/            # HTTP contracts and DTOs
tests/
└── Server.Tests/
```

Keep the architecture deliberately simple:

- ASP.NET Core Minimal APIs for HTTP endpoints.
- Blazor WebAssembly Standalone (hosted, same origin) for the client.
- EF Core and `ApplicationDbContext` for persistence (Postgres).
- `Shared` for HTTP request/response contracts and DTOs.
- Application services only where they keep business logic out of endpoints or background workers.
- No CQRS, MediatR, repository interfaces, separate Domain/Application/Infrastructure projects, or unnecessary abstractions.

## Phase 3 — Remaining verification

**Goal:** Finish the manual passes that automated tests can't cover.

### Tasks

- [ ] Test behavior when push permission is denied or later revoked. (manual: browser)
- [ ] Test notification delivery in the deployed environment, not only localhost.
- [ ] Test database migration and backup/restore procedures.
- [ ] Write the recovery checklist (backup location/retention, restore-into-test-env drill, who holds secrets).
- [ ] Test desktop Chrome or Edge and Android Chrome.
- [ ] Test iPhone/iPad Home Screen installation and Web Push if iOS support is in scope.

### Acceptance criteria

- [ ] The app remains usable if push is unsupported or disabled.
- [ ] The production deployment supports the intended devices and browsers.
- [ ] A tested backup can be restored.

## Phase 5 — Recipe suggestions (planned)

**Goal:** Turn "about to expire" into "dinner idea": suggest recipes from
TheMealDB that use up items expiring within the same 7-day window the
notifications use. Read-only inspiration — no meal planning, no shopping
lists, no saving recipes.

### Tasks

- [ ] Add a server-side recipe lookup proxied through
  `GET /api/recipes/suggestions` (takes the expiring item names, queries
  TheMealDB `filter.php?i=` per ingredient, looks up the top candidates via
  `lookup.php?i=`, ranks meals by how many expiring ingredients they cover,
  returns the top handful with name, thumbnail, and source/video links;
  tolerant timeout — lookup failure hides the section instead of breaking
  the page).
- [ ] Cache suggestion responses server-side with a ~24h TTL (recipe data is
  shared, not household-specific, so an in-memory cache is enough — no table).
- [ ] Add a "Use it up" section on the dashboard under the attention list:
  recipe cards showing the meal thumbnail, which expiring items each recipe
  covers, and a link out to the full recipe (TheMealDB meal page). Empty when
  nothing relevant is found.
- [ ] Add tests: ingredient matching/ranking, TheMealDB mapping (stubbed HTTP
  handler, no live network in tests), cache-hit behavior, failure hides the
  section.

### Verified API notes (checked 2026-10-10, free key `1`)

Base: `https://www.themealdb.com/api/json/v1/1/`. No signup needed for the
dev key; per their terms, publicly released apps should arrange production
access — this app ships local-LAN only, so the dev key stands.

- `filter.php?i={ingredient}` (single ingredient only — multi-ingredient
  filtering is premium v2) returns
  `meals: [{ idMeal, strMeal, strMealThumb }]`, or `{"meals":null}` when
  nothing matches. Ingredient names use underscores for spaces.
- Filter responses prove nothing about coverage, so each candidate needs
  `lookup.php?i={idMeal}`, which returns ingredients in `strIngredient1..20`
  paired with `strMeasure1..20` (unused slots are `""` or `null`), plus
  `strInstructions`, `strYoutube`, `strSource`, `strCategory`, `strArea`.
- Bound the fan-out: at most ~8 detail lookups per suggestion call, return
  the top 5 ranked meals. Unknown ingredients simply yield `null` and are
  skipped.
- Cards use the `{strMealThumb}/small` variant (200px); meal links go to the
  stable page `https://www.themealdb.com/meal/{idMeal}`.
- Required attribution on the section: `Recipe data and imagery: TheMealDB
  (https://www.themealdb.com/)`.
- No rate limit is documented; the small candidate cap plus the 24h cache is
  the politeness mechanism. If throttling ever appears, degrade to hiding
  the section like any other lookup failure.

### Design notes

Same shape as the Open Food Facts lookup: the free TheMealDB API needs no
key, but the lookup still goes through the server to avoid CORS issues and
to keep one cache for all household devices. Matching stays dumb on purpose:
pantry item names are matched against TheMealDB ingredient names with
case-insensitive containment, and meals are ranked purely by coverage count.
No instructions are stored or rendered — the app links out to the recipe.

### Acceptance criteria

- [ ] With expiring items in the pantry, the dashboard shows relevant recipe
  ideas naming the items they use up.
- [ ] With nothing expiring (or the API down), the section stays hidden and
  nothing else breaks.
- [ ] Recipe links open the full instructions outside the app.

## Phase 6 — Realtime updates via SignalR hub (planned)

**Goal:** When one device adds, edits, or removes an item, every other open
device updates live instead of showing stale lists until its next reload.

### Tasks

- [ ] Add a SignalR hub on the server (e.g. `/hubs/pantry`) broadcasting
  item-created, item-updated, and item-removed events from the pantry
  endpoints (create, update, delete, mark-used).
- [ ] Connect from the Blazor client
  (`Microsoft.AspNetCore.SignalR.Client`), reload the visible queries on
  broadcast, and reconnect with backoff on drops.
- [ ] Keep the current manual reload as the fallback: broadcasts are
  progressive enhancement, never required for correctness.
- [ ] Add tests: hub broadcasts on write paths (test client), no broadcast
  on validation failure.

### Design notes

Single server, trusted LAN, no auth by design — the hub inherits that: no
groups per user, no authorization, one broadcast channel for the whole
household. No backplane (one server instance only). Payloads stay tiny
(item id + kind of change); clients re-query instead of applying patches,
so the existing endpoints remain the source of truth.

### Acceptance criteria

- [ ] Adding an item on one phone makes it appear on another open device
  without manual reload.
- [ ] Removing an item disappears everywhere without manual reload.
- [ ] A dropped connection recovers and the UI converges on the next
  broadcast or reload.
- [ ] Everything still works with the hub unreachable (fallback path).

## Release definition of done

The release is complete when:

- [ ] A household can manage local members without sign-in.
- [ ] Household members share and manage one pantry.
- [ ] Scanning a barcode pre-fills item creation where supported.
- [ ] Duplicate reminders are prevented.
- [ ] Core workflows and backup/restore have been tested.
- [ ] The deployment reports healthy status and unhealthy when dependencies are down.
- [ ] A written recovery checklist exists and a backup has been restored from it.
- [ ] The Tailwind build is regenerated and committed (`tailwindcss -i ./wwwroot/css/tailwind.css -o ./wwwroot/css/tailwind.min.css --minify` from `src/Client`).

## Explicitly out of scope

Do not implement these as part of this release:

- Real authentication or authorization (ASP.NET Core Identity, OIDC, passwords, PINs).
- Multiple households per install, household creation, or invitations.
- Per-member notification read state (`NotificationRead` join — reconsider only if shared reads prove annoying).
- Email invitations or email reminders.
- Native Android or iOS applications.
- Offline editing or background synchronization.
- Shopping lists or meal planning (one-off recipe ideas from expiring items
  are planned — see Phase 5).
- OCR or AI features.
- Grocery-store integrations.
- Consumption history or inventory ledgers.
- Advanced role-based permissions.
- Multiple reminder schedules or complex notification preferences.
- Analytics dashboards.
- Message queues, event buses, or a separate job-processing platform unless real operational needs require them.

## Recommended implementation order

1. Reliability, security, and release testing (remaining manual passes).
2. Recipe suggestions for expiring items.
3. Realtime add/remove updates via SignalR hub.
