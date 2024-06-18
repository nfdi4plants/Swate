namespace Shared

open ARCtrl
open TermTypes
open System.Collections.Generic

/// This module contains helper functions which might be useful for ARCtrl
[<AutoOpen>]
module ARCtrlHelper =

    type ArcFiles =
    | Template      of Template
    | Investigation of ArcInvestigation
    | Study         of ArcStudy * ArcAssay list
    | Assay         of ArcAssay

    with
        member this.Tables() : ArcTables =
            match this with
            | Template t -> ResizeArray([t.Table]) |> ArcTables
            | Investigation _ -> ArcTables(ResizeArray[])
            | Study (s,_) -> s
            | Assay a -> a


module Helper =

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

[<AutoOpen>]
module Extensions =

    open ARCtrl.Template
    open ArcTableAux

    type OntologyAnnotation with
        static member empty() = OntologyAnnotation.create()

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

        member this.SetColumn(index: int, column: CompositeColumn) =
            column.Validate(true) |> ignore
            this.Headers.[index] <- column.Header
            let cells = column.Cells
            let keys = this.Values.Keys
            for (ci, ri) in keys do    
                if ci = index then
                    let nextCell = cells |> Array.tryItem ri
                    match nextCell with
                    | Some c -> 
                        this.Values.[(ci,ri)] <- c
                    | None ->
                        this.Values.[(ci,ri)] <- column.GetDefaultEmptyCell()

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

    let internal tryFromContent' (content: string []) =
        match content with
        | [|freetext|] -> CompositeCell.createFreeText freetext |> Ok
        | [|name; tsr; tan|] -> CompositeCell.createTermFromString(name, tsr, tan) |> Ok
        | [|value; name; tsr; tan|] -> CompositeCell.createUnitizedFromString(value, name, tsr, tan) |> Ok
        | anyElse -> sprintf "Unable to convert \"%A\" to CompositeCell." anyElse |> Error

    type CompositeCell with
        
        static member tryFromContent (content: string []) =
            match tryFromContent' content with
            | Ok r -> Some r
            | Error _ -> None

        static member fromContent (content: string []) =
            match tryFromContent' content with
            | Ok r -> r
            | Error msg -> raise (exn msg) 

        member this.ToTabStr() = this.GetContent() |> String.concat "\t"

        static member fromTabStr (str:string) = 
            let content = str.Split('\t', System.StringSplitOptions.TrimEntries)
            CompositeCell.fromContent content

        static member ToTabTxt (cells: CompositeCell []) =
            cells 
            |> Array.map (fun c -> c.ToTabStr())
            |> String.concat (System.Environment.NewLine)

        static member fromTabTxt (tabTxt: string) =
            let lines = tabTxt.Split(System.Environment.NewLine, System.StringSplitOptions.None)
            let cells = lines |> Array.map (fun line -> CompositeCell.fromTabStr line)
            cells 

        member this.ConvertToValidCell (header: CompositeHeader) =
            match header.IsTermColumn, this with
            | true, CompositeCell.Term _ | true, CompositeCell.Unitized _ -> this
            | true, CompositeCell.FreeText txt -> this.ToTermCell()
            | false, CompositeCell.Term _ | false, CompositeCell.Unitized _ -> this.ToFreeTextCell()
            | false, CompositeCell.FreeText _ -> this

        member this.UpdateWithOA(oa:OntologyAnnotation) =
            match this with
            | CompositeCell.Term _ -> CompositeCell.createTerm oa
            | CompositeCell.Unitized (v,_) -> CompositeCell.createUnitized (v,oa)
            | CompositeCell.FreeText _ -> CompositeCell.createFreeText oa.NameText

        member this.ToOA() =
            match this with
            | CompositeCell.Term oa -> oa
            | CompositeCell.Unitized (v, oa) -> oa
            | CompositeCell.FreeText t -> OntologyAnnotation.create t

        member this.UpdateMainField(s: string) =
            match this with
            | CompositeCell.Term oa -> 
                oa.Name <- Some s
                CompositeCell.Term oa
            | CompositeCell.Unitized (_, oa) -> CompositeCell.Unitized (s, oa)
            | CompositeCell.FreeText _ -> CompositeCell.FreeText s

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

    type OntologyAnnotation with
        static member fromTerm (term:Term) = OntologyAnnotation(term.Name, term.FK_Ontology, term.Accession)
        member this.ToTermMinimal() = TermMinimal.create this.NameText this.TermAccessionShort
