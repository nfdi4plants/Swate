module Renderer.Components.LeftSidebar.FileExplorer.RootNoteHelper

open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOHelper
open Renderer.Components.LeftSidebar.FileExplorer.Helper

let private notesRootFolderName = "notes"

let rootNoteActionContextMenuItems (onAddNote: unit -> unit) (item: FileItem) =
    let isRootNotesFolder (item: FileItem) =
        item.IsDirectory
        && (tryGetItemRelativePath item
            |> Option.exists (isRootFolderPath notesRootFolderName))

    ContextMenuItem.whenItem
        isRootNotesFolder
        "Create new item in"
        "swt:fluent--note-add-24-regular"
        (fun _ -> onAddNote ())
        item
