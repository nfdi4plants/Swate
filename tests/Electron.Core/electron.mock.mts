const noop = () => {};
let fromWebContentsMock: ((webContents: unknown) => unknown) | undefined;
let browserWindowFactoryMock: ((options: unknown) => object) | undefined;
let showOpenDialogMock: ((...args: unknown[]) => unknown) | undefined;
let showMessageBoxMock: ((...args: unknown[]) => unknown) | undefined;
let fromIdMock: ((id: number) => unknown) | undefined;
let applicationMenu: Menu | undefined;

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
        fromIdMock = undefined;
        applicationMenu = undefined;
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
    setBrowserWindowFromId: (handler: (id: number) => unknown) => {
        fromIdMock = handler;
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
            const mockWindow = browserWindowFactoryMock(options);
            Object.defineProperties(this, Object.getOwnPropertyDescriptors(mockWindow));
        }
    }

    static getAllWindows = () => [];
    static fromWebContents = (webContents: unknown) => fromWebContentsMock?.(webContents);
    static fromId = (id: number) => fromIdMock?.(id);
}

type MenuOptions = {
    id?: string; label?: string; role?: string; submenu?: MenuOptions[] | Menu;
    click?: (item: MenuItem, window: unknown, event: unknown) => void;
};

export class MenuItem {
    id: string;
    label: string;
    role?: string;
    submenu?: Menu;
    enabled = true;
    private onClick?: MenuOptions['click'];
    constructor(options: MenuOptions) {
        this.id = options.id ?? '';
        this.label = options.label ?? '';
        this.role = options.role;
        this.onClick = options.click;
        this.submenu = Array.isArray(options.submenu) ? Menu.buildFromTemplate(options.submenu) : options.submenu;
    }
    click(event: unknown, window: unknown, _webContents: unknown) {
        this.onClick?.(this, window, event);
    }
}

export class Menu {
    items: MenuItem[] = [];
    static getApplicationMenu = () => applicationMenu;
    static setApplicationMenu = (menu: Menu) => { applicationMenu = menu; };
    static buildFromTemplate(template: (MenuOptions | MenuItem)[]) {
        const menu = new Menu();
        menu.items = template.map(item => item instanceof MenuItem ? item : new MenuItem(item));
        return menu;
    }
    getMenuItemById(id: string): MenuItem | undefined {
        for (const item of this.items) {
            if (item.id === id) return item;
            const nested = item.submenu?.getMenuItemById(id);
            if (nested) return nested;
        }
    }
}

export const contextBridge = { exposeInMainWorld: noop };
export const dialog = {
    showOpenDialog: (...args: unknown[]) =>
        Promise.resolve(showOpenDialogMock?.(...args) ?? { canceled: true, filePaths: [] }),
    showMessageBox: (...args: unknown[]) =>
        Promise.resolve(showMessageBoxMock?.(...args) ?? { response: 1, checkboxChecked: false }),
    showErrorBox: noop,
};
export const ipcMain = { handle: noop, on: noop };
export const ipcRenderer = { invoke: noop, on: noop, removeListener: noop, send: noop };
export const screen = { getPrimaryDisplay: () => ({ workAreaSize: { width: 1280, height: 720 } }) };
export const shell = { openExternal: () => Promise.resolve() };
