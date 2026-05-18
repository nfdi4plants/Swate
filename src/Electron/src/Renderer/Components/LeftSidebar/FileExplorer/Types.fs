module Renderer.Components.LeftSidebar.FileExplorer.Types

open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared

type ArcCreateDraft = { ArcFile: ArcFiles; Path: string }

type ArcRenameDraft = {
    Item: FileItem
    SourcePath: string
    InitialName: string
}
