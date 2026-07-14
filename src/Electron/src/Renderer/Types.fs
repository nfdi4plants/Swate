[<AutoOpenAttribute>]
module Renderer.Types

open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.GitTypes

[<RequireQualifiedAccess>]
type LeftSidebarPage =
    | FileExplorer
    | Git

type GitUnsupportedPageData = GitUnsupportedContentDto

[<RequireQualifiedAccess>]
type PageState =
    | ArcFilePage of ArcFiles
    | MarkdownPage of string
    | TextPage of string
    | UnknownPage
    //| LandingDraftPage
    | NotesDraftPage
    | NotesSearchPage
    | ProvenanceGroupingPage
    | GitDiffPage of GitDiffViewDataDto
    | GitMergeConflictPage of GitMergeConflictViewDataDto
    | GitUnsupportedPage of GitUnsupportedPageData
    | ErrorPage of string
    | DataHubBrowser
    | SettingsPage

    static member fromFileContentDTO(dto: FileContentDTO) : PageState =
        match dto.fileType with
        | FileContentType.Markdown -> PageState.MarkdownPage dto.content
        | FileContentType.FileContentTypeIsPlainTextVariant -> PageState.TextPage dto.content
        | FileContentType.FileContentTypeIsISAFileVariant ->
            let arcfile = FileContentDTO.toArcFile dto

            match arcfile with
            | Some arcFile -> PageState.ArcFilePage arcFile
            | None ->
                PageState.ErrorPage
                    $"Failed to parse ARC file: {dto.path} - {dto.fileType} - unsupported format or corrupted content."
        | _ -> PageState.UnknownPage

let pageStateOfFileContentDTO (dto: FileContentDTO) : PageState = PageState.fromFileContentDTO dto
