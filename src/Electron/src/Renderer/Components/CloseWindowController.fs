module Renderer.Components.CloseWindowController


open Feliz
open Fable.Core
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

type CloseWindowController =

    [<ReactComponent>]
    static member Subscription
        (
            ?onConfirmSave: unit -> JS.Promise<Result<unit, exn>>,
            ?onConfirmClose: unit -> unit,
            ?onCancelClose: unit -> unit
        ) =

        let modalIsOpen, setModalIsOpen = React.useState false
        let isBusy, setIsBusy = React.useState false
        let errorModal = useErrorModalCtx ()

        let unsavedChangesCtx =
            Renderer.Context.UnsavedChangesContext.useUnsavedChangesCtx ()

        let enqueueCloseError (title: string) (saveError: exn) =
            errorModal.enqueue (ErrorModalRequest.create (saveError.Message, title = title))

        let resolveCloseRequest (decision: SaveBeforeQuitDecision) =
            Api.ipcArcVaultApi.resolveCloseRequest decision

        let rec resolveCloseRequestWithNoteGuard decision errorTitle onHandled = promise {
            match! resolveCloseRequest decision with
            | Error resolveError -> enqueueCloseError errorTitle resolveError
            | Ok CloseRequestResolution.Handled ->
                setModalIsOpen false
                onHandled ()
            | Ok CloseRequestResolution.BlockedByUnsavedNote ->
                setModalIsOpen false
                unsavedChangesCtx.RequestAction(fun () -> resolveCloseRequestWithNoteGuard decision errorTitle onHandled)
        }

        let handleCancel () =
            if not isBusy then
                promise {
                    match! resolveCloseRequest SaveBeforeQuitDecision.CancelClose with
                    | Ok _ ->
                        setModalIsOpen false
                        onCancelClose |> Option.iter (fun fn -> fn ())
                    | Error resolveError -> enqueueCloseError "Could not cancel close request" resolveError
                }
                |> Promise.start

        let handleCloseWithoutSaving () =
            if not isBusy then
                resolveCloseRequestWithNoteGuard
                    SaveBeforeQuitDecision.CloseWithoutSaving
                    "Could not close window"
                    (fun () -> onConfirmClose |> Option.iter (fun fn -> fn ()))
                |> Promise.start

        let saveBeforeClose () : JS.Promise<Result<unit, exn>> = promise {
            match! unsavedChangesCtx.SaveActiveGuard() with
            | Error saveError -> return Error saveError
            | Ok() ->
                match onConfirmSave with
                | Some confirmSave -> return! confirmSave ()
                | None -> return Ok()
        }

        let handleSaveAndClose () =
            if not isBusy then
                promise {
                    setIsBusy true
                    let! saveResult = saveBeforeClose ()

                    match saveResult with
                    | Ok() ->
                        do!
                            resolveCloseRequestWithNoteGuard
                                SaveBeforeQuitDecision.SaveAndClose
                                "Could not save and close window"
                                ignore
                    | Error saveError -> enqueueCloseError "Save before close failed" saveError

                    setIsBusy false
                }
                |> Promise.catch (fun exn ->
                    setIsBusy false
                    enqueueCloseError "Save before close failed" exn
                )
                |> Promise.start

        let saveBeforeQuitHandler: IMainSaveBeforeQuitApi = {
            // This IPC call is triggered by the ArcVaults window close event. It should open the modal to ask user what they want to do.
            requestSaveBeforeQuit = fun () -> setModalIsOpen true
        }

        Renderer.IpcReceiver.useProxyReceiver<IMainSaveBeforeQuitApi> ((fun () -> saveBeforeQuitHandler), [||])

        Renderer.Components.Modals.UnsavedChangesModal.Main(
            isOpen = modalIsOpen,
            title = "Close Window",
            description = "Do you want to save your changes before closing this window?",
            cancel = handleCancel,
            discard = handleCloseWithoutSaving,
            save = handleSaveAndClose,
            isBusy = isBusy,
            discardButtonText = "Close",
            saveButtonText = "Save and Close",
            savingText = "Saving...",
            debug = "close-window"
        )
