module ElectronRenderer.MainSyncedStateTests

open Browser.Dom
open Feliz
open Vitest

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
let private SyncedStateProbe
    (
        loadSnapshot: unit -> Fable.Core.JS.Promise<string>,
        subscribe: (string -> unit) -> unit -> unit,
        onObserved: string * bool -> unit
    )
    =
    let synced =
        Renderer.MainSyncedState.useMainSyncedState {
            initial = "initial"
            load = loadSnapshot
            subscribe = subscribe
            onError = ignore
            dependencies = [||]
        }

    React.useEffect (
        (fun () -> onObserved (synced.state, synced.isLoading)),
        [| box synced.state; box synced.isLoading |]
    )

    Html.none

Vitest.describe("MainSyncedState", fun () ->
    Vitest.test("subscribes before loading and does not overwrite a live update with a stale snapshot", fun () ->
        promise {
            let observed = ResizeArray<string * bool>()
            let mutable loadCalls = 0
            let mutable disposeCalls = 0
            let mutable listener: (string -> unit) option = None
            let mutable resolveLoad: (string -> unit) option = None

            let loadSnapshot () =
                promise {
                    loadCalls <- loadCalls + 1

                    return!
                        Promise.create (fun resolve _reject ->
                            resolveLoad <- Some resolve
                        )
                }

            let subscribe handler =
                listener <- Some handler

                fun () ->
                    disposeCalls <- disposeCalls + 1
                    listener <- None

            let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
            document.body.appendChild container |> ignore
            let root = ReactDOM.createRoot container
            let mutable rootUnmounted = false

            try
                root.render (SyncedStateProbe(loadSnapshot, subscribe, fun snapshot -> observed.Add snapshot))

                do! waitForEffect (fun () -> listener.IsSome && loadCalls = 1 && resolveLoad.IsSome)

                listener.Value "live-update"
                do! waitForEffect (fun () -> observed |> Seq.exists (fun (state, isLoading) -> state = "live-update" && not isLoading))

                resolveLoad.Value "stale-snapshot"
                do! Promise.sleep 0

                let latestState, latestIsLoading = observed[observed.Count - 1]
                Vitest.expect(latestState).toBe("live-update")
                Vitest.expect(latestIsLoading).toBe(false)

                root.unmount ()
                rootUnmounted <- true
                do! waitForEffect (fun () -> disposeCalls = 1)

                Vitest.expect(disposeCalls).toBe(1)
            finally
                if not rootUnmounted then
                    root.unmount ()

                container.remove ()
        })
)
