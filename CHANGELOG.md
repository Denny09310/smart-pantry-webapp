## [0.9.0] - 2026-10-11

### ✨ Features

- Live pantry updates across devices via typed SignalR hub
- Rotating rainbow border on the barcode scanner while looking up

### 🐛 Bug Fixes

- Review batch three, server hardening and validation (LIKE escaping, scoped notification dedup, short-lived failure caches, tolerant barcode and push validation)
- Review batch four, client correctness, localization and a11y (member load failure, culture-aware formatting, localized locations, focus ring, service worker registration file)

### 💼 Other

- Minor release 0.9.0

## [0.8.0] - 2026-10-11

### Changed

- Move member selector from Pantry screen (mobile) to Settings page; keep selector on Home.
- Format every @code block to put opening brace on a new line.

### 💼 Other

- Minor release 0.8.0

## [0.7.3] - 2026-10-11


### 🐛 Bug Fixes

- Ship one untrimmed bit-butil.js so the service worker manifest hash matches the served bundle (fixes the blocked script / SRI failure)

### 💼 Other

- Bugfix/pwa-manifest-sri into develop

## [0.7.2] - 2026-10-11

### 🔨 Refactors

- Group notification panel booleans into PanelState

### 💼 Other

- Refactor/panel-state into develop

## [0.7.1] - 2026-10-11

### 🐛 Bug Fixes

- Resolve Blueprint localizer via interface (Italian component chrome)
- Guard push subscribe on service worker registration with actionable message

### 💼 Other

- Bugfix/push-worker-check into develop

## [0.7.0] - 2026-10-11

### ✨ Features

- Product details from Open Food Facts in item sheet (ingredients, nutrition per 100 g, Nutri-Score)
- Link package barcode to already-added items via inline scanner
- Desktop info button opening the same details sheet
- Store barcode on pantry items at scan time

### 💼 Other

- Feature/product-details into develop

## [0.6.3] - 2026-10-11

### 🐛 Bug Fixes

- Resolve notification permission before subscribing, guide on denied
- Report unsupported browsers instead of attempting subscribe

### 💼 Other

- Bugfix/push-permission-flow into develop

## [0.6.2] - 2026-10-10

### 🐛 Bug Fixes

- Single sheet scroller on create dialog
- Clear stale push subscription and retry before reporting blocked

### 💼 Other

- Bugfix/sheet-and-navbar into develop
- Bugfix/push-resubscribe into develop

## [0.6.1] - 2026-10-10

### 🚀 Features

- Show app version on boot splash with release checklist guard

### 🐛 Bug Fixes

- Scan feedback with vibration and visible lookup state

### 📚 Documentation

- Require tailwind rebuild in release checklist
- Plan SignalR realtime updates phase
- Trim roadmap to remaining work only

### 💼 Other

- Prune orphaned utilities from tailwind build
- Bugfix/scan-feedback into develop

## [0.6.0] - 2026-10-10

### 🚀 Features

- Shared UIStrings resources and localized validation
- Structured notifications with per-language push
- Convert client UI to IStringLocalizer with language switcher
- Move language selection to settings page
- Settings page with BbSelect language picker

### 🐛 Bug Fixes

- Request HD capture with continuous autofocus for barcode scan
- Splash layout in plain CSS and sync theme-color via ThemeService
- Remove scroll-into-view and long-press interactions
- Remove nullable annotation warnings in freshness text
- Drop redundant card content on settings

### 🛼 Refactor

- Replace theme-color sync with ThemeInitializer

### 📚 Documentation

- Plan TheMealDB recipe suggestions for expiring items
- Record verified TheMealDB API notes for recipe suggestions

### 💼 Other

- Refactor/pwa-polish into develop
- Feature/localization into develop
- Bugfix/remove-scroll-reveal into develop

## [0.5.0] - 2026-10-10

### 🛼 Refactor

- Drop Bit.Bswup, register service worker via Bit.Butil
- Read connection string eagerly, seed it via environment in tests
- Remove service worker no-cache middleware
- Register service worker from index.html, updater only inspects
- Drop scroll helper script for Butil Dom scrolling

### 💼 Other

- Refactor/remove-bswup into develop

## [0.4.4] - 2026-10-10

### 🐛 Bug Fixes

- Theme PWA shell for device light/dark and unify boot splashes

### 💼 Other

- Update VAPID public key
- Bugfix/pwa-shell into develop

## [0.4.3] - 2026-10-09

### 🐛 Bug Fixes

- Review batch one, server hardening and validation
- Drop ignored update header, align quantity range
- Resolve config lazily for test host compatibility
- Review batch two, client correctness, a11y and cleanup

### 📚 Documentation

- Add readme with setup and workflow
- Rewrite readme as product pitch

### 💼 Other

- Bugfix/review-batch into develop
## [0.4.2] - 2026-10-09

### 🐛 Bug Fixes

- Live sheet drag follow plus scroll to newly created items
- Replace mobile virtualization with infinite scroll list

### 🚜 Refactor

- Rename IdOf to KeySelector on infinite scroll list

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.4.2

### 💼 Other

- Bugfix/mobile-list-and-sheet into develop
## [0.4.1] - 2026-10-09

### 🐛 Bug Fixes

- Never let update reload die silently, fall back to plain reload

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.4.1

### 💼 Other

- Bugfix/update-reload-fallback into develop
## [0.4.0] - 2026-10-09

### 🚀 Features

- Add health probe covering app and database
- Report worker run stats via service result records

### 📚 Documentation

- Add operations phase and recovery checklist

### 🚜 Refactor

- Move OpenFoodFacts client registration to extension

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.4.0

### 💼 Other

- Feature/operations into develop
## [0.3.0] - 2026-10-09

### 🚀 Features

- Add Open Food Facts product lookup with local cache
- Add barcode scanner with product prefill

### 🐛 Bug Fixes

- Suppress text selection and callout on long-press surface
- Render barcode scanner inline instead of nested sheet

### 🧪 Testing

- Document duplicate member names behavior
- Cover notification window boundaries and empty runs
- Prove push failure tolerance and gone-subscription pruning

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.3.0

### 💼 Other

- Bugfix/longpress-touch-css into develop
- Feature/product-lookup into develop
- Feature/barcode-scanner into develop
- Bugfix/inline-barcode-scanner into develop
- Feature/phase-3-server-tests into develop
## [0.2.1] - 2026-10-09

### 🐛 Bug Fixes

- Move member picker to mobile headers, slim add button

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.2.1

### 💼 Other

- Feature/member-mobile-header into develop
## [0.2.0] - 2026-10-09

### 🚀 Features

- Add Member entity with pantry attribution
- Added longpress actions for mobile
- Add member endpoints with tests
- Resolve X-Member-Id attribution on pantry create
- Send current member on pantry writes via MemberService
- Add member picker, switcher and first-run dialog
- Show item attribution in pantry list

### 🐛 Bug Fixes

- Isolate push fan-out failures and handle subscription errors
- Clamp pantry paging and cover search
- Hide chip set scrollbars with important utility
- Validation not propagating from shared
- Validate push unsubscribe via AsParameters dto

### 📚 Documentation

- Replace auth roadmap with local members, add barcode phase
- Tick completed member model tasks

### 🎨 Styling

- One fluent call per line in pantry lookup
- Code formatting

### 🧪 Testing

- Keep expiration worker out of the test host

### ⚙️ Miscellaneous Tasks

- Added validation
- *(release)* Version 0.2.0

### 💼 Other

- Feature/member-attribution into develop
- Feature/member-picker into develop
## [0.1.0] - 2026-10-09

### 🚀 Features

- Added ef core, added initial migration
- Added http transport dependencies
- Added pantry item migration
- Added endpoints for GET/POST of pantry items
- Added blazor blueprint ui
- Added filtering
- Added pantry item creation dialog
- Added adaptive bottom sheet for mobile
- Added pantry dashboard with navigation
- Added pantry item update endpoint and edit dialog
- Added pantry overview page
- Added delete confirmations
- Hardened pantry API and shared expiry status
- Added expiration notifications
- Paginated grids and virtualized lists
- Added bit brouter with view transitions
- Added push notifications
- Enhanced PWA experience with bit bswup
- Branded bwsup loading screen with toast updates
- Custom icons and boot progress text

### 🐛 Bug Fixes

- Table refresh, view-all action and grid background

### 🚜 Refactor

- Injected services via @inject in razor files

### 🧪 Testing

- Added server endpoint tests

### ⚙️ Miscellaneous Tasks

- Added agent skills
- Created bottom sheet component
- Added Shared contracts project
- Added raodmap, removed unnecessary using
- Moved projects into src folder
- Moved VAPID private key to user secrets, rotated pair
- Dev service worker back to network-only stub
- *(release)* Version 0.1.0
