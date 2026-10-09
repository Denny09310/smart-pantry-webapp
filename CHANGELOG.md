## [0.4.0] - 2026-10-09

### 🚀 Features

- Add health probe covering app and database
- Report worker run stats via service result records

### 📚 Documentation

- Add operations phase and recovery checklist

### 🚜 Refactor

- Move OpenFoodFacts client registration to extension

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
