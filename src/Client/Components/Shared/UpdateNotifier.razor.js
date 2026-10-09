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

    activate() {
        if (this.#pendingReload) {
            this.#pendingReload();
        } else {
            window.location.reload();
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

export function disposeBswupHandler() {
    bridge?.dispose();
    bridge = undefined;
}
