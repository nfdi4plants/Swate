import { vi } from "vitest";

vi.mock("fs/promises", () => import("./fs-promises.mock.mts"));

import "./output/ArcVaultHelper.test.js";
