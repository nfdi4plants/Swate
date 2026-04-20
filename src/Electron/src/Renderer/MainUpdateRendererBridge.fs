module Renderer.MainUpdateRendererBridge

open Fable.Electron.Remoting.Renderer
open Feliz
open Renderer.IPCStore
open Swate.Components.Authentication.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

// ---------------------------------------------------------------------------
// Per-channel stores + IPC handler registration
// ---------------------------------------------------------------------------

let private pathChangeStore = IPCStore<string option>()

let private recentArcsStore = IPCStore<ARCPointer[]>()

let private authAccountsStore = IPCStore<AuthStateDto>()

let private fileTreeStore = IPCStore<Map<string, FileEntry>>()

let private gitProgressStore = IPCStore<GitProgressDto>()

let mutable private handlersRegistered = false

let private registerHandlersOnce () =
    if not handlersRegistered then
        Remoting.init |> Remoting.buildHandler { pathChange = pathChangeStore.Update }
        Remoting.init |> Remoting.buildHandler { recentARCsUpdate = recentArcsStore.Update }
        Remoting.init |> Remoting.buildHandler { authAccountsUpdate = authAccountsStore.Update }

        Remoting.init
        |> Remoting.buildHandler {
            fileTreeUpdate =
                fun dict ->
                    dict
                    |> Seq.map (fun kv -> kv.Key, kv.Value)
                    |> Map.ofSeq
                    |> fileTreeStore.Update
        }

        Remoting.init |> Remoting.buildHandler { gitProgressUpdate = gitProgressStore.Update }
        handlersRegistered <- true

// ---------------------------------------------------------------------------
// Stable subscribe/getSnapshot references for useSyncExternalStore.
// Hoisted as module-level let bindings so React sees the same function
// identity across re-renders and does not re-subscribe every render.
// ---------------------------------------------------------------------------

let private subscribeStore (store: IPCStore<'T>) (listener: unit -> unit) : (unit -> unit) =
    registerHandlersOnce ()
    store.Subscribe listener

let private pathChangeSub        = subscribeStore pathChangeStore
let private pathChangeSnap       = pathChangeStore.GetSnapshot
let private recentArcsSub        = subscribeStore recentArcsStore
let private recentArcsSnap       = recentArcsStore.GetSnapshot
let private authAccountsSub      = subscribeStore authAccountsStore
let private authAccountsSnap     = authAccountsStore.GetSnapshot
let private fileTreeSub          = subscribeStore fileTreeStore
let private fileTreeSnap         = fileTreeStore.GetSnapshot
let private gitProgressSub       = subscribeStore gitProgressStore
let private gitProgressSnap      = gitProgressStore.GetSnapshot

// ---------------------------------------------------------------------------
// Event subscribe helpers for Elmish dispatch bridges.
// These intentionally preserve "new IPC event only" semantics.
// Do not replace them with hook snapshots unless replay-on-mount is desired.
// ---------------------------------------------------------------------------

let private makeSubscribe (store: IPCStore<'T>) (handler: 'T -> unit) : (unit -> unit) =
    subscribeStore store (fun () ->
        match store.GetSnapshot() with
        | ValueSome v -> handler v
        | ValueNone -> ()
    )

let subscribePathChange = makeSubscribe pathChangeStore
let subscribeGitProgressUpdate = makeSubscribe gitProgressStore

// ---------------------------------------------------------------------------
// Per-channel React hooks.
// Return ValueOption<'T> — consumers must handle ValueNone.
// Uses the hoisted stable references above to avoid re-subscription.
// ---------------------------------------------------------------------------

[<Hook>]
let usePathChange () : string option voption =
    React.useSyncExternalStore(pathChangeSub, UseSyncExternalStoreSnapshot(pathChangeSnap))

[<Hook>]
let useRecentArcs () : ARCPointer[] voption =
    React.useSyncExternalStore(recentArcsSub, UseSyncExternalStoreSnapshot(recentArcsSnap))

[<Hook>]
let useAuthAccountsUpdate () : AuthStateDto voption =
    React.useSyncExternalStore(authAccountsSub, UseSyncExternalStoreSnapshot(authAccountsSnap))

[<Hook>]
let useFileTreeUpdate () : Map<string, FileEntry> voption =
    React.useSyncExternalStore(fileTreeSub, UseSyncExternalStoreSnapshot(fileTreeSnap))

[<Hook>]
let useGitProgressUpdate () : GitProgressDto voption =
    React.useSyncExternalStore(gitProgressSub, UseSyncExternalStoreSnapshot(gitProgressSnap))
