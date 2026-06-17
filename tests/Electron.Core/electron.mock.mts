const noop = () => {};
let fromWebContentsMock: ((webContents: unknown) => unknown) | undefined;

export const __electronMock = {
    reset: () => {
        fromWebContentsMock = undefined;
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
    static getAllWindows = () => [];
    static fromWebContents = (webContents: unknown) => fromWebContentsMock?.(webContents);
}

export const contextBridge = { exposeInMainWorld: noop };
export const dialog = { showOpenDialog: () => Promise.resolve({ canceled: true, filePaths: [] }) };
export const ipcMain = { handle: noop, on: noop };
export const ipcRenderer = { invoke: noop, on: noop, removeListener: noop, send: noop };
export const screen = { getPrimaryDisplay: () => ({ workAreaSize: { width: 1280, height: 720 } }) };
export const shell = { openExternal: () => Promise.resolve() };
