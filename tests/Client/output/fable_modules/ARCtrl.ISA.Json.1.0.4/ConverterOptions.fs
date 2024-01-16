namespace ARCtrl.ISA.Json

type ConverterOptions() = 

    let mutable setID = false
    let mutable includeType = false

    member this.SetID 
        with get() = setID
        and set(setId) = setID <- setId
    member this.IncludeType 
        with get() = includeType
        and set(iT) = includeType <- iT
