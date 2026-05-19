module ElectronCore.ArcVaultHelperTests

open Main.ArcVaultHelper
open Vitest

Vitest.describe("ArcVaultHelper", fun () ->
    Vitest.test("file watcher polling defaults to Windows only", fun () ->
        Vitest.expect(shouldUsePollingByDefault "win32").toBe(true)
        Vitest.expect(shouldUsePollingByDefault "WIN32").toBe(true)
        Vitest.expect(shouldUsePollingByDefault "linux").toBe(false)
        Vitest.expect(shouldUsePollingByDefault "darwin").toBe(false)
    )
)
