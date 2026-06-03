import path from "node:path";
import os from "node:os";

let nextWindowId = 1;

export class BrowserWindow {
    static fromWebContents(webContents: { __browserWindow?: BrowserWindow } | undefined) {
        return webContents?.__browserWindow;
    }

    static getAllWindows() {
        return [];
    }

    id: number;
    options: unknown;
    webContents: {
        __browserWindow: BrowserWindow;
        send: (..._args: unknown[]) => void;
        openDevTools: (..._args: unknown[]) => void;
    };

    constructor(options: unknown = {}) {
        this.id = nextWindowId++;
        this.options = options;
        this.webContents = {
            __browserWindow: this,
            send: () => {},
            openDevTools: () => {},
        };
    }

    loadFile() {
        return Promise.resolve();
    }

    loadURL() {
        return Promise.resolve();
    }

    on() {
        return this;
    }

    once() {
        return this;
    }

    close() {}

    destroy() {}
}

export const screen = {
    getPrimaryDisplay() {
        return {
            workAreaSize: {
                width: 1280,
                height: 800,
            },
        };
    },
};

export const app = {
    getPath(name: string) {
        return path.join(os.tmpdir(), "swate-electron-core-tests", name);
    },
    quit() {},
    whenReady() {
        return Promise.resolve();
    },
    on() {
        return app;
    },
};

export const safeStorage = {
    isEncryptionAvailable() {
        return false;
    },
    encryptString(value: string) {
        return Buffer.from(value, "utf8");
    },
    decryptString(value: Buffer) {
        return value.toString("utf8");
    },
};

export const dialog = {
    showOpenDialog() {
        return Promise.resolve({ canceled: true, filePaths: [] });
    },
};

export const shell = {
    openPath() {
        return Promise.resolve("");
    },
};

export const ipcMain = {
    handle() {},
    on() {},
    removeHandler() {},
    removeAllListeners() {},
};
