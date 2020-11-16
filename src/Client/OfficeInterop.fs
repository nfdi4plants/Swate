module OfficeInterop

open Fable.Core
open Fable.Core.JsInterop
open OfficeJS
open Excel
open System.Collections.Generic
open System.Text.RegularExpressions

type ColumnRepresentation = {
    Header          : string
    /// TODO: this is meant for future application and should be implemented together with separate unit columns
    Unit            : string option
    TagArray        : string []
    ParentOntology  : string option
} with
    static member init (?header) = {
        Header          = if header.IsSome then header.Value else ""
        Unit            = None
        TagArray        = [||]
        ParentOntology  = None
    }

[<LiteralAttribute>]
let HashNumberPattern = "#\d+"

[<LiteralAttribute>]
let SquaredBracketsPattern = "\[[^\]]*\]"

[<LiteralAttribute>]
let BracketsPattern = "\([^\]]*\)"

[<LiteralAttribute>]
let CoreNamePattern = "^[^[(]*"

[<Global>]
let Office : Office.IExports = jsNative

[<Global>]
//[<CompiledName("Office.Excel")>]
let Excel : Excel.IExports = jsNative

[<Global>]
let RangeLoadOptions : Interfaces.RangeLoadOptions = jsNative

[<Emit("console.log($0)")>]
let consoleLog (message: string): unit = jsNative
        //ranges.format.fill.color <- "red"
        //let ranges = context.workbook.getSelectedRanges()
        //let x = ranges.load(U2.Case1 "address")

open System

type ColHeader = {
    Header  : string
    CoreName: string option
    Ontology: string option
    TagArr: string [] option
}

let parseSquaredBrackets (headerStr:string) =
    match headerStr with
    | AuxFunctions.Regex SquaredBracketsPattern value ->
        // remove brackets
        value.[1..value.Length-2]
        |> Some
    | _ ->
        None

let parseBrackets (headerStr:string) =
    match headerStr with
    | AuxFunctions.Regex BracketsPattern value ->
        value
            // remove brackets
            .[1..value.Length-2]
            // split by separator to get information array
            // can consist of e.g. #h, #id, parentOntology
            .Split([|"; "|], StringSplitOptions.None)
        |> Some
    | _ ->
        None

let parseCoreName (headerStr:string) =
    match headerStr with
    | AuxFunctions.Regex BracketsPattern value ->
        value.Trim()
        |> Some
    | _ ->
        None

let parseColHeader (headerStr:string) =
    {
        Header      = headerStr
        CoreName    = parseCoreName headerStr
        Ontology    = parseSquaredBrackets headerStr
        TagArr      = parseBrackets headerStr
    }

let exampleExcelFunction () =
    Excel.run(fun context ->
    let sheet = context.workbook.worksheets.getActiveWorksheet()
    let annotationTable = sheet.tables.getItem("annotationTable")
    let annoHeaderRange = annotationTable.getHeaderRowRange()
    let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))
    context.sync().``then``(
        fun _ ->
            let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
            let baseColRepresentation =
                headerVals
                |> Array.choose id
                |> Array.map string
                |> Array.map (fun header -> ColumnRepresentation.init(header=header))
            let addTagArr (colRepArr:ColumnRepresentation []) =
                colRepArr
                |> Array.map (fun col ->
                    let parseHeader = parseColHeader col.Header
                    let tagArr =
                        match parseHeader.TagArr with
                        | Some strArr ->
                            // hidden columns are not meant to be represented
                            strArr 
                        | None ->
                            [||]
                    {col with TagArray = tagArr}
                )
            let addParentOntOption (colRepArr:ColumnRepresentation []) =
                colRepArr
                |> Array.map (fun col ->
                    let poOpt = col.TagArray |> Array.tryFind (fun tag -> tag.StartsWith "#" |> not)
                    match poOpt with
                    | Some parentOntology ->
                        {col with ParentOntology = Some parentOntology}
                    | None ->
                        {col with ParentOntology = None}
                )
            let filterOutHiddenCols (colRepArr:ColumnRepresentation []) =
                colRepArr
                |> Array.filter (fun x -> x.TagArray |> ((Array.contains "#h") >> not)  )
            let colReps =
                baseColRepresentation
                |> addTagArr
                |> addParentOntOption
                |> filterOutHiddenCols
            sprintf "Test: %A" colReps
        )
    )

let createEmptyMatrixForTables (colCount:int) (rowCount:int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield   [|
                        for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
                    |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

let createEmptyAnnotationMatrixForTables (rowCount:int) value (header:string) =
    [|
        for ind in 0 .. rowCount-1 do
            yield   [|
                for i in 0 .. 2 do
                    yield
                        match ind, i with
                        | 0, 0 ->
                            U3<bool,string,float>.Case2 header
                        | 0, 1 ->
                            U3<bool,string,float>.Case2 "Term Source REF"
                        | 0, 2 ->
                            U3<bool,string,float>.Case2 "Term Accession Number"
                        | _, _ ->
                            U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

let createValueMatrix (colCount:int) (rowCount:int) value =
    ResizeArray([
        for outer in 0 .. rowCount-1 do
            let tmp = Array.zeroCreate colCount |> Seq.map (fun _ -> Some (value |> box))
            ResizeArray(tmp)
    ])

let createAnnotationTable (isDark:bool) =
    Excel.run(fun context ->
        let tableRange = context.workbook.getSelectedRange()
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        //delete table with the same name if present because there can only be one chosen one <3
        sheet.tables.getItemOrNullObject("annotationTable").delete()

        let annotationTable = sheet.tables.add(U2.Case1 tableRange,true)
        annotationTable.name <- "annotationTable"

        tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount";"address"]))) |> ignore
        annotationTable.load(U2.Case1 "style") |> ignore

        //sync with proxy objects after loading values from excel
        context.sync()
            .``then``( fun _ ->

                let style =
                    if isDark then
                        "TableStyleMedium15"
                    else
                        "TableStyleMedium7"

                annotationTable.style <- style

                (annotationTable.columns.getItemAt 0.).name <- "Source Name"
                //(annotationTable.columns.getItemAt 0.).set

                sheet.getUsedRange().format.autofitColumns()
                sheet.getUsedRange().format.autofitRows()

                sprintf "Annotation Table created in [%s] with dimensions %.0f c x (%.0f + 1h)r" tableRange.address tableRange.columnCount (tableRange.rowCount - 1.)

            )
            //.catch (fun e -> e |> unbox<System.Exception> |> fun x -> x.Message)
    )

let autoFitTable () =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")
        let allCols = annotationTable.columns.load(propertyNames = U2.Case1 "items")
    
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))
        context.sync().``then``(
            fun _ ->
                let allCols = allCols.items |> Array.ofSeq
                let _ =
                    allCols
                    |> Array.map (fun col -> col.getRange())
                    |> Array.map (fun x ->
                        x.columnHidden <- false
                        x.format.autofitColumns()
                        x.format.autofitRows()
                    )
                // get all column headers
                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
                // get only column headers with values inside and map object to string
                let headerArr = headerVals |> Array.choose id |> Array.map string
                // parse header elements into record type
                let parsedHeaderArr = headerArr |> Array.map parseColHeader
                // find all columns to hide
                let colsToHide =
                    parsedHeaderArr
                    |> Array.filter (fun header -> header.TagArr.IsSome && Array.contains "#h" header.TagArr.Value)
                let ranges =
                    colsToHide
                    |> Array.map (fun header -> (annotationTable.columns.getItem (U2.Case2 header.Header)).getRange())
                let hideCols = ranges |> Array.map (fun x -> x.columnHidden <- true)
                "Autoformat Table"
            )
    )

let getTableRepresentation() =
    Excel.run(fun context ->
    let sheet = context.workbook.worksheets.getActiveWorksheet()
    let annotationTable = sheet.tables.getItem("annotationTable")
    let annoHeaderRange = annotationTable.getHeaderRowRange()
    let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))
    context.sync().``then``(
        fun _ ->
            let headerVals =
                annoHeaderRange.values.[0]
                |> Array.ofSeq
                |> Array.choose id
                |> Array.map string
            let parsedHeaders =
                headerVals |> Array.map parseColHeader
            let baseColRepresentation =
                parsedHeaders
                |> Array.map (fun header ->
                    let nColRep = ColumnRepresentation.init(header=header.Header)
                    { nColRep with
                        ParentOntology = header.Ontology
                        TagArray = if header.TagArr.IsSome then header.TagArr.Value else [||]
                    }
                )
            let filterOutHiddenCols (colRepArr:ColumnRepresentation []) =
                colRepArr
                |> Array.filter (fun x -> x.TagArray |> ((Array.contains "#h") >> not)  )
            let colReps =
                baseColRepresentation
                |> filterOutHiddenCols
            colReps, "Update table representation."
        )
    )

let checkIfAnnotationTableIsPresent () =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let t = sheet.load(U2.Case1 "tables")
        let table = t.tables.getItemOrNullObject("annotationTable")
        context.sync()
            .``then``( fun _ ->
                not table.isNullObject
        )
    )

let addThreeAnnotationColumns (colName:string) =

    let mainColName (parsedColHeader:ColHeader) (id:int) =
        match id with
        | 1 ->
            colName
        | _ ->
            sprintf "%s (#%i)" colName id
    let hiddenColAttributes (parsedColHeader:ColHeader) (id:int) =
        let coreName =
            match parsedColHeader.Ontology, parsedColHeader.CoreName with
            | Some o , _ -> o
            | None, Some cn -> cn
            | _ -> parsedColHeader.Header
        match id with
        | 1 ->
            (sprintf "[%s] (#h)" coreName)
        | _ ->
            (sprintf "[%s] (#%i; #h)" coreName id)
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")

        /// This is necessary to place new columns next to selected col
        let tables = annotationTable.columns.load(propertyNames = U2.Case1 "items")
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let range = context.workbook.getSelectedRange()
        annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"|])) |> ignore
        range.load(U2.Case1 "columnIndex") |> ignore

        ///
        let tableRange = annotationTable.getRange()
        tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"]))) |> ignore

        context.sync().``then``( fun _ ->

            /// This is necessary to place new columns next to selected col
            let tableHeaderRangeColIndex = annoHeaderRange.columnIndex
            let selectColIndex = range.columnIndex
            let diff = selectColIndex - tableHeaderRangeColIndex |> int
            let vals =
                tables.items
            let maxLength = vals.Count-1
            let newBaseColIndex =
                if diff < 0 then
                    maxLength+1
                elif diff > maxLength then
                    maxLength+1
                else
                    diff+1
                |> float

            // This is necessary to skip over hidden cols
            /// Get an array of the headers
            let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
            let indexedHiddenCols =
                headerVals
                |> Array.indexed
                |> Array.choose (fun (i,x) ->
                    let prep = parseColHeader (string x.Value) 
                    if x.IsSome && prep.TagArr.IsSome then
                        let checkIsHidden =
                            prep.TagArr.Value |> Array.contains "#h"
                        if checkIsHidden then 
                            Some (i+1 |> float)
                        else
                            None
                    else
                        None
                )
                //|> Array.filter (fun (i,x) -> x.TagArr.IsSome && Array.contains "#h" x.TagArr.Value)
            let rec loopingCheckSkip (newInd:float) =
                let nextIsHidden =
                    Array.exists (fun i -> i = newInd+1.) indexedHiddenCols
                if nextIsHidden then
                    loopingCheckSkip (newInd+1.)
                else
                    newInd
            /// Here is the next col index, which is not hidden, calculated.
            let newBaseColIndex' = loopingCheckSkip newBaseColIndex

            // This is necessary to prevent trying to create a column with an already existing name
            let parsedBaseHeader = parseColHeader colName
            let findNewIdForName() =
                let prepArr =
                    headerVals
                    |> Array.choose id
                    |> Array.map string
                let rec loopingCheck int =
                    let isExisting =
                        prepArr
                        // Should a column with the same name already exist, then count up the id tag.
                        |> Array.exists (fun existingHeader ->
                            existingHeader = mainColName parsedBaseHeader int
                            // i think it is necessary to also check for "T S REF" and "T A N" because of the following possibilities
                            // Parameter [instrument model] | "Term Source REF [instrument model] (#h) | ...
                            // Factor [instrument model] | "Term Source REF [instrument model] (#h) | ...
                            // in the example above the coreColName is different but "T S REF" and "T A N" would be the same.
                            || existingHeader = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader int)
                            || existingHeader = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader int)
                        )
                    if isExisting then
                        loopingCheck (int+1)
                    else
                        int
                loopingCheck 1
            let newId = findNewIdForName()

            let rowCount = tableRange.rowCount |> int
            //create an empty column to insert
            let col =
                createEmptyMatrixForTables 1 rowCount ""
            let createdCol1 =
                annotationTable.columns.add(
                    index = newBaseColIndex',
                    values = U4.Case1 col,
                    name = mainColName parsedBaseHeader newId
                )

            let createdCol2 =
                annotationTable.columns.add(
                    index = newBaseColIndex'+1.,
                    values = U4.Case1 col,
                    name = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader newId)
                )
            let createdCol3 =
                annotationTable.columns.add(
                    index = newBaseColIndex'+2.,
                    values = U4.Case1 col,
                    name = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader newId)
                )

            mainColName parsedBaseHeader newId, sprintf "%s column was added. %A" colName headerVals
        )
    )

let changeTableColumnFormat (colName:string) (format:string) =
    Excel.run(fun context ->
       let sheet = context.workbook.worksheets.getActiveWorksheet()
       let annotationTable = sheet.tables.getItem("annotationTable")

       let colBodyRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
       colBodyRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"]))) |> ignore

       context.sync().``then``( fun _ ->
           let rowCount = colBodyRange.rowCount |> int
           //create an empty column to insert
           let formats = createValueMatrix 1 rowCount format

           colBodyRange.numberFormat <- formats

           sprintf "format of %s was changed to %s" colName format
       )
    )

let fillValue (v,fillTerm:Shared.DbDomain.Term option) =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")
        //let annoRange = annotationTable.getDataBodyRange()
        //let _ = annoRange.load(U2.Case2 (ResizeArray(["address";"values";"columnIndex"; "columnCount"])))
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["address";"values";"columnIndex"; "columnCount"])))
        let nextColsRange = range.getColumnsAfter 2.
        let _ = nextColsRange.load(U2.Case2 (ResizeArray(["address";"values";"columnIndex";"columnCount"])))
        //sync with proxy objects after loading values from excel
        context.sync().``then``( fun _ ->
            if range.columnCount > 1. then failwith "Cannot insert Terms in more than one column at a time."
            let newVals = ResizeArray([
                for arr in range.values do
                    let tmp = arr |> Seq.map (fun _ -> Some (v |> box))
                    ResizeArray(tmp)
            ])

            let nextNewVals = ResizeArray([
                for ind in 0 .. nextColsRange.values.Count-1 do
                    let tmp =
                        nextColsRange.values.[ind]
                        |> Seq.mapi (fun i _ ->
                            match i, fillTerm with
                            | 0, None | 1, None ->
                                Some ("user-specific" |> box)
                            | 1, Some term ->
                                //add "Term Accession Number"
                                let replace = Shared.URLs.TermAccessionBaseUrl + "/" + term.Accession.Replace(@":",@"_")
                                Some ( replace |> box )
                            | 0, Some term ->
                                //add "Term Source REF"
                                Some (term.Accession.Split(@":").[0] |> box)
                            | _, _ ->
                                failwith "The insert should never add more than two extra columns."
                        )
                    ResizeArray(tmp)
            ])

            range.values <- newVals
            nextColsRange.values <- nextNewVals
            //sprintf "%s filled with %s; ExtraCols: %s" range.address v nextColsRange.address
            sprintf "%A, %A" nextColsRange.values.Count nextNewVals
        )
    )

let getTableMetaData () =
    Excel.run (fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")
        annotationTable.columns.load(propertyNames = U2.Case1 "count") |> ignore
        annotationTable.rows.load(propertyNames = U2.Case1 "count")    |> ignore
        let rowRange = annotationTable.getRange()
        rowRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore
        let headerRange = annotationTable.getHeaderRowRange()
        headerRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore

        context.sync().``then``(fun _ ->
            let colCount,rowCount = annotationTable.columns.count, annotationTable.rows.count
            let rowRangeAddr, rowRangeColCount, rowRangeRowCount = rowRange.address,rowRange.columnCount,rowRange.rowCount
            let headerRangeAddr, headerRangeColCount, headerRangeRowCount = headerRange.address,headerRange.columnCount,headerRange.rowCount

            sprintf "Table Metadata: [Table] : %ic x %ir ; [TotalRange] : %s : %ic x %ir ; [HeaderRowRange] : %s : %ic x %ir "
                (colCount            |> int)
                (rowCount            |> int)
                (rowRangeAddr.Replace("Sheet",""))
                (rowRangeColCount    |> int)
                (rowRangeRowCount    |> int)
                (headerRangeAddr.Replace("Sheet",""))
                (headerRangeColCount |> int)
                (headerRangeRowCount |> int)
        )
    )

// Reform this to onSelectionChanged
let getParentTerm () =
    Excel.run (fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")
        let tables = annotationTable.columns.load(propertyNames = U2.Case1 "items")
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let range = context.workbook.getSelectedRange()
        annoHeaderRange.load(U2.Case1 "columnIndex") |> ignore
        range.load(U2.Case1 "columnIndex") |> ignore
        context.sync()
            .``then``( fun _ ->
                let tableHeaderRangeColIndex = annoHeaderRange.columnIndex
                let selectColIndex = range.columnIndex
                let diff = selectColIndex - tableHeaderRangeColIndex |> int
                let vals =
                    tables.items
                let maxLength = vals.Count-1
                let value =
                    if diff < 0 || diff > maxLength then
                        None
                    else
                        let value1 = (vals.[diff].values.Item 0)
                        value1.Item 0
                //sprintf "%A::> %A : %A : %A" value diff tableHeaderRangeColIndex selectColIndex
                value
            )
    )

//let autoGetSelectedHeader () =
//    Excel.run (fun context ->
//        let sheet = context.workbook.worksheets.getActiveWorksheet()
//        let annotationTable = sheet.tables.getItem("annotationTable")
//        annotationTable.onSelectionChanged.add(fun e -> getParentOntology())
//        context.sync()
//    )

let syncContext (passthroughMessage : string) =
    Excel.run (fun context -> context.sync(passthroughMessage))