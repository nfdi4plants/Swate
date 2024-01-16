namespace ARCtrl.ISA

type Material = 
    {
        ID : URI option
        Name : string option
        MaterialType : MaterialType option
        Characteristics : MaterialAttributeValue list option
        DerivesFrom : Material list option   
    }

    static member make id name materialType characteristics derivesFrom : Material=
        {
            ID              = id
            Name            = name
            MaterialType    = materialType
            Characteristics = characteristics     
            DerivesFrom     = derivesFrom       
        }

    static member create(?Id,?Name,?MaterialType,?Characteristics,?DerivesFrom) : Material = 
        Material.make Id Name MaterialType Characteristics DerivesFrom

    static member empty =
        Material.create()

    member this.NameText =
        this.Name
        |> Option.defaultValue ""

    interface IISAPrintable with
        member this.Print() = 
            this.ToString()
        member this.PrintCompact() =
            let chars = this.Characteristics |> Option.defaultValue [] |> List.length
            match this.MaterialType with
            | Some t ->
                sprintf "%s [%s; %i characteristics]" this.NameText t.AsString chars
            | None -> sprintf "%s [%i characteristics]" this.NameText chars

    static member getUnits (m:Material) = 
        m.Characteristics
        |> Option.defaultValue []
        |> List.choose (fun c -> c.Unit)

    static member setCharacteristicValues (values:MaterialAttributeValue list) (m:Material) =
        { m with Characteristics = Some values }