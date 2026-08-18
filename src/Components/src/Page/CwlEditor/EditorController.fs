module Swate.Components.Page.CwlEditor.EditorController

open System
open Swate.Components.Page.CwlEditor.Types
open Swate.Components.Shared.Cwl.EditorControllerLogic
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.HostTypes

type ControllerCallbacks = {
    ResetEditorSelection: unit -> unit
    SetEditorState: EditorState option -> unit
    SetErrorMessage: string option -> unit
    SetInfoMessage: string option -> unit
    SetIsLoading: bool -> unit
    SetIsSaving: bool -> unit
    GetLatestEditorState: unit -> EditorState option
}

let handleLoadCwl (host: CwlEditorHost) (callbacks: ControllerCallbacks) () =
    callbacks.SetIsLoading true
    callbacks.SetErrorMessage None
    callbacks.SetInfoMessage None

    let failWith (prefix: string) (err: obj) =
        callbacks.SetErrorMessage(Some(sprintf "%s: %s" prefix (formatUnhandledError err)))
        callbacks.SetIsLoading false

    match host.pickOpenFile with
    | None -> callbacks.SetIsLoading false
    | Some pickOpenFile ->
        promise {
            try
                let! dialogResult = pickOpenFile ()

                if dialogResult.Canceled then
                    callbacks.SetIsLoading false
                else
                    match dialogResult.FilePath with
                    | Some filePath when String.IsNullOrWhiteSpace filePath |> not ->
                        try
                            let! fileResult = host.loadCwlFile filePath

                            try
                                match tryCreateLoadedState fileResult with
                                | Ok loadedState ->
                                    callbacks.ResetEditorSelection()
                                    callbacks.SetEditorState(Some loadedState)
                                | Error message -> callbacks.SetErrorMessage(Some message)
                            with err ->
                                failWith "Load failed" err

                            callbacks.SetIsLoading false
                        with err ->
                            failWith "Load failed" err
                    | _ -> callbacks.SetIsLoading false
            with err ->
                failWith "Open dialog failed" err
        }
        |> Promise.start

let handleSaveCwl (host: CwlEditorHost) (callbacks: ControllerCallbacks) (state: EditorState) =
    callbacks.SetErrorMessage None
    callbacks.SetInfoMessage None

    match ensureCanSave state with
    | Error message ->
        callbacks.SetIsSaving false
        callbacks.SetErrorMessage(Some message)
    | Ok() ->
        callbacks.SetIsSaving true

        let saveToPath (targetPath: string) =
            promise {
                try
                    let yaml = createSaveYamlForPath state targetPath
                    let! result = host.saveCwlFile targetPath yaml

                    if result.Success then
                        let mergeResult =
                            mergeSuccessfulSave state (callbacks.GetLatestEditorState()) targetPath

                        match mergeResult.NextState with
                        | Some nextState -> callbacks.SetEditorState(Some nextState)
                        | None -> ()

                        callbacks.SetInfoMessage(Some mergeResult.InfoMessage)
                    else
                        let errorText = result.Error |> Option.defaultValue "unknown error"
                        callbacks.SetErrorMessage(Some(sprintf "Save failed: %s" errorText))

                    callbacks.SetIsSaving false
                with err ->
                    callbacks.SetErrorMessage(Some(sprintf "Save failed: %s" (formatUnhandledError err)))
                    callbacks.SetIsSaving false
            }
            |> Promise.start

        match host.pickSavePath, state.FilePath with
        | None, Some filePath -> saveToPath filePath
        | None, None ->
            callbacks.SetErrorMessage(Some "Cannot save: no file path is available.")
            callbacks.SetIsSaving false
        | Some pickSavePath, _ ->
            promise {
                try
                    let! dialogResult = pickSavePath ()

                    if dialogResult.Canceled then
                        callbacks.SetIsSaving false
                    else
                        match dialogResult.FilePath with
                        | Some filePath when String.IsNullOrWhiteSpace filePath |> not -> saveToPath filePath
                        | _ -> callbacks.SetIsSaving false
                with err ->
                    callbacks.SetErrorMessage(Some(sprintf "Save dialog failed: %s" (formatUnhandledError err)))
                    callbacks.SetIsSaving false
            }
            |> Promise.start
