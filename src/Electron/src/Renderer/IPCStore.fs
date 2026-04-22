module Renderer.IPCStore

open System.Collections.Generic
open Renderer.RendererStoreState

type IPCStore<'T>(initialValue: 'T) =
    let mutable snapshot: IPCSnapshot<'T> = {
        Value = initialValue
        Status = LoadStatus.NotRequested
    }

    let mutable nextId = 0
    let listeners = Dictionary<int, unit -> unit>()

    let notify () =
        for listener in listeners.Values |> Seq.toArray do
            listener()

    member _.GetSnapshot() = snapshot

    member _.Subscribe(listener: unit -> unit) : (unit -> unit) =
        let id = nextId
        nextId <- nextId + 1
        listeners.[id] <- listener
        fun () -> listeners.Remove(id) |> ignore

    member _.BeginRefresh() =
        match snapshot.Status with
        | LoadStatus.Loading -> ()
        | LoadStatus.NotRequested
        | LoadStatus.Ready ->
            snapshot <- { snapshot with Status = LoadStatus.Loading }
            notify ()

    member _.Publish(value: 'T) =
        snapshot <- {
            Value = value
            Status = LoadStatus.Ready
        }
        notify ()
