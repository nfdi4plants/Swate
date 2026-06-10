module ElectronCore.ArcObjectWidgetTests

open Swate.Components
open Swate.Components.Page.ARCObjectExplorer
open Swate.Components.Page.ARCObjectExplorer.Types
open Swate.Components.Page.FileExplorer
open Swate.Components.Page.FileExplorer.Types
open Vitest

let private folder id name itemType selectable children = {
    FileTree.createFolder name None FileItemIcon.Folder with
        Id = id
        Children = Some children
        ItemType = itemType
        Selectable = selectable
}

let private file id name icon itemType selectable = {
    FileTree.createFile name None icon with
        Id = id
        ItemType = itemType
        Selectable = selectable
}

let private expectSome value =
    match value with
    | Some value -> value
    | None -> failwith "Expected value to exist."

let private expectEntry itemId (result: ARCObjectExplorerItems) =
    let entry =
        result.Sections
        |> List.collect (fun section -> section.Items)
        |> List.tryFind (fun item -> item.Item.Id = itemId)

    match entry with
    | Some entry -> entry
    | None -> failwith $"Expected explorer item {itemId}."

Vitest.describe (
    "ARCObjectWidgetData.getExplorerItems",
    fun () ->
        Vitest.test (
            "returns selectable descendants recursively and skips structural groups",
            fun () ->
                let sample = file "sample" "Leaf-01" FileItemIcon.Tag "Sample" true
                let table = folder "table" "Metabolite Measurements" "Table" true [ sample ]
                let tablesGroup = folder "tables-group" "Tables" "Group" false [ table ]
                let note = file "note" "Sampling protocol" FileItemIcon.Document "Note" true
                let study = folder "study" "PlantStressStudy" "Study" true [ tablesGroup; note ]

                let result =
                    ARCObjectWidgetData.getExplorerItems (Some "study", [ study ]) |> expectSome

                Vitest.expect(result.SourceName).toBe ("PlantStressStudy")
                Vitest.expect(result.ContextItems |> List.map (fun item -> item.Item.Id)).toEqual ([ "study" ])

                Vitest
                    .expect(
                        result.ContextItems
                        |> List.filter (fun item -> item.IsCurrent)
                        |> List.map (fun item -> item.Item.Id)
                    )
                    .toEqual ([ "study" ])

                Vitest
                    .expect(result.Sections |> List.map (fun section -> section.Label))
                    .toEqual ([ "Table / Note"; "Sample" ])

                Vitest
                    .expect(result.Sections.[0].Items |> List.map (fun item -> item.Item.Id))
                    .toEqual ([ "table"; "note" ])

                Vitest.expect(result.Sections.[1].Items |> List.map (fun item -> item.Item.Id)).toEqual ([ "sample" ])

                let tableEntry = expectEntry "table" result
                let sampleEntry = expectEntry "sample" result
                let noteEntry = expectEntry "note" result

                Vitest
                    .expect(
                        result.Sections
                        |> List.collect (fun section -> section.Items)
                        |> List.exists (fun entry -> entry.Item.Id = "tables-group")
                    )
                    .toBe (false)

                Vitest.expect(tableEntry.Depth).toBe (1)
                Vitest.expect(tableEntry.Lineage).toEqual ([])
                Vitest.expect(sampleEntry.Depth).toBe (2)
                Vitest.expect(sampleEntry.Lineage).toEqual ([ "Metabolite Measurements" ])
                Vitest.expect(noteEntry.Depth).toBe (1)
                Vitest.expect(noteEntry.Lineage).toEqual ([])
        )

        Vitest.test (
            "builds a parent chain for nested leaf selections",
            fun () ->
                let sample = file "sample" "Leaf-01" FileItemIcon.Tag "Sample" true
                let table = folder "table" "Metabolite Measurements" "Table" true [ sample ]
                let tablesGroup = folder "tables-group" "Tables" "Group" false [ table ]
                let study = folder "study" "PlantStressStudy" "Study" true [ tablesGroup ]

                let result =
                    ARCObjectWidgetData.getExplorerItems (Some "sample", [ study ]) |> expectSome

                Vitest
                    .expect(result.ContextItems |> List.map (fun item -> item.Item.Id))
                    .toEqual ([ "study"; "tables-group"; "table"; "sample" ])

                Vitest
                    .expect(
                        result.ContextItems
                        |> List.filter (fun item -> item.IsCurrent)
                        |> List.map (fun item -> item.Item.Id)
                    )
                    .toEqual ([ "sample" ])

                Vitest.expect(result.Sections |> List.map (fun section -> section.Label)).toEqual ([ "Sample" ])
        )

        Vitest.test (
            "returns the selected item when it has no visible descendants",
            fun () ->
                let note = file "note" "Sampling protocol" FileItemIcon.Document "Note" true

                let result =
                    ARCObjectWidgetData.getExplorerItems (Some "note", [ note ]) |> expectSome

                Vitest.expect(result.SourceId).toBe ("note")
                Vitest.expect(result.ContextItems |> List.map (fun item -> item.Item.Id)).toEqual ([ "note" ])
                Vitest.expect(result.Sections.Length).toBe (1)
                Vitest.expect(result.Sections.Head.Label).toBe ("Note")
                Vitest.expect(result.Sections.Head.Items.Length).toBe (1)
                Vitest.expect(result.Sections.Head.Items.Head.Item.Id).toBe ("note")
                Vitest.expect(result.Sections.Head.Items.Head.Depth).toBe (0)
                Vitest.expect(result.Sections.Head.Items.Head.Lineage).toEqual ([])
        )
)
