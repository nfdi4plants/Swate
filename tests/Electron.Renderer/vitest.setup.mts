const BRIDGES = [
    "FABLE_REMOTING_IGitApi",
    "FABLE_REMOTING_IGitLabApi",
    "FABLE_REMOTING_IArcVaultsApi",
    "FABLE_REMOTING_IAuthApi",
];

const okResult = () => ({ tag: 0, fields: [undefined] });

const createFallbackBridge = () =>
    new Proxy(
        {},
        {
            get(target, prop, receiver) {
                if (typeof prop !== "string") {
                    return Reflect.get(target, prop, receiver);
                }

                const existing = Reflect.get(target, prop, receiver);
                if (existing !== undefined) {
                    return existing;
                }

                const fallbackMethod = () => Promise.resolve(okResult());
                Reflect.set(target, prop, fallbackMethod, receiver);
                return fallbackMethod;
            },
        },
    );

for (const bridgeName of BRIDGES) {
    if ((window as Record<string, unknown>)[bridgeName] === undefined) {
        (window as Record<string, unknown>)[bridgeName] = createFallbackBridge();
    }
}
