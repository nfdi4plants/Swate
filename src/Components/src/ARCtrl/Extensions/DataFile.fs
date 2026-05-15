[<AutoOpen>]
module ARCtrl.DataFileExtensions

open ARCtrl

type DataFile with
    member this.ToStringRdb() =
        match this with
        | DataFile.DerivedDataFile -> "Derived Data File"
        | DataFile.ImageFile -> "Image File"
        | DataFile.RawDataFile -> "Raw Data File"

    static member tryFromString(str: string) =
        match str.ToLower() with
        | "derived data file"
        | "deriveddatafile" -> Some DataFile.DerivedDataFile
        | "image file"
        | "imagefile" -> Some DataFile.ImageFile
        | "raw data file"
        | "rawdatafile" -> Some DataFile.RawDataFile
        | _ -> None

    static member fromString(str: string) =
        match DataFile.tryFromString str with
        | Some r -> r
        | None -> failwithf "Unknown DataFile: %s" str
