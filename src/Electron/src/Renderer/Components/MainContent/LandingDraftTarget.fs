module Renderer.Components.MainContent.LandingDraftTarget

open Feliz
open Swate.Components.Landing
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open ARCtrl.Contract

[<ReactComponent>]
let LandingDraftTarget () =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()
    let landingDraft, setLandingDraft = React.useState LandingDraft.init
    let landingUiState, setLandingUiState = React.useState LandingUiState.init

    let onSubmit =
        fun (payload: SubmitPayload) ->

            let finishSuccess (response: FileContentDTO) =
                let selectedPath = normalizePath response.path

                fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                response
                |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                |> Renderer.Components.ARCHelper.applyLoadedView
                    pageStateCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage

                setLandingDraft LandingDraft.init
                setLandingUiState LandingUiState.init

            promise {
                setLandingUiState {
                    landingUiState with
                        IsSubmitting = true
                        Error = None
                }

                let! saveResult = Helper.MainContentHelper.saveArcFileAndOpen payload.ArcFile

                match saveResult with
                | Result.Error message ->
                    setLandingUiState {
                        landingUiState with
                            IsSubmitting = false
                            Error = Some message.Message
                    }
                | Ok previewData ->
                    match payload.ProtocolIntent with
                    | None -> finishSuccess previewData
                    | Some protocolIntent ->
                        let request: FileContentDTO =
                            FileContentDTO.create DTOType.PlainText protocolIntent.Content protocolIntent.RelativePath

                        let! writeResult = Api.ipcArcVaultApi.writeFile (unbox null) request

                        match writeResult with
                        | Ok() -> finishSuccess previewData
                        | Result.Error exn ->
                            setLandingUiState {
                                landingUiState with
                                    IsSubmitting = false
                                    Error = Some $"Saved ARC metadata but failed to write protocol file: {exn.Message}"
                            }
            }
            |> Promise.start

    Landing.Wizard(landingDraft, setLandingDraft, landingUiState, setLandingUiState, onSubmit)
