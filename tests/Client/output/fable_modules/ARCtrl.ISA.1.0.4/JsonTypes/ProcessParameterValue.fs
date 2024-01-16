namespace ARCtrl.ISA

open Fable.Core

[<AttachMembers>]
type ProcessParameterValue =
    {
        Category    : ProtocolParameter option
        Value       : Value option
        Unit        : OntologyAnnotation option
    }

    static member make category value unit : ProcessParameterValue = 
        {
            Category = category
            Value = value
            Unit = unit
        }

    static member create (?Category,?Value,?Unit) : ProcessParameterValue = 
        ProcessParameterValue.make Category Value Unit

    static member empty =
        ProcessParameterValue.create()     

    /// Returns the name of the category as string
    member this.NameText =
        this.Category
        |> Option.map (fun oa -> oa.NameText)
        |> Option.defaultValue ""

    /// Returns the name of the category as string
    member this.TryNameText =
        this.Category
        |> Option.bind (fun oa -> oa.TryNameText)

    member this.ValueText =
    
        this.Value
        |> Option.map (fun oa ->
            match oa with
            | Value.Ontology oa  -> oa.NameText
            | Value.Float f -> string f
            | Value.Int i   -> string i
            | Value.Name s  -> s
        )
        |> Option.defaultValue ""

    member this.ValueWithUnitText =
        let unit = 
            this.Unit |> Option.map (fun oa -> oa.NameText)
        let v = this.ValueText
        match unit with
        | Some u    -> sprintf "%s %s" v u
        | None      -> v

    member this.MapCategory(f : OntologyAnnotation -> OntologyAnnotation) =
        {this with Category = this.Category |> Option.map (fun p -> p.MapCategory f) }

    member this.SetCategory(c : OntologyAnnotation) =
        {this with Category = 
                            match this.Category with
                            | Some p -> Some (p.SetCategory c)
                            | None -> Some (ProtocolParameter.create(ParameterName = c))
        }

    interface IISAPrintable with
        member this.Print() =
            this.ToString()
        member this.PrintCompact() =
            let category = this.Category |> Option.map (fun f -> f.NameText)
            let unit = this.Unit |> Option.map (fun oa -> oa.NameText)
            let value = 
                this.Value
                |> Option.map (fun v ->
                    let s = (v :> IISAPrintable).PrintCompact()
                    match unit with
                    | Some u -> s + " " + u
                    | None -> s
                )
            match category,value with
            | Some category, Some value -> category + ":" + value
            | Some category, None -> category + ":" + "No Value"
            | None, Some value -> value
            | None, None -> ""

    /// Returns the name of the paramater value as string if it exists
    static member tryGetNameText (pv : ProcessParameterValue) =
        pv.TryNameText

    /// Returns the name of the paramater value as string
    static member getNameText (pv : ProcessParameterValue) =
        pv.NameText

    /// Returns true if the given name matches the name of the parameter value
    static member nameEqualsString (name : string) (pv : ProcessParameterValue) =
        pv.NameText = name


    static member getCategory (pv : ProcessParameterValue) =
        pv.Category

    ///// Returns the value of the parameter value as string if it exists (with unit)
    //static member tryGetValueAsString (pv : ProcessParameterValue) =
    //    static member unit = pv.Unit |> Option.bind (OntologyAnnotation.tryGetNameAsString)
    //    pv.Value
    //    |> Option.map (fun v ->
    //        static member s = v |> Value.toString
    //        match unit with
    //        | Some u -> s + " " + u
    //        | None -> s
    //    )

    ///// Returns the value of the parameter value as string (with unit)
    //static member getValueAsString (pv : ProcessParameterValue) =
    //    tryGetValueAsString pv
    //    |> Option.defaultValue ""