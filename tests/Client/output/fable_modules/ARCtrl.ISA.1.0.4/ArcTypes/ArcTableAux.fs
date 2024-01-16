module ARCtrl.ISA.ArcTableAux

open ARCtrl.ISA
open System.Collections.Generic
open Fable.Core

// Taken from FSharpAux.Core
/// .Net Dictionary
module Dictionary = 
    
    /// <summary>Returns the dictionary with the binding added to the given dictionary.
    /// If a binding with the given key already exists in the input dictionary, the existing binding is replaced by the new binding in the result dictionary.</summary>
    /// <param name="key">The input key.</param>
    /// <returns>The dictionary with change in place.</returns>
    let addOrUpdateInPlace key value (table:IDictionary<_,_>) =
        match table.ContainsKey(key) with
        | true  -> table.[key] <- value
        | false -> table.Add(key,value)
        table

    /// <summary>Lookup an element in the dictionary, returning a <c>Some</c> value if the element is in the domain 
    /// of the dictionary and <c>None</c> if not.</summary>
    /// <param name="key">The input key.</param>
    /// <returns>The mapped value, or None if the key is not in the dictionary.</returns>
    let tryFind key (table:IDictionary<_,_>) =
        match table.ContainsKey(key) with
        | true -> Some table.[key]
        | false -> None

let getColumnCount (headers:ResizeArray<CompositeHeader>) = 
    headers.Count

let getRowCount (values:Dictionary<int*int,CompositeCell>) = 
    if values.Count = 0 then 0 else
        values.Keys |> Seq.maxBy snd |> snd |> (+) 1

// TODO: Move to CompositeHeader?
let (|IsUniqueExistingHeader|_|) existingHeaders (input: CompositeHeader) = 
    match input with
    | CompositeHeader.Parameter _
    | CompositeHeader.Factor _
    | CompositeHeader.Characteristic _
    | CompositeHeader.Component _
    | CompositeHeader.FreeText _        -> None
    // Input and Output does not look very clean :/
    | CompositeHeader.Output _          -> Seq.tryFindIndex (fun h -> match h with | CompositeHeader.Output _ -> true | _ -> false) existingHeaders
    | CompositeHeader.Input _           -> Seq.tryFindIndex (fun h -> match h with | CompositeHeader.Input _ -> true | _ -> false) existingHeaders
    | header                            -> Seq.tryFindIndex (fun h -> h = header) existingHeaders
        
// TODO: Move to CompositeHeader?
/// Returns the column index of the duplicate unique column in `existingHeaders`.
let tryFindDuplicateUnique (newHeader: CompositeHeader) (existingHeaders: seq<CompositeHeader>) = 
    match newHeader with
    | IsUniqueExistingHeader existingHeaders index -> Some index
    | _ -> None

/// Returns the column index of the duplicate unique column in `existingHeaders`.
let tryFindDuplicateUniqueInArray (existingHeaders: seq<CompositeHeader>) = 
    let rec loop i (duplicateList: {|Index1: int; Index2: int; HeaderType: CompositeHeader|} list) (headerList: CompositeHeader list) =
        match headerList with
        | _ :: [] | [] -> duplicateList
        | header :: tail -> 
            let hasDuplicate = tryFindDuplicateUnique header tail
            let nextDuplicateList = if hasDuplicate.IsSome then {|Index1 = i; Index2 = hasDuplicate.Value; HeaderType = header|}::duplicateList else duplicateList
            loop (i+1) nextDuplicateList tail
    existingHeaders
    |> Seq.filter (fun x -> not x.IsTermColumn)
    |> List.ofSeq
    |> loop 0 []

module SanityChecks =

    /// Checks if given column index is valid for given number of columns.
    ///
    /// if `allowAppend` = true => `0 < index <= columnCount`
    /// 
    /// if `allowAppend` = false => `0 < index < columnCount`
    let validateColumnIndex (index:int) (columnCount:int) (allowAppend:bool) =
        let eval x y = if allowAppend then x > y else x >= y
        if index < 0 then failwith "Cannot insert CompositeColumn at index < 0."
        if eval index columnCount then failwith $"Specified index is out of table range! Table contains only {columnCount} columns."

    /// Checks if given index is valid for given number of rows.
    ///
    /// if `allowAppend` = true => `0 < index <= rowCount`
    /// 
    /// if `allowAppend` = false => `0 < index < rowCount`
    let validateRowIndex (index:int) (rowCount:int) (allowAppend:bool) =
        let eval x y = if allowAppend then x > y else x >= y
        if index < 0 then failwith "Cannot insert CompositeColumn at index < 0."
        if eval index rowCount then failwith $"Specified index is out of table range! Table contains only {rowCount} rows."

    let validateColumn (column:CompositeColumn) = column.Validate(true) |> ignore

    let inline validateNoDuplicateUniqueColumns (columns:seq<CompositeColumn>) =
        let duplicates = columns |> Seq.map (fun x -> x.Header) |> tryFindDuplicateUniqueInArray
        if not <| List.isEmpty duplicates then
            let baseMsg = "Found duplicate unique columns in `columns`."
            let sb = System.Text.StringBuilder()
            sb.AppendLine(baseMsg) |> ignore
            duplicates |> List.iter (fun (x: {| HeaderType: CompositeHeader; Index1: int; Index2: int |}) -> 
                sb.AppendLine($"Duplicate `{x.HeaderType}` at index {x.Index1} and {x.Index2}.")
                |> ignore
            )
            let msg = sb.ToString()
            failwith msg

    let inline validateNoDuplicateUnique (header: CompositeHeader) (columns:seq<CompositeHeader>) =
        match tryFindDuplicateUnique header columns with
        | None -> ()
        | Some i -> failwith $"Invalid input. Tried setting unique header `{header}`, but header of same type already exists at index {i}."

    let inline validateRowLength (newCells: seq<CompositeCell>) (columnCount: int) =
        let newCellsCount = newCells |> Seq.length
        match columnCount with
        | 0 ->
            failwith $"Table contains no columns! Cannot add row to empty table!"
        | unequal when newCellsCount <> columnCount ->
            failwith $"Cannot add a new row with {newCellsCount} cells, as the table has {columnCount} columns."
        | _ ->
            ()

module Unchecked =
        
    let tryGetCellAt (column: int,row: int) (cells:System.Collections.Generic.Dictionary<int*int,CompositeCell>) = Dictionary.tryFind (column, row) cells
    let setCellAt(columnIndex, rowIndex,c : CompositeCell) (cells:Dictionary<int*int,CompositeCell>) = Dictionary.addOrUpdateInPlace (columnIndex,rowIndex) c cells |> ignore
    let moveCellTo (fromCol:int,fromRow:int,toCol:int,toRow:int) (cells:Dictionary<int*int,CompositeCell>) =
        match Dictionary.tryFind (fromCol, fromRow) cells with
        | Some c ->
            // Remove value. This is necessary in the following scenario:
            //
            // "AddColumn.Existing Table.add less rows, insert at".
            //
            // Assume a table with 5 rows, insert column with 2 cells. All 5 rows at `index` position are shifted +1, but only row 0 and 1 are replaced with new values.
            // Without explicit removing, row 2..4 would stay as is.
            // EDIT: First remove then set cell to otherwise a `amount` = 0 would just remove the cell!
            cells.Remove((fromCol,fromRow)) |> ignore
            setCellAt(toCol,toRow,c) cells
            |> ignore
        | None -> ()
    let removeHeader (index:int) (headers:ResizeArray<CompositeHeader>) = headers.RemoveAt (index)
    /// Remove cells of one Column, change index of cells with higher index to index - 1
    let removeColumnCells (index:int) (cells:Dictionary<int*int,CompositeCell>) = 
        for KeyValue((c,r),_) in cells do
            // Remove cells of column
            if c = index then
                cells.Remove((c,r))
                |> ignore
            else
                ()
    /// Remove cells of one Column, change index of cells with higher index to index - 1
    let removeColumnCells_withIndexChange (index:int) (columnCount:int) (rowCount:int) (cells:Dictionary<int*int,CompositeCell>) = 
        // Cannot loop over collection and change keys of existing.
        // Therefore we need to run over values between columncount and rowcount.
        for col = index to (columnCount-1) do
            for row = 0 to (rowCount-1) do
                if col = index then
                    cells.Remove((col,row))
                    |> ignore
                // move to left if "column index > index"
                elif col > index then
                    moveCellTo(col,row,col-1,row) cells
                else
                    ()
    let removeRowCells (rowIndex:int) (cells:Dictionary<int*int,CompositeCell>) = 
        for KeyValue((c,r),_) in cells do
            // Remove cells of column
            if r = rowIndex then
                cells.Remove((c,r))
                |> ignore
            else
                ()
    /// Remove cells of one Row, change index of cells with higher index to index - 1
    let removeRowCells_withIndexChange (rowIndex:int) (columnCount:int) (rowCount:int) (cells:Dictionary<int*int,CompositeCell>) = 
        // Cannot loop over collection and change keys of existing.
        // Therefore we need to run over values between columncount and rowcount.
        for row = rowIndex to (rowCount-1) do
            for col = 0 to (columnCount-1) do
                if row = rowIndex then
                    cells.Remove((col,row))
                    |> ignore
                // move to top if "row index > index"
                elif row > rowIndex then
                    moveCellTo(col,row,col,row-1) cells
                else
                    ()

    /// Get an empty cell fitting for related column header.
    ///
    /// `columCellOption` is used to decide between `CompositeCell.Term` or `CompositeCell.Unitized`. `columCellOption` can be any other cell in the same column, preferably the first one.
    let getEmptyCellForHeader (header:CompositeHeader) (columCellOption: CompositeCell option) =
        match header.IsTermColumn with
        | false                                 -> CompositeCell.emptyFreeText
        | true ->
            match columCellOption with
            | Some (CompositeCell.Term _) 
            | None                              -> CompositeCell.emptyTerm
            | Some (CompositeCell.Unitized _)   -> CompositeCell.emptyUnitized
            | _                                 -> failwith "[extendBodyCells] This should never happen, IsTermColumn header must be paired with either term or unitized cell."

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newHeader"></param>
    /// <param name="newCells"></param>
    /// <param name="index"></param>
    /// <param name="forceReplace"></param>
    /// <param name="onlyHeaders">If set to true, no values will be added</param>
    /// <param name="headers"></param>
    /// <param name="values"></param>
    let addColumn (newHeader: CompositeHeader) (newCells: CompositeCell []) (index: int) (forceReplace: bool) (onlyHeaders: bool) (headers: ResizeArray<CompositeHeader>) (values:Dictionary<int*int,CompositeCell>) =
        let mutable numberOfNewColumns = 1
        let mutable index = index
        /// If this isSome and the function does not raise exception we are executing a forceReplace.
        let hasDuplicateUnique = tryFindDuplicateUnique newHeader headers
        // implement fail if unique column should be added but exists already
        if not forceReplace && hasDuplicateUnique.IsSome then failwith $"Invalid new column `{newHeader}`. Table already contains header of the same type on index `{hasDuplicateUnique.Value}`"
        // Example: existingCells contains `Output io` (With io being any IOType) and header is `Output RawDataFile`. This should replace the existing `Output io`.
        // In this case the number of new columns drops to 0 and we insert the index of the existing `Output io` column.
        if hasDuplicateUnique.IsSome then
            numberOfNewColumns <- 0
            index <- hasDuplicateUnique.Value
        /// This ensures nothing gets messed up during mutable insert, for example inser header first and change ColumCount in the process
        let startColCount, startRowCount = getColumnCount headers, getRowCount values
        // headers are easily added. Just insert at position of index. This will insert without replace.
        let setNewHeader() = 
            // if duplication found and we want to forceReplace we remove related header
            if hasDuplicateUnique.IsSome then
                removeHeader(index) headers
            headers.Insert(index, newHeader)
        /// For all columns with index >= we need to increase column index by `numberOfNewColumns`.
        /// We do this by moving all these columns one to the right with mutable dictionary set logic (cells.[key] <- newValue), 
        /// Therefore we need to start with the last column to not overwrite any values we still need to shift
        let increaseColumnIndices() =
            /// Get last column index
            let lastColumnIndex = System.Math.Max(startColCount - 1, 0) // If there are no columns. We get negative last column index. In this case just return 0.
            // start with last column index and go down to `index`
            for columnIndex = lastColumnIndex downto index do
                for rowIndex in 0 .. startRowCount do
                    moveCellTo(columnIndex,rowIndex,columnIndex+numberOfNewColumns,rowIndex) values
        /// Then we can set the new column at `index`
        let setNewCells() =
            // Not sure if this is intended? If we for example `forceReplace` a single column table with `Input`and 5 rows with a new column of `Input` ..
            // ..and only 2 rows, then table RowCount will decrease from 5 to 2.
            // Related Test: `All.ArcTable.addColumn.Existing Table.add less rows, replace input, force replace
            if hasDuplicateUnique.IsSome then
                removeColumnCells(index) values
            let f = 
                if index >= startColCount then 
                    fun (colIndex,rowIndex,cell) (values : Dictionary<int*int,CompositeCell>) -> 
                        values.Add((colIndex,rowIndex),cell) |> ignore
                else 
                   setCellAt
            newCells 
            |> Array.iteri (fun rowIndex cell ->
                let columnIndex = index
                f (columnIndex,rowIndex,cell) values
            )
        setNewHeader()
        // Only do this if column is inserted and not appended AND we do not execute forceReplace!
        if index < startColCount && hasDuplicateUnique.IsNone then
            increaseColumnIndices()
        // 
        if not onlyHeaders then 
            setNewCells()
        ()

    // We need to calculate the max number of rows between the new columns and the existing columns in the table.
    // `maxRows` will be the number of rows all columns must have after adding the new columns.
    // This behaviour should be intuitive for the user, as Excel handles this case in the same way.
    let fillMissingCells (headers: ResizeArray<CompositeHeader>) (values:Dictionary<int*int,CompositeCell>) =
        let rowCount = getRowCount values
        let columnCount = getColumnCount headers
        let maxRows = rowCount
        let lastColumnIndex = columnCount - 1
        /// Get all keys, to map over relevant rows afterwards
        let keys = values.Keys
        // iterate over columns
        for columnIndex in 0 .. lastColumnIndex do
            /// Only get keys for the relevant column
            let colKeys = keys |> Seq.filter (fun (c,_) -> c = columnIndex) |> Set.ofSeq 
            /// Create set of expected keys
            let expectedKeys = Seq.init maxRows (fun i -> columnIndex,i) |> Set.ofSeq 
            /// Get the missing keys
            let missingKeys = Set.difference expectedKeys colKeys 
            // if no missing keys, we are done and skip the rest, if not empty missing keys we ...
            if missingKeys.IsEmpty |> not then
                /// .. first check which empty filler `CompositeCells` we need. 
                ///
                /// We use header to decide between CompositeCell.Term/CompositeCell.Unitized and CompositeCell.FreeText
                let relatedHeader = headers.[columnIndex]
                /// We use the first cell in the column to decide between CompositeCell.Term and CompositeCell.Unitized
                ///
                /// Not sure if we can add a better logic to infer if empty cells should be term or unitized ~Kevin F
                let tryExistingCell = if colKeys.IsEmpty then None else Some values.[colKeys.MinimumElement]
                let empty = getEmptyCellForHeader relatedHeader tryExistingCell
                for missingColumn,missingRow in missingKeys do
                    setCellAt (missingColumn,missingRow,empty) values

    /// Increases the table size to the given new row count and fills the new rows with the last value of the column
    let extendToRowCount rowCount (headers: ResizeArray<CompositeHeader>) (values:Dictionary<int*int,CompositeCell>) =
        let columnCount = getColumnCount headers
        let previousRowCount = getRowCount values
        // iterate over columns
        for columnIndex = 0 to columnCount - 1 do
            let lastValue = values[columnIndex,previousRowCount-1]
            for rowIndex = previousRowCount - 1 to rowCount - 1 do
                setCellAt (columnIndex,rowIndex,lastValue) values

    let addRow (index:int) (newCells:CompositeCell []) (headers: ResizeArray<CompositeHeader>) (values:Dictionary<int*int,CompositeCell>) =
        /// Store start rowCount here, so it does not get changed midway through
        let rowCount = getRowCount values
        let columnCount = getColumnCount headers
        let increaseRowIndices =  
            // Only do this if column is inserted and not appended!
            if index < rowCount then
                /// Get last row index
                let lastRowIndex = System.Math.Max(rowCount - 1, 0) // If there are no rows. We get negative last column index. In this case just return 0.
                // start with last row index and go down to `index`
                for rowIndex = lastRowIndex downto index do
                    for columnIndex in 0 .. (columnCount-1) do
                        moveCellTo(columnIndex,rowIndex,columnIndex,rowIndex+1) values
        /// Then we can set the new row at `index`
        let setNewCells =
            newCells |> Array.iteri (fun columnIndex cell ->
                let rowIndex = index
                setCellAt (columnIndex,rowIndex,cell) values
            )
        ()

/// Functions for transforming base level ARC Table and ISA Json Objects
module JsonTypes = 

    /// Convert a CompositeCell to a ISA Value and Unit tuple.
    let valueOfCell (value : CompositeCell) =
        match value with
        | CompositeCell.FreeText (text) -> Value.fromString text, None
        | CompositeCell.Term (term) -> Value.Ontology term, None
        | CompositeCell.Unitized (text,unit) -> Value.fromString text, Some unit

    /// Convert a CompositeHeader and Cell tuple to a ISA Component
    let composeComponent (header : CompositeHeader) (value : CompositeCell) : Component =
        let v,u = valueOfCell value
        Component.fromOptions (Some v) u (header.ToTerm() |> Some)

    /// Convert a CompositeHeader and Cell tuple to a ISA ProcessParameterValue
    let composeParameterValue (header : CompositeHeader) (value : CompositeCell) : ProcessParameterValue =
        let v,u = valueOfCell value
        let p = ProtocolParameter.create(ParameterName = header.ToTerm())
        ProcessParameterValue.create(p,v,?Unit = u)

    /// Convert a CompositeHeader and Cell tuple to a ISA FactorValue
    let composeFactorValue (header : CompositeHeader) (value : CompositeCell) : FactorValue =
        let v,u = valueOfCell value
        let f = Factor.create(Name = header.ToString(),FactorType = header.ToTerm())
        FactorValue.create(Category = f,Value = v,?Unit = u)

    /// Convert a CompositeHeader and Cell tuple to a ISA MaterialAttributeValue
    let composeCharacteristicValue (header : CompositeHeader) (value : CompositeCell) : MaterialAttributeValue =
        let v,u = valueOfCell value
        let c = MaterialAttribute.create(CharacteristicType = header.ToTerm())
        MaterialAttributeValue.create(Category = c,Value = v,?Unit = u)

    /// Convert a CompositeHeader and Cell tuple to a ISA ProcessInput
    let composeProcessInput (header : CompositeHeader) (value : CompositeCell) : ProcessInput =
        match header with
        | CompositeHeader.Input IOType.Source -> ProcessInput.createSource(value.ToString())
        | CompositeHeader.Input IOType.Sample -> ProcessInput.createSample(value.ToString())
        | CompositeHeader.Input IOType.Material -> ProcessInput.createMaterial(value.ToString())
        | CompositeHeader.Input IOType.ImageFile -> ProcessInput.createImageFile(value.ToString())
        | CompositeHeader.Input IOType.RawDataFile ->  ProcessInput.createRawData(value.ToString())
        | CompositeHeader.Input IOType.DerivedDataFile -> ProcessInput.createDerivedData(value.ToString())
        | _ ->
            failwithf "Could not parse input header %O" header


    /// Convert a CompositeHeader and Cell tuple to a ISA ProcessOutput
    let composeProcessOutput (header : CompositeHeader) (value : CompositeCell) : ProcessOutput =
        match header with
        | CompositeHeader.Output IOType.Sample -> ProcessOutput.createSample(value.ToString())
        | CompositeHeader.Output IOType.Material -> ProcessOutput.createMaterial(value.ToString())
        | CompositeHeader.Output IOType.ImageFile -> ProcessOutput.createImageFile(value.ToString())
        | CompositeHeader.Output IOType.RawDataFile -> ProcessOutput.createRawData(value.ToString())
        | CompositeHeader.Output IOType.DerivedDataFile -> ProcessOutput.createDerivedData(value.ToString())
        | _ ->
            failwithf "Could not parse output header %O" header

    /// Convert an ISA Value and Unit tuple to a CompositeCell
    let cellOfValue (value : Value option) (unit : OntologyAnnotation option) =
        let value = value |> Option.defaultValue (Value.Name "")
        match value,unit with
        | Value.Ontology oa, None -> CompositeCell.Term oa
        | Value.Name text, None -> CompositeCell.FreeText text
        | Value.Name name, Some u -> CompositeCell.Unitized (name,u)
        | Value.Float f, Some u -> CompositeCell.Unitized (f.ToString(),u)
        | Value.Float f, None -> CompositeCell.FreeText (f.ToString())
        | Value.Int i, Some u -> CompositeCell.Unitized (i.ToString(),u)
        | Value.Int i, None -> CompositeCell.FreeText (i.ToString())
        | _ -> failwithf "Could not parse value %O with unit %O" value unit

    /// Convert an ISA Component to a CompositeHeader and Cell tuple
    let decomposeComponent (c : Component) : CompositeHeader*CompositeCell =
        CompositeHeader.Component (c.ComponentType.Value),
        cellOfValue c.ComponentValue c.ComponentUnit 

    /// Convert an ISA ProcessParameterValue to a CompositeHeader and Cell tuple
    let decomposeParameterValue (ppv : ProcessParameterValue) : CompositeHeader*CompositeCell =
        CompositeHeader.Parameter (ppv.Category.Value.ParameterName.Value),
        cellOfValue ppv.Value ppv.Unit

    /// Convert an ISA FactorValue to a CompositeHeader and Cell tuple
    let decomposeFactorValue (fv : FactorValue) : CompositeHeader*CompositeCell =
        CompositeHeader.Factor (fv.Category.Value.FactorType.Value),
        cellOfValue fv.Value fv.Unit

    /// Convert an ISA MaterialAttributeValue to a CompositeHeader and Cell tuple
    let decomposeCharacteristicValue (cv : MaterialAttributeValue) : CompositeHeader*CompositeCell =
        CompositeHeader.Characteristic (cv.Category.Value.CharacteristicType.Value),
        cellOfValue cv.Value cv.Unit
    
    /// Convert an ISA ProcessOutput to a CompositeHeader and Cell tuple
    let decomposeProcessInput (pi : ProcessInput) : CompositeHeader*CompositeCell =
        match pi with
        | ProcessInput.Source s -> CompositeHeader.Input IOType.Source, CompositeCell.FreeText (s.Name |> Option.defaultValue "")
        | ProcessInput.Sample s -> CompositeHeader.Input IOType.Sample, CompositeCell.FreeText (s.Name |> Option.defaultValue "")
        | ProcessInput.Material m -> CompositeHeader.Input IOType.Material, CompositeCell.FreeText (m.Name |> Option.defaultValue "")
        | ProcessInput.Data d -> 
            let dataType = d.DataType.Value
            match dataType with
            | DataFile.ImageFile -> CompositeHeader.Input IOType.ImageFile, CompositeCell.FreeText (d.Name |> Option.defaultValue "")
            | DataFile.RawDataFile -> CompositeHeader.Input IOType.RawDataFile, CompositeCell.FreeText (d.Name |> Option.defaultValue "")
            | DataFile.DerivedDataFile -> CompositeHeader.Input IOType.DerivedDataFile, CompositeCell.FreeText (d.Name |> Option.defaultValue "")

    /// Convert an ISA ProcessOutput to a CompositeHeader and Cell tuple
    let decomposeProcessOutput (po : ProcessOutput) : CompositeHeader*CompositeCell =
        match po with
        | ProcessOutput.Sample s -> CompositeHeader.Output IOType.Sample, CompositeCell.FreeText (s.Name |> Option.defaultValue "")
        | ProcessOutput.Material m -> CompositeHeader.Output IOType.Material, CompositeCell.FreeText (m.Name |> Option.defaultValue "")
        | ProcessOutput.Data d -> 
            let dataType = d.DataType.Value
            match dataType with
            | DataFile.ImageFile -> CompositeHeader.Output IOType.ImageFile, CompositeCell.FreeText (d.Name |> Option.defaultValue "")
            | DataFile.RawDataFile -> CompositeHeader.Output IOType.RawDataFile, CompositeCell.FreeText (d.Name |> Option.defaultValue "")
            | DataFile.DerivedDataFile -> CompositeHeader.Output IOType.DerivedDataFile, CompositeCell.FreeText (d.Name |> Option.defaultValue "")

/// Functions for parsing ArcTables to ISA json Processes and vice versa
module ProcessParsing = 

    open ARCtrl.ISA.ColumnIndex

    /// If the headers of a node depict a component, returns a function for parsing the values of the matrix to the values of this component
    let tryComponentGetter (generalI : int) (valueI : int) (valueHeader : CompositeHeader) =
        match valueHeader with
        | CompositeHeader.Component oa ->
            let cat = CompositeHeader.Component (oa.SetColumnIndex valueI)
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                JsonTypes.composeComponent cat matrix.[generalI,i]
            |> Some
        | _ -> None    
            
    /// If the headers of a node depict a protocolType, returns a function for parsing the values of the matrix to the values of this type
    let tryParameterGetter (generalI : int) (valueI : int) (valueHeader : CompositeHeader) =
        match valueHeader with
        | CompositeHeader.Parameter oa ->
            let cat = CompositeHeader.Parameter (oa.SetColumnIndex valueI)
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                JsonTypes.composeParameterValue cat matrix.[generalI,i]
            |> Some
        | _ -> None 

    let tryFactorGetter (generalI : int) (valueI : int) (valueHeader : CompositeHeader) =
        match valueHeader with
        | CompositeHeader.Factor oa ->
            let cat = CompositeHeader.Factor (oa.SetColumnIndex valueI)
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                JsonTypes.composeFactorValue cat matrix.[generalI,i]
            |> Some
        | _ -> None 

    let tryCharacteristicGetter (generalI : int) (valueI : int) (valueHeader : CompositeHeader) =
        match valueHeader with
        | CompositeHeader.Characteristic oa ->
            let cat = CompositeHeader.Characteristic (oa.SetColumnIndex valueI)
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                JsonTypes.composeCharacteristicValue cat matrix.[generalI,i]
            |> Some
        | _ -> None 

    /// If the headers of a node depict a protocolType, returns a function for parsing the values of the matrix to the values of this type
    let tryGetProtocolTypeGetter (generalI : int) (header : CompositeHeader) =
        match header with
        | CompositeHeader.ProtocolType ->
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                matrix.[generalI,i].AsTerm
            |> Some
        | _ -> None 


    let tryGetProtocolREFGetter (generalI : int) (header : CompositeHeader) =
        match header with
        | CompositeHeader.ProtocolREF ->
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                matrix.[generalI,i].AsFreeText
            |> Some
        | _ -> None

    let tryGetProtocolDescriptionGetter (generalI : int) (header : CompositeHeader) =
        match header with
        | CompositeHeader.ProtocolDescription ->
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                matrix.[generalI,i].AsFreeText
            |> Some
        | _ -> None

    let tryGetProtocolURIGetter (generalI : int) (header : CompositeHeader) =
        match header with
        | CompositeHeader.ProtocolUri ->
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                matrix.[generalI,i].AsFreeText
            |> Some
        | _ -> None

    let tryGetProtocolVersionGetter (generalI : int) (header : CompositeHeader) =
        match header with
        | CompositeHeader.ProtocolVersion ->
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                matrix.[generalI,i].AsFreeText
            |> Some
        | _ -> None

    let tryGetInputGetter (generalI : int) (header : CompositeHeader) =
        match header with
        | CompositeHeader.Input io ->
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                JsonTypes.composeProcessInput header matrix.[generalI,i]
            |> Some
        | _ -> None

    let tryGetOutputGetter (generalI : int) (header : CompositeHeader) =
        match header with
        | CompositeHeader.Output io ->
            fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                JsonTypes.composeProcessOutput header matrix.[generalI,i]
            |> Some
        | _ -> None

    /// Given the header sequence of an ArcTable, returns a function for parsing each row of the table to a process
    let getProcessGetter (processNameRoot : string) (headers : CompositeHeader seq) =
    
        let headers = 
            headers
            |> Seq.indexed

        let valueHeaders =
            headers
            |> Seq.filter (snd >> fun h -> h.IsCvParamColumn)
            |> Seq.indexed
            |> Seq.toList

        let charGetters =
            valueHeaders 
            |> List.choose (fun (valueI,(generalI,header)) -> tryCharacteristicGetter generalI valueI header)

        let factorValueGetters =
            valueHeaders
            |> List.choose (fun (valueI,(generalI,header)) -> tryFactorGetter generalI valueI header)

        let parameterValueGetters =
            valueHeaders
            |> List.choose (fun (valueI,(generalI,header)) -> tryParameterGetter generalI valueI header)

        let componentGetters =
            valueHeaders
            |> List.choose (fun (valueI,(generalI,header)) -> tryComponentGetter generalI valueI header)

        let protocolTypeGetter = 
            headers
            |> Seq.tryPick (fun (generalI,header) -> tryGetProtocolTypeGetter generalI header)

        let protocolREFGetter = 
            headers
            |> Seq.tryPick (fun (generalI,header) -> tryGetProtocolREFGetter generalI header)

        let protocolDescriptionGetter = 
            headers
            |> Seq.tryPick (fun (generalI,header) -> tryGetProtocolDescriptionGetter generalI header)

        let protocolURIGetter = 
            headers
            |> Seq.tryPick (fun (generalI,header) -> tryGetProtocolURIGetter generalI header)

        let protocolVersionGetter =
            headers
            |> Seq.tryPick (fun (generalI,header) -> tryGetProtocolVersionGetter generalI header)

        // This is a little more complex, as data and material objects can't contain characteristics. So in the case where the input of the table is a data object but characteristics exist. An additional sample object with the same name is created to contain the characteristics.
        let inputGetter =
            match headers |> Seq.tryPick (fun (generalI,header) -> tryGetInputGetter generalI header) with
            | Some inputGetter ->
                fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                    let chars = charGetters |> Seq.map (fun f -> f matrix i) |> Seq.toList
                    let input = inputGetter matrix i

                    if ((input.isSample() || input.isSource())|> not) && (chars.IsEmpty |> not) then
                        [
                        input
                        ProcessInput.createSample(input.Name, characteristics = chars)
                        ]
                    else
                        input
                        |> ProcessInput.setCharacteristicValues chars
                        |> List.singleton
            | None ->
                fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                    let chars = charGetters |> Seq.map (fun f -> f matrix i) |> Seq.toList
                    ProcessInput.Source (Source.create(Name = $"{processNameRoot}_Input_{i}", Characteristics = chars))
                    |> List.singleton
            
        // This is a little more complex, as data and material objects can't contain factors. So in the case where the output of the table is a data object but factors exist. An additional sample object with the same name is created to contain the factors.
        let outputGetter =
            match headers |> Seq.tryPick (fun (generalI,header) -> tryGetOutputGetter generalI header) with
            | Some outputGetter ->
                fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                    let factors = factorValueGetters |> Seq.map (fun f -> f matrix i) |> Seq.toList
                    let output = outputGetter matrix i
                    if (output.isSample() |> not) && (factors.IsEmpty |> not) then
                        [
                        output
                        ProcessOutput.createSample(output.Name, factors = factors)
                        ]
                    else
                        output
                        |> ProcessOutput.setFactorValues factors
                        |> List.singleton
            | None ->
                fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->
                    let factors = factorValueGetters |> Seq.map (fun f -> f matrix i) |> Seq.toList
                    ProcessOutput.Sample (Sample.create(Name = $"{processNameRoot}_Output_{i}", FactorValues = factors))
                    |> List.singleton

        fun (matrix : System.Collections.Generic.Dictionary<(int * int),CompositeCell>) i ->

            let pn = processNameRoot |> Aux.Option.fromValueWithDefault "" |> Option.map (fun p -> Process.composeName p i)

            let paramvalues = parameterValueGetters |> List.map (fun f -> f matrix i) |> Aux.Option.fromValueWithDefault [] 
            let parameters = paramvalues |> Option.map (List.map (fun pv -> pv.Category.Value))

            let protocol : Protocol option = 
                Protocol.make 
                    None
                    (protocolREFGetter |> Option.map (fun f -> f matrix i))
                    (protocolTypeGetter |> Option.map (fun f -> f matrix i))
                    (protocolDescriptionGetter |> Option.map (fun f -> f matrix i))
                    (protocolURIGetter |> Option.map (fun f -> f matrix i))
                    (protocolVersionGetter |> Option.map (fun f -> f matrix i))
                    (parameters)
                    (componentGetters |> List.map (fun f -> f matrix i) |> Aux.Option.fromValueWithDefault [])
                    None
                |> fun p ->     
                    match p with
                    | {       
                            Name            = None
                            ProtocolType    = None
                            Description     = None
                            Uri             = None
                            Version         = None
                            Components      = None
                        } -> None
                    | _ -> Some p

            let inputs,outputs = 
                let inputs = inputGetter matrix i
                let outputs = outputGetter matrix i
                if inputs.Length = 1 && outputs.Length = 2 then 
                    [inputs.[0];inputs.[0]],outputs
                elif inputs.Length = 2 && outputs.Length = 1 then
                    inputs,[outputs.[0];outputs.[0]]
                else
                    inputs,outputs

            Process.make 
                None 
                pn 
                (protocol) 
                (paramvalues)
                None
                None
                None
                None          
                (Some inputs)
                (Some outputs)
                None

    /// Groups processes by their name, or by the name of the protocol they execute
    ///
    /// Process names are taken from the Worksheet name and numbered: SheetName_1, SheetName_2, etc.
    /// 
    /// This function decomposes this name into a root name and a number, and groups processes by root name.
    let groupProcesses (ps : Process list) = 
        ps
        |> List.groupBy (fun x -> 
            if x.Name.IsSome && (x.Name.Value |> Process.decomposeName |> snd).IsSome then
                (x.Name.Value |> Process.decomposeName |> fst)
            elif x.ExecutesProtocol.IsSome && x.ExecutesProtocol.Value.Name.IsSome then
                x.ExecutesProtocol.Value.Name.Value 
            elif x.Name.IsSome && x.Name.Value.Contains "_" then
                let lastUnderScoreIndex = x.Name.Value.LastIndexOf '_'
                x.Name.Value.Remove lastUnderScoreIndex
            elif x.Name.IsSome then
                x.Name.Value
            elif x.ExecutesProtocol.IsSome && x.ExecutesProtocol.Value.ID.IsSome then 
                x.ExecutesProtocol.Value.ID.Value              
            else
                ARCtrl.ISA.Identifier.createMissingIdentifier()        
        )

    /// Merges processes with the same name, protocol and param values
    let mergeIdenticalProcesses (processes : list<Process>) =
        processes
        |> List.groupBy (fun x -> 
            if x.Name.IsSome && (x.Name.Value |> Process.decomposeName |> snd).IsSome then
                (x.Name.Value |> Process.decomposeName |> fst), x.ExecutesProtocol, x.ParameterValues |> Option.map Aux.HashCodes.boxHashSeq
            elif x.ExecutesProtocol.IsSome && x.ExecutesProtocol.Value.Name.IsSome then
                x.ExecutesProtocol.Value.Name.Value, x.ExecutesProtocol, x.ParameterValues |> Option.map Aux.HashCodes.boxHashSeq
            else
                ARCtrl.ISA.Identifier.createMissingIdentifier(), x.ExecutesProtocol, x.ParameterValues |> Option.map Aux.HashCodes.boxHashSeq
        )
        |> fun l ->
            l
            |> List.mapi (fun i ((n,protocol,_),processes) -> 
                let pVs = processes.[0].ParameterValues
                let inputs = processes |> List.collect (fun p -> p.Inputs |> Option.defaultValue []) |> Aux.Option.fromValueWithDefault []
                let outputs = processes |> List.collect (fun p -> p.Outputs |> Option.defaultValue []) |> Aux.Option.fromValueWithDefault []
                let n = if l.Length > 1 then Process.composeName n i else n
                Process.create(Name = n,?ExecutesProtocol = protocol,?ParameterValues = pVs,?Inputs = inputs,?Outputs = outputs)
            )


    // Transform a isa json process into a isa tab row, where each row is a header+value list
    let processToRows (p : Process) =
        let pvs = p.ParameterValues |> Option.defaultValue [] |> List.map (fun ppv -> JsonTypes.decomposeParameterValue ppv, ColumnIndex.tryGetParameterColumnIndex ppv)
        // Get the component
        let components = 
            match p.ExecutesProtocol with
            | Some prot ->
                prot.Components |> Option.defaultValue [] |> List.map (fun ppv -> JsonTypes.decomposeComponent ppv, ColumnIndex.tryGetComponentIndex ppv)
            | None -> []
        // Get the values of the protocol
        let protVals = 
            match p.ExecutesProtocol with
            | Some prot ->
                [
                    if prot.Name.IsSome then CompositeHeader.ProtocolREF, CompositeCell.FreeText prot.Name.Value
                    if prot.ProtocolType.IsSome then CompositeHeader.ProtocolType, CompositeCell.Term prot.ProtocolType.Value
                    if prot.Description.IsSome then CompositeHeader.ProtocolDescription, CompositeCell.FreeText prot.Description.Value
                    if prot.Uri.IsSome then CompositeHeader.ProtocolUri, CompositeCell.FreeText prot.Uri.Value
                    if prot.Version.IsSome then CompositeHeader.ProtocolVersion, CompositeCell.FreeText prot.Version.Value
                ]
            | None -> []
        // zip the inputs and outpus so they are aligned as rows
        p.Outputs |> Option.defaultValue []
        |> List.zip (p.Inputs |> Option.defaultValue [])
        // This grouping here and the picking of the "inputForCharas" etc is done, so there can be rows where data do have characteristics, which is not possible in isa json
        |> List.groupBy (fun (i,o) ->
            i.Name,o.Name
        )
        |> List.map (fun ((i,o),ios) ->
            let inputForCharas = 
                ios
                |> List.tryPick (fun (i,o) -> if i.isSource() || i.isSample() then Some i else None)
                |> Option.defaultValue (ios.Head |> fst)
            let inputForType =
                ios
                |> List.tryPick (fun (i,o) -> if i.isData() || i.isMaterial() then Some i  else None)
                |> Option.defaultValue (ios.Head |> fst)
            let chars = 
                inputForCharas |> ProcessInput.getCharacteristicValues |> List.map (fun cv -> JsonTypes.decomposeCharacteristicValue cv, ColumnIndex.tryGetCharacteristicColumnIndex cv)
            let outputForFactors = 
                ios
                |> List.tryPick (fun (i,o) -> if o.isSample() then Some o else None)
                |> Option.defaultValue (ios.Head |> snd)
            let outputForType = 
                ios
                |> List.tryPick (fun (i,o) -> if o.isData() || o.isMaterial() then Some o else None)
                |> Option.defaultValue (ios.Head |> snd)
            let factors = outputForFactors |> ProcessOutput.getFactorValues |> List.map (fun fv -> JsonTypes.decomposeFactorValue fv, ColumnIndex.tryGetFactorColumnIndex fv)
            let vals = 
                (chars @ components @ pvs @ factors)
                |> List.sortBy (snd >> Option.defaultValue 10000)
                |> List.map fst
            [
                yield JsonTypes.decomposeProcessInput inputForType
                yield! protVals
                yield! vals
                yield JsonTypes.decomposeProcessOutput outputForType
            ]
        )
        
    /// Returns true, if two composite headers share the same main header string
    let compositeHeaderEqual (ch1 : CompositeHeader) (ch2 : CompositeHeader) = 
        ch1.ToString() = ch2.ToString()

    /// From a list of rows consisting of headers and values, creates a list of combined headers and the values as a sparse matrix
    ///
    /// The values cant be directly taken as they are, as there is no guarantee that the headers are aligned
    ///
    /// This function aligns the headers and values by the main header string
    ///
    /// If keepOrder is true, the order of values per row is kept intact, otherwise the values are allowed to be reordered
    let alignByHeaders (keepOrder : bool) (rows : ((CompositeHeader * CompositeCell) list) list) = 
        let headers : ResizeArray<CompositeHeader> = ResizeArray()
        let values : Dictionary<int*int,CompositeCell> = Dictionary()
        let getFirstElem (rows : ('T list) list) : 'T =
            List.pick (fun l -> if List.isEmpty l then None else List.head l |> Some) rows
        let rec loop colI (rows : ((CompositeHeader * CompositeCell) list) list) =
            if List.exists (List.isEmpty >> not) rows |> not then 
                headers,values
            else 
                
                let firstElem = rows |> getFirstElem |> fst
                headers.Add firstElem
                let rows = 
                    rows
                    |> List.mapi (fun rowI l ->
                        if keepOrder then                           
                            match l with
                            | [] -> []
                            | (h,c)::t ->
                                if compositeHeaderEqual h firstElem then
                                    values.Add((colI,rowI),c)
                                    t
                                else
                                    l
                                
                        else
                            let firstMatch,newL = 
                                l
                                |> Aux.List.tryPickAndRemove (fun (h,c) ->
                                    if compositeHeaderEqual h firstElem then Some c 
                                    else None
                                )
                            match firstMatch with
                            | Some m -> 
                                values.Add((colI,rowI),m)
                                newL
                            | None -> newL
                    )
                loop (colI+1) rows
        loop 0 rows
                
        