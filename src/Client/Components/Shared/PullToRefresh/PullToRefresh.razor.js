export function attach(el, instance, threshold) {
    const indicator = el.querySelector(':scope > .ptr-indicator');
    const spinner = el.querySelector(':scope > .ptr-indicator > .ptr-spinner');
    let startY = null;
    let lastDy = 0;
    let pulling = false;
    const abort = new AbortController();
    const signal = abort.signal;

    function reset() {
        startY = null;
        lastDy = 0;
        pulling = false;
        indicator.style.height = '0px';
        spinner.style.transform = '';
        el.classList.remove('ptr-pulling');
    }

    el.addEventListener('touchstart', (event) => {
        if (event.touches.length !== 1 || startY !== null) {
            return;
        }
        // Only at the very top of the page; form fields keep their own gestures.
        if (window.scrollY > 0) {
            return;
        }
        if (event.target.closest?.('input, textarea, select, [contenteditable="true"], [data-nodrag]')) {
            return;
        }
        startY = event.touches[0].clientY;
    }, { signal, passive: true });

    el.addEventListener('touchmove', (event) => {
        if (startY === null) {
            return;
        }
        const dy = event.touches[0].clientY - startY;
        if (dy <= 0 || window.scrollY > 0) {
            reset();
            return;
        }
        pulling = true;
        lastDy = dy;
        // Suppress native overscroll glow and the browser's own pull-reload.
        event.preventDefault();
        const shown = Math.min(dy, threshold * 1.6);
        indicator.style.height = `${shown}px`;
        spinner.style.transform = `rotate(${Math.round(dy * 2)}deg)`;
        el.classList.add('ptr-pulling');
    }, { signal, passive: false });

    el.addEventListener('touchend', async () => {
        if (startY === null) {
            return;
        }
        const dy = lastDy;
        const wasPulling = pulling;
        reset();
        if (wasPulling && dy >= threshold) {
            el.classList.add('ptr-refreshing');
            indicator.style.height = `${threshold}px`;
            try {
                await instance.invokeMethodAsync('OnPulled');
            } catch {
                // Circuit gone or component disposed; nothing to do.
            } finally {
                el.classList.remove('ptr-refreshing');
                el.classList.remove('ptr-pulling');
                indicator.style.height = '0px';
            }
        }
    }, { signal });

    el.addEventListener('touchcancel', reset, { signal });

    return {
        dispose() {
            abort.abort();
        }
    };
}
