module Main.Git.JsonDecoder

open System
open System.Collections.Generic
open Thoth.Json.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

let private gitLfsLsFileInfoDecoder : Decoder<GitLfsLsFileInfo option> =
    Decode.object (fun get ->
        let name = get.Optional.Field "name" Decode.string
        let size = get.Optional.Field "size" Decode.float
        let checkout = get.Optional.Field "checkout" Decode.bool
        let downloaded = get.Optional.Field "downloaded" Decode.bool
        let oidType = get.Optional.Field "oid_type" Decode.string
        let oid = get.Optional.Field "oid" Decode.string
        let version = get.Optional.Field "version" Decode.string

        match name, size, checkout, downloaded, oidType, oid, version with
        | Some name, Some size, Some checkout, Some downloaded, Some oidType, Some oid, Some version ->
            Some {
                name = PathHelpers.normalizeSeparators name
                size = size
                checkout = checkout
                downloaded = downloaded
                ``oid_type`` = oidType
                oid = oid
                version = version
            }
        | _ ->
            None
    )

let private lsFilesResponseDecoder : Decoder<GitLfsLsFileInfo[]> =
    Decode.object (fun get ->
        get.Optional.Field "files" (Decode.array gitLfsLsFileInfoDecoder)
        |> Option.defaultValue [||]
        |> Array.choose id
    )

let internal parseLsFiles (stdoutText: string) : GitLfsLsFileInfo[] =
    ARCtrl.Json.Decode.fromJsonString lsFilesResponseDecoder stdoutText

let internal indexUsingRelativePath (files: GitLfsLsFileInfo[]) : Dictionary<string, GitLfsLsFileInfo> =
    let filesByRelativePath = Dictionary<string, GitLfsLsFileInfo>()

    files
    |> Array.iter (fun info ->
        if not (String.IsNullOrWhiteSpace info.name) then
            let relativePath = PathHelpers.normalizeSeparators info.name
            filesByRelativePath.[relativePath] <- { info with name = relativePath }
    )

    filesByRelativePath