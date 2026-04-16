module Renderer.MainUpdateRendererBridge

open System.Collections.Generic
open Fable.Electron.Remoting.Renderer
open Feliz
open Swate.Components.Authentication.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

/// Minimal external-store adapter for React.useSyncExternalStore.
/// Snapshots are ValueOption<'T> so "no IPC event yet" (ValueNone)
/// is distinguishable from a valid domain value.
///
/// Subscribe uses integer IDs (not ReferenceEquals) so disposal is
/// deterministic — mirrors the pattern used by the old bridge code.
type IPCStore<'T>() =
    let mutable snapshot: 'T voption = ValueNone
    let mutable nextId = 0
    let listeners = Dictionary<int, unit -> unit>()

    member _.GetSnapshot() = snapshot

    member _.Subscribe(listener: unit -> unit) : (unit -> unit) =
        let id = nextId
        nextId <- nextId + 1
        listeners.[id] <- listener
        fun () -> listeners.Remove(id) |> ignore

    member _.Update(value: 'T) =
        snapshot <- ValueSome value
        for kv in listeners do kv.Value()

// ---------------------------------------------------------------------------
// Per-channel stores + IPC handler registration
// ---------------------------------------------------------------------------

let private pathChangeStore = IPCStore<string option>()
do Remoting.init |> Remoting.buildHandler { pathChange = pathChangeStore.Update }

let private recentArcsStore = IPCStore<ARCPointer[]>()
do Remoting.init |> Remoting.buildHandler { recentARCsUpdate = recentArcsStore.Update }

let private authAccountsStore = IPCStore<AuthStateDto>()
do Remoting.init |> Remoting.buildHandler { authAccountsUpdate = authAccountsStore.Update }

let private fileTreeStore = IPCStore<Dictionary<string, FileEntry>>()
do Remoting.init |> Remoting.buildHandler { fileTreeUpdate = fileTreeStore.Update }

let private gitProgressStore = IPCStore<GitProgressDto>()
do Remoting.init |> Remoting.buildHandler { gitProgressUpdate = gitProgressStore.Update }

// ---------------------------------------------------------------------------
// Stable subscribe/getSnapshot references for useSyncExternalStore.
// Hoisted as module-level let bindings so React sees the same function
// identity across re-renders and does not re-subscribe every render.
// ---------------------------------------------------------------------------

let private pathChangeSub        = pathChangeStore.Subscribe
let private pathChangeSnap       = pathChangeStore.GetSnapshot
let private recentArcsSub        = recentArcsStore.Subscribe
let private recentArcsSnap       = recentArcsStore.GetSnapshot
let private authAccountsSub      = authAccountsStore.Subscribe
let private authAccountsSnap     = authAccountsStore.GetSnapshot
let private fileTreeSub          = fileTreeStore.Subscribe
let private fileTreeSnap         = fileTreeStore.GetSnapshot
let private gitProgressSub       = gitProgressStore.Subscribe
let private gitProgressSnap      = gitProgressStore.GetSnapshot

// ---------------------------------------------------------------------------
// Subscribe helpers — backward-compatible ('T -> unit) -> (unit -> unit)
// Wraps the store's raw Subscribe to unwrap ValueOption so typed
// subscribers only fire after a real IPC event.
// ---------------------------------------------------------------------------

let private makeSubscribe (store: IPCStore<'T>) (handler: 'T -> unit) : (unit -> unit) =
    store.Subscribe(fun () ->
        match store.GetSnapshot() with
        | ValueSome v -> handler v
        | ValueNone -> ()
    )

let subscribePathChange = makeSubscribe pathChangeStore
let subscribeRecentArcsUpdate = makeSubscribe recentArcsStore
let subscribeAuthAccountsUpdate = makeSubscribe authAccountsStore
let subscribeFileTreeUpdate = makeSubscribe fileTreeStore
let subscribeGitProgressUpdate = makeSubscribe gitProgressStore

// ---------------------------------------------------------------------------
// Per-channel React hooks (for future consumer migration)
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
let useFileTreeUpdate () : Dictionary<string, FileEntry> voption =
    React.useSyncExternalStore(fileTreeSub, UseSyncExternalStoreSnapshot(fileTreeSnap))

[<Hook>]
let useGitProgressUpdate () : GitProgressDto voption =
    React.useSyncExternalStore(gitProgressSub, UseSyncExternalStoreSnapshot(gitProgressSnap))
