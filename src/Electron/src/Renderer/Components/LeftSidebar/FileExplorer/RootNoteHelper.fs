module Renderer.Components.LeftSidebar.FileExplorer.RootNoteHelper

open System
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.LeftSidebar.FileExplorer.Helper

let createUntitledRootNotePath (dateCreated: DateTime) =
    NoteConversion.mkNewRootNoteRelativePath dateCreated.Date "untitled-note"
    |> Option.defaultWith (fun () -> failwith "Could not create a safe untitled note path.")

let createUntitledRootNoteRequest (dateCreated: DateTime) =
    let dateCreated = dateCreated.Date
    let path = createUntitledRootNotePath dateCreated

    let draft: NotesDraft = {
        NotesDraft.init with
            Title = "Untitled Note"
            DateCreated = Some dateCreated
    }

    FileContentDTO.create FileContentType.Markdown (NoteConversion.formatMarkdown draft) path

let rootNoteActionContextMenuItems (dateCreated: DateTime) (onAddNote: FileItem -> unit) (item: FileItem) =
    let createTargetPath date =
        NoteConversion.mkNewRootNoteRelativePath date "untitled-note"

    let isRootFolderForNewRootNote (item: FileItem) =
        item.IsDirectory
        && (tryGetItemRelativePath item
            |> Option.exists (isRootFolderForDatedTargetPath createTargetPath dateCreated))

    ContextMenuItem.whenItem
        isRootFolderForNewRootNote
        "Create new item in"
        "swt:fluent--note-add-24-regular"
        onAddNote
        item
