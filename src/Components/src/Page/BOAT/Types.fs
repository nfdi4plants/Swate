module Types

open ARCtrl

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

    /// <summary>
    /// Returns true if the Building Block is a term column
    /// </summary>
    member this.IsTermColumn() =
        match this with
        | Component
        | Characteristic
        | Factor
        | Parameter
        | Input
        | Output
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

type SearchComponent = {
    Key: OntologyAnnotation
    KeyType: CompositeHeaderDiscriminate
    Body: CompositeCell

}

type Annotation = {
    IsOpen: bool
    Search: SearchComponent
    Height: float
    XCoordinate: float
} with


    static member init(key, body, ?keyType, ?isOpen, ?search, ?height, ?xCoordinate) =
        let isOpen = defaultArg isOpen true
        let keyType = defaultArg keyType CompositeHeaderDiscriminate.Parameter

        let search =
            defaultArg search {
                Key = key
                KeyType = keyType
                Body = body
            }

        let height = defaultArg height 0.0
        let xCoordinate = defaultArg xCoordinate 0.0
        {
            IsOpen = isOpen
            Search = search
            Height = height
            XCoordinate = xCoordinate
        }

    member this.ToggleOpen() = { this with IsOpen = not this.IsOpen }

[<RequireQualifiedAccess>]

type UploadFileType =
    | Docx
    | PDF
    | Txt

type UploadedFile =
    | PDF of string
    | Docx of string
    | Txt of string
    | Unset
