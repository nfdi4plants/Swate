namespace Shared

open ARCtrl.ISA
open TermTypes

/// This module contains helper functions which might be useful for ARCtrl
[<AutoOpen>]
module ARCtrlHelper =

    type ArcFiles =
    | Investigation of ArcInvestigation
    | Study         of ArcStudy * ArcAssay list
    | Assay         of ArcAssay

    with
        member this.Tables() : ArcTables =
            match this with
            | Investigation _ -> ArcTables(ResizeArray[])
            | Study (s,_) -> s
            | Assay a -> a

[<AutoOpen>]
module Extensions =

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

        member this.GetOA() =
            match this with
            | CompositeHeader.Component oa
            | CompositeHeader.Parameter oa
            | CompositeHeader.Characteristic oa
            | CompositeHeader.Factor oa -> oa
            | _ -> failwithf "Cannot get OntologyAnnotation from CompositeHeader without OntologyAnnotation: '%A'" this

        static member ParameterEmpty = CompositeHeader.Parameter OntologyAnnotation.empty
        static member CharacteristicEmpty = CompositeHeader.Characteristic OntologyAnnotation.empty
        static member ComponentEmpty = CompositeHeader.Component OntologyAnnotation.empty
        static member FactorEmpty = CompositeHeader.Factor OntologyAnnotation.empty
        static member InputEmpty = CompositeHeader.Input <| IOType.FreeText ""
        static member OutputEmpty = CompositeHeader.Output <| IOType.FreeText ""

    type CompositeCell with
        member this.UpdateWithOA(oa:OntologyAnnotation) =
            match this with
            | CompositeCell.Term _ -> CompositeCell.createTerm oa
            | CompositeCell.Unitized (v,_) -> CompositeCell.createUnitized (v,oa)
            | CompositeCell.FreeText _ -> CompositeCell.createFreeText oa.NameText

        member this.GetOA() =
            match this with
            | CompositeCell.Term oa -> oa
            | CompositeCell.Unitized (v, oa) -> oa
            | CompositeCell.FreeText t -> OntologyAnnotation.fromString t

    type OntologyAnnotation with
        static member fromTerm (term:Term) = OntologyAnnotation.fromString(term.Name, term.FK_Ontology, term.Accession)
        member this.ToTermMinimal() = TermMinimal.create this.NameText this.TermAccessionShort
