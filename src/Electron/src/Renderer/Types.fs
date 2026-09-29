[<AutoOpenAttribute>]
module Renderer.Types

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.VersionControlTypes
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Page.GitComparison.GitPagedDiffTypes

[<RequireQualifiedAccess>]
type LeftSidebarPage =
    | FileExplorer
    | Git

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

/// Why a text diff cannot be shown.
[<RequireQualifiedAccess>]
type GitDiffBlockReason =
    | Binary of evidence: string
    | LocalContentUnavailable of objectId: string option
    | NotRegularFile
    | ProviderUnsupported
    /// A side failed strict decoding after the diff was opened.
    | NotText of evidence: string

[<RequireQualifiedAccess>]
type GitDiffPageStatus =
    | Opening
    | Scanning
    | EncodingChoice of side: DiffSideDto * token: PreparationTokenDto * candidates: EncodingCandidateDto[]
    | Ready
    | LoadingNext
    | Expanding of gapId: string
    | Blocked of side: DiffSideDto option * reason: GitDiffBlockReason
    | SourceChanged
    | WorkerFailed of message: string
    | Closed
    | Failed of message: string

/// The rows one expansion of a gap returned. Collapsing puts a hidden gap with the expanded
/// gap's id back in their place, covering exactly their lines. Expanding that id again returns
/// the result the library recorded for it, so the same rows come back.
type GitDiffExpandedGap = {
    GapId: string
    /// Lines of the expanded rows on each side.
    Previous: PagedRange
    Current: PagedRange
    /// JSON length of the expansion result and of the line slices merged into its rows.
    PayloadBytes: float
}

/// One page of the loaded window. Parts are already mapped for the viewer, so a part keeps
/// its identity across renders. An evicted page holds a single placeholder part.
type GitDiffWindowPage = {
    PageId: string
    Parts: PagedPart[]
    RowCount: int
    /// JSON length of the DTOs this page was built from, including expanded context and line slices.
    PayloadBytes: float
    IsEvicted: bool
    /// One entry per expansion result on this page, the least recently expanded first.
    ExpandedGaps: GitDiffExpandedGap list
}

/// The running request for the page after the last loaded one.
type GitDiffNextRequest = { Cursor: string; OperationId: string }

/// A paged text diff of one changed file. Generation is the page load request id of the
/// selection that opened it, so responses for an older selection can be recognized.
type GitDiffPageData = {
    Path: string
    PreviousPath: string option
    ChangeKind: GitDiffChangeKind option
    Generation: int
    Handle: DiffHandleDto option
    SourceInfos: DiffSourceInfoPairDto option
    Pages: GitDiffWindowPage[]
    /// The page the user asked for last. Eviction drops the pages farthest from it.
    RequestedPageIndex: int
    NextCursor: string option
    /// Set while a next page is loading, so the same cursor is not requested twice.
    NextRequest: GitDiffNextRequest option
    /// Line slices requested and not answered yet.
    PendingLineSlices: PagedLineSliceRequest list
    Progress: ScanProgressDto option
    Pending: PendingPreviewDto option
    OutputComplete: bool
    Status: GitDiffPageStatus
    /// Encodings the user picked, sent with every later open of this page.
    PreviousEncoding: string option
    CurrentEncoding: string option
    /// Requests still running, canceled when the page closes.
    RunningOperations: string list
}

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
    | GitDiffPage of GitDiffPageData
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
