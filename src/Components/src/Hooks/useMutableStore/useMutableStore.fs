module Swate.Components.Hooks.UseMutableStore

open Fable.Core
open Feliz
open Swate.Components.Shared

type MutableStoreSnapshot<'T> = { value: 'T; version: int }

type MutableStore<'T>(initialValue: 'T) =
    let mutable value = initialValue
    let mutable version = 0

    let mutable snapshot = {
        value = initialValue
        version = version
    }

    let subscribers = ResizeArray<unit -> unit>()

    member private this.Notify() =
        version <- version + 1
        snapshot <- { value = value; version = version }

        for callback in subscribers do
            callback ()

    member _.GetSnapshot() = snapshot

    member _.Subscribe(callback: unit -> unit) =
        subscribers.Add callback

        fun () -> subscribers.Remove callback |> ignore

    member this.Replace(newValue: 'T) =
        if not (obj.ReferenceEquals(value, newValue)) then
            value <- newValue
            this.Notify()

    member this.Set(newValue: 'T) =
        if obj.ReferenceEquals(value, newValue) then
            this.Notify()
        else
            value <- newValue
            this.Notify()

    member this.Mutate(fn: 'T -> unit) =
        fn value
        this.Notify()

[<Hook>]
let useMutableStore (initialValue: 'T) : ('T * (('T -> unit) -> unit) * ('T -> unit) * int) =
    let storeRef = React.useRef (MutableStore(initialValue))

    let snapshot =
        React.useSyncExternalStore (
            storeRef.current.Subscribe,
            UseSyncExternalStoreSnapshot(fun () -> storeRef.current.GetSnapshot()),
            UseSyncExternalStoreSnapshot(fun () -> storeRef.current.GetSnapshot())
        )

    snapshot.value, storeRef.current.Mutate, storeRef.current.Set, snapshot.version

[<Hook>]
let useMutableArcFilesStore (arcFile: ArcFiles) : (ArcFiles * ((ArcFiles -> unit) -> unit) * (ArcFiles -> unit) * int) =
    useMutableStore arcFile
