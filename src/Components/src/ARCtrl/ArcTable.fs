module ARCtrl.Table

let private isNull value = obj.ReferenceEquals(box value, null)

let private ifNull fallback value = if isNull value then fallback else value

let tryNormalizeCell (cell: CompositeCell) =
    if isNull cell then
        None
    else
        match cell with
        | CompositeCell.FreeText text -> CompositeCell.FreeText(ifNull "" text)
        | CompositeCell.Term ontologyAnnotation -> CompositeCell.Term(ifNull (OntologyAnnotation()) ontologyAnnotation)
        | CompositeCell.Unitized(value, ontologyAnnotation) ->
            CompositeCell.Unitized(ifNull "" value, ifNull (OntologyAnnotation()) ontologyAnnotation)
        | CompositeCell.Data data -> CompositeCell.Data(ifNull (Data()) data)
        |> Some

let normalizeCells (table: ArcTable) =
    let normalizedTable = ArcTable.init table.Name

    for columnIndex in 0 .. table.Headers.Count - 1 do
        let header = table.Headers.[columnIndex]

        let cells =
            [
                for rowIndex in 0 .. table.RowCount - 1 do
                    table.TryGetCellAt(columnIndex, rowIndex)
                    |> Option.bind tryNormalizeCell
            ]

        let emptyCell = cells |> List.tryPick id |> ArcTableAux.getEmptyCellForHeader header

        normalizedTable.AddColumn(header, cells |> List.map (Option.defaultValue emptyCell) |> ResizeArray)

    normalizedTable

/// <summary>
/// This functions returns a **copy** of `toJoinTable` without any column already in `activeTable`.
/// </summary>
/// <param name="activeTable"></param>
/// <param name="toJoinTable"></param>
let distinctByHeader (activeTable: ArcTable) (toJoinTable: ArcTable) : ArcTable =
    // Remove existing columns
    let mutable columnsToRemove = []
    // find duplicate columns
    let tablecopy = toJoinTable.Copy()

    for header in activeTable.Headers do
        let containsAtIndex =
            tablecopy.Headers
            |> Seq.tryFindIndex (fun h ->
                let isEqual = h = header
                let isInput = h.isInput && header.isInput
                let isOutput = h.isOutput && header.isOutput
                isEqual || isInput || isOutput
            )

        if containsAtIndex.IsSome then
            columnsToRemove <- containsAtIndex.Value :: columnsToRemove

    tablecopy.RemoveColumns(Array.ofList columnsToRemove)
    tablecopy


/// <summary>
/// This function is meant to prepare a table for joining with another table.
///
/// It removes columns that are already present in the active table.
/// It also fills new Input/Output columns with the input/output values of the active table.
///
/// The output of this function can be used with the SpreadsheetInterface.JoinTable Message.
/// </summary>
/// <param name="activeTable">The active/current table</param>
/// <param name="toJoinTable">The new table, which will be added to the existing one.</param>
let selectiveTablePrepare (activeTable: ArcTable) (toJoinTable: ArcTable) (removeColumns: int list) : ArcTable =
    // Remove existing columns
    let mutable columnsToRemove = removeColumns

    // find duplicate columns
    let tablecopy = toJoinTable.Copy()

    for header in activeTable.Headers do
        let containsAtIndex = tablecopy.Headers |> Seq.tryFindIndex (fun h -> h = header)

        if containsAtIndex.IsSome then
            columnsToRemove <- containsAtIndex.Value :: columnsToRemove

    //Remove duplicates because unselected and already existing columns can overlap
    tablecopy.RemoveColumns(Array.ofList columnsToRemove)

    tablecopy.IteriColumns(fun i c0 ->

        let c2 =
            if c0.Header.isInput then
                match activeTable.TryGetInputColumn() with
                | Some ic -> { c0 with Cells = ic.Cells }
                | _ -> c0
            elif c0.Header.isOutput then
                match activeTable.TryGetOutputColumn() with
                | Some oc -> { c0 with Cells = oc.Cells }
                | _ -> c0
            else
                c0

        tablecopy.UpdateColumn(i, c2.Header, c2.Cells)
    )

    tablecopy
