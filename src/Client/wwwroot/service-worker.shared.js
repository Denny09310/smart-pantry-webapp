// Shared service worker logic, imported by both service-worker.js (development)
// and service-worker.published.js (published) via importScripts().
// Keeps push handling in one place so the two workers can't drift apart.

// Web Push: the page cannot receive pushes, so the worker shows the notification.
// Chromium requires userVisibleOnly subscriptions to always show one.
self.addEventListener('push', event => {
    const body = event.data ? event.data.text() : 'Pantry update';
    event.waitUntil(self.registration.showNotification('Smart Pantry', {
        body,
        icon: 'icon-192.png',
        badge: 'icon-192.png'
    }));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(clients.openWindow('./'));
});

// The push service can rotate the endpoint; re-subscribe and tell the server,
// or pushes keep going to the old endpoint.
self.addEventListener('pushsubscriptionchange', event => {
    event.waitUntil(
        self.registration.pushManager.subscribe(event.oldSubscription.options)
            .then(subscription => {
                const json = subscription.toJSON();
                return fetch('api/push/subscriptions', {
                    method: 'POST',
                    headers: { 'content-type': 'application/json' },
                    body: JSON.stringify({
                        endpoint: subscription.endpoint,
                        p256dh: json.keys.p256dh,
                        auth: json.keys.auth
                    })
                }).catch(err => console.warn('pushsubscriptionchange sync failed:', err));
            })
    );
});
