[<AutoOpenAttribute>]
module Renderer.Types


open ARCtrl
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper

[<RequireQualifiedAccess>]
type ExplorerMode =
    | NormalFileTree
    | ArcObjectTree

[<RequireQualifiedAccess>]
type LeftSidebarPage = | FileExplorer

[<RequireQualifiedAccess>]
type PageState =
    | ArcFilePage of ArcFiles
    | TextPage of string
    | UnknownPage
    | LandingDraftPage
    | NotesDraftPage
    | NotesSearchPage
    | ErrorPage of string

    static member fromFileContentDTO(dto: FileContentDTO) : PageState =
        match dto.fileType with
        | DTOType.DTOTypeIsPlainTextVariant -> PageState.TextPage dto.content
        | DTOType.DTOTypeIsISAFileVariant ->
            let arcfile = FileContentDTO.toArcFile dto

            match arcfile with
            | Some arcFile -> PageState.ArcFilePage arcFile
            | None ->
                PageState.ErrorPage
                    $"Failed to parse ARC file: {dto.path} - {dto.fileType} - unsupported format or corrupted content."
        | _ -> PageState.UnknownPage
