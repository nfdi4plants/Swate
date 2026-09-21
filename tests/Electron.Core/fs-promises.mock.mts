import * as fsPromises from "node:fs/promises";
import { resolve } from "node:path";

type ReaddirGate = {
    path: string;
    started: Promise<void>;
    markStarted: () => void;
    completion: Promise<void>;
    release: () => void;
};

let readdirGate: ReaddirGate | undefined;

export const __fsPromisesMock = {
    reset: () => {
        readdirGate?.release();
        readdirGate = undefined;
    },
    blockNextReaddir: (path: string) => {
        let released = false;
        let markStarted = () => {};
        let release = () => {};
        const started = new Promise<void>((resolveStarted) => {
            markStarted = resolveStarted;
        });
        const completion = new Promise<void>((resolveCompletion) => {
            release = resolveCompletion;
        });

        readdirGate = { path, started, markStarted, completion, release };
        return {
            started,
            release: () => {
                released = true;
                release();
            },
            isReleased: () => released,
        };
    },
};

export const readdir = async (...args: Parameters<typeof fsPromises.readdir>) => {
    const gate = readdirGate;

    if (gate && resolve(args[0].toString()) === resolve(gate.path)) {
        readdirGate = undefined;
        gate.markStarted();
        await gate.completion;
    }

    return fsPromises.readdir(...args);
};

export * from "node:fs/promises";
