namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update

[<RequireQualifiedAccess>]
type ProcessOutput =
    | Sample of Sample
    | Data of Data
    | Material of Material 

    member this.TryName =
        match this with
        | ProcessOutput.Sample s     -> s.Name
        | ProcessOutput.Material m   -> m.Name
        | ProcessOutput.Data d       -> d.Name

    member this.Name =
        this.TryName
        |> Option.defaultValue ""

    static member Default = Sample (Sample.empty)

    interface IISAPrintable with
        member this.Print() = 
            this.ToString()
        member this.PrintCompact() =
            match this with 
            | ProcessOutput.Sample s     -> sprintf "Sample {%s}" ((s :> IISAPrintable).PrintCompact())
            | ProcessOutput.Material m   -> sprintf "Material {%s}" ((m :> IISAPrintable).PrintCompact())
            | ProcessOutput.Data d       -> sprintf "Data {%s}" ((d :> IISAPrintable).PrintCompact())

    /// Returns name of processOutput
    static member tryGetName (po : ProcessOutput) =
        po.TryName

    /// Returns name of processInput
    static member getName (po : ProcessOutput) =
        po.Name

    /// Returns true, if given name equals name of processOutput
    static member nameEquals (name : string) (po : ProcessOutput) =
        po.Name = name

    /// Returns true, if Process Output is Sample
    static member isSample (po : ProcessOutput) =
        match po with
        | ProcessOutput.Sample _ -> true
        | _ -> false

    /// Returns true, if Process Output is Data
    static member isData (po : ProcessOutput) =
        match po with
        | ProcessOutput.Data _ -> true
        | _ -> false

    /// Returns true, if Process Output is Material
    static member isMaterial (po : ProcessOutput) =
        match po with
        | ProcessOutput.Material _ -> true
        | _ -> false

    /// Returns true, if Process Output is Sample
    member this.isSample() =
        ProcessOutput.isSample this

    /// Returns true, if Process Output is Data
    member this.isData() =
        ProcessOutput.isData this

    /// Returns true, if Process Output is Material
    member this.isMaterial() =
        ProcessOutput.isMaterial this

    /// If given process output is a sample, returns it, else returns None
    static member trySample (po : ProcessOutput) =
        match po with
        | ProcessOutput.Sample s -> Some s
        | _ -> None

    /// If given process output is a data, returns it, else returns None
    static member tryData (po : ProcessOutput) =
        match po with
        | ProcessOutput.Data d -> Some d
        | _ -> None

    /// If given process output is a material, returns it, else returns None
    static member tryMaterial (po : ProcessOutput) =
        match po with
        | ProcessOutput.Material m -> Some m
        | _ -> None


    /// If given process output contains characteristics, returns them
    static member tryGetCharacteristicValues (po : ProcessOutput) =
        match po with
        | ProcessOutput.Sample s     -> s.Characteristics
        | ProcessOutput.Material m   -> m.Characteristics
        | ProcessOutput.Data _       -> None

    /// If given process output contains characteristics, returns them
    static member tryGetCharacteristics (po : ProcessOutput) =
        ProcessOutput.tryGetCharacteristicValues po
        |> Option.map (List.choose (fun c -> c.Category))


    /// If given process output contains factors, returns them
    static member tryGetFactorValues (po : ProcessOutput) =
        match po with
        | ProcessOutput.Sample s     -> s.FactorValues
        | ProcessOutput.Material _   -> None
        | ProcessOutput.Data _       -> None

    static member setFactorValues (values:FactorValue list) (po:ProcessOutput) =
        match po with
        | ProcessOutput.Sample s     -> Sample.setFactorValues values s |> Sample
        | ProcessOutput.Material _   -> po
        | ProcessOutput.Data _       -> po

    static member getFactorValues (po : ProcessOutput) =
        po |> ProcessOutput.tryGetFactorValues |> Option.defaultValue []

    /// If given process output contains units, returns them
    static member getUnits (po : ProcessOutput) =
        match po with
        | ProcessOutput.Sample s     -> Sample.getUnits s
        | ProcessOutput.Material m   -> Material.getUnits m
        | ProcessOutput.Data _       -> []

    static member createSample (name : string, ?characteristics, ?factors, ?derivesFrom) = 
        Sample.create(Name = name, ?Characteristics = characteristics, ?FactorValues = factors, ?DerivesFrom = derivesFrom)
        |> ProcessOutput.Sample

    static member createMaterial (name : string, ?characteristics, ?derivesFrom) =
        Material.create(Name = name, ?Characteristics = characteristics, ?DerivesFrom = derivesFrom)
        |> ProcessOutput.Material

    static member createImageFile (name : string) =
        Data.create(Name = name, DataType = DataFile.ImageFile)
        |> ProcessOutput.Data

    static member createRawData (name : string) =
        Data.create(Name = name, DataType = DataFile.RawDataFile)
        |> ProcessOutput.Data

    static member createDerivedData (name : string) =
        Data.create(Name = name, DataType = DataFile.DerivedDataFile)
        |> ProcessOutput.Data