namespace Shared

open ARCtrl
open Database
open System.Collections.Generic

/// This module contains helper functions which might be useful for ARCtrl
[<AutoOpen>]
module ARCtrlHelper =

    [<RequireQualifiedAccess>]
    type ArcFilesDiscriminate =
        | Assay
        | Study
        | Investigation
        | Template

    type ArcFiles =
        | Template      of Template
        | Investigation of ArcInvestigation
        | Study         of ArcStudy * ArcAssay list
        | Assay         of ArcAssay

    with
        member this.HasTableAt(index: int) =
            match this with
            | Template _ -> index = 0 // Template always has exactly one table
            | Investigation i -> false
            | Study (s,_) -> s.TableCount <= index
            | Assay a -> a.TableCount <= index
        member this.HasDataMap() =
            match this with
            | Template _ -> false
            | Investigation i -> false
            | Study (s,_) -> s.DataMap.IsSome
            | Assay a -> a.DataMap.IsSome
        member this.Tables() : ArcTables =
            match this with
            | Template t -> ResizeArray([t.Table]) |> ArcTables
            | Investigation _ -> ArcTables(ResizeArray[])
            | Study (s,_) -> s
            | Assay a -> a

    [<RequireQualifiedAccess>]
    type JsonExportFormat =
        | ARCtrl
        | ARCtrlCompressed
        | ISA
        | ROCrate

        static member fromString (str: string) =
            match str.ToLower() with
            | "arctrl" -> ARCtrl
            | "arctrlcompressed" -> ARCtrlCompressed
            | "isa" -> ISA
            | "rocrate" -> ROCrate
            | _ -> failwithf "Unknown JSON export format: %s" str

        member this.AsStringRdbl =
            match this with
            | ARCtrl -> "ARCtrl"
            | ARCtrlCompressed -> "ARCtrl Compressed"
            | ISA -> "ISA"
            | ROCrate -> "RO-Crate Metadata"

module Table =

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
            let containsAtIndex = tablecopy.Headers |> Seq.tryFindIndex (fun h -> h = header)
            if containsAtIndex.IsSome then
                columnsToRemove <- containsAtIndex.Value::columnsToRemove
        tablecopy.RemoveColumns (Array.ofList columnsToRemove)
        tablecopy

    /// <summary>
    /// This function is meant to prepare a table for joining with another table.
    ///
    /// It removes columns that are already present in the active table.
    /// It removes all values from the new table.
    /// It also fills new Input/Output columns with the input/output values of the active table.
    ///
    /// The output of this function can be used with the SpreadsheetInterface.JoinTable Message.
    /// </summary>
    /// <param name="activeTable">The active/current table</param>
    /// <param name="toJoinTable">The new table, which will be added to the existing one.</param>
    let selectiveTablePrepare (activeTable: ArcTable) (toJoinTable: ArcTable) : ArcTable =
        // Remove existing columns
        let mutable columnsToRemove = []
        // find duplicate columns
        let tablecopy = toJoinTable.Copy()
        for header in activeTable.Headers do
            let containsAtIndex = tablecopy.Headers |> Seq.tryFindIndex (fun h -> h = header)
            if containsAtIndex.IsSome then
                columnsToRemove <- containsAtIndex.Value::columnsToRemove
        tablecopy.RemoveColumns (Array.ofList columnsToRemove)
        tablecopy.IteriColumns(fun i c0 ->
            let c1 = {c0 with Cells = [||]}
            let c2 =
                if c1.Header.isInput then
                    match activeTable.TryGetInputColumn() with
                    | Some ic ->
                        {c1 with Cells = ic.Cells}
                    | _ -> c1
                elif c1.Header.isOutput then
                    match activeTable.TryGetOutputColumn() with
                    | Some oc ->
                        {c1 with Cells = oc.Cells}
                    | _ -> c1
                else
                    c1
            tablecopy.UpdateColumn(i, c2.Header, c2.Cells)
        )
        tablecopy

module Helper =

    let doptstr (o: string option) = Option.defaultValue "" o

    let arrayMoveColumn (currentColumnIndex: int) (newColumnIndex: int) (arr: ResizeArray<'A>) =
        let ele = arr.[currentColumnIndex]
        arr.RemoveAt(currentColumnIndex)
        arr.Insert(newColumnIndex, ele)

    let dictMoveColumn (currentColumnIndex: int) (newColumnIndex: int) (table: Dictionary<int * int, 'A>) =
        /// This is necessary to always access the correct value for an index.
        /// It is possible to only copy the specific target column at "currentColumnIndex" and sort the keys in the for loop depending on "currentColumnIndex" and "newColumnIndex".
        /// this means. If currentColumnIndex < newColumnIndex then Seq.sortByDescending keys else Seq.sortBy keys.
        /// this implementation would result in performance increase, but readability would decrease a lot.
        let backupTable = Dictionary(table)
        let range = [System.Math.Min(currentColumnIndex, newColumnIndex) .. System.Math.Max(currentColumnIndex,newColumnIndex)]
        for columnIndex, rowIndex in backupTable.Keys do
            let value = backupTable.[(columnIndex,rowIndex)]
            let newColumnIndex =
              if columnIndex = currentColumnIndex then
                newColumnIndex
              elif List.contains columnIndex range then
                let modifier = if currentColumnIndex < newColumnIndex then -1 else +1
                let moveTo = modifier + columnIndex
                moveTo
              else
                0 + columnIndex
            let updatedKey = (newColumnIndex, rowIndex)
            table.[updatedKey] <- value

[<RequireQualifiedAccess>]
type CompositeHeaderDiscriminate =
| Component
| Characteristic
| Factor
| Parameter
| ProtocolType
| ProtocolDescription
| ProtocolUri
| ProtocolVersion
| ProtocolREF
| Performer
| Date
| Input
| Output
| Comment
| Freetext
with
    /// <summary>
    /// Returns true if the Building Block is a term column
    /// </summary>
    member this.IsTermColumn() =
        match this with
        | Component
        | Characteristic
        | Factor
        | Parameter
        | ProtocolType -> true
        | _ -> false
    member this.HasOA() =
        match this with
        | Component
        | Characteristic
        | Factor
        | Parameter -> true
        | _ -> false

    member this.HasIOType() =
        match this with
        | Input
        | Output -> true
        | _ -> false

    static member fromString(str: string) =
        match str with
        | "Component"           -> Component
        | "Characteristic"      -> Characteristic
        | "Factor"              -> Factor
        | "Parameter"           -> Parameter
        | "ProtocolType"        -> ProtocolType
        | "ProtocolDescription" -> ProtocolDescription
        | "ProtocolUri"         -> ProtocolUri
        | "ProtocolVersion"     -> ProtocolVersion
        | "ProtocolREF"         -> ProtocolREF
        | "Performer"           -> Performer
        | "Date"                -> Date
        | "Input"               -> Input
        | "Output"              -> Output
        | "Comment"             -> Comment
        | anyElse -> failwithf "BuildingBlock.HeaderCellType.fromString: '%s' is not a valid HeaderCellType" anyElse

[<RequireQualifiedAccess>]
type CompositeCellDiscriminate =
| Term
| Unitized
| Text
| Data

[<AutoOpen>]
module Extensions =

    open Helper
    open ArcTableAux

    [<RequireQualifiedAccess>]
    module DataMapIndices =

        [<Literal>]
        let Data = 0
        [<Literal>]
        let Description = 1
        [<Literal>]
        let GeneratedBy = 2
        [<Literal>]
        let Explication = 3
        [<Literal>]
        let Unit = 4
        [<Literal>]
        let ObjectType = 5

    type DataMap with

        member this.GetCell(columnIndex: int, rowIndex: int) =
            let r = this.DataContexts.[rowIndex]
            match columnIndex with
            | DataMapIndices.Data -> CompositeCell.createData r
            | DataMapIndices.Description -> doptstr r.Description |> CompositeCell.FreeText
            | DataMapIndices.GeneratedBy -> doptstr r.GeneratedBy |> CompositeCell.FreeText
            | DataMapIndices.Explication -> r.Explication |> Option.defaultValue (OntologyAnnotation()) |> CompositeCell.Term
            | DataMapIndices.Unit -> r.Unit |> Option.defaultValue (OntologyAnnotation()) |> CompositeCell.Term
            | DataMapIndices.ObjectType -> r.ObjectType |> Option.defaultValue (OntologyAnnotation()) |> CompositeCell.Term
            | i -> failwithf "Invalid column index for DataMap: %i" i
        member this.SetCell(columnIndex: int, rowIndex: int, cell: CompositeCell) =
            let r = this.DataContexts.[rowIndex]
            match columnIndex with
            | DataMapIndices.Data ->
                let nd = cell.AsData
                r.FilePath <- nd.FilePath
                r.Selector <- nd.Selector
                r.Format <- nd.Format
                r.SelectorFormat <- nd.SelectorFormat
            | DataMapIndices.Description -> r.Description <- Some cell.AsFreeText
            | DataMapIndices.GeneratedBy -> r.GeneratedBy <- Some cell.AsFreeText
            | DataMapIndices.Explication -> r.Explication <- Some cell.AsTerm
            | DataMapIndices.Unit -> r.Unit <- Some cell.AsTerm
            | DataMapIndices.ObjectType -> r.ObjectType <- Some cell.AsTerm
            | i -> failwithf "Invalid column index for DataMap: %i" i

        static member getHeader(columnIndex: int) =
            match columnIndex with
            | DataMapIndices.Data -> CompositeHeader.Input IOType.Data
            | DataMapIndices.Description -> CompositeHeader.FreeText "Description"
            | DataMapIndices.GeneratedBy -> CompositeHeader.FreeText "Generated By"
            | DataMapIndices.Explication -> CompositeHeader.Parameter (TermCollection.Explication)
            | DataMapIndices.Unit -> CompositeHeader.Parameter (TermCollection.Unit)
            | DataMapIndices.ObjectType -> CompositeHeader.Parameter (TermCollection.ObjectType)
            | i -> failwithf "Invalid column index for DataMap: %i" i

        member this.GetHeader(columnIndex: int) =
            DataMap.getHeader(columnIndex)

        static member ColumnCount = 6
        member this.ColumnCount = DataMap.ColumnCount
        member this.RowCount = this.DataContexts.Count

    type DataFile with
        member this.ToStringRdb() =
            match this with
            | DataFile.DerivedDataFile -> "Derived Data File"
            | DataFile.ImageFile -> "Image File"
            | DataFile.RawDataFile -> "Raw Data File"
        static member tryFromString (str: string) =
            match str.ToLower() with
            | "derived data file" | "deriveddatafile" -> Some DataFile.DerivedDataFile
            | "image file" | "imagefile" -> Some DataFile.ImageFile
            | "raw data file" | "rawdatafile" -> Some DataFile.RawDataFile
            | _ -> None
        static member fromString (str:string) =
            match DataFile.tryFromString str with
            | Some r -> r
            | None -> failwithf "Unknown DataFile: %s" str

    type OntologyAnnotation with
        static member empty() = OntologyAnnotation.create()
        static member fromTerm (term:Term) = OntologyAnnotation(term.Name, term.FK_Ontology, term.Accession)

    type ArcTable with
        member this.SetCellAt(columnIndex: int, rowIndex: int, cell: CompositeCell) =
            SanityChecks.validateColumn <| CompositeColumn.create(this.Headers.[columnIndex],[|cell|])
            Unchecked.setCellAt(columnIndex, rowIndex,cell) this.Values
            Unchecked.fillMissingCells this.Headers this.Values

        member this.SetCellsAt (cells: ((int*int)*CompositeCell) []) =
            let columns = cells |> Array.groupBy (fun (index, cell) -> fst index)
            for columnIndex, items in columns do
                SanityChecks.validateColumn <| CompositeColumn.create(this.Headers.[columnIndex], items |> Array.map snd)
            for index, cell in cells do
                Unchecked.setCellAt(fst index, snd index, cell) this.Values
            Unchecked.fillMissingCells this.Headers this.Values

        member this.MoveColumn(currentIndex: int, nextIndex: int) =
            let updateHeaders =
                Helper.arrayMoveColumn currentIndex nextIndex this.Headers
            let updateBody =
                Helper.dictMoveColumn currentIndex nextIndex this.Values
            ()


    type Template with
        member this.FileName
            with get() = this.Name.Replace(" ","_") + ".xlsx"

    type CompositeHeader with

        member this.UpdateWithOA (oa: OntologyAnnotation) =
            match this with
            | CompositeHeader.Component _ -> CompositeHeader.Component oa
            | CompositeHeader.Parameter _ -> CompositeHeader.Parameter oa
            | CompositeHeader.Characteristic _ -> CompositeHeader.Characteristic oa
            | CompositeHeader.Factor _ -> CompositeHeader.Factor oa
            | _ ->  failwithf "Cannot update OntologyAnnotation on CompositeHeader without OntologyAnnotation: '%A'" this

        static member ParameterEmpty = CompositeHeader.Parameter <| OntologyAnnotation.empty()
        static member CharacteristicEmpty = CompositeHeader.Characteristic <| OntologyAnnotation.empty()
        static member ComponentEmpty = CompositeHeader.Component <| OntologyAnnotation.empty()
        static member FactorEmpty = CompositeHeader.Factor <| OntologyAnnotation.empty()
        static member InputEmpty = CompositeHeader.Input <| IOType.FreeText ""
        static member OutputEmpty = CompositeHeader.Output <| IOType.FreeText ""

        /// <summary>
        /// Keep the outer `CompositeHeader` information (e.g.: Parameter, Factor, Input, Output..) and update the inner "of" value with the value from `other.`
        /// This will only run successfully if the inner values are of the same type
        /// </summary>
        /// <param name="other">The header from which the inner value will be taken.</param>
        member this.UpdateDeepWith(other:CompositeHeader) =
            match this, other with
            | h1, h2 when this.IsIOType && other.IsIOType ->
                let io1 = h2.TryIOType().Value
                match h1 with
                | CompositeHeader.Input _ -> CompositeHeader.Input io1
                | CompositeHeader.Output _ -> CompositeHeader.Output io1
                | _ -> failwith "Error 1 in UpdateSurfaceTo. This should never hit."
            | h1, h2 when this.IsTermColumn && other.IsTermColumn && not this.IsFeaturedColumn && not other.IsFeaturedColumn ->
                let oa1 = h2.ToTerm()
                h1.UpdateWithOA oa1
            | _ ->
                this

        member this.TryOA() =
            match this with
            | CompositeHeader.Component oa -> Some oa
            | CompositeHeader.Parameter oa -> Some oa
            | CompositeHeader.Characteristic oa -> Some oa
            | CompositeHeader.Factor oa -> Some oa
            | _ -> None

        member this.AsDiscriminate =
            match this with
            | CompositeHeader.Component _ -> CompositeHeaderDiscriminate.Component
            | CompositeHeader.Characteristic _      -> CompositeHeaderDiscriminate.Characteristic
            | CompositeHeader.Factor _              -> CompositeHeaderDiscriminate.Factor
            | CompositeHeader.Parameter _           -> CompositeHeaderDiscriminate.Parameter
            | CompositeHeader.ProtocolType          -> CompositeHeaderDiscriminate.ProtocolType
            | CompositeHeader.ProtocolDescription   -> CompositeHeaderDiscriminate.ProtocolDescription
            | CompositeHeader.ProtocolUri           -> CompositeHeaderDiscriminate.ProtocolUri
            | CompositeHeader.ProtocolVersion       -> CompositeHeaderDiscriminate.ProtocolVersion
            | CompositeHeader.ProtocolREF           -> CompositeHeaderDiscriminate.ProtocolREF
            | CompositeHeader.Performer             -> CompositeHeaderDiscriminate.Performer
            | CompositeHeader.Date                  -> CompositeHeaderDiscriminate.Date
            | CompositeHeader.Input _               -> CompositeHeaderDiscriminate.Input
            | CompositeHeader.Output _              -> CompositeHeaderDiscriminate.Output
            | CompositeHeader.Comment _             -> CompositeHeaderDiscriminate.Comment
            | CompositeHeader.FreeText _            -> CompositeHeaderDiscriminate.Freetext

    type CompositeCell with

        /// <summary>
        /// This is an override of an existing ARCtrl version which does not return what i want 😤
        /// </summary>
        member this.GetContentSwate() =
            match this with
            | CompositeCell.FreeText s -> [|s|]
            | CompositeCell.Term oa -> [| oa.NameText; defaultArg oa.TermSourceREF ""; defaultArg oa.TermAccessionNumber ""|]
            | CompositeCell.Unitized (v,oa) -> [| v; oa.NameText; defaultArg oa.TermSourceREF ""; defaultArg oa.TermAccessionNumber ""|]
            | CompositeCell.Data d -> [| defaultArg d.FilePath ""; defaultArg d.Selector ""; defaultArg d.Format ""; defaultArg d.SelectorFormat ""|]

        member this.ToDataCell() =
            match this with
            | CompositeCell.Unitized (_, unit) -> CompositeCell.createDataFromString unit.NameText
            | CompositeCell.FreeText txt -> CompositeCell.createDataFromString txt
            | CompositeCell.Term term -> CompositeCell.createDataFromString term.NameText
            | CompositeCell.Data _ -> this

        //static member fromContent (content: string []) =
        //    match tryFromContent' content with
        //    | Ok r -> r
        //    | Error msg -> raise (exn msg)

        /// <summary>
        ///
        /// </summary>
        /// <param name="content"></param>
        /// <param name="header"></param>
        static member fromContentValid (content: string [], ?header: CompositeHeader) =
            if header.IsSome then
                let header = header.Value
                match content with
                | [|freetext|] when header.IsSingleColumn -> CompositeCell.createFreeText freetext
                | [|freetext|] -> CompositeCell.createFreeText freetext |> _.ConvertToValidCell(header)
                | [|name; tsr; tan|] when header.IsTermColumn -> CompositeCell.createTermFromString(name, tsr, tan)
                | [|name; tsr; tan|] -> CompositeCell.createTermFromString(name, tsr, tan) |> _.ConvertToValidCell(header)
                | [|path; selector; format; selectorFormat|] when header.IsDataColumn ->
                    let data = Data.empty
                    data.FilePath          <- Some path
                    data.Selector          <- Some selector
                    data.Format            <- Some format
                    data.SelectorFormat    <- Some selectorFormat
                    CompositeCell.createData data
                | [|value; name; tsr; tan|] when header.IsTermColumn -> CompositeCell.createUnitizedFromString(value, name, tsr, tan)
                | [|value; name; tsr; tan|] -> CompositeCell.createUnitizedFromString(value, name, tsr, tan) |> _.ConvertToValidCell(header)
                | anyElse -> failwithf "Invalid content for header: %A" anyElse
            else
                match content with
                | [|freetext|] -> CompositeCell.createFreeText freetext
                | [|name; tsr; tan|] -> CompositeCell.createTermFromString(name, tsr, tan)
                | [|value; name; tsr; tan|] -> CompositeCell.createUnitizedFromString(value, name, tsr, tan)
                | anyElse -> failwithf "Invalid content to parse to CompositeCell: %A" anyElse

        member this.ToTabStr() = this.GetContentSwate() |> String.concat "\t"

        static member fromTabStr (str:string) (header: CompositeHeader) =
            let content = str.Split('\t', System.StringSplitOptions.TrimEntries)
            CompositeCell.fromContentValid(content, header)

        static member ToTabTxt (cells: CompositeCell []) =
            cells
            |> Array.map (fun c -> c.ToTabStr())
            |> String.concat (System.Environment.NewLine)

        static member fromTabTxt (tabTxt: string) (header: CompositeHeader) =
            let lines = tabTxt.Split(System.Environment.NewLine, System.StringSplitOptions.None)
            let cells = lines |> Array.map (fun line -> CompositeCell.fromTabStr line header)
            cells

        member this.ConvertToValidCell (header: CompositeHeader) =
            match this with
            // term header
            | CompositeCell.Term _ when header.IsTermColumn -> this
            | CompositeCell.Unitized _ when header.IsTermColumn -> this
            | CompositeCell.FreeText _ when header.IsTermColumn -> this.ToTermCell()
            | CompositeCell.Data _ when header.IsTermColumn -> this.ToTermCell()
            // data header
            | CompositeCell.Term _ when header.IsDataColumn -> this.ToDataCell()
            | CompositeCell.Unitized _ when header.IsDataColumn -> this.ToDataCell()
            | CompositeCell.FreeText _ when header.IsDataColumn -> this.ToDataCell()
            | CompositeCell.Data _ when header.IsDataColumn -> this
            // freetext header?
            | CompositeCell.Term _ | CompositeCell.Unitized _ -> this.ToFreeTextCell()
            | CompositeCell.FreeText _ -> this
            | CompositeCell.Data _ -> this.ToFreeTextCell()

        member this.UpdateWithData(d:Data) =
            match this with
            | CompositeCell.Term _ -> CompositeCell.createTerm (OntologyAnnotation.create d.NameText)
            | CompositeCell.Unitized (v,_) -> CompositeCell.createUnitized (v, OntologyAnnotation.create d.NameText)
            | CompositeCell.FreeText _ -> CompositeCell.createFreeText d.NameText
            | CompositeCell.Data _ -> CompositeCell.createData d

        member this.ToOA() =
            match this with
            | CompositeCell.Term oa -> oa
            | CompositeCell.Unitized (v, oa) -> oa
            | CompositeCell.FreeText t -> OntologyAnnotation.create t
            | CompositeCell.Data d -> OntologyAnnotation.create d.NameText

        member this.UpdateMainField(s: string) =
            match this with
            | CompositeCell.Term oa ->
                oa.Name <- Some s
                CompositeCell.Term oa
            | CompositeCell.Unitized (_, oa) -> CompositeCell.Unitized (s, oa)
            | CompositeCell.FreeText _ -> CompositeCell.FreeText s
            | CompositeCell.Data d ->
                d.FilePath <- Some s
                CompositeCell.Data d

        /// <summary>
        /// Will return `this` if executed on Freetext cell.
        /// </summary>
        /// <param name="tsr"></param>
        member this.UpdateTSR(tsr: string) =
            let updateTSR (oa: OntologyAnnotation) = oa.TermSourceREF <- Some tsr ;oa
            match this with
            | CompositeCell.Term oa -> CompositeCell.Term (updateTSR oa)
            | CompositeCell.Unitized (v, oa) -> CompositeCell.Unitized (v, updateTSR oa)
            | _ -> this

        /// <summary>
        /// Will return `this` if executed on Freetext cell.
        /// </summary>
        /// <param name="tsr"></param>
        member this.UpdateTAN(tan: string) =
            let updateTAN (oa: OntologyAnnotation) = oa.TermSourceREF <- Some tan ;oa
            match this with
            | CompositeCell.Term oa -> CompositeCell.Term (updateTAN oa)
            | CompositeCell.Unitized (v, oa) -> CompositeCell.Unitized (v, updateTAN oa)
            | _ -> this
