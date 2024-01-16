namespace ARCtrl.ISA


type Source = 
    {
        ID : URI option
        Name : string option
        Characteristics : MaterialAttributeValue list option
    }

    static member make id name characteristics : Source=
        {
            ID              = id
            Name            = name
            Characteristics = characteristics          
        }

    static member create(?Id,?Name,?Characteristics) =
        Source.make Id Name Characteristics

    static member empty =
        Source.create()

    member this.NameAsString =
        this.Name
        |> Option.defaultValue ""

    interface IISAPrintable with
        member this.Print() = 
            this.ToString()
        member this.PrintCompact() =
            let l = this.Characteristics |> Option.defaultValue [] |> List.length
            sprintf "%s [%i characteristics]" this.NameAsString l 

    static member getUnits (m:Source) = 
        m.Characteristics
        |> Option.defaultValue []
        |> List.choose (fun c -> c.Unit)

    static member setCharacteristicValues (values:MaterialAttributeValue list) (m:Source) =
        { m with Characteristics = Some values }