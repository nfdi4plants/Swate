module Renderer.Components.Helper.ArcVaultHelper

open System
open Browser.Dom
open Fable.Core
open Swate.Components.PageComponents.SettingsPage
open Swate.Components.Primitive.ErrorModal.Types

let private tryParseLocalStorageBool (raw: string option) : bool option =
    raw
    |> Option.bind (fun value ->
        if String.Equals(value, "true", StringComparison.OrdinalIgnoreCase) then
            Some true
        elif String.Equals(value, "false", StringComparison.OrdinalIgnoreCase) then
            Some false
        else
            None)

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
    fun errorMessage ->
        enqueueErrorModal (ErrorModalRequest.create (errorMessage, title = title, ?scopeId = scopeId))

let ensureNotesFolder (onError: string -> unit) : JS.Promise<unit> =
    promise {
        match! Api.ipcArcVaultApi.ensureNotesFolder () with
        | Ok() -> ()
        | Error exn -> onError exn.Message
    }

let ensureNotesFolderIfEnabled (onError: string -> unit) : JS.Promise<unit> =
    promise {
        if isAutoCreateNotesFolderEnabled () then
            do! ensureNotesFolder onError
    }

let openArc (onError: string -> unit) : JS.Promise<bool> =
    promise {
        match! Api.ipcArcVaultApi.openARC () with
        | Error exn ->
            onError exn.Message
            return false
        | Ok None -> return false
        | Ok(Some _) ->
            do! ensureNotesFolderIfEnabled onError
            return true
    }

let openArcByPath (onError: string -> unit) (arcPath: string) : JS.Promise<bool> =
    promise {
        match! Api.ipcArcVaultApi.openARCByPath arcPath with
        | Error exn ->
            onError exn.Message
            return false
        | Ok _ ->
            do! ensureNotesFolderIfEnabled onError
            return true
    }

let createArc (onError: string -> unit) (identifier: string) : JS.Promise<bool> =
    promise {
        match! Api.ipcArcVaultApi.createARC identifier with
        | Error exn ->
            onError exn.Message
            return false
        | Ok _ ->
            do! ensureNotesFolderIfEnabled onError
            return true
    }
