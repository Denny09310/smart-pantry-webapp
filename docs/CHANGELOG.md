## [1.0.0] - 2026-10-20

### ✨ Features

- Cosmetic items with full CRUD: name, category, quantity/unit, notes, expiry date; images-free, barcode-aware product model
- Five categories (face, hair, body, hands, perfume) with icons; expiry status derived on the fly (expired / expiring soon / good), never stored
- Household member profiles (display name, color, archive) with per-item "who owns it" attribution — no login by design
- Dashboard with at-a-glance expiry overview; swipeable item and notification rows with edit/used/delete actions on mobile, sidebar layout on desktop
- Persistent in-app notifications plus a daily reminder worker (7-day warning window, idempotent per item/expiry/type)
- PWA install (manifest, icons, offline shell) with service-worker update toast flow
- Web push subscriptions with VAPID; reminder pushes best-effort per device, rejected subscriptions cleaned up
- Barcode scanning (BarcodeDetector) and Open Beauty Facts product lookup to pre-fill name, brand, quantity and unit
- Open Beauty Facts results cached locally; unknown products fall back gracefully to manual entry
- Live updates over SignalR with automatic reconnect and per-item list patching
- Realtime multi-row sync: single-item fetch patches, sorted insert, full reload fallback; item form keeps external updates visible
- Localized UI (English + Italian) with browser-language default and in-app culture switch

### 🧹 Refactoring & Chores

- BlazorBlueprint-first UI (Lucide icons, portal toasts/dialogs); custom only where the kit doesn't fit (bottom sheets, swipe rows)
- PostgreSQL with snake_case naming, migrations at startup; validated locally against the domain's date-only expiry semantics
- Shared validation contracts enforced at the API boundary (400 ProblemDetails before handlers run)
- Test suite: API integration over a real host and database, plus pure-logic unit tests for expiry buckets, filters, push dedup and lookup cache
