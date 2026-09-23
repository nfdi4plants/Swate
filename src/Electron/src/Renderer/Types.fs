[<AutoOpenAttribute>]
module Renderer.Types

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.VersionControlTypes
open Swate.Components.Page.ArcFileEditor.Types

[<RequireQualifiedAccess>]
type LeftSidebarPage =
    | FileExplorer
    | Git

/// A text diff of one changed file: the committed base, the current file and the
/// provider's word diff.
type VersionControlDiffPage = {
    Path: string
    PreviousContent: string
    CurrentContent: string
    WordDiffText: string
}

/// A conflicted file with the provider's combined preview. The handle and the
/// workspace token are the ones the preview was taken with, so a confirmation is
/// checked against exactly that state.
type VersionControlConflictPage = {
    Path: string
    ConflictContent: string
    Handle: ConflictSessionHandleDto
    WorkspaceVersion: string
}

type FileChoiceVersion = {
    CandidateId: string
    SizeBytes: float option
    /// None when the version is not a separately stored object, so download state does not apply.
    IsDownloaded: bool option
    /// Short source revision of the version (first 7 characters), when the provider names one.
    Revision: string option
    IsDeleted: bool
}

type VersionControlFileChoicePage = {
    Path: string
    Handle: ConflictSessionHandleDto
    WorkspaceVersion: string
    Mine: FileChoiceVersion
    Online: FileChoiceVersion
}

type GitUnsupportedPageData = { Path: string; Reason: string option }

[<RequireQualifiedAccess>]
type PageState =
    | ArcFilePage of arcFile: ArcFiles * requestedView: ActiveView option
    | MarkdownPage of string
    | TextPage of string
    | UnknownPage
    //| LandingDraftPage
    | NotesDraftPage
    | NotesSearchPage
    | ProvenanceGroupingPage
    | GitDiffPage of VersionControlDiffPage
    | GitMergeConflictPage of VersionControlConflictPage
    | GitFileChoiceConflictPage of VersionControlFileChoicePage
    | GitUnsupportedPage of GitUnsupportedPageData
    | ErrorPage of string
    | DataHubBrowser
    | ValidationPackageBrowser
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


type ArcSelection = {
    TreePath: string option
    ExplorerNodeId: string option
} with

    static member Empty = {
        TreePath = None
        ExplorerNodeId = None
    }

[<RequireQualifiedAccess>]
module ArcSelection =

    let private normalizeTreePath = Option.map PathHelpers.normalizePath

    let empty = ArcSelection.Empty

    let normalize (selection: ArcSelection) = {
        selection with
            TreePath = normalizeTreePath selection.TreePath
    }

    let forTreePath (treePath: string option) =
        {
            TreePath = treePath
            ExplorerNodeId = None
        }
        |> normalize

    let forExplorerNode (explorerNodeId: string) (treePath: string option) =
        {
            TreePath = treePath
            ExplorerNodeId = Some explorerNodeId
        }
        |> normalize

    let clearExplorerNode (selection: ArcSelection) = {
        normalize selection with
            ExplorerNodeId = None
    }
