namespace Renderer.Components.LeftSidebar.FileExplorer

open System
open Fable.Core
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper
open Renderer.Components.LeftSidebar.FileExplorer.Types

module FileTreeAssignNoteHelper =

    type AssignNoteMoveConfig = {
        selectedTreePath: string option
        pageState: Renderer.Types.PageState option
        closeDialog: unit -> unit
        setIsAssigning: bool -> unit
        setSelection: ArcSelection -> unit
        refreshGitStatus: unit -> unit
        reloadPreviewByPath: string -> JS.Promise<Result<unit, string>>
        movePath: MovePathRequest -> JS.Promise<Result<unit, exn>>
        enqueueError: ErrorModalRequest -> unit
    }

    let enqueueAssignNoteError (enqueueError: ErrorModalRequest -> unit) errorMessage =
        enqueueError (ErrorModalRequest.create (errorMessage, title = "Could not assign note"))

    let private tryCreateNoteAssignmentTarget kind entityName =
        if String.IsNullOrWhiteSpace entityName then
            None
        else
            Some { Name = entityName; Kind = kind }

    let tryGetNoteAssignmentTarget (item: FileItem) =
        if not item.IsDirectory then
            None
        else
            item.Path
            |> Option.map PathHelpers.normalizeCanonicalRelativePath
            |> Option.bind (fun path ->
                match getNonEmptyPathParts path with
                | [| root; entityName |] when PathHelpers.pathsEqual root ARCtrl.ArcPathHelper.StudiesFolderName ->
                    tryCreateNoteAssignmentTarget NotesTargetKind.Study entityName
                | [| root; entityName |] when PathHelpers.pathsEqual root ARCtrl.ArcPathHelper.AssaysFolderName ->
                    tryCreateNoteAssignmentTarget NotesTargetKind.Assay entityName
                | _ -> None
            )

    let canAssignNoteToItem item =
        tryGetNoteAssignmentTarget item |> Option.isSome

    let private markdownExtension = ".md"

    let private withoutMarkdownExtension (fileName: string) =
        if fileName.EndsWith(markdownExtension, StringComparison.OrdinalIgnoreCase) then
            fileName.Substring(0, fileName.Length - markdownExtension.Length)
        else
            fileName

    let private tryGetRootNoteFromFilePath (relativePath: string) =
        let normalizedPath = PathHelpers.normalizeCanonicalRelativePath relativePath

        if
            normalizedPath.EndsWith(markdownExtension, StringComparison.OrdinalIgnoreCase)
            |> not
        then
            None
        else
            match getNonEmptyPathParts normalizedPath with
            | [| root; dateFolder; noteFolderName; fileName |] when PathHelpers.pathsEqual root "notes" ->
                if PathHelpers.pathsEqual noteFolderName (withoutMarkdownExtension fileName) then
                    Some {
                        SourceFolderPath = $"notes/{dateFolder}/{noteFolderName}"
                        NoteFolderName = noteFolderName
                        Label = $"{dateFolder} / {noteFolderName}"
                    }
                else
                    None
            | _ -> None

    let createAssignableNoteOptions (fileEntries: FileEntry seq) =
        fileEntries
        |> Seq.choose (fun entry ->
            if entry.isDirectory then
                None
            else
                tryGetRootNoteFromFilePath entry.path
        )
        |> Seq.distinctBy (fun note -> PathHelpers.normalizeForComparison note.SourceFolderPath)
        |> Seq.sortBy (fun note -> note.Label.ToLowerInvariant())
        |> ResizeArray

    let buildAssignedNoteFolderPath (target: ExistingTargetRef) (noteFolderName: string) =
        let targetFolderName =
            match target.Kind with
            | NotesTargetKind.Study -> ARCtrl.ArcPathHelper.StudiesFolderName
            | NotesTargetKind.Assay -> ARCtrl.ArcPathHelper.AssaysFolderName

        $"{targetFolderName}/{target.Name}/protocols/{noteFolderName}"
        |> PathHelpers.normalizeCanonicalRelativePath

    let private reloadAssignedNotePreviewIfNeeded config path = promise {
        match config.pageState with
        | Some(Renderer.Types.PageState.MarkdownPage _)
        | Some(Renderer.Types.PageState.TextPage _)
        | Some Renderer.Types.PageState.UnknownPage
        | Some(Renderer.Types.PageState.ErrorPage _) ->
            let! reloadResult = config.reloadPreviewByPath path

            match reloadResult with
            | Ok() -> ()
            | Error reloadError ->
                enqueueAssignNoteError
                    config.enqueueError
                    $"Assigned note, but could not refresh the open preview: {reloadError}"
        | _ -> ()
    }

    let assignNoteToTarget (config: AssignNoteMoveConfig) (target: ExistingTargetRef) (note: AssignableNoteRef) =
        let targetFolderPath = buildAssignedNoteFolderPath target note.NoteFolderName

        if PathHelpers.pathsEqual note.SourceFolderPath targetFolderPath then
            config.closeDialog ()
        else
            config.setIsAssigning true

            promise {
                let! moveResult =
                    config.movePath {
                        sourceRelativePath = note.SourceFolderPath
                        targetRelativePath = targetFolderPath
                        overwrite = false
                    }

                match moveResult with
                | Error moveError -> enqueueAssignNoteError config.enqueueError moveError.Message
                | Ok() ->
                    let remappedSelectionPath =
                        tryRemapSelectionPath note.SourceFolderPath targetFolderPath config.selectedTreePath

                    remappedSelectionPath
                    |> Option.iter (fun path -> config.setSelection (ArcSelection.forTreePath (Some path)))

                    match remappedSelectionPath with
                    | Some path -> do! reloadAssignedNotePreviewIfNeeded config path
                    | None -> ()

                    config.refreshGitStatus ()
                    config.closeDialog ()
            }
            |> Promise.catch (fun error -> enqueueAssignNoteError config.enqueueError error.Message)
            |> Promise.map (fun _ -> config.setIsAssigning false)
            |> Promise.start
