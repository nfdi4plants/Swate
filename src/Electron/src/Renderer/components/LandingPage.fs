module Renderer.components.LandingPage

open Fable.Core

open Swate.Electron.Shared.IPCTypes

open Renderer.components.ExperimentLanding

///Landing module
let createFromLanding
    landingUiState setLandingUiState landingDraft setLandingDraft setShowLandingDraft setLandingDraftActive appState
        setSelectedTreeItemPath setPreviewData setPreviewError setDidSelectFile (target: ExperimentTarget) =
    promise {
        setLandingUiState {
            landingUiState with
                IsSubmitting = true
                Error = None
        }

        let request = toCreateRequest landingDraft target
        let! result = Api.arcVaultApi.createExperimentFromLanding JS.undefined request

        match result with
        | Ok response ->
            response.CreatedIdentifier
            |> ARCHelper.tryGetCreatedFilePath appState target
            |> setSelectedTreeItemPath
            setPreviewData (Some response.PreviewData)
            setPreviewError None
            setDidSelectFile true
            setShowLandingDraft false
            setLandingDraftActive false
            setLandingDraft LandingDraft.init
            setLandingUiState LandingUiState.init
        | Error exn ->
            setLandingUiState {
                landingUiState with
                    IsSubmitting = false
                    Error = Some exn.Message
            }
    }
    |> Promise.start