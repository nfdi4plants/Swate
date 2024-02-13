namespace Shared

open ARCtrl.ISA
open TermTypes

/// This module contains helper functions which might be useful for ARCtrl
[<AutoOpen>]
module ARCtrlHelper =

    type ArcFiles =
    | Template      of ARCtrl.Template.Template
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

[<AutoOpen>]
module Extensions =

    open ARCtrl.Template
    open ArcTableAux

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


    type Template with
        member this.FileName 
            with get() = this.Name.Replace(" ","_") + ".xlsx"

    type CompositeHeader with
        member this.AsButtonName =
            match this with
            | CompositeHeader.Parameter _ -> "Parameter"
            | CompositeHeader.Characteristic _ -> "Characteristic"
            | CompositeHeader.Component _ -> "Component"
            | CompositeHeader.Factor _ -> "Factor"
            | CompositeHeader.Input _ -> "Input"
            | CompositeHeader.Output _ -> "Output"
            | anyElse -> anyElse.ToString()

        member this.UpdateWithOA (oa: OntologyAnnotation) =
            match this with
            | CompositeHeader.Component _ -> CompositeHeader.Component oa
            | CompositeHeader.Parameter _ -> CompositeHeader.Parameter oa
            | CompositeHeader.Characteristic _ -> CompositeHeader.Characteristic oa
            | CompositeHeader.Factor _ -> CompositeHeader.Factor oa
            | _ ->  failwithf "Cannot update OntologyAnnotation on CompositeHeader without OntologyAnnotation: '%A'" this

        static member ParameterEmpty = CompositeHeader.Parameter OntologyAnnotation.empty
        static member CharacteristicEmpty = CompositeHeader.Characteristic OntologyAnnotation.empty
        static member ComponentEmpty = CompositeHeader.Component OntologyAnnotation.empty
        static member FactorEmpty = CompositeHeader.Factor OntologyAnnotation.empty
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

    type CompositeCell with
        member this.UpdateWithOA(oa:OntologyAnnotation) =
            match this with
            | CompositeCell.Term _ -> CompositeCell.createTerm oa
            | CompositeCell.Unitized (v,_) -> CompositeCell.createUnitized (v,oa)
            | CompositeCell.FreeText _ -> CompositeCell.createFreeText oa.NameText

        member this.ToOA() =
            match this with
            | CompositeCell.Term oa -> oa
            | CompositeCell.Unitized (v, oa) -> oa
            | CompositeCell.FreeText t -> OntologyAnnotation.fromString t

        member this.UpdateMainField(s: string) =
            match this with
            | CompositeCell.Term oa -> CompositeCell.Term ({oa with Name = Some s})
            | CompositeCell.Unitized (_, oa) -> CompositeCell.Unitized (s, oa)
            | CompositeCell.FreeText _ -> CompositeCell.FreeText s

        /// <summary>
        /// Will return `this` if executed on Freetext cell.
        /// </summary>
        /// <param name="tsr"></param>
        member this.UpdateTSR(tsr: string) =
            let updateTSR (oa: OntologyAnnotation) = {oa with TermSourceREF = tsr |> Some}
            match this with
            | CompositeCell.Term oa -> CompositeCell.Term (updateTSR oa)
            | CompositeCell.Unitized (v, oa) -> CompositeCell.Unitized (v, updateTSR oa)
            | _ -> this

        /// <summary>
        /// Will return `this` if executed on Freetext cell.
        /// </summary>
        /// <param name="tsr"></param>
        member this.UpdateTAN(tan: string) =
            let updateTAN (oa: OntologyAnnotation) = {oa with TermAccessionNumber = tan |> Some}
            match this with
            | CompositeCell.Term oa -> CompositeCell.Term (updateTAN oa)
            | CompositeCell.Unitized (v, oa) -> CompositeCell.Unitized (v, updateTAN oa)
            | _ -> this

    type OntologyAnnotation with
        static member fromTerm (term:Term) = OntologyAnnotation.fromString(term.Name, term.FK_Ontology, term.Accession)
        member this.ToTermMinimal() = TermMinimal.create this.NameText this.TermAccessionShort
