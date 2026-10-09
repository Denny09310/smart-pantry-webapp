// Colocated with BswupUpdateNotifier.razor. Registers the global Bswup lifecycle
// handler late, from Blazor, so index.html needs no inline handler script.
// Bswup re-resolves the handler name until it is found, so registering after
// bit-bswup.js loads is supported. The window assignment below is required by
// Bswup (it looks the handler up by name); everything else stays module-scoped.
// The staged-update reload() callback cannot cross the JS/.NET boundary, so it
// is stashed on the bridge and invoked via activateUpdate() on user accept.
const ON_UPDATE_READY = 'OnUpdateReady';

class BswupBridge {
    #dotNetRef;
    #pendingReload = null;

    constructor(dotNetRef) {
        this.#dotNetRef = dotNetRef;
    }

    start() {
        window.smartPantryBswupHandler = (type, data) => this.#onEvent(type, data);
    }

    async #onEvent(type, data) {
        if (type === 'UPDATE_READY' ||
            (type === 'DOWNLOAD_FINISHED' && data && data.firstInstall === false)) {
            this.#pendingReload = (data && typeof data.reload === 'function') ? data.reload : null;
            try {
                await this.#dotNetRef.invokeMethodAsync(ON_UPDATE_READY);
            } catch { /* circuit disconnected */ }
        }
    }

    async activate() {
        // Prefer the engine's own path: it re-reads the live registration
        // instead of trusting the worker captured when the event fired.
        let posted = false;
        try {
            if (window.BitBswup && typeof window.BitBswup.skipWaiting === 'function') {
                posted = await window.BitBswup.skipWaiting();
            }
        } catch (err) {
            console.warn('BitBswup.skipWaiting failed:', err);
        }

        // Fall back to the callback stashed from the update event.
        if (!posted && this.#pendingReload) {
            try {
                this.#pendingReload();
            } catch (err) {
                console.warn('Stashed update reload failed:', err);
            }
        }

        // Bswup reloads on controllerchange once the new worker takes over.
        // If that never comes (stuck worker), reload anyway so the click
        // always does something observable.
        let settled = false;
        const fallback = setTimeout(() => {
            if (!settled) {
                console.warn('BitBswup: no controller change after activation; reloading.');
                window.location.reload();
            }
        }, 4000);
        if (navigator.serviceWorker) {
            navigator.serviceWorker.addEventListener('controllerchange', () => {
                settled = true;
                clearTimeout(fallback);
            }, { once: true });
        }
    }

    dispose() {
        if (window.smartPantryBswupHandler) {
            window.smartPantryBswupHandler = undefined;
        }
    }
}

let bridge;

export function registerBswupHandler(dotNetRef) {
    bridge = new BswupBridge(dotNetRef);
    bridge.start();
}

export function activateUpdate() {
    if (bridge) {
        bridge.activate();
    } else {
        window.location.reload();
    }
}

export function forceReload() {
    window.location.reload();
}

export function disposeBswupHandler() {
    bridge?.dispose();
    bridge = undefined;
}
