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

Vitest.afterEach (fun () -> RTL.cleanup ())

Vitest.test (
    "an expanded directory renders all supplied children",
    fun () ->
        RTL.render (FileExplorer.FileExplorer(initialItems = [ createFolder "folder" 250 ]))
        |> ignore

        Vitest.expect(isRendered "folder-000").toBe true
        Vitest.expect(isRendered "folder-100").toBe true
        Vitest.expect(isRendered "folder-249").toBe true
        Vitest.expect(RTL.screen.queryByText("Load more").IsNone).toBe true
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
    "the load-more button is available for an expanded directory with no supplied children",
    fun () ->
        let mutable requests = 0

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ createFolder "folder" 0 ],
                hasMoreChildren = (fun _ -> true),
                onLoadMoreChildren = (fun _ -> requests <- requests + 1)
            )
        )
        |> ignore

        RTL.fireEvent.click (loadMoreButton "folder")
        Vitest.expect(requests).toBe 1
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
    "expanded directories request their own pages independently",
    fun () ->
        let requestedIds = ResizeArray<string>()

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ createFolder "folder-a" 100; createFolder "folder-b" 100 ],
                hasMoreChildren = (fun _ -> true),
                onLoadMoreChildren = (fun item -> requestedIds.Add item.Id)
            )
        )
        |> ignore

        RTL.fireEvent.click (loadMoreButton "folder-b")
        RTL.fireEvent.click (loadMoreButton "folder-a")
        Vitest.expect(requestedIds.ToArray()).toEqual [| "folder-b"; "folder-a" |]
        Vitest.expect(isRendered "folder-a-099").toBe true
        Vitest.expect(isRendered "folder-b-099").toBe true
)

Vitest.test (
    "the button uses the latest callback after the caller updates",
    fun () ->
        let mutable initialRequests = 0
        let mutable updatedRequests = 0
        let folder = createFolder "folder" 100

        let explorer onLoadMore =
            FileExplorer.FileExplorer(
                initialItems = [ folder ],
                hasMoreChildren = (fun _ -> true),
                onLoadMoreChildren = onLoadMore
            )

        let rendered =
            RTL.render (explorer (fun _ -> initialRequests <- initialRequests + 1))

        rendered.rerender (explorer (fun _ -> updatedRequests <- updatedRequests + 1))
        RTL.fireEvent.click (loadMoreButton "folder")
        Vitest.expect(initialRequests).toBe 0
        Vitest.expect(updatedRequests).toBe 1
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
