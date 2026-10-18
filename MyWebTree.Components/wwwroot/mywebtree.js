const watches = new WeakMap();
const wheels = new WeakMap();

export function size(element) {
    const rect = element.getBoundingClientRect();
    return { width: rect.width, height: rect.height };
}

export function watch(element, dotnet) {
    const current = watches.get(element);
    if (current) {
        current.disconnect();
    }

    let frame = 0;
    const observer = new ResizeObserver(() => {
        cancelAnimationFrame(frame);
        frame = requestAnimationFrame(() => dotnet.invokeMethodAsync("OnViewportResize"));
    });
    observer.observe(element);
    watches.set(element, observer);
}

export function unwatch(element) {
    const current = watches.get(element);
    if (current) {
        current.disconnect();
        watches.delete(element);
    }

    const wheel = wheels.get(element);
    if (wheel) {
        element.removeEventListener("wheel", wheel);
        wheels.delete(element);
    }
}

export function bindWheel(viewport, dotnet) {
    if (wheels.has(viewport)) {
        return;
    }

    let pending = 0;
    const onWheel = event => {
        const world = viewport.querySelector(".mwt-world");
        if (!world) {
            return;
        }

        event.preventDefault();
        const rect = viewport.getBoundingClientRect();
        const localX = event.clientX - rect.left;
        const localY = event.clientY - rect.top;
        const matrix = new DOMMatrix(getComputedStyle(world).transform);
        const current = matrix.a || 1;
        const delta = event.deltaMode === 1 ? event.deltaY * 16 : event.deltaY;
        const next = Math.min(2.2, Math.max(0.12, current * Math.exp(-delta * 0.0015)));
        const worldX = (localX - matrix.e) / current;
        const worldY = (localY - matrix.f) / current;
        const panX = localX - (worldX * next);
        const panY = localY - (worldY * next);
        world.style.transform = `translate(${panX}px, ${panY}px) scale(${next})`;
        clearTimeout(pending);
        pending = setTimeout(() => dotnet.invokeMethodAsync("ApplyView", panX, panY, next), 40);
    };

    viewport.addEventListener("wheel", onWheel, { passive: false });
    wheels.set(viewport, onWheel);
}
