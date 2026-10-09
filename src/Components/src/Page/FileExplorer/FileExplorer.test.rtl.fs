module internal Swate.Components.Page.FileExplorer.Tests

open Fable.Core
open Feliz
open Swate.Components.Page.FileExplorer.Types
open Vitest

[<Import("installIntersectionObserver", "./FileExplorer.test.helpers.ts")>]
let private installIntersectionObserver () : unit = jsNative

[<Import("restoreIntersectionObserver", "./FileExplorer.test.helpers.ts")>]
let private restoreIntersectionObserver () : unit = jsNative

[<Import("triggerIntersection", "./FileExplorer.test.helpers.ts")>]
let private triggerIntersection (testId: string) (isIntersecting: bool) : unit = jsNative

let private createChild prefix index =
    let id = sprintf "%s-%03d" prefix index

    {
        FileTree.createFile id None FileItemIcon.Document with
            Id = id
    }

let private createFolder id childCount = {
    FileTree.createFolder id None FileItemIcon.Folder with
        Id = id
        IsExpanded = true
        Children = Some(List.init childCount (createChild id))
}

let private isRendered (name: string) = RTL.screen.queryByText(name).IsSome

let private renderBatched items =
    RTL.render (FileExplorer.FileExplorer(initialItems = items, childRenderBatchSize = 100))
    |> ignore

let private revealNextBatch directoryId = promise {
    let sentinelId = $"file-explorer-load-more-{directoryId}"
    do! RTL.act (fun () -> promise { triggerIntersection sentinelId true })

    if RTL.screen.queryByTestId(sentinelId).IsSome then
        do! RTL.act (fun () -> promise { triggerIntersection sentinelId false })
}

Vitest.beforeEach (fun () -> installIntersectionObserver ())

Vitest.afterEach (fun () ->
    RTL.cleanup ()
    restoreIntersectionObserver ()
)

Vitest.test (
    "an expanded directory initially renders only its first child batch",
    fun () ->
        renderBatched [ createFolder "folder" 250 ]
        Vitest.expect(isRendered "folder-099").toBe true
        Vitest.expect(isRendered "folder-100").toBe false

        RTL.screen.getByTestId "file-explorer-load-more-folder" |> ignore
)

Vitest.test (
    "a directory with ten thousand loaded children still renders only its first batch",
    fun () ->
        renderBatched [ createFolder "large-folder" 10000 ]
        Vitest.expect(isRendered "large-folder-099").toBe true
        Vitest.expect(isRendered "large-folder-100").toBe false
        Vitest.expect(isRendered "large-folder-9999").toBe false
)

Vitest.test (
    "sentinel activations reveal one additional batch at a time and stop after the final batch",
    fun () -> promise {
        renderBatched [ createFolder "folder" 250 ]

        do! revealNextBatch "folder"
        Vitest.expect(isRendered "folder-199").toBe true
        Vitest.expect(isRendered "folder-200").toBe false

        do! revealNextBatch "folder"
        Vitest.expect(isRendered "folder-249").toBe true
        Vitest.expect(RTL.screen.queryByTestId("file-explorer-load-more-folder").IsNone).toBe true
    }
)

Vitest.test (
    "a sentinel that remains intersecting cannot cascade through every batch",
    fun () -> promise {
        let sentinelId = "file-explorer-load-more-folder"
        renderBatched [ createFolder "folder" 250 ]

        do! RTL.act (fun () -> promise { triggerIntersection sentinelId true })
        do! RTL.act (fun () -> promise { triggerIntersection sentinelId true })

        Vitest.expect(isRendered "folder-199").toBe true
        Vitest.expect(isRendered "folder-200").toBe false

        do! RTL.act (fun () -> promise { triggerIntersection sentinelId false })
        do! RTL.act (fun () -> promise { triggerIntersection sentinelId true })

        Vitest.expect(isRendered "folder-249").toBe true
    }
)

Vitest.test (
    "the end sentinel requests an external page when all currently loaded children are visible",
    fun () -> promise {
        let mutable requests = 0

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ createFolder "folder" 100 ],
                childRenderBatchSize = 100,
                hasMoreChildren = (fun item -> item.Id = "folder"),
                onLoadMoreChildren = (fun _ -> requests <- requests + 1)
            )
        )
        |> ignore

        do! RTL.act (fun () -> promise { triggerIntersection "file-explorer-load-more-folder" true })

        Vitest.expect(requests).toBe 1
        Vitest.expect(isRendered "folder-099").toBe true
    }
)

Vitest.test (
    "the load-more control requests an external page when clicked",
    fun () ->
        let mutable requestedDirectoryId = None

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ createFolder "folder" 100 ],
                childRenderBatchSize = 100,
                hasMoreChildren = (fun item -> item.Id = "folder"),
                onLoadMoreChildren = (fun item -> requestedDirectoryId <- Some item.Id)
            )
        )
        |> ignore

        RTL.fireEvent.click (RTL.screen.getByText "Load more")

        Vitest.expect(requestedDirectoryId).toEqual (Some "folder")
)

Vitest.test (
    "the load-more control remains available when automatic loading is disabled",
    fun () -> promise {
        let mutable requests = 0
        let sentinelId = "file-explorer-load-more-folder"

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ createFolder "folder" 100 ],
                childRenderBatchSize = 100,
                hasMoreChildren = (fun item -> item.Id = "folder"),
                onLoadMoreChildren = (fun _ -> requests <- requests + 1),
                automaticallyLoadChildren = false
            )
        )
        |> ignore

        do! RTL.act (fun () -> promise { triggerIntersection sentinelId true })

        Vitest.expect(requests).toBe 0
        Vitest.expect(RTL.screen.queryByTestId(sentinelId).IsSome).toBe true

        RTL.fireEvent.click (RTL.screen.getByText "Load more")
        Vitest.expect(requests).toBe 1
    }
)

Vitest.test (
    "expanded directories maintain independent child limits",
    fun () -> promise {
        renderBatched [ createFolder "folder-a" 250; createFolder "folder-b" 250 ]

        do! revealNextBatch "folder-a"
        Vitest.expect(isRendered "folder-a-199").toBe true
        Vitest.expect(isRendered "folder-b-099").toBe true
        Vitest.expect(isRendered "folder-b-100").toBe false
    }
)

Vitest.test (
    "collapsing a directory resets it to the initial batch",
    fun () -> promise {
        renderBatched [ createFolder "folder" 300 ]
        do! revealNextBatch "folder"
        do! revealNextBatch "folder"
        Vitest.expect(isRendered "folder-299").toBe true

        RTL.fireEvent.click (RTL.screen.getByText "folder")
        Vitest.expect(isRendered "folder-000").toBe false
        RTL.fireEvent.click (RTL.screen.getByText "folder")

        Vitest.expect(isRendered "folder-099").toBe true
        Vitest.expect(isRendered "folder-100").toBe false
    }
)

Vitest.test (
    "selection expands the parent batch enough to render the selected child",
    fun () ->
        let folder = {
            createFolder "folder" 250 with
                IsExpanded = false
        }

        RTL.render (
            FileExplorer.FileExplorer(
                initialItems = [ folder ],
                selectedItemId = Some "folder-149",
                childRenderBatchSize = 100
            )
        )
        |> ignore

        Vitest.expect(isRendered "folder-149").toBe true
        Vitest.expect(isRendered "folder-200").toBe false
)

Vitest.test (
    "batching remains disabled when no child batch size is supplied",
    fun () ->
        RTL.render (FileExplorer.FileExplorer(initialItems = [ createFolder "folder" 250 ]))
        |> ignore

        Vitest.expect(isRendered "folder-249").toBe true
        Vitest.expect(RTL.screen.queryByTestId("file-explorer-load-more-folder").IsNone).toBe true
)
