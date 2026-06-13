[<AutoOpenAttribute>]
module Renderer.Types

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.GitTypes

[<RequireQualifiedAccess>]
type LeftSidebarPage =
    | FileExplorer
    | Git

type GitUnsupportedPageData = GitUnsupportedContentDto

type GitLfsPointerPageData = {
    Path: string
    SizeFormatted: string option
}

[<RequireQualifiedAccess>]
type PageState =
    | ArcFilePage of ArcFiles
    | MarkdownPage of string
    | TextPage of string
    | UnknownPage
    //| LandingDraftPage
    | NotesDraftPage
    | NotesSearchPage
    | GitDiffPage of GitDiffViewDataDto
    | GitMergeConflictPage of GitMergeConflictViewDataDto
    | GitUnsupportedPage of GitUnsupportedPageData
    | GitLfsPointerPage of GitLfsPointerPageData
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

    static member fromGitLfsPointer(path: string, sizeFormatted: string option) : PageState =
        PageState.GitLfsPointerPage {
            Path = PathHelpers.normalizePath path
            SizeFormatted = sizeFormatted
        }

let pageStateOfFileContentDTO (dto: FileContentDTO) : PageState = PageState.fromFileContentDTO dto
