module Renderer.Components.Helper.ArcVaultHelper

open System
open Browser.Dom
open Fable.Core
open Swate.Components.PageComponents.SettingsPage
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Electron.Shared.IPCTypes

let private tryParseLocalStorageBool (raw: string option) : bool option =
    raw
    |> Option.bind (fun value ->
        if String.Equals(value, "true", StringComparison.OrdinalIgnoreCase) then
            Some true
        elif String.Equals(value, "false", StringComparison.OrdinalIgnoreCase) then
            Some false
        else
            None
    )

let private isAutoCreateNotesFolderEnabled () =
    window.localStorage.getItem SettingsPageDefaults.AutoCreateNotesFolderLocalStorageKey
    |> Option.ofObj
    |> tryParseLocalStorageBool
    |> Option.defaultValue true

let createErrorModalCallback
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (title: string)
    (scopeId: string option)
    : (string -> unit) =
    fun errorMessage -> enqueueErrorModal (ErrorModalRequest.create (errorMessage, title = title, ?scopeId = scopeId))

let ensureNotesFolder (onError: string -> unit) : JS.Promise<unit> = promise {
    match! Api.ipcArcVaultApi.ensureNotesFolder () with
    | Ok() -> ()
    | Error exn -> onError exn.Message
}

let ensureNotesFolderIfEnabled (onError: string -> unit) : JS.Promise<unit> = promise {
    if isAutoCreateNotesFolderEnabled () then
        do! ensureNotesFolder onError
}

let openArc (onError: string -> unit) : JS.Promise<bool> = promise {
    match! Api.ipcArcVaultApi.openARC () with
    | Error exn ->
        onError exn.Message
        return false
    | Ok None -> return false
    | Ok(Some _) ->
        do! ensureNotesFolderIfEnabled onError
        return true
}

let openArcByPath (onError: string -> unit) (arcPath: string) : JS.Promise<bool> = promise {
    match! Api.ipcArcVaultApi.openARCByPath arcPath with
    | Error exn ->
        onError exn.Message
        return false
    | Ok _ ->
        do! ensureNotesFolderIfEnabled onError
        return true
}

/// Opens a selected ARC while reporting progress. Concurrent requests are ignored,
/// and progress is cleared even when opening fails.
let openArcByPathWithProgress
    (isOpeningArc: bool)
    (arcPath: string)
    (openArcByPath: string -> JS.Promise<bool>)
    (setIsOpeningArc: bool -> unit)
    =
    promise {
        if not isOpeningArc then
            setIsOpeningArc true

            try
                let! _ = openArcByPath arcPath
                ()
            finally
                setIsOpeningArc false
    }

/// Selects an ARC directory before reporting progress, so cancelling the picker does
/// not briefly show the opening modal. Concurrent requests are ignored.
let openArcWithProgress
    (isOpeningArc: bool)
    (pickDirectory: unit -> JS.Promise<Result<string, exn>>)
    (openArcByPath: string -> JS.Promise<bool>)
    (onError: string -> unit)
    (setIsOpeningArc: bool -> unit)
    =
    promise {
        if not isOpeningArc then
            match! pickDirectory () with
            | Error error when error.Message = "Cancelled" -> ()
            | Error error -> onError error.Message
            | Ok arcPath -> do! openArcByPathWithProgress isOpeningArc arcPath openArcByPath setIsOpeningArc
    }

let createArc (onError: string -> unit) (identifier: string) (initGit: bool) : JS.Promise<string option> = promise {
    let request = {
        identifier = identifier
        initGit = initGit
    }

    match! Api.ipcArcVaultApi.createARC request with
    | Error exn ->
        onError exn.Message
        return None
    | Ok path ->
        do! ensureNotesFolderIfEnabled onError
        return Some path
}
