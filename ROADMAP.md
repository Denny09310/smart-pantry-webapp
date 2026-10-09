# Smart Pantry — MVP Implementation Roadmap

## Goal

Build the smallest useful version of Smart Pantry:

> A user can record pantry items, where they are stored, when they expire, and see which items need attention soon.

The MVP is complete when a user can add, view, edit, delete, and filter pantry items, and receives an in-app notification when an item approaches its expiration date.

---

## Phase 1 — Solution foundation

Create the three projects:

- `SmartPantry.Server`
- `SmartPantry.Client`
- `SmartPantry.Shared`

Set up:

- ASP.NET Core server
- Blazor WebAssembly standalone client
- Shared project referenced by both
- HTTP communication between client and server
- EF Core
- Development database

Keep the solution otherwise minimal.

Do not introduce CQRS, MediatR, repository abstractions, domain frameworks, messaging infrastructure, or additional projects.

### Done when

The Blazor client can call a test endpoint on the ASP.NET Core server and receive a response using a shared DTO.

---

## Phase 2 — Pantry data model

Create the `PantryItem` entity.

Required properties:

```text
Id
Name
Quantity
Unit
Location
ExpirationDate
Notes
CreatedAt
UpdatedAt
```

Create:

- `AppDbContext`
- EF Core configuration
- Initial migration
- Database creation/update

Use a relational database.

Keep the entity simple and EF Core-friendly.

### Done when

A pantry item can be persisted and retrieved from the database.

---

## Phase 3 — Shared HTTP contracts

Create the DTOs and request models in `SmartPantry.Shared`.

Minimum contracts:

```text
PantryItemDto
CreatePantryItemRequest
UpdatePantryItemRequest
NotificationDto
```

The client should know only about these transport models, not server/domain entities.

Include the calculated expiry status in the response DTO:

```text
Fresh
ExpiringSoon
ExpiresToday
Expired
```

Do not expose EF Core entities directly over HTTP.

### Done when

The API can return a complete pantry item without leaking server implementation details.

---

## Phase 4 — Pantry API

Implement the minimal pantry endpoints:

```text
GET    /api/pantry
GET    /api/pantry/{id}
POST   /api/pantry
PUT    /api/pantry/{id}
DELETE /api/pantry/{id}
```

Support simple query/filter parameters where useful:

```text
search
location
status
```

Return validation errors using normal HTTP responses.

Do not build a generic API framework or abstraction layer.

### Done when

The complete pantry CRUD workflow works through HTTP.

---

## Phase 5 — Pantry UI

Build the minimum client screens.

### Dashboard

Show:

- Total items
- Expiring soon
- Expired
- Upcoming expirations

Include an **Add item** action.

### Pantry

Show all items with:

- Name
- Quantity
- Unit
- Location
- Expiration date
- Status

Add:

- Search
- Location filter
- Expiration filter
- Edit
- Delete
- Add item

### Item form

Fields:

- Name
- Quantity
- Unit
- Location
- Expiration date
- Notes

The same form can be used for both creating and editing.

### Done when

A user can perform the entire CRUD workflow from the browser without touching the API manually.

---

## Phase 6 — Expiration logic

Implement one simple rule:

```text
Expired
    expiration date < today

Expires today
    expiration date == today

Expiring soon
    expiration date <= today + 7 days

Fresh
    otherwise
```

Do not store `ExpiryStatus` in the database.

Calculate it when presenting the item.

Make the calculation independent of the UI.

### Done when

An item automatically moves between statuses as the current date changes.

---

## Phase 7 — Notifications

Add the minimal notification model:

```text
Notification
------------
Id
PantryItemId
Message
CreatedAt
ReadAt
```

Implement:

```text
GET  /api/notifications
POST /api/notifications/{id}/read
POST /api/notifications/read-all
```

Add a simple in-app notification area in the client.

For the MVP, notifications only need to communicate:

```text
"Milk expires tomorrow."

"Tomato sauce expires in 5 days."
```

Do not implement email, push notifications, SMS, browser notification APIs, or notification preferences.

### Done when

A user can see upcoming-expiration notifications and mark them as read.

---

## Phase 8 — Daily expiration check

Add a simple server-side background worker.

Once per day:

1. Load pantry items that are entering the warning period.
2. Determine whether a notification has already been generated.
3. Create a notification when necessary.
4. Avoid duplicates.

The worker should call a small application-level service rather than placing the entire process directly inside the background worker.

Conceptually:

```text
Background Worker
       ↓
NotificationService
       ↓
AppDbContext
```

No queues, event buses, schedulers, or external infrastructure are required for the MVP.

### Done when

A pantry item approaching expiration automatically generates an in-app notification without the client needing to be open.

---

## Phase 9 — MVP polish

Only after all functionality works, add basic quality improvements:

- Empty states
- Loading indicators
- Form validation messages
- Error handling
- Confirmation before delete
- Responsive mobile layout
- Clear expiration-status styling
- Reasonable sorting by expiration date

Do not add new product functionality during this phase.

---

# MVP Definition of Done

The MVP is finished when all of the following are true:

### Pantry

- User can add an item.
- User can edit an item.
- User can delete an item.
- User can see all items.
- User can search items.
- User can filter by location.
- User can filter by expiration status.
- Items are sorted by expiration date.

### Expiration

- Expiry status is calculated automatically.
- Expired items are clearly identified.
- Items expiring within 7 days are clearly identified.

### Notifications

- Upcoming expirations generate in-app notifications.
- Duplicate notifications are prevented.
- Notifications can be marked as read.

### Architecture

- Client contains only UI and HTTP client logic.
- Shared contains HTTP contracts.
- Server contains domain, application, infrastructure, endpoints, and background processing.
- EF Core is used directly for persistence.
- No repository abstraction is required.
- No CQRS/use-case/handler architecture is introduced.

### Out of scope

Do not implement:

- Authentication
- Multiple users
- Households
- Recipes
- Shopping lists
- Barcode scanning
- OCR
- AI
- Nutrition data
- Product databases
- External grocery integrations
- Email notifications
- Push notifications
- Analytics
- Consumption history
- Inventory transaction history
- Advanced permissions
- Mobile apps

These can be considered only after the MVP has been used and the next product requirement is known.