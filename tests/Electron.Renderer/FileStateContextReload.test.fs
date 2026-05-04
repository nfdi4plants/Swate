module ElectronRenderer.FileStateContextReloadTests

open System.Collections.Generic
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Renderer.Context.FileStateContext
open Swate.Electron.Shared.FileIOTypes
open Vitest

let private bridgeName typeName = $"FABLE_REMOTING_{typeName}"

let private setBridgeProperty name value = window?(name) <- value

let private clearBridgeProperty name =
    emitJsStatement name "delete window[$0]"

let rec private waitUntil (predicate: unit -> bool, attempts: int) =
    promise {
        if predicate () then
            return ()
        elif attempts <= 0 then
            failwith "Timed out waiting for React effect."
        else
            do! Promise.sleep 1
            return! waitUntil (predicate, attempts - 1)
    }

let private waitForEffect predicate = waitUntil (predicate, 50)

[<ReactComponent>]
let private FileTreeProbe (onFileTree: string[] -> unit) =
    let fileStateCtx = useFileStateCtx ()

    React.useEffect (
        (fun () ->
            fileStateCtx.state.FileTree
            |> Array.map _.path
            |> onFileTree),
        [| box fileStateCtx.state.FileTree |]
    )

    Html.none

let private createSnapshot () =
    let snapshot = Dictionary<string, FileEntry>()
    snapshot.Add("", FileEntry.create("arc", "", true, None))
    snapshot.Add("assays", FileEntry.create("assays", "assays", true, None))

    snapshot.Add(
        "assays/assay-1/isa.assay.xlsx",
        FileEntry.create("isa.assay.xlsx", "assays/assay-1/isa.assay.xlsx", false, None)
    )

    snapshot

Vitest.describe("FileStateContext reload hydration", fun () ->
    Vitest.test("loads the current file tree snapshot when the provider mounts", fun () ->
        promise {
            let name = bridgeName "IFileTreeRendererApi"
            let observedFileTrees = ResizeArray<string[]>()
            let mutable listenerRegistered = false
            let mutable disposeCalled = false
            let mutable snapshotLoadCalls = 0

            let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
            document.body.appendChild container |> ignore
            let root = ReactDOM.createRoot container
            let mutable rootUnmounted = false

            try
                setBridgeProperty
                    name
                    (createObj [
                        "fileTreeUpdate"
                        ==>
                            fun (_listener: Dictionary<string, FileEntry> -> unit) ->
                                listenerRegistered <- true

                                fun () -> disposeCalled <- true
                    ])

                let loadSnapshot () =
                    promise {
                        snapshotLoadCalls <- snapshotLoadCalls + 1
                        return Ok(createSnapshot ())
                    }

                root.render (
                    FileStateCtxProviderWithFileTreeSnapshot(
                        loadSnapshot,
                        FileTreeProbe(fun paths -> observedFileTrees.Add paths)
                    )
                )

                do!
                    waitForEffect (fun () ->
                        observedFileTrees
                        |> Seq.exists (Array.contains "assays/assay-1/isa.assay.xlsx"))

                Vitest.expect(listenerRegistered).toBe(true)
                Vitest.expect(snapshotLoadCalls).toBe(1)

                root.unmount ()
                rootUnmounted <- true
                do! waitForEffect (fun () -> disposeCalled)

                Vitest.expect(disposeCalled).toBe(true)
            finally
                if not rootUnmounted then
                    root.unmount ()

                container.remove ()
                clearBridgeProperty name
        })
)
