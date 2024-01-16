namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update

[<RequireQualifiedAccess>]
type ProcessInput =
    
    | Source of Source
    | Sample of Sample
    | Data of Data
    | Material of Material 

    member this.TryName =
        match this with
        | ProcessInput.Sample s     -> s.Name
        | ProcessInput.Source s     -> s.Name
        | ProcessInput.Material m   -> m.Name
        | ProcessInput.Data d       -> d.Name

    member this.Name =
        this.TryName
        |> Option.defaultValue ""

    static member Default = Source (Source.empty)

    interface IISAPrintable with
        member this.Print() = 
            this.ToString()
        member this.PrintCompact() =
            match this with 
            | ProcessInput.Sample s     -> sprintf "Sample {%s}" ((s :> IISAPrintable).PrintCompact())
            | ProcessInput.Source s     -> sprintf "Source {%s}" ((s :> IISAPrintable).PrintCompact())
            | ProcessInput.Material m   -> sprintf "Material {%s}" ((m :> IISAPrintable).PrintCompact())
            | ProcessInput.Data d       -> sprintf "Data {%s}" ((d :> IISAPrintable).PrintCompact())

    /// Returns name of processInput
    static member tryGetName (pi : ProcessInput) =
        pi.TryName

    /// Returns name of processInput
    static member getName (pi : ProcessInput) =
        pi.Name

    /// Returns true, if given name equals name of processInput
    static member nameEquals (name : string) (pi : ProcessInput) =
        pi.Name = name

    /// Returns true, if Process Input is Sample
    static member isSample (pi : ProcessInput) =
        match pi with
        | ProcessInput.Sample _ -> true
        | _ -> false

    /// Returns true, if Process Input is Source
    static member isSource (pi : ProcessInput) =
        match pi with
        | ProcessInput.Source _ -> true
        | _ -> false

    /// Returns true, if Process Input is Data
    static member isData (pi : ProcessInput) =
        match pi with
        | ProcessInput.Data _ -> true
        | _ -> false

    /// Returns true, if Process Input is Material
    static member isMaterial (pi : ProcessInput) =
        match pi with
        | ProcessInput.Material _ -> true
        | _ -> false

    /// Returns true, if Process Input is Source
    member this.isSource() =
        ProcessInput.isSource this

    /// Returns true, if Process Input is Sample
    member this.isSample() =
        ProcessInput.isSample this

    /// Returns true, if Process Input is Data
    member this.isData() =
        ProcessInput.isData this

    /// Returns true, if Process Input is Material
    member this.isMaterial() =
        ProcessInput.isMaterial this

    /// If given process input is a sample, returns it, else returns None
    static member trySample (pi : ProcessInput) =
        match pi with
        | ProcessInput.Sample s -> Some s
        | _ -> None

    /// If given process input is a source, returns it, else returns None
    static member trySource (pi : ProcessInput) =
        match pi with
        | ProcessInput.Source s -> Some s
        | _ -> None

    /// If given process input is a data, returns it, else returns None
    static member tryData (pi : ProcessInput) =
        match pi with
        | ProcessInput.Data d -> Some d
        | _ -> None

    /// If given process input is a material, returns it, else returns None
    static member tryMaterial (pi : ProcessInput) =
        match pi with
        | ProcessInput.Material m -> Some m
        | _ -> None

    static member setCharacteristicValues (characteristics : MaterialAttributeValue list) (pi : ProcessInput) =
        match pi with
        | ProcessInput.Sample s     -> ProcessInput.Sample (Sample.setCharacteristicValues characteristics s)
        | ProcessInput.Source s     -> ProcessInput.Source (Source.setCharacteristicValues characteristics s)
        | ProcessInput.Material m   -> ProcessInput.Material (Material.setCharacteristicValues characteristics m)
        | ProcessInput.Data _       -> pi

    /// If given process input contains characteristics, returns them
    static member tryGetCharacteristicValues (pi : ProcessInput) =
        match pi with
        | ProcessInput.Sample s     -> s.Characteristics
        | ProcessInput.Source s     -> s.Characteristics
        | ProcessInput.Material m   -> m.Characteristics
        | ProcessInput.Data _       -> None

    /// If given process input contains characteristics, returns them
    static member tryGetCharacteristics (pi : ProcessInput) =
        ProcessInput.tryGetCharacteristicValues pi
        |> Option.map (List.choose (fun c -> c.Category))

    static member getCharacteristicValues (pi : ProcessInput) = 
        pi |> ProcessInput.tryGetCharacteristicValues |> Option.defaultValue []

    /// If given process output contains units, returns them
    static member getUnits (pi : ProcessInput) =
        match pi with
        | ProcessInput.Source s     -> Source.getUnits s
        | ProcessInput.Sample s     -> Sample.getUnits s
        | ProcessInput.Material m   -> Material.getUnits m
        | ProcessInput.Data _       -> []


    static member createSource (name : string, ?characteristics) =
        Source.create(Name = name, ?Characteristics = characteristics)
        |> ProcessInput.Source

    static member createSample (name : string, ?characteristics, ?factors, ?derivesFrom) = 
        Sample.create(Name = name, ?Characteristics = characteristics, ?FactorValues = factors, ?DerivesFrom = derivesFrom)
        |> ProcessInput.Sample

    static member createMaterial (name : string, ?characteristics, ?derivesFrom) =
        Material.create(Name = name, ?Characteristics = characteristics, ?DerivesFrom = derivesFrom)
        |> ProcessInput.Material

    static member createImageFile (name : string) =
        Data.create(Name = name, DataType = DataFile.ImageFile)
        |> ProcessInput.Data

    static member createRawData (name : string) =
        Data.create(Name = name, DataType = DataFile.RawDataFile)
        |> ProcessInput.Data

    static member createDerivedData (name : string) =
        Data.create(Name = name, DataType = DataFile.DerivedDataFile)
        |> ProcessInput.Data