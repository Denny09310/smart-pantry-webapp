// BottomSheet.razor.js — colocated interop for the BottomSheet component.
// Drag handle, snap ("stop") points, pull-down-to-dismiss, scroll lock.

const ON_SHEET_EVENT = 'OnSheetEvent';
const TRANSITION_MS = 300;

// Minimum downward drag (px) that dismisses, and the fraction of the
// current snap height that also dismisses when dragged past it.
const DISMISS_OFFSET_PX = 110;
const DISMISS_OFFSET_FRACTION = 0.22;
// Downward velocity (px/ms) that flings the sheet closed.
const DISMISS_VELOCITY = 0.55;
// Velocity that biases snap selection toward the drag direction.
const SNAP_BIAS_VELOCITY = 0.25;

function viewportHeight() {
    return window.visualViewport ? window.visualViewport.height : window.innerHeight;
}

class BottomSheetController {
    #sheet;
    #overlay;
    #handle;
    #dotNetRef;
    #snapFractions;
    #snapPx = [];
    #snapIndex;
    #dismissible;
    #overlayDismiss;
    #escapeDismiss;

    #dragging = false;
    #pointerId = null;
    #startY = 0;
    #offset = 0;
    #lastY = 0;
    #lastTime = 0;
    #velocity = 0;
    #done = false;

    #onPointerDown;
    #onPointerMove;
    #onPointerUp;
    #onOverlayClick;
    #onKeyDown;
    #onResize;
    #previousBodyOverflow = '';

    constructor(id, sheet, overlay, handle, dotNetRef, options) {
        this.id = id;
        this.#sheet = sheet;
        this.#overlay = overlay;
        this.#handle = handle;
        this.#dotNetRef = dotNetRef;
        this.#snapFractions = options.snapPoints && options.snapPoints.length > 0
            ? options.snapPoints.map((s) => Math.min(1, Math.max(0.15, s)))
            : [0.5, 0.9];
        this.#snapIndex = Math.min(Math.max(0, options.initialSnap || 0), this.#snapFractions.length - 1);
        this.#dismissible = options.dismissible !== false;
        this.#overlayDismiss = options.overlayDismiss !== false;
        this.#escapeDismiss = options.escapeDismiss !== false;

        this.#onPointerDown = (e) => this.#startDrag(e);
        this.#onPointerMove = (e) => this.#moveDrag(e);
        this.#onPointerUp = (e) => this.#endDrag(e);
        this.#onOverlayClick = () => {
            if (this.#dismissible && this.#overlayDismiss) this.dismiss();
        };
        this.#onKeyDown = (e) => {
            if (e.key === 'Escape' && this.#dismissible && this.#escapeDismiss) this.dismiss();
        };
        this.#onResize = () => this.#refreshHeights();
    }

    open() {
        this.#previousBodyOverflow = document.documentElement.style.overflow;
        document.documentElement.style.overflow = 'hidden';

        this.#refreshHeights();

        if (this.#handle) this.#handle.addEventListener('pointerdown', this.#onPointerDown);
        this.#overlay.addEventListener('click', this.#onOverlayClick);
        document.addEventListener('keydown', this.#onKeyDown);
        if (window.visualViewport) window.visualViewport.addEventListener('resize', this.#onResize);
        window.addEventListener('resize', this.#onResize);

        // Start collapsed, then animate to the initial snap point.
        this.#sheet.style.transition = 'none';
        this.#sheet.style.height = '0px';
        this.#sheet.style.transform = 'translateY(0px)';
        void this.#sheet.offsetHeight;
        this.#sheet.style.transition = '';

        requestAnimationFrame(() => {
            this.#overlay.dataset.visible = 'true';
            this.#applySnap(this.#snapIndex, true);
            setTimeout(() => this.#notify('opened', this.#snapIndex), TRANSITION_MS + 50);
        });
    }

    setSnap(index, animate) {
        if (this.#done) return;
        this.#snapIndex = Math.min(Math.max(0, index), this.#snapPx.length - 1);
        this.#offset = 0;
        this.#applySnap(this.#snapIndex, animate);
        if (!animate) this.#notify('settled', this.#snapIndex);
        else setTimeout(() => this.#notify('settled', this.#snapIndex), TRANSITION_MS + 50);
    }

    dismiss() {
        if (this.#done) return;
        this.#done = true;
        this.#sheet.classList.add('bs-animating');
        this.#sheet.style.transform = 'translateY(105%)';
        delete this.#overlay.dataset.visible;
        setTimeout(async () => {
            await this.#notify('dismissed', this.#snapIndex);
            this.detach();
        }, TRANSITION_MS + 50);
    }

    detach() {
        if (this.#handle) this.#handle.removeEventListener('pointerdown', this.#onPointerDown);
        this.#overlay.removeEventListener('click', this.#onOverlayClick);
        document.removeEventListener('keydown', this.#onKeyDown);
        if (window.visualViewport) window.visualViewport.removeEventListener('resize', this.#onResize);
        window.removeEventListener('resize', this.#onResize);
        document.documentElement.style.overflow = this.#previousBodyOverflow;
        controllers.delete(this.id);
    }

    #refreshHeights() {
        const vh = viewportHeight();
        this.#snapPx = this.#snapFractions.map((f) => Math.round(f * vh));
        if (!this.#dragging && !this.#done) {
            this.#sheet.classList.remove('bs-animating');
            this.#sheet.style.height = `${this.#snapPx[this.#snapIndex]}px`;
            this.#sheet.style.transform = 'translateY(0px)';
        }
    }

    #applySnap(index, animate) {
        this.#sheet.classList.toggle('bs-animating', animate);
        this.#sheet.style.transform = 'translateY(0px)';
        this.#sheet.style.height = `${this.#snapPx[index]}px`;
    }

    #startDrag(e) {
        if (this.#done || this.#dragging) return;
        this.#dragging = true;
        this.#pointerId = e.pointerId;
        this.#startY = e.clientY;
        this.#lastY = e.clientY;
        this.#lastTime = e.timeStamp;
        this.#velocity = 0;
        this.#offset = 0;
        try { this.#handle.setPointerCapture(e.pointerId); } catch { /* noop */ }
        this.#sheet.classList.remove('bs-animating');
        this.#handle.addEventListener('pointermove', this.#onPointerMove);
        this.#handle.addEventListener('pointerup', this.#onPointerUp);
        this.#handle.addEventListener('pointercancel', this.#onPointerUp);
        e.preventDefault();
    }

    #moveDrag(e) {
        if (!this.#dragging || e.pointerId !== this.#pointerId) return;
        const dy = e.clientY - this.#lastY;
        const dt = Math.max(1, e.timeStamp - this.#lastTime);
        this.#velocity = 0.8 * this.#velocity + 0.2 * (dy / dt);

        const base = this.#snapPx[this.#snapIndex];
        const maxSnap = this.#snapPx[this.#snapPx.length - 1];
        const minOffset = base - maxSnap - 60;
        const maxOffset = base + 40;
        this.#offset = Math.min(maxOffset, Math.max(minOffset, this.#offset + dy));

        this.#sheet.style.transform = `translateY(${this.#offset}px)`;
        this.#lastY = e.clientY;
        this.#lastTime = e.timeStamp;
        e.preventDefault();
    }

    #endDrag(e) {
        if (!this.#dragging || (e.pointerId !== undefined && e.pointerId !== this.#pointerId)) return;
        this.#dragging = false;
        this.#handle.removeEventListener('pointermove', this.#onPointerMove);
        this.#handle.removeEventListener('pointerup', this.#onPointerUp);
        this.#handle.removeEventListener('pointercancel', this.#onPointerUp);

        const velocity = this.#velocity;
        const base = this.#snapPx[this.#snapIndex];
        const downFling = velocity > DISMISS_VELOCITY && this.#offset > 40;
        const draggedFar = this.#offset > Math.max(DISMISS_OFFSET_PX, base * DISMISS_OFFSET_FRACTION);

        if (this.#dismissible && (downFling || draggedFar)) {
            this.dismiss();
            return;
        }

        const currentH = base - this.#offset;
        let target = this.#nearestSnap(currentH);
        if (velocity < -SNAP_BIAS_VELOCITY) target = this.#ceilSnap(currentH);
        else if (velocity > SNAP_BIAS_VELOCITY) target = this.#floorSnap(currentH);

        this.#snapIndex = target;
        this.#offset = 0;
        this.#applySnap(target, true);
        setTimeout(() => this.#notify('settled', target), TRANSITION_MS + 50);
    }

    #nearestSnap(h) {
        let best = 0;
        for (let i = 1; i < this.#snapPx.length; i++) {
            if (Math.abs(this.#snapPx[i] - h) < Math.abs(this.#snapPx[best] - h)) best = i;
        }
        return best;
    }

    #ceilSnap(h) {
        for (let i = 0; i < this.#snapPx.length; i++) {
            if (this.#snapPx[i] >= h - 1) return i;
        }
        return this.#snapPx.length - 1;
    }

    #floorSnap(h) {
        for (let i = this.#snapPx.length - 1; i >= 0; i--) {
            if (this.#snapPx[i] <= h + 1) return i;
        }
        return 0;
    }

    async #notify(type, snapIndex) {
        try {
            await this.#dotNetRef.invokeMethodAsync(ON_SHEET_EVENT, type, snapIndex);
        } catch { /* circuit disconnected */ }
    }
}

const controllers = new Map();

export function attach(id, sheet, overlay, handle, dotNetRef, options) {
    detach(id);
    const controller = new BottomSheetController(id, sheet, overlay, handle, dotNetRef, options || {});
    controllers.set(id, controller);
    controller.open();
}

export function setSnap(id, index, animate) {
    controllers.get(id)?.setSnap(index, animate);
}

export function dismiss(id) {
    controllers.get(id)?.dismiss();
}

export function detach(id) {
    controllers.get(id)?.detach();
}
