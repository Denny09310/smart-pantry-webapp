# Smart Pantry — Implementation Roadmap

Use this as the end-to-end implementation checklist. Check off work that is already complete; the phases are ordered by dependency, not a claim that every item is still outstanding.

## Architecture and product decisions

- `Server` — ASP.NET Core Minimal APIs, EF Core persistence, and background services.
- `Client` — Blazor WebAssembly Standalone PWA.
- `Shared` — API request/response contracts and DTOs.
- Use `AppDbContext` directly where appropriate. Keep the solution simple; no CQRS, MediatR, repository interfaces, or extra projects without a concrete need.
- **One server instance and database per household.** Do not implement multi-tenant household isolation.
- **No real authentication.** A household member is a simple profile, not a login identity: no usernames, passwords, or sign-in flow. Anyone who can reach the instance may be able to use it, so deployment access restrictions matter.
- Prefer BlazorBlueprint components and patterns for UI before building custom components.

## Phase 1 — Solution foundation and data model

- [x] Confirm the three-project solution builds and project references are correct.
- [x] Configure EF Core, `AppDbContext`, the selected database provider, and migrations.
- [x] Define the core pantry item fields: name, location, quantity, unit, and expiry date.
- [x] Define simple household member profiles (for example, display name and active/archived state).
- [x] Define transport DTOs and request/response contracts in `Shared`.
- [x] Add input validation and consistent API error handling.
- [x] Keep environment-specific settings in configuration; keep secrets out of source control.

**Done when:** a clean restore/build succeeds, migrations can create the database, and the core model is documented by code and tests.

## Phase 2 — Pantry API and persistence

- [x] Implement Minimal API endpoints to list, create, read, update, and delete pantry items.
- [x] Validate required fields, quantities, units, and expiry dates at the API boundary.
- [x] Return the shared DTOs rather than exposing EF entities directly.
- [x] Handle missing records and invalid requests consistently.
- [x] Add tests for CRUD behavior and validation.

**Done when:** pantry data can be managed through the API and the main success/error cases are covered by tests.

## Phase 3 — Core Blazor UI

- [x] Build the pantry list, item details/edit form, and add-item flow.
- [x] Build a dashboard with useful at-a-glance pantry and expiry information.
- [x] Support locations, quantities/units, and expiry dates in everyday workflows.
- [x] Add loading, empty, validation, and error states.
- [x] Make the layouts usable on phone and desktop.
- [x] Check BlazorBlueprint first; create custom UI components only when existing components do not fit.

**Done when:** a user can add, browse, edit, and remove pantry items comfortably on desktop and mobile.

## Phase 4 — Expiry status and everyday usability

- [x] Derive expiry status from the expiry date (for example, expired, expiring soon, and okay); avoid storing a value that can become stale.
- [x] Use consistent date-only semantics for expiry dates and test boundary dates.
- [x] Sort and filter the list by expiry date, status, and location where useful.
- [x] Make status labels understandable and consistent across dashboard, list, and item details.

**Done when:** the same item has a consistent status everywhere, including on date boundaries and after the date changes.

## Phase 5 — Household member profiles

- [x] Let the household create, rename, and archive simple member profiles.
- [x] Use profiles only to distinguish people in the household; do not add credentials, authentication, or authorization roles.
- [x] Where the product needs attribution (such as who an item belongs to or who recorded an action), link that data to a profile explicitly.
- [x] Define how archived profiles appear in historical records and whether they can be selected for new activity.

**Done when:** multiple people can be represented distinctly in one household instance without any login system.

## Phase 6 — In-app notifications and reminder worker

- [x] Add persistent in-app notifications and a UI for viewing them.
- [x] Implement a daily background worker that evaluates expiry dates and creates reminder notifications.
- [x] Include the agreed reminder window (initially, an expiry warning seven days ahead) and avoid duplicate reminders.
- [x] Make repeated worker runs safe and ensure a failure for one item does not stop processing all other items.
- [x] Log worker start/end, counts, and failures.

**Done when:** reminders are recorded once for the intended window, survive application restarts as in-app history, and worker failures are diagnosable.

## Phase 7 — PWA installation

- [x] Add the web app manifest, app icons, and service worker.
- [x] Configure the app to be installable and serve it over HTTPS in deployed environments.
- [x] Test installation and app launch on supported desktop and mobile browsers.
- [x] Keep offline editing/synchronization out of scope unless it becomes a separate requirement.

**Done when:** the app can be installed and launched as a PWA on the target browsers.

## Phase 8 — Browser/device push subscriptions

- [x] Request browser notification permission only after an explicit user action.
- [x] Implement Blazor JavaScript interop to create and manage push subscriptions.
- [x] Persist subscription endpoint and cryptographic keys on the server; keep the VAPID private key server-side.
- [x] Treat each subscription as belonging to a browser/device, **not an authenticated user**.
- [x] If notifications need to target a particular household member, make the device-to-profile association explicit; do not infer identity from a device.
- [x] Support unsubscribing, permission denial, missing subscriptions, and invalid/expired subscriptions.

**Done when:** a supported browser can subscribe, unsubscribe, and store its subscription safely, with denial and failure paths handled.

## Phase 9 — Barcode scanning & OpenFoodFacts integration

- [x] Add barcode scanner using the platform BarcodeDetector API (EAN-13, EAN-8, UPC-A, UPC-E, Code 128, Code 39, ITF).
- [x] Integrate OpenFoodFacts API to look up product name, brand, quantity, unit, Nutri-Score, and nutrition facts per 100g.
- [x] Persist scanned barcode, Nutri-Score, and nutrition JSON on the pantry item.
- [x] Pre-fill the item form when a barcode is recognized; allow manual override.
- [x] Fall back to local database for previously scanned items when offline or API unavailable.
- [x] Handle missing products gracefully - show info toast and let user fill manually.

**Done when:** a user can scan a product barcode and have the form pre-filled with accurate product data, with sensible fallbacks.

Keep native apps, offline synchronization, email reminders, recipes, barcode/OCR/AI, complex permissions, inventory ledgers, queues, event buses, and additional architectural layers out of scope unless requirements change.

## Recommended implementation order

1. Foundation and data model
2. Pantry API and persistence
3. Core UI
4. Expiry status
5. Member profiles
6. In-app notifications and worker
7. PWA installation
8. Push subscriptions
9. Barcode scanning & OpenFoodFacts

The goal is a straightforward, reliable household app—not a multi-tenant service or an identity platform.
