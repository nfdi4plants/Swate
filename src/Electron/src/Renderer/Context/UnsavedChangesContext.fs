module Renderer.Context.UnsavedChangesContext

open System
open Fable.Core
open Feliz

type GuardedAction = unit -> JS.Promise<unit>

type UnsavedChangesGuard = {
    Title: string
    Description: string
    SaveButtonText: string
    DiscardButtonText: string
    SavingText: string
    HasUnsavedChanges: unit -> bool
    Save: unit -> JS.Promise<Result<unit, exn>>
}

module UnsavedChangesGuard =

    let note save hasUnsavedChanges = {
        Title = "Unsaved Note"
        Description = "This note has unsaved changes. Save it before closing?"
        SaveButtonText = "Save"
        DiscardButtonText = "Don't Save"
        SavingText = "Saving..."
        HasUnsavedChanges = hasUnsavedChanges
        Save = save
    }

module UnsavedChangesSaveError =

    exception Hidden of exn

    let hide error = Hidden error

    let toModalMessage (error: exn) =
        match error with
        | Hidden _ -> None
        | _ when String.IsNullOrWhiteSpace error.Message -> None
        | _ -> Some error.Message

type UnsavedChangesController = {
    SetActiveGuard: UnsavedChangesGuard option -> unit
    RequestAction: GuardedAction -> unit
    RunWithoutGuard: GuardedAction -> JS.Promise<unit>
    SaveActiveGuard: unit -> JS.Promise<Result<unit, exn>>
}

let private defaultController = {
    SetActiveGuard = ignore
    RequestAction = fun action -> action () |> Promise.start
    RunWithoutGuard = fun action -> action ()
    SaveActiveGuard = fun () -> promise { return Ok() }
}

let UnsavedChangesCtx =
    React.createContext<UnsavedChangesController> defaultController

[<Hook>]
let useUnsavedChangesCtx () = React.useContext UnsavedChangesCtx

[<Hook>]
let useUnsavedChangesGuard (guard: UnsavedChangesGuard) =
    let unsavedChangesCtx = useUnsavedChangesCtx ()
    unsavedChangesCtx.SetActiveGuard(Some guard)
