const watches = new WeakMap();

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
    if (!current) {
        return;
    }

    current.disconnect();
    watches.delete(element);
}
