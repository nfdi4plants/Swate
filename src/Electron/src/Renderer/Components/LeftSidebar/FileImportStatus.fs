module Renderer.Components.LeftSidebar.FileImportStatus

open Swate.Electron.Shared.FileIOTypes

let fileImportStatusText (state: ActiveFileImportState) (isCancelling: bool) : string =
    let progress = state.progress

    if isCancelling then
        "Cancelling import..."
    elif state.phase = FileImportPhase.Finalizing then
        "Finalizing import..."
    else
        match progress.step with
        | FileImportStep.Scanning -> "Checking the selected items..."
        | FileImportStep.Copying when progress.totalItems > 0 ->
            $"Copying {progress.completedItems} of {progress.totalItems} items..."
        | FileImportStep.Publishing when progress.totalItems > 0 ->
            $"Adding {progress.completedItems} of {progress.totalItems} items to the ARC..."
        | _ -> "Importing..."

let fileImportStepText (state: ActiveFileImportState) (isCancelling: bool) : string =
    if isCancelling then
        "Cancelling import..."
    elif state.phase = FileImportPhase.Finalizing then
        "Finalizing import..."
    else
        match state.progress.step with
        | FileImportStep.Scanning -> "Checking the selected items..."
        | FileImportStep.Copying when state.progress.totalItems > 0 -> "Copying items..."
        | FileImportStep.Publishing when state.progress.totalItems > 0 -> "Adding items to the ARC..."
        | _ -> "Importing..."

let showsFileImportProgressBar (state: ActiveFileImportState) (isCancelling: bool) : bool =
    not isCancelling
    && state.phase = FileImportPhase.Copying
    && state.progress.step <> FileImportStep.Scanning
    && state.progress.totalItems > 0
