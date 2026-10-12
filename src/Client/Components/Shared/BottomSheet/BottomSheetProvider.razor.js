const ON_CLOSED = 'NotifyClosed';
const ON_ESCAPE = 'NotifyEscape';

const CLOSE_VELOCITY = 0.55; // px per ms, downward
const CLOSE_DISTANCE_RATIO = 0.22;
const CLOSE_MIN_PX = 110;
const TRANSITION_TIMEOUT = 400; // ms fallback if transitionend never fires

function afterTransition(element) {
    return new Promise((resolve) => {
        const done = (event) => {
            if (event.target === element) {
                element.removeEventListener('transitionend', done);
                resolve();
            }
        };
        element.addEventListener('transitionend', done);
        setTimeout(resolve, TRANSITION_TIMEOUT);
    });
}

class Sheet {
    #inner;
    #handle;
    #dotNet;
    #snaps;
    #index;
    #drag = null;
    #moves = [];
    #abort = null;

    constructor(inner, handle, dotNet, snapPoints, initialIndex) {
        this.#inner = inner;
        this.#handle = handle;
        this.#dotNet = dotNet;
        this.#snaps = [...snapPoints]
            .map(Number)
            .filter((f) => Number.isFinite(f) && f > 0 && f <= 1)
            .sort((a, b) => a - b);
        if (this.#snaps.length === 0) {
            this.#snaps = [0.9];
        }
        this.#index = Math.min(Math.max(initialIndex ?? 0, 0), this.#snaps.length - 1);
    }

    async enter() {
        this.#attach();
        this.#setHeight(this.#height(this.#index));
        // Force reflow so the reveal animates, then clear any previous
        // transform (a reopen can reuse the element mid-close) and the
        // pre-hidden class.
        void this.#inner.offsetHeight;
        this.#inner.style.transform = '';
        this.#inner.classList.remove('sheet-pre');
    }

    async close() {
        this.#inner.classList.remove('sheet-drag');
        this.#inner.style.transform = 'translateY(105%)';
        await afterTransition(this.#inner);
        this.teardown();
    }

    snapTo(index) {
        this.#index = index;
        this.#inner.style.transform = '';
        this.#setHeight(this.#height(index));
    }

    teardown() {
        this.#abort?.abort();
        this.#abort = null;
        if (active === this) {
            active = null;
        }
    }

    #height(index) {
        return Math.round(window.innerHeight * this.#snaps[index]);
    }

    #setHeight(px) {
        this.#inner.style.setProperty('--sheet-h', `${px}px`);
    }

    #attach() {
        this.#abort = new AbortController();
        const options = { signal: this.#abort.signal };
        this.#handle.addEventListener('pointerdown', (event) => this.#onDown(event), options);
        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                this.#notify(ON_ESCAPE);
            }
        }, options);
        window.addEventListener('resize', () => {
            this.#inner.style.transform = '';
            this.#setHeight(this.#height(this.#index));
        }, options);
    }

    #onDown(event) {
        if (event.button !== undefined && event.button !== 0) {
            return;
        }
        event.preventDefault();
        this.#handle.setPointerCapture(event.pointerId);
        this.#drag = { startY: event.clientY };
        this.#moves = [{ t: performance.now(), y: event.clientY }];
        this.#inner.classList.add('sheet-drag');

        const move = (moveEvent) => this.#onMove(moveEvent);
        const up = (upEvent) => {
            this.#handle.removeEventListener('pointermove', move);
            this.#handle.removeEventListener('pointerup', up);
            this.#handle.removeEventListener('pointercancel', up);
            this.#onUp(upEvent);
        };
        this.#handle.addEventListener('pointermove', move);
        this.#handle.addEventListener('pointerup', up);
        this.#handle.addEventListener('pointercancel', up);
    }

    #onMove(event) {
        if (!this.#drag) {
            return;
        }
        const dy = event.clientY - this.#drag.startY;
        // Resist upward overshoot; downward motion is free (close direction).
        const ty = dy < 0 ? dy * 0.25 : dy;
        this.#inner.style.transform = `translateY(${Math.max(ty, -48)}px)`;

        const now = performance.now();
        this.#moves.push({ t: now, y: event.clientY });
        this.#moves = this.#moves.filter((m) => now - m.t < 120);
    }

    async #onUp(event) {
        if (!this.#drag) {
            return;
        }
        const dy = event.clientY - this.#drag.startY;
        this.#drag = null;
        this.#inner.classList.remove('sheet-drag');

        const moves = this.#moves;
        let velocity = 0;
        if (moves.length > 1) {
            const first = moves[0];
            const last = moves[moves.length - 1];
            if (last.t > first.t) {
                velocity = (last.y - first.y) / (last.t - first.t);
            }
        }

        const height = this.#height(this.#index);
        const closeDistance = Math.max(CLOSE_MIN_PX, height * CLOSE_DISTANCE_RATIO);
        if (dy > closeDistance || velocity > CLOSE_VELOCITY) {
            await this.close();
            this.#notify(ON_CLOSED);
            return;
        }

        // Settle to the nearest snap point.
        const visible = height - dy;
        let best = 0;
        for (let i = 1; i < this.#snaps.length; i++) {
            if (Math.abs(this.#height(i) - visible) < Math.abs(this.#height(best) - visible)) {
                best = i;
            }
        }
        this.snapTo(best);
    }

    async #notify(method, arg) {
        try {
            if (arg === undefined) {
                await this.#dotNet.invokeMethodAsync(method);
            } else {
                await this.#dotNet.invokeMethodAsync(method, arg);
            }
        } catch {
            // Circuit gone or component disposed; nothing to do.
        }
    }
}

let active = null;

export function show(inner, handle, dotNet, snapPoints, initialSnapIndex) {
    active?.teardown();
    active = new Sheet(inner, handle, dotNet, snapPoints, initialSnapIndex ?? 0);
    active.enter();
}

export function close() {
    return active?.close() ?? Promise.resolve();
}

export function dispose() {
    active?.teardown();
    active = null;
}
