module ElectronRenderer.ArcFilePreviewTargetTests

open Vitest
open ARCtrl
open Renderer.Components.MainContent.ArcFilePreviewTargetHelper
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Shared


let private createAssayArcFile (tableNames: string[]) =
    let assay = ArcAssay.init "TestAssay"
    assay.Title <- Some "Metadata title"
    assay.Description <- Some "Metadata description"

    tableNames
    |> Array.iter (fun tableName -> assay.AddTable(ArcTable.init tableName))

    ArcFiles.Assay assay, assay

let private deleteSelectedTable arcFile tableIndex =
    let mutable publishedArcFile: ArcFiles option = None
    let mutable activeView: ActiveView option = None

    deleteSelectedTable
        arcFile
        tableIndex
        (fun nextArcFile -> publishedArcFile <- Some nextArcFile)
        (fun nextActiveView -> activeView <- Some nextActiveView)

    publishedArcFile, activeView

let private expectMetadataPreserved (assay: ArcAssay) =
    Vitest.expect(assay.Identifier).toBe ("TestAssay")
    Vitest.expect(assay.Title).toEqual (Some "Metadata title")
    Vitest.expect(assay.Description).toEqual (Some "Metadata description")

let private expectMetadataView activeView =
    match activeView with
    | Some ActiveView.Metadata -> ()
    | _ -> failwith "Expected the metadata view to be activated after table deletion."

Vitest.describe("ArcFilePreviewTarget table deletion", fun () ->
    Vitest.test("deletes only the selected middle table and preserves metadata", fun () ->
        let arcFile, assay = createAssayArcFile [| "First"; "Selected"; "Last" |]

        let publishedArcFile, activeView = deleteSelectedTable arcFile 1

        Vitest.expect(assay.Tables |> Seq.map _.Name |> Seq.toArray).toEqual([| "First"; "Last" |])
        expectMetadataPreserved assay
        Vitest.expect(publishedArcFile.IsSome).toBe(true)
        expectMetadataView activeView
    )

    Vitest.test("deletes the only table while preserving metadata", fun () ->
        let arcFile, assay = createAssayArcFile [| "Only Table" |]

        let publishedArcFile, activeView = deleteSelectedTable arcFile 0

        Vitest.expect(assay.Tables.Count).toBe(0)
        expectMetadataPreserved assay
        Vitest.expect(publishedArcFile.IsSome).toBe(true)
        expectMetadataView activeView
    )
)
