module ARCtrl.ISA.Spreadsheet.ArcTable

open ARCtrl.ISA
open FsSpreadsheet

// I think we really should not add FSharpAux for exactly one function.
module Aux =

    module List =

        /// Iterates over elements of the input list and groups adjacent elements.
        /// A new group is started when the specified predicate holds about the element
        /// of the list (and at the beginning of the iteration).
        ///
        /// For example: 
        ///    List.groupWhen isOdd [3;3;2;4;1;2] = [[3]; [3; 2; 4]; [1; 2]]
        let groupWhen f list =
            list
            |> List.fold (
                fun acc e ->
                    match f e, acc with
                    | true  , _         -> [e] :: acc       // true case
                    | false , h :: t    -> (e :: h) :: t    // false case, non-empty acc list
                    | false , _         -> [[e]]            // false case, empty acc list
            ) []
            |> List.map List.rev
            |> List.rev
 

type ColumnOrder =
    | InputClass = 1
    | ProtocolClass = 2
    | ParamsClass = 3
    | OutputClass = 4

let classifyHeaderOrder (header : CompositeHeader) =     
    match header with
    | CompositeHeader.Input             _ -> ColumnOrder.InputClass

    | CompositeHeader.ProtocolType          
    | CompositeHeader.ProtocolDescription
    | CompositeHeader.ProtocolUri
    | CompositeHeader.ProtocolVersion
    | CompositeHeader.ProtocolREF       
    | CompositeHeader.Performer
    | CompositeHeader.Date                -> ColumnOrder.ProtocolClass

    | CompositeHeader.Component         _
    | CompositeHeader.Characteristic    _
    | CompositeHeader.Factor            _
    | CompositeHeader.Parameter         _ 
    | CompositeHeader.FreeText          _ -> ColumnOrder.ParamsClass

    | CompositeHeader.Output            _ -> ColumnOrder.OutputClass

let classifyColumnOrder (column : CompositeColumn) =
    column.Header
    |> classifyHeaderOrder

[<Literal>]
let annotationTablePrefix = "annotationTable"

let groupColumnsByHeader (columns : list<FsColumn>) = 
    columns
    |> Aux.List.groupWhen (fun c -> 
        let v = c.[1].ValueAsString()
        Regex.tryParseReferenceColumnHeader v
        |> Option.isNone
        &&
        (v.StartsWith "Unit" |> not)
    )

/// Returns the annotation table of the worksheet if it exists, else returns None
let tryAnnotationTable (sheet : FsWorksheet) =
    sheet.Tables
    |> Seq.tryFind (fun t -> t.Name.StartsWith annotationTablePrefix)

/// Groups and parses a collection of single columns into the according ISA composite columns
let composeColumns (columns : seq<FsColumn>) : CompositeColumn [] =
    columns
    |> Seq.toList
    |> groupColumnsByHeader
    |> List.map CompositeColumn.fromFsColumns
    |> List.toArray

/// Returns the protocol described by the headers and a function for parsing the values of the matrix to the processes of this protocol
let tryFromFsWorksheet (sheet : FsWorksheet) =
    match tryAnnotationTable sheet with
    | Some (t: FsTable) -> 
        let compositeColumns = 
            t.GetColumns(sheet.CellCollection)
            |> Seq.map CompositeColumn.fixDeprecatedIOHeader
            |> composeColumns
        ArcTable.init sheet.Name
        |> ArcTable.addColumns(compositeColumns,SkipFillMissing = true)
        |> Some
    | None ->
        None

let toFsWorksheet (table : ArcTable) =
    /// This dictionary is used to add spaces at the end of duplicate headers.
    let stringCount = System.Collections.Generic.Dictionary<string,string>()
    let ws = FsWorksheet(table.Name)
    let columns = 
        table.Columns
        |> List.ofArray
        |> List.sortBy classifyColumnOrder
        |> List.collect CompositeColumn.toFsColumns
    let maxRow = columns.Head.Length
    let maxCol = columns.Length
    let fsTable = ws.Table("annotationTable",FsRangeAddress(FsAddress(1,1),FsAddress(maxRow,maxCol)))
    columns
    |> List.iteri (fun colI col ->         
        col
        |> List.iteri (fun rowI cell -> 
            let value = 
                let v = cell.ValueAsString()
                if rowI = 0 then
                    
                    match Dictionary.tryGet v stringCount with
                    | Some spaces ->
                        stringCount.[v] <- spaces + " "
                        v + " " + spaces
                    | None ->
                        stringCount.Add(cell.ValueAsString(),"")
                        v
                else v
            let address = FsAddress(rowI+1,colI+1)
            fsTable.Cell(address, ws.CellCollection).SetValueAs value
        )  
    )
    ws