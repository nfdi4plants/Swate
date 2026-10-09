type ObserverRecord = {
    callback: IntersectionObserverCallback;
    observer: MockIntersectionObserver;
};

const originalIntersectionObserver = globalThis.IntersectionObserver;
const observers = new Map<Element, ObserverRecord>();

class MockIntersectionObserver implements IntersectionObserver {
    readonly root = null;
    readonly rootMargin = "0px";
    readonly thresholds = [0];
    private target: Element | null = null;

    constructor(private readonly callback: IntersectionObserverCallback) {}

    observe(target: Element): void {
        this.target = target;
        observers.set(target, { callback: this.callback, observer: this });
    }

    unobserve(target: Element): void {
        observers.delete(target);
        if (this.target === target) this.target = null;
    }

    disconnect(): void {
        if (this.target) observers.delete(this.target);
        this.target = null;
    }

    takeRecords(): IntersectionObserverEntry[] {
        return [];
    }
}

export function installIntersectionObserver(): void {
    observers.clear();
    globalThis.IntersectionObserver = MockIntersectionObserver;
}

export function restoreIntersectionObserver(): void {
    observers.clear();
    globalThis.IntersectionObserver = originalIntersectionObserver;
}

export function triggerIntersection(testId: string, isIntersecting: boolean): void {
    const target = document.querySelector(`[data-testid="${testId}"]`);
    if (!target) throw new Error(`Missing intersection target: ${testId}`);

    const record = observers.get(target);
    if (!record) throw new Error(`Target is not observed: ${testId}`);

    record.callback(
        [{ isIntersecting, target } as IntersectionObserverEntry],
        record.observer,
    );
}
