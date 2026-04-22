module Renderer.Context.FileTreeContext

open Feliz
open Renderer.RendererStoreState
open Swate.Electron.Shared.FileIOTypes

type FileTreeState = {
    Entries: FileEntry[]
    Status: LoadStatus
}

let EmptyFileTreeState = {
    Entries = [||]
    Status = LoadStatus.NotRequested
}

let FileTreeCtx = React.createContext<FileTreeState> EmptyFileTreeState

[<Hook>]
let useFileTreeCtx () = React.useContext FileTreeCtx
