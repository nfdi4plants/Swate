module ElectronCore.GitIpcProgressTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Vitest

let private electronMock: obj = import "__electronMock" "electron"

let private resetElectronMock () = electronMock?reset () |> ignore

let private setBrowserWindowFromWebContents (handler: obj -> obj) =
    electronMock?setBrowserWindowFromWebContents (handler) |> ignore

let private ipcEventWithSenderId senderId : IpcMainInvokeEvent =
    createObj [ "sender" ==> createObj [ "id" ==> senderId ] ]
    |> unbox<IpcMainInvokeEvent>

Vitest.describe (
    "Git IPC progress reporters",
    fun () ->
        Vitest.afterEach (fun () -> resetElectronMock ())

        Vitest.test (
            "clone progress can target the invoking renderer window without a loaded ARC vault",
            fun () ->
                let expectedWindow = TestHelpers.testWindow ()

                setBrowserWindowFromWebContents (fun webContents ->
                    Vitest.expect(webContents?id).toBe (37)
                    expectedWindow :> obj
                )

                let reporter =
                    Main.IPC.IGitApi.tryCreateGitProgressReporterFromIpcEvent (ipcEventWithSenderId 37)

                Vitest.expect(reporter.IsSome).toBe (true)
        )
)
