module Swate.Components.Shared.Cwl.State.EffectRunner

open System
open ARCtrl.CWL
open Fable.Core
open Swate.Components.Shared.Cwl.Adapters.ArCtrlDecode
open Swate.Components.Shared.Cwl.HostTypes
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.Effects

type TimerPort = {
    SetTimeout: int -> (unit -> unit) -> float
    ClearTimeout: float -> unit
}

type Ports = {
    HostApi: CwlHostApi
    Timers: TimerPort
}

let private attachHandlers (promise: JS.Promise<'T>) (onSuccess: 'T -> unit) (onError: obj -> unit) =
    promise.``then`` ((fun value -> onSuccess value), (fun error -> onError error))
    |> ignore

let run (ports: Ports) (dispatch: AppAction -> unit) (effect: AppEffect) =
    match effect with
    | FocusMainWindow _ ->
        // Swate hosts do not implement window focus.
        ()

    | LoadCwlFile(requestId, filePath) ->
        attachHandlers
            (ports.HostApi.LoadCwlFile filePath)
            (fun response ->
                match response.Success, response.Yaml with
                | true, Some yaml ->
                    let document = yaml |> Decode.decodeCWLProcessingUnit |> fromProcessingUnit

                    let loadedFilePath =
                        if String.IsNullOrWhiteSpace response.FilePath then
                            filePath
                        else
                            response.FilePath

                    dispatch (LoadSucceeded(requestId, document, loadedFilePath))
                | _ -> dispatch (LoadFailed(requestId, response.Error |> Option.defaultValue "Load failed"))
            )
            (fun error -> dispatch (LoadFailed(requestId, string error)))

    | SaveCwlFile(requestId, revision, filePath, yaml) ->
        attachHandlers
            (ports.HostApi.SaveCwlFile filePath yaml)
            (fun response ->
                if response.Success then
                    let savedFilePath =
                        if String.IsNullOrWhiteSpace response.FilePath then
                            filePath
                        else
                            response.FilePath

                    dispatch (SaveSucceeded(requestId, revision, savedFilePath))
                else
                    dispatch (SaveFailed(requestId, response.Error |> Option.defaultValue "Save failed"))
            )
            (fun error -> dispatch (SaveFailed(requestId, string error)))

    | ShowOpenDialog requestId ->
        attachHandlers
            (ports.HostApi.ShowOpenDialog())
            (fun result -> dispatch (LoadDialogCompleted(requestId, result)))
            (fun error -> dispatch (LoadFailed(requestId, string error)))

    | ShowSaveDialog(requestId, revision) ->
        attachHandlers
            (ports.HostApi.ShowSaveDialog())
            (fun result -> dispatch (SaveDialogCompleted(requestId, revision, result)))
            (fun error -> dispatch (SaveFailed(requestId, string error)))
