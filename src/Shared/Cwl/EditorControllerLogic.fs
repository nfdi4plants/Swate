module Swate.Components.Shared.Cwl.EditorControllerLogic

open System
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.CwlService
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationEngine
open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.HostTypes

type SaveMergeResult = {
    NextState: EditorState option
    InfoMessage: string
}

let formatUnhandledError (err: obj) =
    if isNull err then "unknown error" else string err

let private ruleIdText (RuleId value) = value

let private formatDecodeError (message: string) =
    let prefix = "Failed to decode CWL:"

    if String.IsNullOrWhiteSpace message then
        "Failed to decode CWL."
    elif message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
        message
    else
        sprintf "%s %s" prefix message

let private formatBlockedSaveMessage (validation: ValidationResult) =
    let firstDetail =
        validation.Errors
        |> List.tryHead
        |> Option.map (fun issue ->
            sprintf " First issue (%s at %s): %s" (ruleIdText issue.RuleId) issue.Path issue.Message
        )
        |> Option.defaultValue ""

    sprintf "Save blocked: %d validation error(s).%s" validation.Errors.Length firstDetail

let tryCreateLoadedState (fileResult: LoadCwlResponse) =
    if not fileResult.Success then
        let errorText = fileResult.Error |> Option.defaultValue "unknown error"
        Result.Error(sprintf "Load failed: %s" errorText)
    else
        match fileResult.Yaml with
        | Some yaml ->
            tryLoadToEditorWithResolved yaml fileResult.ResolvedYaml fileResult.FilePath
            |> Result.mapError formatDecodeError
        | None -> Result.Error "Load failed: missing YAML payload."

let ensureCanSave (state: EditorState) =
    let validation = validateProcessingUnit state.ProcessingUnit OnSave

    if validation.IsValid then
        Result.Ok()
    else
        Result.Error(formatBlockedSaveMessage validation)

let createSaveYamlForPath (state: EditorState) (targetPath: string) = saveFromEditorForPath state targetPath

let mergeSuccessfulSave (stateAtSaveClick: EditorState) (latestState: EditorState option) (targetPath: string) =
    match latestState with
    | Some current when Object.ReferenceEquals(current.ProcessingUnit, stateAtSaveClick.ProcessingUnit) ->
        let hasNewUnsavedChanges = current.Version > stateAtSaveClick.Version

        let mergedState = {
            current with
                FilePath = Some targetPath
                IsDirty = hasNewUnsavedChanges
        }

        let message =
            if hasNewUnsavedChanges then
                sprintf "Saved snapshot to %s. New edits remain unsaved." targetPath
            else
                sprintf "Saved to %s" targetPath

        {
            NextState = Some mergedState
            InfoMessage = message
        }
    | _ -> {
        NextState = None
        InfoMessage = sprintf "Saved to %s" targetPath
      }
