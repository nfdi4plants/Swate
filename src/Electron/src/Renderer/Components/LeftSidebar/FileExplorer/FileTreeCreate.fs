namespace Renderer.Components.LeftSidebar.FileExplorer

open Fable.Core
open ARCtrl
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Renderer.Components.Helper
open Renderer.Components.LeftSidebar.FileExplorer.Types

module FileTreeCreateWorkflow =

    /// A created or imported item whose folders open once the file watcher shows it.
    type PendingReveal = {
        ArcScopeId: string option
        Path: string
    }

    type RevealResolution = {
        /// While the reveal belongs to the current ARC, the tree loads the folders down to this path.
        RevealPath: string option
        /// The reveal path once the file tree contains it.
        VisiblePath: string option
        /// True once the path is visible or when the reveal belongs to another ARC.
        ShouldClear: bool
    }

    type CreateConfig = {
        setIsCreating: bool -> unit
        applyError: string -> unit
        revealInTree: string -> unit
        closeCreateModal: unit -> unit
    }

    let tryCreatePendingReveal (arcScopeId: string option) (path: string) : PendingReveal option =
        let path = PathHelpers.normalizePath path

        if System.String.IsNullOrWhiteSpace path then
            None
        else
            Some { ArcScopeId = arcScopeId; Path = path }

    let resolveReveal (pending: PendingReveal option) (arcScopeId: string option) (entries: FileEntry[]) =
        let revealPath =
            pending
            |> Option.filter (fun reveal -> reveal.ArcScopeId = arcScopeId)
            |> Option.map _.Path

        // The create call can return before the file watcher has sent the new item, so the folders only open
        // once the tree contains it.
        let visiblePath =
            revealPath
            |> Option.bind (fun path ->
                entries
                |> Array.tryFind (fun entry -> PathHelpers.pathsEqual entry.path path)
                |> Option.map (fun entry -> PathHelpers.normalizePath entry.path)
            )

        {
            RevealPath = revealPath
            VisiblePath = visiblePath
            ShouldClear = pending.IsSome && (visiblePath.IsSome || revealPath.IsNone)
        }

    let importRevealPath
        (result: Result<ImportExternalFilesResult option, exn>)
        (targetRelativePath: string)
        : string option =
        match result with
        | Ok(Some ImportExternalFilesResult.Completed) -> Some targetRelativePath
        | _ -> None

    /// Creates the item and asks the tree to reveal it. Success closes the dialog and an error keeps it open.
    /// Nothing selects or opens the item, so the open view keeps its unsaved work.
    let createAndReveal (config: CreateConfig) (create: unit -> JS.Promise<Result<string, exn>>) =
        FileTreeDialogWorkflow.run
            config.setIsCreating
            config.applyError
            (fun () -> promise {
                match! create () with
                | Ok createdPath ->
                    config.revealInTree createdPath
                    config.closeCreateModal ()
                    return Ok()
                | Error exn -> return Error exn.Message
            })

    let createArcEntry
        (config: CreateConfig)
        (addArcFile: FileContentDTO -> JS.Promise<Result<unit, exn>>)
        (draft: ArcCreateDraft)
        =
        createAndReveal
            config
            (fun () -> promise {
                let! result = ArcFileApiHelper.withArcFileRequest draft.ArcFile addArcFile
                return result |> Result.map (fun () -> draft.Path)
            })

    let createFileSystemItem
        (config: CreateConfig)
        (createItem: CreateFileSystemItemRequest -> JS.Promise<Result<string, exn>>)
        (request: CreateFileSystemItemRequest)
        =
        createAndReveal config (fun () -> createItem request)

    let createDataMap
        (config: CreateConfig)
        (addArcFile: FileContentDTO -> JS.Promise<Result<unit, exn>>)
        (parentInfo: DatamapParentInfo)
        =
        createAndReveal
            config
            (fun () -> promise {
                let! result =
                    ArcFileApiHelper.withArcFileRequest (ArcFiles.DataMap(Some parentInfo, DataMap.init ())) addArcFile

                return result |> Result.map (fun () -> DatamapParentInfo.toPath parentInfo)
            })
