namespace ARCtrl


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

    member this.CreateEmptyDefaultCells(?count: int) =
        let count = defaultArg count 1

        match this with
        | CompositeHeaderDiscriminate.Component
        | CompositeHeaderDiscriminate.Characteristic
        | CompositeHeaderDiscriminate.Factor
        | CompositeHeaderDiscriminate.Parameter
        | CompositeHeaderDiscriminate.ProtocolType -> ResizeArray(Array.create count (CompositeCell.emptyTerm))
        | CompositeHeaderDiscriminate.ProtocolDescription
        | CompositeHeaderDiscriminate.ProtocolUri
        | CompositeHeaderDiscriminate.ProtocolVersion
        | CompositeHeaderDiscriminate.ProtocolREF
        | CompositeHeaderDiscriminate.Performer
        | CompositeHeaderDiscriminate.Date
        | CompositeHeaderDiscriminate.Input
        | CompositeHeaderDiscriminate.Output
        | CompositeHeaderDiscriminate.Freetext
        | CompositeHeaderDiscriminate.Comment -> ResizeArray(Array.create count (CompositeCell.emptyFreeText))

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
        | "Component" -> Component
        | "Characteristic" -> Characteristic
        | "Factor" -> Factor
        | "Parameter" -> Parameter
        | "ProtocolType" -> ProtocolType
        | "ProtocolDescription" -> ProtocolDescription
        | "ProtocolUri" -> ProtocolUri
        | "ProtocolVersion" -> ProtocolVersion
        | "ProtocolREF" -> ProtocolREF
        | "Performer" -> Performer
        | "Date" -> Date
        | "Input" -> Input
        | "Output" -> Output
        | "Comment" -> Comment
        | anyElse -> failwithf "BuildingBlock.HeaderCellType.fromString: '%s' is not a valid HeaderCellType" anyElse
