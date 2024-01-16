namespace ARCtrl.ISA

type MaterialType =
    | ExtractName // "Extract Name"
    | LabeledExtractName // "Labeled Extract Name"

    static member create t =
        if t = "Extract Name" then ExtractName
        elif t = "Labeled Extract Name" then LabeledExtractName
        else failwith "No other value than \"Extract Name\" or \"Labeled Extract Name\" allowed for materialtype"

    /// Returns the type of the MaterialType
    member this.AsString =
        match this with
        | ExtractName -> "Extract"
        | LabeledExtractName -> "Labeled Extract"
