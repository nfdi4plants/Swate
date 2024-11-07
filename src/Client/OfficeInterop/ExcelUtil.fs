module OfficeInterop.ExcelUtil

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings
open System.Collections.Generic
open System

open ARCtrl.Spreadsheet

[<Literal>]
let AppendIndex = -1.

[<Literal>]
let TableStyleLight = "TableStyleMedium7"

// ExcelApi 1.1
/// This function returns the names of all annotationTables in all worksheets.
let getAllTableNames (context: RequestContext) =
    // Ref. 2

    let tables = context.workbook.tables.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
    let _ = tables.load(propertyNames = U2.Case1 "name")

    context.sync()
        .``then``( fun _ ->

            /// Get all names of all tables in the whole workbook.
            let tableNames =
                tables.items
                |> Seq.toArray
                |> Array.map (fun x -> x.name)

            tableNames
    )

let createMatrixForTables (colCount: int) (rowCount: int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield [|
                for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

let inline excelRunWith<'A> (context: RequestContext option) (promise: RequestContext -> JS.Promise<'A>) =
    match context with
    | Some ctx -> promise ctx
    | None -> Excel.run (fun ctx -> promise ctx)

/// This is based on a excel hack on how to add multiple header of the same name to an excel table.,
/// by just appending more whitespace to the name.
let extendName (existingNames: string []) (baseName: string) =
    /// https://unicode-table.com/en/200B/
    /// Play with Fire 🔥
    let zeroWidthChar = '​'
    let rec loop (baseName:string) =
        if existingNames |> Array.contains baseName then
            loop (baseName + " ")
        else
            baseName
    loop baseName

let createNewTableName() =
    let guid = System.Guid.NewGuid().ToString("N")
    ArcTable.annotationTablePrefix + guid

/// <summary>
/// Get the excel table of the given context and name
/// </summary>
/// <param name="context"></param>
/// <param name="tableName"></param>
let tryGetTableByName (context: RequestContext) (tableName: string) =
    promise {
        let _ = context.workbook.load(U2.Case1 "tables")
        let excelTable = context.workbook.tables.getItem(tableName)

        if String.IsNullOrWhiteSpace tableName then
            return None
        else
            let annoHeaderRange = excelTable.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
            let annoBodyRange = excelTable.getDataBodyRange()
            let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore
            return Some (excelTable, annoHeaderRange, annoBodyRange)
    }

/// <summary>
/// Swaps 'Rows with column values' to 'Columns with row values'
/// </summary>
/// <param name="rows"></param>
let viewRowsByColumns (rows: ResizeArray<ResizeArray<'a>>) =
    rows
    |> Seq.collect (fun row -> Seq.indexed row)
    |> Seq.groupBy fst
    |> Seq.map (snd >> Seq.map snd >> Seq.toArray)
    |> Seq.toArray

 /// <summary>
/// Converts a sequence of sequence of excel data into a resizearray, compatible with Excel.Range
/// </summary>
/// <param name="metadataValues"></param>
let convertSeqToExcelRangeInput (metadataValues: seq<seq<string option>>) =

    //Selects the longest sequence of the metadata values
    //In the next step, determines the length of the longest metadata value sequence
    let maxLength = metadataValues |> Seq.maxBy Seq.length |> Seq.length

    //Adapts the length of the smaller sequences to the length of the longest sequence in order to avoid problems with the insertion into excel.Range
    let ra = ResizeArray()
    for seq in metadataValues do
        //Parse string to obj option
        let ira = ResizeArray (seq |> Seq.map (fun cell -> cell |> Option.map box))
        if ira.Count < maxLength then
            ira.AddRange (Array.create (maxLength - ira.Count) None)
        ra.Add ira
    ra

/// <summary>
/// This function is used as minimal test function to ensure a working excel function test suit
/// </summary>
let getSelectedRangeAdress (context: RequestContext) =
    promise {
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case1 "address") |> ignore
        do! context.sync()

        return range.address
    }

module Table =

    /// <summary>
    /// Add a new column at the index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="name"></param>
    /// <param name="rowCount"></param>
    /// <param name="value"></param>
    let addColumn (index: float) (excelTable: Table) name rowCount value =
        let col = createMatrixForTables 1 rowCount value

        excelTable.columns.add(
            index   = index,
            values  = U4.Case1 col,
            name    = name
        )

    /// <summary>
    /// Add a new column with the given values at the index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="values"></param>
    let addColumnAt (index: int) (excelTable: Table) (values: string []) =
        let columnValues =
            U4<Array<Array<U3<bool,string,float>>>,bool,string,float>.Case1 [|
                for row in values do
                    [|
                        U3.Case2 row
                    |] 
            |]

        excelTable.columns.add(
            index,
            columnValues
        )

    /// <summary>
    /// Delete an existing column at the given index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="name"></param>
    /// <param name="rowCount"></param>
    /// <param name="value"></param>
    let deleteColumn (index: float) (excelTable: Table) =
        let col = excelTable.columns.getItemAt index
        col.delete()

     /// <summary>
    /// Add only the row values of the column you are adding
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="excelTable"></param>
    /// <param name="columnName"></param>
    /// <param name="rows"></param>
    let addColumnAndRows (columnIndex: float) (excelTable: Table) columnName rows =
        excelTable.columns.add(
            index   = columnIndex,
            values  = U4.Case1 rows,
            name    = columnName
        )

    /// <summary>
    /// Add new rows with the same value to the table
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="rowCount"></param>
    /// <param name="value"></param>
    let addRows (index: float) (excelTable: Table) columnCount rowCount value =
        let col = createMatrixForTables columnCount rowCount value
        excelTable.rows.add(
            index   = index,
            values  = U4.Case1 col
        )
