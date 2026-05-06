namespace Renderer.Components.LeftSidebar.FileExplorer

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.ErrorModal

module private FileExplorerHelper =
    open Swate.Electron.Shared.FileIOHelper

    let copyArcPathToClipboard (onError: exn -> unit) =
        fun (path: string) -> promise {
            try
                do! navigator.clipboard.writeText path
            with ex ->
                onError ex
        }

    let openArcFolderInFileExplorer (onError: exn -> unit) =
        fun () -> promise {
            match! Api.ipcArcVaultApi.openArcFolderInFileExplorer () with
            | Ok() -> ()
            | Error exn -> onError exn
        }

open FileExplorerHelper

[<Erase; Mangle(false)>]
type Main =

    [<ReactComponent>]
    static member private NoArcOpenPlaceholder() =
        Html.div [
            prop.className "swt:p-4 swt:text-sm swt:text-center swt:text-muted-foreground"
            prop.children [
                Html.text "No ARC open. Please open an ARC to explore files."
            ]
        ]

    [<ReactComponent>]
    static member Main() =
        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
        let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
        let errorModalCtx = ErrorModal.Context.useErrorModalCtx ()

        let copyArcPathToClipboard =
            copyArcPathToClipboard (fun ex ->
                errorModalCtx.enqueue (
                    ErrorModalRequest.create (
                        $"Failed to copy path: {ex.Message}",
                        title = "Copy path failed",
                        ?scopeId = appStateCtx
                    )
                )
            )
            >> Promise.start

        let openArcFolderInFileExplorer =
            openArcFolderInFileExplorer (fun ex ->
                errorModalCtx.enqueue (
                    ErrorModalRequest.create (
                        $"Failed to open folder: {ex.Message}",
                        title = "Open folder failed",
                        ?scopeId = appStateCtx
                    )
                )
            )
            >> Promise.start

        match appStateCtx with
        | Some path ->
            Html.div [
                prop.testId "left-sidebar-file-explorer"
                prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col"
                prop.children [
                    Html.div [
                        prop.testId "left-sidebar-file-explorer-toolbar"
                        prop.className "swt:mb-2 swt:flex swt:shrink-0 swt:justify-center swt:bg-base-100"
                        prop.children [
                            Swate.Components.Actionbar.Main(
                                [|
                                    Actionbar.ButtonInfo.create (
                                        "swt:fluent--document-bullet-list-24-regular swt:size-5",
                                        "Lab book view",
                                        fun _ -> pageStateCtx.setState (Some Renderer.Types.PageState.LandingDraftPage)
                                    )
                                    Actionbar.ButtonInfo.create (
                                        "swt:fluent--document-24-regular swt:size-5",
                                        "Create Note",
                                        fun _ -> pageStateCtx.setState (Some Renderer.Types.PageState.NotesDraftPage)
                                    )
                                    Actionbar.ButtonInfo.create (
                                        "swt:fluent--search-24-regular swt:size-5",
                                        "Note Search",
                                        fun _ -> pageStateCtx.setState (Some Renderer.Types.PageState.NotesSearchPage)
                                    )
                                |],
                                3
                            )
                        ]
                    ]
                    Swate.Components.ArcVaultActions.ArcVaultActions.ArcVaultActions(
                        path,
                        copyArcPathToClipboard,
                        openArcFolderInFileExplorer
                    )
                    Html.div [
                        prop.testId "left-sidebar-file-explorer-tree"
                        prop.className
                            "swt:flex-1 swt:min-h-0 swt:overflow-y-auto swt:overflow-x-auto swt:[scrollbar-gutter:stable]"
                        prop.children [ FileTree.FileTree() ]
                    ]
                ]
            ]
        | None -> Main.NoArcOpenPlaceholder()