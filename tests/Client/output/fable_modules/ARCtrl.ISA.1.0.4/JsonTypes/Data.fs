namespace ARCtrl.ISA

type Data = 
    {
        ID : URI option
        Name : string option
        DataType : DataFile option
        Comments : Comment list option
    }

    static member make id name dataType comments =
        {
            ID      = id
            Name    = name
            DataType = dataType
            Comments = comments         
        }

    static member create (?Id,?Name,?DataType,?Comments) = 
        Data.make Id Name DataType Comments

    static member empty =
        Data.create()

    member this.NameAsString =
        this.Name
        |> Option.defaultValue ""

    interface IISAPrintable with
        member this.Print() = 
            this.ToString()
        member this.PrintCompact() =
            match this.DataType with
            | Some t ->
                sprintf "%s [%s]" this.NameAsString t.AsString 
            | None -> sprintf "%s" this.NameAsString
