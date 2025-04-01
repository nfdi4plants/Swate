module OfficeInterop.ArcTableHelper

open Fable.Core
open ExcelJS.Fable
open Excel

open ARCtrl
open ARCtrl.Spreadsheet

open OfficeInterop

open ExcelHelper

type ArcTable with

    /// <summary>
    /// Transforms ArcTable to excel compatible "values", row major
    /// </summary>
    member this.ToStringSeqs() =

        // Cancel if there are no columns
        if this.Columns.Length = 0 then
            ResizeArray()
        else
            let columns =
                this.Columns
                |> List.ofArray
                |> List.sortBy ArcTable.classifyColumnOrder
                |> List.collect CompositeColumn.toStringCellColumns
                |> Seq.transpose
                |> Seq.map (fun column -> column |> Seq.map (box >> Some) |> ResizeArray)
                |> ResizeArray

            columns

    /// <summary>
    /// Creates ArcTable based on table name and collections of strings, representing columns and rows.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="headers"></param>
    /// <param name="rows"></param>
    static member fromStringSeqs(name: string, headers: #seq<string>, rows: #seq<#seq<string>>) =

        let columns = Seq.append [ headers ] rows |> Seq.transpose

        let columnsList = columns |> Seq.toArray |> Array.map (Seq.toArray)

        let compositeColumns = ArcTable.composeColumns columnsList

        let arcTable =
            ArcTable.init name
            |> ArcTable.addColumns (compositeColumns, skipFillMissing = true)

        arcTable

    /// <summary>
    /// Try to create an arc table from an excel table
    /// </summary>
    /// <param name="excelTable"></param>
    /// <param name="context"></param>
    static member fromExcelTable(excelTable: Table, context: RequestContext) = promise {
        //Get headers and body
        let headerRange = excelTable.getHeaderRowRange ()
        let bodyRowRange = excelTable.getDataBodyRange ()

        let _ =
            excelTable.load (U2.Case2(ResizeArray [| "name" |])) |> ignore
            headerRange.load (U2.Case2(ResizeArray [| "values" |])) |> ignore
            bodyRowRange.load (U2.Case2(ResizeArray [| "values" |])) |> ignore


        return!
            context
                .sync()
                .``then`` (fun _ ->
                    let headers =
                        headerRange.values.[0]
                        |> Seq.map (fun item ->
                            item |> Option.map string |> Option.defaultValue "" |> (fun s -> s.TrimEnd()))

                    let bodyRows =
                        bodyRowRange.values
                        |> Seq.map (fun items ->
                            items
                            |> Seq.map (fun item -> item |> Option.map string |> Option.defaultValue ""))

                    try
                        ArcTable.fromStringSeqs (excelTable.worksheet.name, headers, bodyRows)
                        |> Result.Ok
                    with exn ->
                        Result.Error exn)
    }

    /// <summary>
    /// Try to get a arc table from excel file based on excel table name
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="context"></param>
    static member fromExcelTableName(tableName: string, context: RequestContext) = promise {
        let! table = tryGetTableByName context tableName

        match table with
        | Some(table, _, _) ->
            let! inMemoryTable = ArcTable.fromExcelTable (table, context)
            return inMemoryTable
        | None -> return Result.Error(exn $"Error. No table with the given name {tableName} found!")
    }

    /// <summary>
    /// Try find annotation table in active worksheet and parse to ArcTable
    /// </summary>
    /// <param name="context"></param>
    static member tryGetActiveArcTable(context: RequestContext) : JS.Promise<Result<ArcTable, exn>> = promise {
        let! excelTable = tryGetActiveExcelTable context

        match excelTable with
        | Some excelTable ->
            let! arcTable = ArcTable.fromExcelTable (excelTable, context)
            return arcTable
        | None -> return Result.Error(exn "Error! No active annotation table found!")
    }

    /// <summary>
    /// Get the previous arc table to the active worksheet
    /// </summary>
    /// <param name="context"></param>
    static member tryGetPrevArcTable(context: RequestContext) : JS.Promise<ArcTable option> = promise {
        let! prevTableName = tryGetPrevAnnotationTableName context

        match prevTableName with
        | Some name ->
            let! result = ArcTable.fromExcelTableName (name, context)

            return
                match result with
                | Ok table -> Some table
                | Result.Error _ -> None

        | None -> return None
    }