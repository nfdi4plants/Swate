module ARCtrl.ISA.Json.ValidationTypes

type ValidationResult = 
    | Ok
    | Failed of string []

    member this.Success =
        match this with
        | Ok -> true
        | _ -> false

    member this.GetErrors() =
        match this with
        | Ok -> [||]
        | Failed errors -> errors

    static member internal OfJSchemaOutput (output : bool * string []) =
        match output with
        | true, _ -> Ok
        | false, errors -> Failed errors