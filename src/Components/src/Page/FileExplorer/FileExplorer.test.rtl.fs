module internal Swate.Components.Page.FileExplorer.Tests

open Browser.Types
open Feliz
open Swate.Components.Page.FileExplorer.Types
open Vitest

let private createChild prefix index =
    let id = sprintf "%s-%03d" prefix index

    {
        FileTree.createFile id None FileItemIcon.Document with
            Id = id
    }

let private createFolder id childCount = {
    FileTree.createFolder id (Some id) FileItemIcon.Folder with
        Id = id
        IsExpanded = true
        Children = Some(List.init childCount (createChild id))
}

let private isRendered (name: string) = RTL.screen.queryByText(name).IsSome

let private loadMoreButton directoryId =
    (RTL.screen.getByTestId $"file-explorer-load-more-{directoryId}").querySelector ("button") :?> HTMLButtonElement

let private rootLoadMoreButton () =
    (RTL.screen.getByTestId "file-explorer-root-load-more").querySelector ("button") :?> HTMLButtonElement

Vitest.afterEach (fun () -> RTL.cleanup ())

Vitest.test (
    "root pages append on request and exhaustion removes the root load-more button",
    fun () ->
        let mutable requests = 0
        let clicked = ResizeArray<FileItem>()

        let explorer itemCount hasMore =
            FileExplorer.FileExplorer(
                initialItems = List.init itemCount (createChild "root"),
                hasMoreRootItems = hasMore,
                onLoadMoreRootItems = (fun () -> requests <- requests + 1),
                onItemClick = clicked.Add,
                selectedItemId = Some "root-019"
            )

        let rendered = RTL.render (explorer 100 true)
        Vitest.expect(requests).toBe 0
        Vitest.expect(isRendered "root-099").toBe true
        Vitest.expect(isRendered "root-100").toBe false

        RTL.fireEvent.click (rootLoadMoreButton ())
        Vitest.expect(requests).toBe 1
        Vitest.expect(clicked.Count).toBe 0

        rendered.rerender (explorer 200 true)
        Vitest.expect(isRendered "root-000").toBe true
        Vitest.expect(isRendered "root-199").toBe true
        Vitest.expect((RTL.screen.getByText "root-019").classList.contains "swt:text-primary").toBe true

        RTL.fireEvent.click (rootLoadMoreButton ())
        rendered.rerender (explorer 250 false)
        Vitest.expect(isRendered "root-249").toBe true
        Vitest.expect(RTL.screen.queryByText("Load more").IsNone).toBe true
        Vitest.expect(requests).toBe 2
)

Vitest.test (
    "root and directory pagination request their own pages independently",
    fun () ->
        let mutable rootRequests = 0
        let directoryRequests = ResizeArray<FileItem>()
        let folder = createFolder "root" 0

        let explorer hasMoreRootItems =
            FileExplorer.FileExplorer(
                initialItems = [ folder ],
                hasMoreRootItems = hasMoreRootItems,
                onLoadMoreRootItems = (fun () -> rootRequests <- rootRequests + 1),
                hasMoreChildren = (fun _ -> true),
                onLoadMoreChildren = directoryRequests.Add
            )

        let rendered = RTL.render (explorer false)
        Vitest.expect(RTL.screen.queryByTestId("file-explorer-root-load-more").IsNone).toBe true

        rendered.rerender (explorer true)
        RTL.fireEvent.click (rootLoadMoreButton ())
        Vitest.expect(rootRequests).toBe 1
        Vitest.expect(directoryRequests.Count).toBe 0

        RTL.fireEvent.click (loadMoreButton "root")
        Vitest.expect(rootRequests).toBe 1
        Vitest.expect(directoryRequests.ToArray()).toEqual [| folder |]

        rendered.rerender (explorer false)
        Vitest.expect(RTL.screen.queryByTestId("file-explorer-root-load-more").IsNone).toBe true
        Vitest.expect(RTL.screen.queryByTestId("file-explorer-load-more-root").IsSome).toBe true
)

Vitest.test (
    "more available children are requested only when the button is clicked",
    fun () -> promise {
        let requested = ResizeArray<FileItem>()
        let clicked = ResizeArray<FileItem>()
        let expansionChanges = ResizeArray<bool>()
        let folder = createFolder "folder" 100

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ folder ],
                hasMoreChildren = (fun item -> item.Id = folder.Id),
                onLoadMoreChildren = requested.Add,
                onItemClick = clicked.Add,
                onDirectoryExpansionChange = (fun _ expanded -> expansionChanges.Add expanded)
            )
        )
        |> ignore

        do! RTL.act (fun () -> promise { return () })
        Vitest.expect(requested.Count).toBe 0
        Vitest.expect(isRendered "folder-099").toBe true

        RTL.fireEvent.click (loadMoreButton "folder")

        Vitest.expect(requested.ToArray()).toEqual [| folder |]
        Vitest.expect(clicked.Count).toBe 0
        Vitest.expect(expansionChanges.Count).toBe 0
        Vitest.expect(isRendered "folder-099").toBe true
        Vitest.expect(isRendered "folder-100").toBe false
    }
)

Vitest.test (
    "new pages render without another expansion and exhaustion removes the button",
    fun () ->
        let mutable requests = 0

        let explorer childCount hasMore =
            FileExplorer.FileExplorer(
                initialItems = [ createFolder "folder" childCount ],
                hasMoreChildren = (fun _ -> hasMore),
                onLoadMoreChildren = (fun _ -> requests <- requests + 1)
            )

        let rendered = RTL.render (explorer 100 true)

        RTL.fireEvent.click (loadMoreButton "folder")
        rendered.rerender (explorer 200 true)
        Vitest.expect(isRendered "folder-000").toBe true
        Vitest.expect(isRendered "folder-199").toBe true
        Vitest.expect(requests).toBe 1

        RTL.fireEvent.click (loadMoreButton "folder")
        rendered.rerender (explorer 250 false)
        Vitest.expect(isRendered "folder-199").toBe true
        Vitest.expect(isRendered "folder-249").toBe true
        Vitest.expect(RTL.screen.queryByText("Load more").IsNone).toBe true
        Vitest.expect(requests).toBe 2
)

Vitest.test (
    "pagination availability updates even when the supplied children do not change",
    fun () ->
        let folder = createFolder "folder" 100

        let explorer hasMore =
            FileExplorer.FileExplorer(initialItems = [ folder ], hasMoreChildren = (fun _ -> hasMore))

        let rendered = RTL.render (explorer false)
        Vitest.expect(RTL.screen.queryByText("Load more").IsNone).toBe true

        rendered.rerender (explorer true)
        Vitest.expect(RTL.screen.queryByText("Load more").IsSome).toBe true

        rendered.rerender (explorer false)
        Vitest.expect(RTL.screen.queryByText("Load more").IsNone).toBe true
        Vitest.expect(isRendered "folder-099").toBe true
)

Vitest.test (
    "collapse hides the button and reopening renders all supplied children without requesting a page",
    fun () ->
        let mutable requests = 0

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ createFolder "folder" 250 ],
                hasMoreChildren = (fun _ -> true),
                onLoadMoreChildren = (fun _ -> requests <- requests + 1)
            )
        )
        |> ignore

        RTL.fireEvent.click (RTL.screen.getByText "folder")
        Vitest.expect(isRendered "folder-000").toBe false
        Vitest.expect(RTL.screen.queryByText("Load more").IsNone).toBe true

        RTL.fireEvent.click (RTL.screen.getByText "folder")
        Vitest.expect(isRendered "folder-249").toBe true
        Vitest.expect(RTL.screen.queryByText("Load more").IsSome).toBe true
        Vitest.expect(requests).toBe 0
)

Vitest.test (
    "selection opens ancestors and the selected child stays visible after reopening",
    fun () ->
        let folder = {
            createFolder "folder" 250 with
                IsExpanded = false
        }

        RTL.render (FileExplorer.FileExplorer(initialItems = [ folder ], selectedItemId = Some "folder-149"))
        |> ignore

        Vitest.expect(isRendered "folder-149").toBe true
        Vitest.expect(isRendered "folder-249").toBe true

        RTL.fireEvent.click (RTL.screen.getByRole ("button", ByRoleOptions(name = Text "Collapse folder")))
        Vitest.expect(isRendered "folder-149").toBe false

        RTL.fireEvent.click (RTL.screen.getByRole ("button", ByRoleOptions(name = Text "Expand folder")))
        Vitest.expect(isRendered "folder-149").toBe true
        Vitest.expect((RTL.screen.getByText "folder-149").classList.contains "swt:text-primary").toBe true
)
