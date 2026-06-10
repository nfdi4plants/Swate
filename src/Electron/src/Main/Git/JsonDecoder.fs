module Main.Git.JsonDecoder

open System
open Thoth.Json.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

let private normalizedNonEmptyNameDecoder: Decoder<string> =
    Decode.string
    |> Decode.andThen (fun name ->
        if String.IsNullOrWhiteSpace name then
            Decode.fail "Field `name` must be a non-empty, non-whitespace string."
        else
            name |> PathHelpers.normalizeSeparators |> Decode.succeed
    )

let private gitLfsLsFileInfoDecoder: Decoder<GitLfsLsFileInfo> =
    Decode.object (fun get -> {
        name = get.Required.Field "name" normalizedNonEmptyNameDecoder
        size = get.Required.Field "size" Decode.float
        checkout = get.Required.Field "checkout" Decode.bool
        downloaded = get.Required.Field "downloaded" Decode.bool
        ``oid_type`` = get.Required.Field "oid_type" Decode.string
        oid = get.Required.Field "oid" Decode.string
        version = get.Required.Field "version" Decode.string
    })

let internal lsFilesResponseDecoder: Decoder<GitLfsLsFileInfo[]> =
    Decode.object (fun get ->
        get.Optional.Field "files" (Decode.array gitLfsLsFileInfoDecoder)
        |> Option.defaultValue [||]
    )
