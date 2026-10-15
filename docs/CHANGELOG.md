## [1.5.1] - 2026-10-15

### 🐛 Bug Fixes

- Fresh installs now follow the browser language instead of starting English with mismatched labels
- Items count moved under the title so it can't overlap header actions on mobile

## [1.5.0] - 2026-10-15

### ✨ Features

- Swipeable pantry and notification rows with edit/used/delete actions (custom 60fps gesture engine, pointer-adaptive static rows on desktop)
- In-app changelog page linked from settings (single-sourced from this file, Markdig + Tailwind prose)
- Delete archived members (confirmation dialog; their items keep working with nulled attribution)

### 🐛 Bug Fixes

- Row taps not opening details on desktop; swipe actions stuck open after mark-as-read
- Keyboard trap in swipe rows (activation on keypress, Tab/arrows/Escape unaffected)
- Realtime double-subscribe and member double-init races; unread badge drift
- Stale list dates, loading skeletons never painting, recipe detail not reloading
- Item form losing external updates; barcode scanner start/stop failures
- Product units misparsed (`cl`/`lb` read as liters); malformed nutrition snapshots 500ing lists
- Reminder worker dying on transient DB errors; unbounded dedup scan; dead subscriptions deleted N times
- Hub broadcasts turning committed writes into 500s; `ILike` wildcard over-matching
- Member delete race and bodyless 409; mark-read mutating before 404ing; push upsert always 200
- Blank unsubscribe endpoint and blank barcode silently accepted
- Migrations crashing boot when Postgres isn't ready yet

### 🧹 Refactoring & Chores

- Shared expiry badge, sheet header, member avatars, sheet scroll class, product lookup, not-found problems
- Test seed/cleanup helpers unified; coverage for validation, nutrition, hub updates, sorting, push dedup
- 26 unused localized strings removed; doc drift, manifest metadata and PWA config corrected
- Lookup indexes on item barcodes and notification dates; translation quota capped per search

## [1.4.1] - 2026-10-14

### 🐛 Bug Fixes

- Marking an item as used now deletes it outright; consumed tracking and its filter are gone
- Details error state binds the real error instead of a literal string, so it only shows on failure
- Delete action lives inside the item card

## [1.4.0] - 2026-10-14

### ✨ Features

- Mark pantry items as used (consumed) — check-circle button on each item, hidden when consumed
- Consumed items excluded from lists by default; toggle in filter sheet to include them
- Consumption tracked with timestamp and member attribution

### 🎨 UI Improvements

- Quantity input in item form is now an invisible numeric field (no outline/background)
- Item name truncates in list so badges never wrap

## [1.3.3] - 2026-10-14

### ✨ Features

- Delete archived members (confirmation dialog; their items keep working with nulled attribution)

### 🧹 Refactoring & Chores

- Split large components (`ItemDetailsBody`, `RecipeDetailBody`) into Header/Content/Actions sub-components
- Organized folder structure: components used by one page now live under that page's subfolder
- Prettified `Program.cs` (Client & Server) with categorized service registration sections
- `LocalStorage` via Bit.Butil instead of raw JS interop
- Removed redundant `@using Client.Resources` from pages/components

## [1.3.2] - 2026-10-14

### 🐛 Bug Fixes

- Sort filter trigger showing the raw enum name instead of the localized label

### 🧹 Chores

- Consistent `Instance` naming for JS interop references across components

## [1.3.1] - 2026-10-14

### 🐛 Bug Fixes

- Sidebar scrolling with the page on desktop; it now stays pinned while only the main content scrolls
- Sort filter rendering as an inline list instead of a proper select dropdown
- Missing "Shortcuts" label string in the item form
- Recipe results not repainting after a language switch
- Realtime reconnect races leaking connections and ghost presence on rapid member switches
- Unhandled shutdown noise on hub disconnect

## [1.3.0] - 2026-10-14

### 🚀 Features

- Member presence with activity dots on avatars over the realtime hub
- Granular realtime list patching instead of full reloads
- Pull-to-refresh on the Home and Recipes lists, with body scroll lock while sheets are open
- Bottom sheet drag from the full handle row
- Recipes page with page scroll and horizontal cards on mobile
- BlazorBlueprint component chrome localized through a scoped localizer
- Client.Tests suite covering expiry, members and realtime filter mirror
- Transparent maskable PWA icon for the Android splash screen

### 🐛 Bug Fixes

- Runtime culture switch crash via full globalization data
- Recipe results trapped in a non-scrolling container
- Sidebar animating with page view transitions
- Sheet drag dead on touch from scoped touch-action CSS
- Presence dot detached from avatars in the member list
- Styled select with binding instead of the native select in filters

## [1.2.0] - 2026-10-13

### 🚀 Features

- Product details in the item sheet: Nutri-Score, nutrition facts, and ingredients from the cached lookup
- Unlink barcodes to re-associate product data
- ml unit chip

### 🐛 Bug Fixes

- Restore the rotating rainbow lookup border as a frame around the camera preview, with a lookup status line

## [1.1.1] - 2026-10-13

### 🐛 Bug Fixes

- Scan flow: no success toast on lookup hit, rainbow glow while searching, blue viewfinder guide removed
- Sanitize OpenFoodFacts data before save (single a–e Nutri-Score, 200-char names) so scanned items no longer fail validation silently

## [1.1.0] - 2026-10-13

### 🚀 Features

- Liveness and readiness health endpoints (`/health/live`, `/health/ready` with Postgres check)

### 💼 Other

- Show the app version at the bottom of the splash screen

## [1.0.1] - 2026-10-13

### 🐛 Bug Fixes

- Ship one untrimmed bit-butil.js bundle so the service-worker manifest hash matches the served file (fixes blocked script/SRI failure in production)

## [1.0.0] - 2026-10-13

### 🚀 Features

- Phase 1 foundation - members, validation, api tests
- Phase 2 pantry crud endpoints and tests
- Mobile pantry ui with custom bottom sheet
- Mobile ui refresh - custom sheet, bar and item form
- Desktop bbsidebar layout and ui polish
- Details in sheet, filter sheet, form refresh
- Phase 5 member profiles with header attribution
- Phase 6 in-app notifications and reminder worker
- Infinite scroll pagination with server sort/filter
- Added inital push notification infrastructure
- Phases 8-9 push subscriptions and delivery
- Add shared product lookup models and extend pantry item DTOs
- Add barcode, nutri-score, nutrition fields to PantryItem entity
- Add OpenFoodFacts product lookup service and endpoint
- Add client product API interface and register in ServerApi
- Add barcode scanner component using Bit.Butil BarcodeDetector and MediaDevices
- Integrate barcode scanner into item form, pre-fill from OpenFoodFacts
- Move add-item button to header, fix list reload on sheet close
- Add localization infrastructure with English and Italian resources
- Add settings page with language and user selection
- Update bottom bar with localized Settings link
- Remove Strings singleton registration from DI
- Localize Home page and ItemForm
- Native-style square bottom bar icons and centered loading screen with app icon
- *(realtime)* Typed SignalR pantry hub with client auto-reconnect
- *(server)* Cache OpenFoodFacts lookups in cached_products table
- *(recipes)* Tag-based recipe search via TheMealDB with MyMemory translation
- *(recipes)* Barcode backfill, translation hardening, quota savings, UI polish

### 🐛 Bug Fixes

- *(client)* Keep infinite-list position across parent re-renders, prefetch earlier
- Confirm dialogs open after sheet closes
- Brouter flat composition and notfound
- Sheet bodies own scroll with fixed actions
- Centerd splash screen
- Better divison
- *(server)* Map Open Food Facts snake-case payload with tolerant nutriments
- *(client)* Persist scanned barcode and nutrition on item save
- *(client)* Barcode scanner lifecycle, namespace and feedback
- *(client)* Debounce pantry search and serialize infinite-list reloads
- *(client)* Make mandatory first-member sheet non-dismissible
- *(server)* Exact location match, status all, 201 creates, blank names, reminder dedup index
- *(client)* Localize expiry labels and banners
- *(tests)* Capture hub item before helper cleanup, make reminder test race-proof
- *(server)* Drop UseExceptionHandler so binding failures return 400, not 500

### 📚 Documentation

- Update roadmap - phase 9 now barcode scanning & OpenFoodFacts
- Remove phase 10 and 11 from roadmap

### 🚜 Refactor

- *(client,server)* Add server-paged providers to infinite lists and tables; simplify like search
- Typed bottom sheet results
- *(server)* Move push sender and reminder worker under Services, VapidOptions under Models
- *(shared)* PantryChangeKinds string constants to typed enum

### 🎨 Styling

- Formatted code
- Code formatting
- *(scanner)* Floating frosted-glass close button

### 🧪 Testing

- *(server)* Pin list paging past default take

### ⚙️ Miscellaneous Tasks

- *(release)* 0.9.1
- Added apply migration on start
- Removed unusued classes
- Init roadmap
- Move projects under src, add Shared and Server.Tests projects
- AGENTS.md updated
- Phase 7 PWA installation
- Better barcode scanner UI
- Remove unused imports and whitespace cleanup
- Remove unused imports from migrations and server code
- Add generated Strings.Designer.cs and tailwind config
- Removed dashboard
- Updated settings UI
- *(server)* Sync model snapshot for dedup migration
- Move recipes page before settings
- Rebuild tailwind utilities

### 💼 Other

- Bugfix/pantry-listing into develop
- Refactor/overhaul into develop
- Develop into master for 1.0.0
## [0.9.0] - 2026-10-11

### 🚀 Features

- *(scanner)* Rotating rainbow border on the camera preview while looking up
- *(realtime)* Typed SignalR hub with live pantry refresh and broadcast tests

### 📚 Documentation

- Update README; chore(release): 0.9.0

### 💼 Other

- Feature/recipe-suggestions into develop
- Develop into master for 0.8.0
- Full pass - server hardening, validation, client correctness, a11y and docs
- Review/full-pass-fixes into develop
- Feature/scanner-rainbow-border into develop
- Feature/realtime-updates into develop
- Develop into master for 0.9.0
## [0.8.0] - 2026-10-11

### 🚀 Features

- *(recipes)* TheMealDB suggestions for expiring items
- *(recipes)* Translate Italian pantry names for TheMealDB lookup
- *(recipes)* MyMemory pipeline for non-dictionary Italian names
- Translation and ordinal comparison
- *(recipes)* Mix candidates across ingredients and isolate tests
- *(settings)* Add member switcher; chore: move pantry mobile selector to settings; style: put @code braces on new line; chore(release): 0.8.0

### 📚 Documentation

- Tick implemented recipe suggestion tasks

### 🚜 Refactor

- Removed local translations

### ⚙️ Miscellaneous Tasks

- Tailwind build
## [0.7.3] - 2026-10-11

### 🐛 Bug Fixes

- Serve one untrimmed bit-butil.js so the worker manifest hash matches

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.7.3

### 💼 Other

- Develop into master for 0.2.0
- Develop into master for 0.2.1
- Develop into master for 0.3.0
- Develop into master for 0.4.0
- Develop into master for 0.4.1
- Develop into master for 0.4.2
- Develop into master
- Develop into master for 0.4.3
- Develop into master for 0.4.4
- Develop into master for 0.5.0
- Develop into master for 0.6.0
- Develop into master for 0.6.1
- Develop into master for 0.6.2
- Develop into master for 0.6.3
- Develop into master for 0.7.0
- Develop into master for 0.7.1
- Develop into master for 0.7.2
- Bugfix/pwa-manifest-sri into develop
- Develop into master for 0.7.3
## [0.7.2] - 2026-10-11

### 🚜 Refactor

- Group notification panel booleans into PanelState

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.7.2

### 💼 Other

- Refactor/panel-state into develop
## [0.7.1] - 2026-10-11

### 🐛 Bug Fixes

- Resolve Blueprint localizer via interface, guard push on worker registration

### 🎨 Styling

- Code formatted (settings)

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.7.1

### 💼 Other

- Bugfix/push-worker-check into develop
## [0.7.0] - 2026-10-11

### 🚀 Features

- *(product-details)* OFF ingredients, nutrition and Nutri-Score in item sheet

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.7.0

### 💼 Other

- Feature/product-details into develop
## [0.6.3] - 2026-10-11

### 🐛 Bug Fixes

- Resolve notification permission before subscribing, guide on denied
- Report unsupported browsers instead of attempting subscribe

### 🎨 Styling

- Code formatted

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.6.3

### 💼 Other

- Bugfix/push-permission-flow into develop
## [0.6.2] - 2026-10-11

### 🚀 Features

- Modernize settings page with language section

### 🐛 Bug Fixes

- Single sheet scroller on create dialog
- Clear stale push subscription and retry before reporting blocked

### ⚙️ Miscellaneous Tasks

- Tailwind build
- *(release)* Version 0.6.2

### 💼 Other

- Bugfix/settings-ui into develop
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

### ⚙️ Miscellaneous Tasks

- Prune orphaned utilities from tailwind build
- *(release)* Version 0.6.1

### 💼 Other

- Bugfix/scan-feedback into develop
## [0.6.0] - 2026-10-10

### 🚀 Features

- *(localization)* Shared UIStrings resources and localized validation
- *(localization)* Structured notifications with per-language push
- *(localization)* Convert client UI to IStringLocalizer with language switcher
- *(localization)* Move language selection to settings page
- *(localization)* Settings page with BbSelect language picker

### 🐛 Bug Fixes

- Request HD capture with continuous autofocus for barcode scan
- Splash layout in plain CSS and sync theme-color via ThemeService
- *(localization)* Remove nullable annotation warnings in freshness text
- *(localization)* Drop redundant card content on settings
- Remove scroll-into-view and long-press interactions

### 📚 Documentation

- Plan TheMealDB recipe suggestions for expiring items
- Record verified TheMealDB API notes for recipe suggestions

### 🚜 Refactor

- Replace theme-color sync with ThemeInitializer

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.6.0

### 💼 Other

- Refactor/pwa-polish into develop
- Feature/localization into develop
- Bugfix/remove-scroll-reveal into develop
## [0.5.0] - 2026-10-10

### 🚜 Refactor

- Drop Bit.Bswup, register service worker via Bit.Butil
- Read connection string eagerly, seed it via environment in tests
- Remove service worker no-cache middleware
- Register service worker from index.html, updater only inspects
- Drop scroll helper script for Butil Dom scrolling

### ⚙️ Miscellaneous Tasks

- Bump test SDK packages past vulnerable versions
- *(release)* Version 0.5.0

### 💼 Other

- Refactor/remove-bswup into develop
## [0.4.4] - 2026-10-10

### 🐛 Bug Fixes

- Theme PWA shell for device light/dark and unify boot splashes

### ⚙️ Miscellaneous Tasks

- Update VAPID public key
- *(release)* Version 0.4.4

### 💼 Other

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

### ⚙️ Miscellaneous Tasks

- *(release)* Version 0.4.3

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
