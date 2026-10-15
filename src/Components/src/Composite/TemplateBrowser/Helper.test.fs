module internal Swate.Components.Composite.Template.HelperTests

open ARCtrl
open ARCtrl.Json
open ARCtrl.Spreadsheet
open Swate.Components.Shared
open Swate.Components.Composite.Template.Types
open Swate.Components.Composite.Template.Helper
open Vitest

let private createBasicAssay () =
    let table = ArcTable.init "New Table 0"
    table.AddColumn(CompositeHeader.Input IOType.Source)
    table.AddColumn CompositeHeader.ProtocolUri
    table.AddColumn(CompositeHeader.Output IOType.Sample)
    table.AddRowsEmpty 3
    table.SetCellAt(0, 0, CompositeCell.FreeText "source-1")
    let assay = ArcAssay.init "TestAssay"
    assay.AddTable table
    ArcFiles.Assay assay

let private createTemplate rowCount =
    let table = ArcTable.init "Template"
    table.AddColumn(CompositeHeader.Input IOType.Source)
    table.AddColumn CompositeHeader.ProtocolUri
    table.AddColumn(CompositeHeader.Output IOType.Sample)

    for index in 0..6 do
        table.AddColumn(
            CompositeHeader.Parameter(OntologyAnnotation($"Parameter {index}")),
            ResizeArray [
                for _ in 1..rowCount ->
                    CompositeCell.Unitized("21", OntologyAnnotation("degree Celsius", "UO", "UO:0000027"))
            ]
        )

    table

let private importTemplate importType fullImport deselected source target =
    let config = {
        SelectiveImportConfig.init () with
            ImportType = importType
            ImportTables = [ { Index = 0; FullImport = fullImport } ]
            DeselectedColumns = deselected
    }

    updateTables (ResizeArray [ source ]) config (Some 0) (Some target)

let private verifyReadableAndSerializable (file: ArcFiles) =
    // Materialize every column, including its cells, as the spreadsheet writer does.
    for table in file.Tables() do
        Vitest.expect(table.Columns.Count).toBe table.ColumnCount

        for column in table.Columns do
            Vitest.expect(column.Cells.Count).toBe table.RowCount

    match file with
    | ArcFiles.Assay assay -> ArcAssay.toFsWorkbook assay |> ignore
    | _ -> failwith "Expected an assay"

    for format in
        [
            JsonExportFormat.ARCtrl
            JsonExportFormat.ARCtrlCompressed
        ] do
        let _, json = Export.parseToJsonString (file, format)

        let restored =
            Generic.readFromJsonMap.[file.RelatedArcFilesDiscriminate, format] json

        for index in 0 .. file.Tables().Count - 1 do
            let expected = file.Tables().[index]
            let actual = restored.Tables().[index]
            Vitest.expect(actual.Headers).toEqual expected.Headers
            Vitest.expect(actual.RowCount).toBe expected.RowCount
            Vitest.expect(actual.Columns).toEqual expected.Columns

for sourceRows in [ 0; 2 ] do
    Vitest.test (
        $"header-only template with {sourceRows} rows leaves all ten columns readable and saveable",
        fun () ->
            let source = createTemplate sourceRows
            let target = createBasicAssay ()
            let updated = importTemplate TableJoinOptions.Headers false Set.empty source target
            let table = updated.Tables().[0]
            Vitest.expect(table.ColumnCount).toBe 10
            Vitest.expect(table.RowCount).toBe 3
            Vitest.expect(table.GetCellAt(0, 0)).toEqual (CompositeCell.FreeText "source-1")

            for columnIndex in 3..9 do
                Vitest.expect(table.GetCellAt(columnIndex, 0)).toEqual CompositeCell.emptyTerm

            Vitest.expect(source.RowCount).toBe sourceRows

            if sourceRows > 0 then
                Vitest.expect(source.GetCellAt(3, 0).AsUnitized |> fst).toBe "21"

            verifyReadableAndSerializable (ArcFiles.refreshRef updated)
    )

Vitest.test (
    "header-only imports honor deselected columns",
    fun () ->
        let updated =
            importTemplate
                TableJoinOptions.Headers
                false
                (Set.singleton (0, 4))
                (createTemplate 2)
                (createBasicAssay ())

        let table = updated.Tables().[0]
        Vitest.expect(table.ColumnCount).toBe 9
        Vitest.expect(table.Headers.Contains(CompositeHeader.Parameter(OntologyAnnotation("Parameter 1")))).toBe false
        verifyReadableAndSerializable updated
)

Vitest.test (
    "header-only templates imported as new tables stay readable after adding rows",
    fun () ->
        let updated =
            importTemplate TableJoinOptions.Headers true Set.empty (createTemplate 2) (createBasicAssay ())

        Vitest.expect(updated.Tables().Count).toBe 2
        let imported = updated.Tables().[1]
        Vitest.expect(imported.ColumnCount).toBe 10
        Vitest.expect(imported.RowCount).toBe 0
        imported.AddRowsEmpty 3
        verifyReadableAndSerializable (ArcFiles.refreshRef updated)
)

for importType in [ TableJoinOptions.WithUnit; TableJoinOptions.WithValues ] do
    Vitest.test (
        $"{importType} template imports preserve units and the requested values",
        fun () ->
            let updated =
                importTemplate importType false Set.empty (createTemplate 5) (createBasicAssay ())

            let table = updated.Tables().[0]
            Vitest.expect(table.ColumnCount).toBe 10
            Vitest.expect(table.GetCellAt(0, 0)).toEqual (CompositeCell.FreeText "source-1")
            let value, unit = table.GetCellAt(3, 0).AsUnitized
            Vitest.expect(unit.NameText).toBe "degree Celsius"

            let expectedValue =
                if importType = TableJoinOptions.WithValues then
                    "21"
                else
                    ""

            Vitest.expect(value).toBe expectedValue
            Vitest.expect(table.RowCount).toBe 5
            verifyReadableAndSerializable updated
    )
