// Own gesture engine for swipe-to-reveal rows. Tracking and transforms run
// entirely in the browser at full frame rate; .NET is never called per move.
// Taps are left alone until they prove to be drags: capture starts only after
// movement exceeds the tap slop, so plain taps produce untouched native clicks.
const TAP_SLOP_PX = 10;
const OPEN_FRACTION = 0.4;
const FLICK_VELOCITY_PX_MS = 0.5;
const MIN_FLICK_PX = 16;

const states = new WeakMap();

function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
}

function setOffset(content, px, animate) {
    content.style.transition = animate ? '' : 'none';
    content.style.transform = px === 0 ? '' : `translateX(${px}px)`;
}

function suppressNextClick() {
    const stop = (event) => {
        event.stopPropagation();
        event.preventDefault();
    };
    window.addEventListener('click', stop, { capture: true, once: true });
    // Hygiene: if no click follows (pointerup off-element), don't eat a later one.
    setTimeout(() => window.removeEventListener('click', stop, { capture: true }), 300);
}

export function attach(content, actionsWidth) {
    if (!content || states.has(content)) return;
    states.set(content, { width: actionsWidth, open: false, active: null });
    content.addEventListener('pointerdown', onDown);
}

export function update(content, actionsWidth) {
    const state = states.get(content);
    if (!state) return;
    state.width = actionsWidth;
    // The action set changed under an open row: snap shut.
    if (state.open) {
        state.open = false;
        setOffset(content, 0, true);
    }
}

export function reset(content) {
    const state = states.get(content);
    if (!state) return;
    state.open = false;
    setOffset(content, 0, true);
}

export function detach(content) {
    if (!content) return;
    states.delete(content);
    content.removeEventListener('pointerdown', onDown);
}

function onDown(event) {
    const content = event.currentTarget;
    const state = states.get(content);
    if (!state || state.active) return;
    if (event.pointerType === 'mouse' && event.button !== 0) return;

    const gesture = {
        startX: event.clientX,
        startY: event.clientY,
        startT: performance.now(),
        moved: false,
        moves: [{ t: performance.now(), x: event.clientX }],
        abort: new AbortController()
    };
    state.active = gesture;

    const options = { signal: gesture.abort.signal };
    window.addEventListener('pointermove', (e) => onMove(e, content, state, gesture), options);
    window.addEventListener('pointerup', (e) => onUp(e, content, state, gesture), options);
    window.addEventListener('pointercancel', () => onCancel(content, state, gesture), options);
}

function onMove(event, content, state, gesture) {
    const dx = event.clientX - gesture.startX;
    const dy = event.clientY - gesture.startY;

    if (!gesture.moved) {
        if (Math.abs(dx) < TAP_SLOP_PX && Math.abs(dy) < TAP_SLOP_PX) return;
        gesture.moved = true;
        try {
            content.setPointerCapture(event.pointerId);
        } catch {
            // Already released; the gesture still settles on pointerup.
        }
    }

    const now = performance.now();
    gesture.moves.push({ t: now, x: event.clientX });
    gesture.moves = gesture.moves.filter((m) => now - m.t < 100);

    const base = state.open ? -state.width : 0;
    setOffset(content, clamp(base + dx, -state.width, 0), false);
}

function onUp(event, content, state, gesture) {
    gesture.abort.abort();
    state.active = null;

    if (!gesture.moved) {
        // Pure tap: untouched clicks reach Blazor when closed; when open the
        // row closes instead and the click is swallowed.
        if (state.open) {
            state.open = false;
            setOffset(content, 0, true);
            suppressNextClick();
        }
        return;
    }

    const dx = event.clientX - gesture.startX;
    const moves = gesture.moves;
    let velocity = 0;
    if (moves.length > 1) {
        const first = moves[0];
        const last = moves[moves.length - 1];
        if (last.t > first.t) velocity = (last.x - first.x) / (last.t - first.t);
    }

    const offset = clamp((state.open ? -state.width : 0) + dx, -state.width, 0);
    const quickLeft = velocity < -FLICK_VELOCITY_PX_MS && dx < -MIN_FLICK_PX;
    const quickRight = velocity > FLICK_VELOCITY_PX_MS && dx > MIN_FLICK_PX;

    if (!state.open) {
        state.open = offset < -state.width * OPEN_FRACTION || quickLeft;
    } else {
        state.open = !(offset > -state.width * (1 - OPEN_FRACTION) || quickRight);
    }
    setOffset(content, state.open ? -state.width : 0, true);
}

function onCancel(content, state, gesture) {
    gesture.abort.abort();
    state.active = null;
    // Interrupted (scroll takeover, element disposal): settle where we were.
    setOffset(content, state.open ? -state.width : 0, true);
}
