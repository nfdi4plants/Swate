[<AutoOpenAttribute>]
module Renderer.Types

open ARCtrl
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.GitTypes

[<RequireQualifiedAccess>]
type LeftSidebarPage =
    | FileExplorer
    | Git
    | Settings

type GitUnsupportedPageData = GitUnsupportedContentDto

[<RequireQualifiedAccess>]
type PageState =
    | ArcFilePage of ArcFiles
    | TextPage of string
    | UnknownPage
    | LandingDraftPage
    | NotesDraftPage
    | NotesSearchPage
    | GitDiffPage of GitDiffViewDataDto
    | GitMergeConflictPage of GitMergeConflictViewDataDto
    | GitUnsupportedPage of GitUnsupportedPageData
    | ErrorPage of string
    | DataHubBrowser
with
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

let pageStateOfFileContentDTO (dto: FileContentDTO) : PageState =
    PageState.fromFileContentDTO dto
