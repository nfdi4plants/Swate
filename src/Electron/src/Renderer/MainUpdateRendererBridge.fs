module Renderer.MainUpdateRendererBridge

open System.Collections.Generic
open Browser.Dom
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Feliz
open Renderer.IPCStore
open Renderer.RendererStoreState
open Swate.Components.Authentication.Types
open Swate.Components.Shared
open Swate.Electron.Shared.AuthTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

type private EventStore<'T>() =
    let mutable snapshot: 'T option = None
    let mutable nextId = 0
    let listeners = Dictionary<int, unit -> unit>()

    member _.GetSnapshot() = snapshot

    member _.Subscribe(listener: unit -> unit) : (unit -> unit) =
        let id = nextId
        nextId <- nextId + 1
        listeners.[id] <- listener
        fun () -> listeners.Remove(id) |> ignore

    member _.Update(value: 'T) =
        snapshot <- Some value

        for listener in listeners.Values |> Seq.toArray do
            listener()

let private pathChangeStore = IPCStore<string option>(None)
do Remoting.init |> Remoting.buildHandler { pathChange = pathChangeStore.Publish }

let private recentArcsStore = IPCStore<ARCPointer[]>([||])
do Remoting.init |> Remoting.buildHandler { recentARCsUpdate = recentArcsStore.Publish }

let private authAccountsStore = IPCStore<AuthStateDto>(AuthStateDto.Empty)
do Remoting.init |> Remoting.buildHandler { authAccountsUpdate = authAccountsStore.Publish }

let private fileTreeStore = IPCStore<Dictionary<string, FileEntry>>(Dictionary<string, FileEntry>())
do Remoting.init |> Remoting.buildHandler { fileTreeUpdate = fileTreeStore.Publish }

let private gitProgressStore = EventStore<GitProgressDto>()
do Remoting.init |> Remoting.buildHandler { gitProgressUpdate = gitProgressStore.Update }

let private pathChangeSub = pathChangeStore.Subscribe
let private pathChangeSnap = pathChangeStore.GetSnapshot
let private recentArcsSub = recentArcsStore.Subscribe
let private recentArcsSnap = recentArcsStore.GetSnapshot
let private authAccountsSub = authAccountsStore.Subscribe
let private authAccountsSnap = authAccountsStore.GetSnapshot
let private fileTreeSub = fileTreeStore.Subscribe
let private fileTreeSnap = fileTreeStore.GetSnapshot

let mutable private durableBridgeSyncInFlight: JS.Promise<Result<unit, exn>> option = None
let mutable private authRefreshInFlight: JS.Promise<Result<unit, exn>> option = None

let private recoverReadySnapshot (store: IPCStore<'T>) =
    let currentValue = store.GetSnapshot().Value
    store.Publish currentValue

let private isEmptyAuthState (state: AuthStateDto) =
    state.ActiveAccount.IsNone && state.StoredAccounts.Length = 0

let private shouldLogAuthRevalidationFailure (response: AuthResult) =
    match response.Success, response.FailureKind, response.User with
    | false, Some AuthFailureKind.Unauthorized, Some state when isEmptyAuthState state -> false
    | false, _, _ -> true
    | true, _, _ -> false

let private requestDurableBridgeSync () : JS.Promise<Result<unit, exn>> =
    match durableBridgeSyncInFlight with
    | Some inflight -> inflight
    | None ->
        let request =
            promise {
                try
                    return! Api.ipcRendererBridgeSyncApi.syncRendererBridgeState (unbox null)
                finally
                    durableBridgeSyncInFlight <- None
            }

        durableBridgeSyncInFlight <- Some request
        request

let private requestAuthRefresh () : JS.Promise<Result<unit, exn>> =
    match authRefreshInFlight with
    | Some inflight -> inflight
    | None ->
        let request =
            promise {
                try
                    match! Api.ipcAuthApi.revalidate () with
                    | Ok response ->
                        if shouldLogAuthRevalidationFailure response then
                            console.error (
                                "[Renderer] Auth revalidation completed with failure",
                                response.FailureKind,
                                Fable.Core.JS.JSON.stringify response.Message
                            )

                        return Ok()
                    | Error ex -> return Error ex
                finally
                    authRefreshInFlight <- None
            }

        authRefreshInFlight <- Some request
        request

[<Hook>]
let private useDurableIpcSnapshot
    (store: IPCStore<'T>)
    (subscribe: (unit -> unit) -> (unit -> unit))
    (getSnapshot: unit -> IPCSnapshot<'T>)
    (requestCurrentFromMain: unit -> JS.Promise<Result<unit, exn>>)
    : IPCSnapshot<'T> =

    let snapshot =
        React.useSyncExternalStore(subscribe, UseSyncExternalStoreSnapshot(getSnapshot))

    React.useEffectOnce (fun () ->
        match store.GetSnapshot().Status with
        | LoadStatus.NotRequested ->
            store.BeginRefresh()

            promise {
                match! requestCurrentFromMain () with
                | Ok () -> ()
                | Error ex ->
                    recoverReadySnapshot store
                    console.error ($"[Renderer] Durable IPC refresh failed: {ex.Message}")
            }
            |> Promise.start
        | LoadStatus.Loading
        | LoadStatus.Ready -> ()
    )

    snapshot

let private makeEventSubscribe (store: EventStore<'T>) (handler: 'T -> unit) : (unit -> unit) =
    store.Subscribe(fun () ->
        match store.GetSnapshot() with
        | Some value -> handler value
        | None -> ()
    )

[<Hook>]
let usePathChange () : IPCSnapshot<string option> =
    useDurableIpcSnapshot pathChangeStore pathChangeSub pathChangeSnap requestDurableBridgeSync

[<Hook>]
let useRecentArcs () : IPCSnapshot<ARCPointer[]> =
    useDurableIpcSnapshot recentArcsStore recentArcsSub recentArcsSnap requestDurableBridgeSync

[<Hook>]
let useAuthAccountsUpdate () : IPCSnapshot<AuthStateDto> =
    useDurableIpcSnapshot authAccountsStore authAccountsSub authAccountsSnap requestAuthRefresh

[<Hook>]
let useFileTreeUpdate () : IPCSnapshot<Dictionary<string, FileEntry>> =
    useDurableIpcSnapshot fileTreeStore fileTreeSub fileTreeSnap requestDurableBridgeSync

let subscribeGitProgressUpdate = makeEventSubscribe gitProgressStore
