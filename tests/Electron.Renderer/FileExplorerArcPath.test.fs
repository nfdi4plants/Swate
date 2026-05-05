module ElectronRenderer.FileExplorerArcPathTests

open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Renderer.Components.FileExplorerArcPath
open Swate.Components
open Swate.Components.ErrorModal.Context
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Vitest

let rec private waitUntil (predicate: unit -> bool, attempts: int) = promise {
    if predicate () then
        return ()
    elif attempts <= 0 then
        failwith "Timed out waiting for React effect."
    else
        do! Promise.sleep 1
        return! waitUntil (predicate, attempts - 1)
}

let private waitForEffect predicate = waitUntil (predicate, 50)

let private renderToBody (element: ReactElement) = promise {
    let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
    document.body.appendChild container |> ignore
    let root = ReactDOM.createRoot container
    root.render element
    do! Promise.sleep 0

    return
        container,
        (fun () ->
            root.unmount ()
            container.remove ()
        )
}

let private findByTestId<'T when 'T :> Browser.Types.Element> (testId: string) : 'T =
    let selector = $"[data-testid='{testId}']"

    match document.querySelector selector with
    | null -> failwith $"Could not find element '{testId}'."
    | element -> element :?> 'T

[<ReactComponent>]
let private FileExplorerTreeHarness
    (arcRootPath: string option, enqueueError: Swate.Components.ErrorModal.ErrorModalRequest -> unit)
    =
    let pageStateCtx: StateContext<Renderer.Types.PageState option> = { state = None; setState = ignore }

    let fileTree: FileEntry[] = [|
        FileEntry.create ("arc", "", true)
        FileEntry.create ("plain.txt", "plain.txt", false)
    |]

    let fileStateController: Renderer.Context.FileStateContext.FileStateController = {
        state = {
            FileTree = fileTree
            Selection = ArcSelection.empty
        }
        fileTreeIsLoading = false
        refreshFileTree = ignore
        setSelection = ignore
        updateSelection = ignore
    }

    let errorModalContext: ErrorModalContext = {
        ErrorModalContext.Empty with
            enqueue = enqueueError
    }

    Renderer.Context.AppStateContext.AppStateCtx.Provider(
        arcRootPath,
        Renderer.Context.PageStateContext.PageStateCtx.Provider(
            pageStateCtx,
            Renderer.Context.FileStateContext.FileStateCtx.Provider(
                fileStateController,
                ErrorModalCtx.Provider(
                    errorModalContext,
                    Renderer.Components.LeftSidebar.FileExplorer.FileTree.FileTree()
                )
            )
        )
    )

Vitest.afterEach (fun () -> document.body.innerHTML <- "")

Vitest.describe (
    "FileExplorer ARC path popover",
    fun () ->
        Vitest.test (
            "hover tooltip and click popover show full ARC path and actions",
            fun () -> promise {
                let path = @"C:\arcs\my-arc"
                let mutable copiedPath: string option = None
                let mutable openedFolderCount = 0

                let! _container, cleanup =
                    renderToBody (
                        ArcPathPopover(
                            "my-arc",
                            Some path,
                            (fun copied -> copiedPath <- Some copied),
                            (fun () -> openedFolderCount <- openedFolderCount + 1)
                        )
                    )

                try
                    let trigger =
                        findByTestId<Browser.Types.HTMLButtonElement> "left-sidebar-file-explorer-arc-name"

                    Vitest.expect(trigger.getAttribute "title").toBe (path)
                    trigger.click ()

                    do!
                        waitForEffect (fun () ->
                            document.querySelector ("[data-testid='popover_content_FileExplorerArcPath']")
                            <> null
                        )

                    let pathValue =
                        findByTestId<Browser.Types.HTMLElement> "file-explorer-arc-path-value"

                    let copyButton =
                        findByTestId<Browser.Types.HTMLButtonElement> "file-explorer-arc-path-copy"

                    let openFolderButton =
                        findByTestId<Browser.Types.HTMLButtonElement> "file-explorer-arc-path-open-folder"

                    Vitest.expect(pathValue.textContent).toBe (path)
                    Vitest.expect(copyButton.disabled).toBe (false)
                    Vitest.expect(openFolderButton.disabled).toBe (false)

                    copyButton.click ()
                    openFolderButton.click ()

                    Vitest.expect(copiedPath).toEqual (Some path)
                    Vitest.expect(openedFolderCount).toBe (1)
                finally
                    cleanup ()
            }
        )

        Vitest.test (
            "open-folder action invokes ArcVault IPC from FileExplorer tree",
            fun () -> promise {
                let mutable openFolderCalls = 0
                let mutable enqueuedErrors = 0
                let apiObj: obj = box Api.ipcArcVaultApi
                let originalOpenFolderMethod: obj = apiObj?openArcFolderInFileExplorer

                apiObj?openArcFolderInFileExplorer <-
                    (fun () ->
                        openFolderCalls <- openFolderCalls + 1
                        promise { return Ok() }
                    )

                let! _container, cleanup =
                    renderToBody (
                        FileExplorerTreeHarness(Some @"C:\arcs\my-arc", fun _ -> enqueuedErrors <- enqueuedErrors + 1)
                    )

                try
                    let trigger =
                        findByTestId<Browser.Types.HTMLButtonElement> "left-sidebar-file-explorer-arc-name"

                    trigger.click ()

                    do!
                        waitForEffect (fun () ->
                            document.querySelector ("[data-testid='file-explorer-arc-path-open-folder']")
                            <> null
                        )

                    let openFolderButton =
                        findByTestId<Browser.Types.HTMLButtonElement> "file-explorer-arc-path-open-folder"

                    openFolderButton.click ()
                    do! Promise.sleep 0

                    Vitest.expect(openFolderCalls).toBe (1)
                    Vitest.expect(enqueuedErrors).toBe (0)
                finally
                    apiObj?openArcFolderInFileExplorer <- originalOpenFolderMethod
                    cleanup ()
            }
        )
)