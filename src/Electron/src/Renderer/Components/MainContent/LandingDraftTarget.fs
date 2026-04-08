module Renderer.Components.MainContent.LandingDraftTarget

open Feliz
open Swate.Components.Landing
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Renderer
open ARCtrl.Contract

[<ReactComponent>]
let LandingDraftTarget () =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let landingDraft, setLandingDraft = React.useState LandingDraft.init
    let landingUiState, setLandingUiState = React.useState LandingUiState.init

    let onSubmit =
        fun (payload: SubmitPayload) ->

            let finishSuccess (response: FileContentDTO) =

                fileStateCtx.setSelectedTreeItemPath (Some response.path)

                let page = PageState.fromFileContentDTO response

                pageStateCtx.setState (Some page)
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