const noop = () => {};
let fromWebContentsMock: ((webContents: unknown) => unknown) | undefined;
let browserWindowFactoryMock: ((options: unknown) => object) | undefined;

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
    },
    setBrowserWindowFactory: (handler: (options: unknown) => object) => {
        browserWindowFactoryMock = handler;
    },
    setBrowserWindowFromWebContents: (handler: (webContents: unknown) => unknown) => {
        fromWebContentsMock = handler;
    },
};

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
    showOpenDialog: () => Promise.resolve({ canceled: true, filePaths: [] }),
    showErrorBox: noop,
};
export const ipcMain = { handle: noop, on: noop };
export const ipcRenderer = { invoke: noop, on: noop, removeListener: noop, send: noop };
export const screen = { getPrimaryDisplay: () => ({ workAreaSize: { width: 1280, height: 720 } }) };
export const shell = { openExternal: () => Promise.resolve() };
