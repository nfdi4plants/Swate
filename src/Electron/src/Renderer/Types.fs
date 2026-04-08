[<AutoOpenAttribute>]
module Renderer.Types

open ARCtrl
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper

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

    static member fromFileContentDTO(dto: FileContentDTO) : Result<PageState, string> =
        match dto.fileType with
        | DTOType.DTOTypeIsPlainTextVariant -> Ok(PageState.TextPage dto.content)
        | DTOType.DTOTypeIsISAFileVariant ->
            let arcfile = FileContentDTO.toArcFile dto

            match arcfile with
            | Some arcFile -> Ok(PageState.ArcFilePage arcFile)
            | None ->
                Error $"Failed to parse ARC file: {dto.path} - {dto.fileType} - unsupported format or corrupted content."
        | _ -> Ok PageState.UnknownPage

