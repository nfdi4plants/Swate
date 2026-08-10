[<AutoOpenAttribute>]
module Renderer.Types

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.GitTypes
open Swate.Components.Page.ArcFileEditor.Types

[<RequireQualifiedAccess>]
type LeftSidebarPage =
    | FileExplorer
    | Git

type GitUnsupportedPageData = GitUnsupportedContentDto

[<RequireQualifiedAccess>]
type PageState =
    | ArcFilePage of arcFile: ArcFiles * activeView: ActiveView option
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
            | Some arcFile ->
                let normalizedPath = PathHelpers.normalizePath dto.path

                if
                    normalizedPath.EndsWith(
                        ARCtrl.ArcPathHelper.DataMapFileName,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                then
                    PageState.ArcFilePage(arcFile, Some ActiveView.DataMap)
                elif normalizedPath.EndsWith(".xlsx", System.StringComparison.OrdinalIgnoreCase) then
                    let startingView =
                        if arcFile.Tables().Count > 0 then
                            ActiveView.Table 0
                        else
                            ActiveView.Metadata

                    PageState.ArcFilePage(arcFile, Some startingView)
                else
                    PageState.ArcFilePage(arcFile, None)
            | None ->
                PageState.ErrorPage
                    $"Failed to parse ARC file: {dto.path} - {dto.fileType} - unsupported format or corrupted content."
        | _ -> PageState.UnknownPage

let pageStateOfFileContentDTO (dto: FileContentDTO) : PageState = PageState.fromFileContentDTO dto
