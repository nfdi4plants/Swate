import * as fsPromises from "node:fs/promises";
import { resolve } from "node:path";

const noop = () => {};
let fromWebContentsMock: ((webContents: unknown) => unknown) | undefined;
let browserWindowFactoryMock: ((options: unknown) => object) | undefined;
let showOpenDialogMock: ((...args: unknown[]) => unknown) | undefined;
let showMessageBoxMock: ((...args: unknown[]) => unknown) | undefined;

type ReaddirGate = {
    path: string;
    started: Promise<void>;
    markStarted: () => void;
    completion: Promise<void>;
    release: () => void;
};

let readdirGate: ReaddirGate | undefined;

// Electron Forge supplies these globals to the main process at build time.
Object.assign(globalThis, {
    MAIN_WINDOW_VITE_DEV_SERVER_URL: undefined,
    MAIN_WINDOW_VITE_NAME: "main_window",
    __dirname: "",
});

export const __electronMock = {
    reset: () => {
        fromWebContentsMock = undefined;
        browserWindowFactoryMock = undefined;
        showOpenDialogMock = undefined;
        showMessageBoxMock = undefined;
        readdirGate?.release();
        readdirGate = undefined;
    },
    setBrowserWindowFactory: (handler: (options: unknown) => object) => {
        browserWindowFactoryMock = handler;
    },
    setBrowserWindowFromWebContents: (handler: (webContents: unknown) => unknown) => {
        fromWebContentsMock = handler;
    },
    setShowOpenDialog: (handler: (...args: unknown[]) => unknown) => {
        showOpenDialogMock = handler;
    },
    setShowMessageBox: (handler: (...args: unknown[]) => unknown) => {
        showMessageBoxMock = handler;
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

export const app = {
    getPath: () => {
        const userDataPath = process.env.SWATE_TEST_USER_DATA;

        if (!userDataPath) {
            throw new Error("SWATE_TEST_USER_DATA is not configured.");
        }

        return userDataPath;
    },
    quit: noop,
    whenReady: () => Promise.resolve(),
    on: noop,
};

export const safeStorage = {
    isEncryptionAvailable: () => true,
    encryptString: (value: string) => Buffer.from(value, "utf8"),
    decryptString: (value: Buffer) => value.toString("utf8"),
};

export class BrowserWindow {
    constructor(options: unknown) {
        if (browserWindowFactoryMock) {
            Object.assign(this, browserWindowFactoryMock(options));
        }
    }

    static getAllWindows = () => [];
    static fromWebContents = (webContents: unknown) => fromWebContentsMock?.(webContents);
}

export const contextBridge = { exposeInMainWorld: noop };
export const dialog = {
    showOpenDialog: (...args: unknown[]) =>
        Promise.resolve(showOpenDialogMock?.(...args) ?? { canceled: true, filePaths: [] }),
    showMessageBox: (...args: unknown[]) =>
        Promise.resolve(showMessageBoxMock?.(...args) ?? { response: 0, checkboxChecked: false }),
    showErrorBox: noop,
};
export const ipcMain = { handle: noop, on: noop };
export const ipcRenderer = { invoke: noop, on: noop, removeListener: noop, send: noop };
export const screen = { getPrimaryDisplay: () => ({ workAreaSize: { width: 1280, height: 720 } }) };
export const shell = { openExternal: () => Promise.resolve() };
