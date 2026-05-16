module Swate.Components.Primitive.ErrorModal.Context

open Swate.Components

open Feliz


type ErrorModalContext = {
    current: ErrorModalEntry option
    queue: ErrorModalEntry list
    enqueue: ErrorModalRequest -> unit
    enqueueMany: ErrorModalRequest list -> unit
    enqueueBatch: ErrorModalBatch -> unit
    report: string -> unit
    dismissCurrent: unit -> unit
    dismissById: string -> unit
    dismissBatchItem: string -> string -> unit
    dismissAll: unit -> unit
} with

    static member Empty = {
        current = None
        queue = []
        enqueue = ignore
        enqueueMany = ignore
        enqueueBatch = ignore
        report = ignore
        dismissCurrent = ignore
        dismissById = ignore
        dismissBatchItem = fun _ _ -> ()
        dismissAll = ignore
    }

let ErrorModalCtx = React.createContext<ErrorModalContext> (ErrorModalContext.Empty)

[<Hook>]
let useErrorModalCtx () = React.useContext ErrorModalCtx