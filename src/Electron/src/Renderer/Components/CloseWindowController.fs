module Renderer.Components.CloseWindowController


open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Swate.Components
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
        let pageCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
        let previewStateCtx = Renderer.Context.PreviewStateContext.usePreviewStateCtx ()

        let saveBeforeClose () : JS.Promise<Result<unit, exn>> = promise {
            let saveTarget =
                match previewStateCtx.state.PendingArcFileSave with
                | Some pendingArcFile -> Some pendingArcFile
                | None ->
                    match pageCtx.state with
                    | Some(Renderer.Types.PageState.ArcFilePage arcFile) -> Some arcFile
                    | _ -> None

            match saveTarget with
            | Some arcFile ->
                let! saveResult = Renderer.Components.MainContent.Helper.MainContentHelper.saveArcFile arcFile

                match saveResult with
                | Ok() ->
                    previewStateCtx.setPendingArcFileSave None
                    return Ok()
                | Error exn -> return Error exn
            | None -> return Ok()
        }

        let resolveCloseRequest (decision: SaveBeforeQuitDecision) =
            Api.ipcArcVaultApi.resolveCloseRequest (unbox null) decision
            |> Promise.map (
                function
                | Ok _ -> ()
                | Error exn -> console.error ($"Failed to resolve close request: {exn.Message}")
            )

        let handleCancel () =
            setModalIsOpen false
            onCancelClose |> Option.iter (fun fn -> fn ())
            resolveCloseRequest SaveBeforeQuitDecision.CancelClose |> Promise.start

        let handleCloseWithoutSaving () =
            setModalIsOpen false
            onConfirmClose |> Option.iter (fun fn -> fn ())
            resolveCloseRequest SaveBeforeQuitDecision.CloseWithoutSaving |> Promise.start

        let handleSaveAndClose () =
            promise {
                let! saveResult =
                    if onConfirmSave.IsNone then
                        // If no custom save function is provided, we use the default one that saves the current arc file.
                        saveBeforeClose ()
                    else
                        onConfirmSave.Value()

                match saveResult with
                | Ok() ->
                    setModalIsOpen false
                    do! resolveCloseRequest SaveBeforeQuitDecision.SaveAndClose
                | Error msg -> console.error ($"Save before close failed: {msg}")
            }
            |> Promise.start

        let saveBeforeQuitHandler: IMainSaveBeforeQuitApi = {
            // This IPC call is triggered by the ArcVaults window close event. It should open the modal to ask user what they want to do.
            requestSaveBeforeQuit = fun () -> setModalIsOpen true
        }

        React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler saveBeforeQuitHandler)

        BaseModal.Modal(
            isOpen = modalIsOpen,
            setIsOpen =
                (fun isOpen ->
                    if not isOpen then
                        handleCancel ()
                ),
            header = Html.text "Close Window",
            description = Html.text "Do you want to save your changes before closing this window?",
            children = Html.none,
            footer =
                Html.div [
                    prop.className "swt:flex swt:gap-2 swt:w-full"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-neutral"
                            prop.text "Cancel"
                            prop.onClick (fun _ -> handleCancel ())
                        ]
                        Html.button [
                            prop.className "swt:btn swt:ml-auto"
                            prop.text "Close"
                            prop.onClick (fun _ -> handleCloseWithoutSaving ())
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-primary"
                            prop.text "Save and Close"
                            prop.onClick (fun _ -> handleSaveAndClose ())
                        ]
                    ]
                ]
        )
