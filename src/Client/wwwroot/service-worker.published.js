// bit version: 10.6.2
// app version: 0.4.4 (keep in sync with the git tag and Server.csproj <Version>)

// Offline support is owned by the Bswup engine (see bit-bswup.sw.js): the
// default Blazor template handlers below are superseded by it. Keep this
// file's own settings identical to service-worker.js - the published file
// is what deployed builds actually ship.

// Cache bucket version: bumps once per release instead of on every rebuild.
self.cacheVersion = '0.4.4';

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');

// App push handling (see service-worker.shared.js).
self.importScripts('service-worker.shared.js');
