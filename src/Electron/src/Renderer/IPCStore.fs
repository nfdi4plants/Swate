module Renderer.IPCStore

open System.Collections.Generic

/// Minimal external-store adapter for React.useSyncExternalStore.
/// Snapshots are ValueOption<'T> so "no IPC event yet" (ValueNone)
/// is distinguishable from a valid domain value.
///
/// Subscribe uses integer IDs so disposal is deterministic.
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
        // Snapshot listeners before iterating so subscribe/dispose calls from
        // within a listener do not mutate the collection mid-loop.
        for listener in listeners.Values |> Seq.toArray do
            listener()
