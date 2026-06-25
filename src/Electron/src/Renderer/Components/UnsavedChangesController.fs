module Renderer.Components.UnsavedChangesController

open Browser.Dom
open Browser.Types
open Fable.Core
open Feliz
open Renderer.Context.UnsavedChangesContext
open Swate.Components.ReactHooks

[<Emit("$0.returnValue = ''")>]
let private setBeforeUnloadReturnValue (_event: Event) : unit = jsNative

type UnsavedChangesControllerView = {
    Controller: UnsavedChangesController
    Modal: ReactElement
}

[<Hook>]
let useUnsavedChangesController () =
    let activeGuardRef = React.useRef<UnsavedChangesGuard option> None
    let bypassGuardDepthRef = React.useRef 0
    let allowWindowUnloadRef = React.useRef false
    let pendingAction, setPendingAction = React.useState (None: GuardedAction option)
    let isRunning, setIsRunning = React.useState false
    let saveError, setSaveError = React.useState (None: string option)

    let setActiveGuard guard = activeGuardRef.current <- guard

    let runWithoutGuard (action: GuardedAction) = promise {
        setActiveGuard None
        bypassGuardDepthRef.current <- bypassGuardDepthRef.current + 1

        try
            do! action ()
        finally
            bypassGuardDepthRef.current <- max 0 (bypassGuardDepthRef.current - 1)
    }

    let hasActiveUnsavedChanges () =
        bypassGuardDepthRef.current = 0
        && activeGuardRef.current |> Option.exists (fun guard -> guard.HasUnsavedChanges())

    let startWithoutGuard action =
        runWithoutGuard action
        |> Promise.catch (fun ex -> Browser.Dom.console.error ("Guarded action failed", ex.Message))
        |> Promise.start

    let requestAction action =
        if hasActiveUnsavedChanges () then
            setSaveError None
            setPendingAction (Some action)
        else
            startWithoutGuard action

    let clearPendingAction () =
        setPendingAction None
        setSaveError None

    let saveActiveGuard () = promise {
        match activeGuardRef.current with
        | Some guard when guard.HasUnsavedChanges() -> return! guard.Save()
        | _ -> return Ok()
    }

    let closeWindowAfterGuard () = promise {
        allowWindowUnloadRef.current <- true
        window.close ()
    }

    React.useListener.on (
        window,
        "beforeunload",
        fun (event: Event) ->
            if not allowWindowUnloadRef.current && hasActiveUnsavedChanges () then
                event.preventDefault ()
                setBeforeUnloadReturnValue event
                requestAction closeWindowAfterGuard
    )

    let controller: UnsavedChangesController =
        React.useMemo (
            (fun _ -> {
                SetActiveGuard = setActiveGuard
                RequestAction = requestAction
                RunWithoutGuard = runWithoutGuard
                SaveActiveGuard = saveActiveGuard
            }),
            [||]
        )

    let cancel () =
        if not isRunning then
            clearPendingAction ()

    let discardAndRun () =
        if not isRunning then
            match pendingAction with
            | Some action ->
                clearPendingAction ()
                startWithoutGuard action
            | None -> ()

    let saveAndRun () =
        if not isRunning then
            match pendingAction with
            | Some action ->
                promise {
                    setIsRunning true
                    setSaveError None

                    match! saveActiveGuard () with
                    | Ok() ->
                        setPendingAction None
                        do! runWithoutGuard action
                    | Error exn -> setSaveError (UnsavedChangesSaveError.toModalMessage exn)

                    setIsRunning false
                }
                |> Promise.catch (fun exn ->
                    setSaveError (Some exn.Message)
                    setIsRunning false
                )
                |> Promise.start
            | None -> ()

    let modal =
        match pendingAction, activeGuardRef.current with
        | Some _, Some guard ->
            Renderer.Components.Modals.UnsavedChangesModal.Main(
                isOpen = true,
                title = guard.Title,
                description = guard.Description,
                cancel = cancel,
                discard = discardAndRun,
                save = saveAndRun,
                isBusy = isRunning,
                ?saveError = saveError,
                discardButtonText = guard.DiscardButtonText,
                saveButtonText = guard.SaveButtonText,
                savingText = guard.SavingText,
                debug = "unsaved-changes"
            )
        | _ -> Html.none

    {
        Controller = controller
        Modal = modal
    }
