# Smart Pantry

A local-first household pantry app. One install serves one household on its
trusted local network: everyone shares the same pantry, picks their name
from a member list (no passwords, no sign-in), and gets expiry reminders
in-app and via push notifications on opted-in devices.

There is intentionally **no authentication**. Anyone who can reach the
server can read and change everything — fine on a trusted LAN, but never
expose it to the internet as-is.

## Features

- Shared pantry with quantities, locations, expiry dates, and urgency statuses
- Local members with per-item attribution ("added by…")
- Barcode scanning (camera on Chromium, manual entry elsewhere) with
  Open Food Facts auto-fill and a local product cache
- Daily expiration check with in-app notifications and Web Push (VAPID)
- Installable PWA with controlled updates (Bswup) and offline support

## Tech stack

- .NET 10, ASP.NET Core Minimal APIs (`GeneratedEndpoints`), EF Core + Postgres
- Blazor WebAssembly Standalone (hosted, same origin), Refit clients
- Bit.Butil (browser APIs), Bit.Bswup (service worker), Bit.Brouter, BlazorBlueprint
- Tailwind CSS via the standalone CLI (checked-in `tailwind.min.css` artifact)
- xUnit + `WebApplicationFactory` against a local Postgres test database

## Getting started

Prerequisites: .NET 10 SDK, a local Postgres instance, and the Tailwind
standalone CLI (`tailwindcss`) for CSS rebuilds.

```powershell
# 1. Point the app at your database
#    (ConnectionStrings:Default in src/Server/appsettings.Development.json;
#    default database smart_pantry on localhost)

# 2. Push notifications need a VAPID pair. The public key lives in
#    appsettings.json; the private key must go to user secrets:
dotnet user-secrets set "Vapid:PrivateKey" "<your-private-key>" --project src/Server/Server.csproj

# 3. Run (EF migrations apply automatically at startup)
dotnet run --project src/Server/Server.csproj

# 4. Tests (needs Postgres with a smart_pantry_tests database;
#    user postgres / mypassword123 by default, see tests/Server.Tests/PantryApiFactory.cs)
dotnet test
```

Regenerate the CSS after changing Tailwind classes (the build does not do it):

```powershell
tailwindcss --input ./wwwroot/css/tailwind.css -o ./wwwroot/css/tailwind.min.css --minify
# run from src/Client
```

## Layout

```text
src/
├── Server/      # Minimal API endpoints, services, workers, EF Core
├── Client/      # Blazor WASM app (pages, components, PWA assets)
├── Shared/      # HTTP contracts and DTOs
tests/
└── Server.Tests/
ROADMAP.md       # phased plan with ticked progress
CHANGELOG.md     # git-cliff generated release notes
```

## Workflow

- `master` holds releases only; work happens on `develop` via `feature/*`
  and `bugfix/*` branches merged with `--no-ff`.
- Release ritual: bump `<Version>` + `service-worker.published.js`
  `cacheVersion`, `git cliff --tag <v> -o CHANGELOG.md`, commit, tag
  `v<...>`, merge `develop` into `master`.
