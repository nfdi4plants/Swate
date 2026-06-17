[<AutoOpen>]
module ARCtrl.CompositeHeaderExtensions

open ARCtrl

type CompositeHeader with

    member this.UpdateWithOA(oa: OntologyAnnotation) =
        match this with
        | CompositeHeader.Component _ -> CompositeHeader.Component oa
        | CompositeHeader.Parameter _ -> CompositeHeader.Parameter oa
        | CompositeHeader.Characteristic _ -> CompositeHeader.Characteristic oa
        | CompositeHeader.Factor _ -> CompositeHeader.Factor oa
        | _ -> failwithf "Cannot update OntologyAnnotation on CompositeHeader without OntologyAnnotation: '%A'" this

    static member ParameterEmpty = CompositeHeader.Parameter <| OntologyAnnotation.empty ()

    static member CharacteristicEmpty =
        CompositeHeader.Characteristic <| OntologyAnnotation.empty ()

    static member ComponentEmpty = CompositeHeader.Component <| OntologyAnnotation.empty ()
    static member FactorEmpty = CompositeHeader.Factor <| OntologyAnnotation.empty ()
    static member InputEmpty = CompositeHeader.Input <| IOType.FreeText ""
    static member OutputEmpty = CompositeHeader.Output <| IOType.FreeText ""

    /// <summary>
    /// Keep the outer `CompositeHeader` information (e.g.: Parameter, Factor, Input, Output..) and update the inner "of" value with the value from `other.`
    /// This will only run successfully if the inner values are of the same type
    /// </summary>
    /// <param name="other">The header from which the inner value will be taken.</param>
    member this.UpdateDeepWith(other: CompositeHeader) =
        match this, other with
        | h1, h2 when this.IsIOType && other.IsIOType ->
            let io1 = h2.TryIOType().Value

            match h1 with
            | CompositeHeader.Input _ -> CompositeHeader.Input io1
            | CompositeHeader.Output _ -> CompositeHeader.Output io1
            | _ -> failwith "Error 1 in UpdateSurfaceTo. This should never hit."
        | h1, h2 when
            this.IsTermColumn
            && other.IsTermColumn
            && not this.IsFeaturedColumn
            && not other.IsFeaturedColumn
            ->
            let oa1 = h2.ToTerm()
            h1.UpdateWithOA oa1
        | _ -> this

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
        | CompositeHeader.Characteristic _ -> CompositeHeaderDiscriminate.Characteristic
        | CompositeHeader.Factor _ -> CompositeHeaderDiscriminate.Factor
        | CompositeHeader.Parameter _ -> CompositeHeaderDiscriminate.Parameter
        | CompositeHeader.ProtocolType -> CompositeHeaderDiscriminate.ProtocolType
        | CompositeHeader.ProtocolDescription -> CompositeHeaderDiscriminate.ProtocolDescription
        | CompositeHeader.ProtocolUri -> CompositeHeaderDiscriminate.ProtocolUri
        | CompositeHeader.ProtocolVersion -> CompositeHeaderDiscriminate.ProtocolVersion
        | CompositeHeader.ProtocolREF -> CompositeHeaderDiscriminate.ProtocolREF
        | CompositeHeader.Performer -> CompositeHeaderDiscriminate.Performer
        | CompositeHeader.Date -> CompositeHeaderDiscriminate.Date
        | CompositeHeader.Input _ -> CompositeHeaderDiscriminate.Input
        | CompositeHeader.Output _ -> CompositeHeaderDiscriminate.Output
        | CompositeHeader.Comment _ -> CompositeHeaderDiscriminate.Comment
        | CompositeHeader.FreeText _ -> CompositeHeaderDiscriminate.Freetext
