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

## Shipped

These roadmap items are already implemented and verified — they are not
planned work:

- **Installable PWA:** web manifest, RealFaviconGenerator icons,
  Bswup service-worker engine (`service-worker.published.js`), network-only
  dev stub, branded splash + progress UI, `UpdateNotifier` toast flow.
- **Push subscription management:** `PushSubscription` entity + migration,
  `/api/push` endpoints (public key, idempotent subscribe/unsubscribe),
  `NotificationsPanel` enable/disable UI via Bit.Butil `Push`, minimal
  `push` / `notificationclick` / `pushsubscriptionchange` handlers in
  `service-worker.shared.js`, VAPID keys via user secrets (private key never
  in source control).
- **Expiration push notifications:** daily `ExpirationCheckWorker` →
  `NotificationService` → `PushService` fan-out (per-subscription error
  isolation, 404/410 pruning), in-app notification list as history,
  best-effort push that never deletes in-app records.

## Phase 1 — Local members (current work)

**Goal:** Each person in the household picks their name so activity can be
attributed ("added by…"). No passwords, no PINs, no data isolation — every
member sees and manages the same pantry.

### Tasks

- [x] Add a `Member` entity in `Server.Data.Entities` (`Id` string GUIDv7
  like the other entities, `Name` required ≤ 30 chars, optional `Color`,
  `CreatedAt`).
- [x] Add an optional `CreatedByMemberId` FK on `PantryItem`
  (`SetNull` on member delete — items survive, attribution clears).
- [x] Add the EF Core migration (new table + nullable column, no backfill).
- [x] Add member endpoints: `GET /api/members`, `POST /api/members`
  (validated name), `DELETE /api/members/{id}`.
- [x] Send the current member as `X-Member-Id` on pantry writes; resolve
  and validate it server-side for attribution only.
- [x] Add a first-run create-member dialog when zero members exist.
- [x] Add a member picker/switcher in the app header, persisted in
  `localStorage` (Bit.Butil `Storage`).
- [x] Show attribution in the pantry UI where useful.
- [x] Add tests: member CRUD + name validation, write attribution,
  delete-member-keeps-items.

### Design notes

Members are UX, not security. Never treat `X-Member-Id` as proof of
identity or use it to gate access — the server accepts it as a label and
nothing more. There is exactly one household per install, so no
`Household` / `HouseholdMember` entities, no membership checks, no query
scoping.

`Notification.ReadAt` stays global for now: one member marking a
notification read clears it for everyone. Per-member reads would need a
`NotificationRead` join table — deferred, see below.

### Acceptance criteria

- [ ] A household can create, switch between, and remove members without
  sign-in.
- [ ] All members see and manage the same pantry and notifications.
- [ ] New items record who added them; deleting a member keeps the items.
- [ ] A fresh install with no members prompts to create the first one.

## Phase 2 — Barcode-assisted item creation

**Goal:** Point the camera at a product barcode (or type it in) and have
the creation dialog pre-fill name/brand/quantity from Open Food Facts.

### Tasks

- [x] Add a server-side product lookup proxied through
  `GET /api/products/lookup?barcode=…` (validates EAN-8/12/13/UPC digits,
  calls Open Food Facts API v2 with a proper `User-Agent`, maps to
  `{ barcode, name, brand, quantity, imageUrl }`, 404 when OFF reports
  `status != 1`, tolerant timeout ~8s — lookup failure never blocks manual
  creation).
- [x] Cache lookups in a small `BarcodeProduct` table (barcode PK) so
  repeat scans resolve locally and survive offline stretches.
- [x] Add a `BarcodeScanner` dialog: `<video>` + Bit.Butil
  `MediaDevices.GetUserMedia` (`facingMode: environment`) +
  `BarcodeDetector.StartScan`, checking `IsSupported` /
  `GetSupportedFormats` first and debouncing repeat detections; dispose
  stops the scan and the stream (camera light off).
- [x] Fall back to manual barcode entry + "Look up" everywhere the native
  decoder is missing (Safari/iOS — `BarcodeDetector` is Chromium-only).
- [x] On lookup success, pre-fill `CreatePantryItemDialog` (name, brand
  into notes, OFF quantity string into notes, unit defaults to `pcs`);
  expiry stays manual — OFF has no expiry data.
- [x] Add tests: barcode validation, OFF mapping + 404 path (stubbed HTTP
  handler, no live network in tests), cache-hit behavior.

### Design notes

The lookup goes through the server rather than calling OFF from WASM:
OFF requires a meaningful `User-Agent` (browsers can't set it), the proxy
avoids CORS issues, and the cache table is what makes the "shipped
locally" part real — the full OFF dump is far too large to bundle, but a
cache of products this household actually buys stays tiny and works
offline on re-scan. Quantity parsing stays dumb on purpose: OFF's
`quantity` ("330 ml", "500 g") goes into notes, unit defaults to `pcs`.

### Acceptance criteria

- [ ] On desktop Chrome/Edge and Android Chrome, scanning a known barcode
  pre-fills the dialog.
- [ ] On Safari/iOS, manual barcode entry + lookup works; camera button is
  hidden with an explanation instead of a dead scanner.
- [ ] Unknown barcodes and OFF outages degrade to "not found, fill
  manually" without breaking the dialog.
- [ ] A previously scanned product resolves from local cache with no
  network.

## Phase 3 — Reliability and release testing

**Goal:** Verify that shared-pantry usage and push delivery are safe to rely on.

### Tasks

- [x] Test member edge cases (duplicate names, delete current member,
  write with unknown member ID).
- [x] Test duplicate-notification prevention.
- [x] Test expiration-date boundary conditions.
- [x] Test the background worker when no items qualify.
- [x] Test recovery from transient push-provider errors.
- [x] Test invalid or expired push subscriptions.
- [ ] Test behavior when push permission is denied or later revoked. (manual: browser)
- [ ] Test notification delivery in the deployed environment, not only localhost.
- [ ] Test database migration and backup/restore procedures.
- [ ] Write the recovery checklist (backup location/retention, restore-into-test-env drill, who holds secrets).
- [x] Verify logs do not expose invitation tokens (n/a), VAPID private keys,
  or push subscription secrets. (reviewed: only endpoint URLs, barcodes, counts)
- [ ] Test desktop Chrome or Edge and Android Chrome.
- [ ] Test iPhone/iPad Home Screen installation and Web Push if iOS support is in scope.

### Acceptance criteria

- [x] The background worker can encounter a delivery failure and continue processing.
- [x] Notifications are not duplicated during normal scheduled runs.
- [ ] The app remains usable if push is unsupported or disabled.
- [ ] The production deployment supports the intended devices and browsers.
- [ ] A tested backup can be restored.

## Phase 4 — Operations

**Goal:** Know the deployment is healthy and what each background run did.

### Tasks

- [ ] Add a liveness endpoint plus a Postgres readiness check; the deployment
  must report unhealthy when a critical dependency is unavailable.
- [ ] Log each expiration-worker run with start/end time, duration, items
  evaluated, notifications created, and pushes sent/failed.

### Acceptance criteria

- [ ] You can tell whether the API, database, and worker are healthy without
  reproducing a user report.
- [ ] Recent worker runs and delivery counts are inspectable from logs alone.

## Release definition of done

The release is complete when:

- [x] The client is installable as a PWA in supported browsers.
- [x] Users can enable push notifications on supported devices.
- [x] A daily server-side check creates in-app notifications for approaching expiration dates.
- [x] Push notifications are delivered to opted-in devices where supported.
- [ ] A household can manage local members without sign-in.
- [ ] Household members share and manage one pantry.
- [ ] Scanning a barcode pre-fills item creation where supported.
- [ ] Duplicate reminders are prevented.
- [ ] Core workflows and backup/restore have been tested.
- [ ] The deployment reports healthy status and unhealthy when dependencies are down.
- [ ] A written recovery checklist exists and a backup has been restored from it.

## Explicitly out of scope

Do not implement these as part of this release:

- Real authentication or authorization (ASP.NET Core Identity, OIDC, passwords, PINs).
- Multiple households per install, household creation, or invitations.
- Per-member notification read state (`NotificationRead` join — reconsider only if shared reads prove annoying).
- Email invitations or email reminders.
- Native Android or iOS applications.
- Offline editing or background synchronization.
- Shopping lists, recipes, or meal planning.
- OCR or AI features.
- Grocery-store integrations.
- Consumption history or inventory ledgers.
- Advanced role-based permissions.
- Multiple reminder schedules or complex notification preferences.
- Analytics dashboards.
- Message queues, event buses, or a separate job-processing platform unless real operational needs require them.

## Recommended implementation order

1. Local members (attribution only).
2. Barcode-assisted item creation.
3. Reliability, security, and release testing.
4. Operations (health checks, worker run logging).
