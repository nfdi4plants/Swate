module Renderer.Components.LeftSidebar.FileExplorer.FileTreeDialogWorkflow

open Fable.Core

let run setIsBusy applyError (operation: unit -> JS.Promise<Result<unit, string>>) =
    setIsBusy true

    promise {
        match! operation () with
        | Ok() -> ()
        | Error errorMessage -> applyError errorMessage
    }
    |> Promise.catch (fun exn -> applyError exn.Message)
    |> Promise.map (fun _ -> setIsBusy false)
    |> Promise.start
