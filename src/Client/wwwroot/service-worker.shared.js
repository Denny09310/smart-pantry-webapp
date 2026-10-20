// Shared service worker behavior, imported by both service-worker.js and
// service-worker.published.js so the two never drift apart.
self.addEventListener('message', (event) => {
    if (event.data?.__butil === 'skipWaiting') {
        self.skipWaiting();
    }
});

self.addEventListener('push', (event) => {
    let data = {};
    try {
        data = event.data ? event.data.json() : {};
    } catch {
        data = {};
    }

    const options = {
        body: data.body || '',
        icon: 'icon-192.png',
        badge: 'icon-192.png',
        data: { url: (data && data.url) || '/' }
    };

    event.waitUntil(self.registration.showNotification(data.title || 'Smart Cosmetics', options));
});

self.addEventListener('notificationclick', (event) => {
    event.notification.close();

    const url = (event.notification.data && event.notification.data.url) || '/';

    event.waitUntil(
        self.clients.matchAll({ type: 'window' }).then((clients) => {
            for (const client of clients) {
                if ('focus' in client) {
                    return client.focus();
                }
            }

            if (self.clients.openWindow) {
                return self.clients.openWindow(url);
            }

            return undefined;
        })
    );
});
