// Shared service worker behavior, imported by both service-worker.js and
// service-worker.published.js so the two never drift apart.
self.addEventListener('message', (event) => {
    if (event.data?.__butil === 'skipWaiting') {
        self.skipWaiting();
    }
});
