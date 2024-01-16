namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Regex.ActivePatterns

type Component = 
    {
        // TODO: Maybe remove as field and add as member?
        ComponentName : string option
        /// This can be the main column value of the component column. (e.g. "SCIEX instrument model" as `OntologyAnnotation`; 14;..)
        ComponentValue : Value option
        /// This can be the unit describing a non `OntologyAnnotation` value in `ComponentValue`. (e.g. "degree celcius")
        ComponentUnit : OntologyAnnotation option
        /// This can be the component column header (e.g. "instrument model")
        ComponentType : OntologyAnnotation option
    }

    static member make name value unit componentType =
        {
            ComponentName = name
            ComponentValue = value
            ComponentUnit = unit
            ComponentType = componentType
        }

    static member create (?Name,?Value,?Unit,?ComponentType) : Component =
        Component.make Name Value Unit ComponentType

    static member empty =
        Component.create()
    
    /// This function creates a string containing full isa term triplet information about the component
    ///
    /// Components do not have enough fields in ISA-JSON to include all existing ontology term information. 
    /// This function allows us, to add the same information as `Parameter`, `Characteristics`.., to `Component`. 
    /// Without this string composition we loose the ontology information for the header value.
    static member composeName (value : Value Option) (unit : OntologyAnnotation option) = 
        match value,unit with
        | Some (Value.Ontology oa), _ ->
            $"{oa.NameText} ({oa.TermAccessionShort})"
        | Some v, None ->
            $"{v.Text}"
        | Some v, Some u ->
            $"{v.Text} {u.NameText} ({u.TermAccessionShort})"
        | None, _ -> ""

    /// This function parses the given Component header string format into the ISA-JSON Component type
    ///
    /// Components do not have enough fields in ISA-JSON to include all existing ontology term information. 
    /// This function allows us, to add the same information as `Parameter`, `Characteristics`.., to `Component`. 
    /// Without this string composition we loose the ontology information for the header value.
    static member decomposeName (name : string) = 
        let pattern = """(?<value>[^\(]+) \((?<ontology>[^(]*:[^)]*)\)"""
        let unitPattern = """(?<value>[\d\.]+) (?<unit>.+) \((?<ontology>[^(]*:[^)]*)\)"""

        match name with
        | Regex unitPattern unitr ->
            let oa = (unitr.Groups.Item "ontology").Value   |> OntologyAnnotation.fromTermAnnotation 
            let v =  (unitr.Groups.Item "value").Value      |> Value.fromString
            let u =  (unitr.Groups.Item "unit").Value
            v, Some {oa with Name = Some u}
        | Regex pattern r ->
            let oa = (r.Groups.Item "ontology").Value   |> OntologyAnnotation.fromTermAnnotation 
            let v =  (r.Groups.Item "value").Value      |> Value.fromString
            Value.Ontology {oa with Name = (Some  v.Text)}, None
        | _ -> 
            Value.Name (name), None       

    /// Create a ISAJson Component from ISATab string entries
    static member fromString (?name: string, ?term:string, ?source:string, ?accession:string, ?comments : Comment []) = 
        let cType = OntologyAnnotation.fromString (?termName = term, ?tsr=source, ?tan=accession, ?comments = comments) |> Option.fromValueWithDefault OntologyAnnotation.empty
        match name with
        | Some n -> 
            let v,u = Component.decomposeName n
            Component.make (name) (Option.fromValueWithDefault (Value.Name "") v) u cType
        | None ->
            Component.make None None None cType
        
    static member fromOptions (value: Value option) (unit: OntologyAnnotation Option) (header:OntologyAnnotation option) = 
        let name = Component.composeName value unit |> Option.fromValueWithDefault ""
        Component.make name value unit header

    /// Get ISATab string entries from an ISAJson Component object
    static member toString (c : Component) =
        let oa = c.ComponentType |> Option.map OntologyAnnotation.toString |> Option.defaultValue {|TermName = ""; TermAccessionNumber = ""; TermSourceREF = ""|}
        c.ComponentName |> Option.defaultValue "", oa

    member this.NameText =
        this.ComponentType
        |> Option.map (fun c -> c.NameText)
        |> Option.defaultValue ""

    /// Returns the ontology of the category of the Value as string
    member this.UnitText = 
        this.ComponentUnit
        |> Option.map (fun c -> c.NameText)
        |> Option.defaultValue ""

    member this.ValueText = 
        this.ComponentValue
        |> Option.map (fun c -> c.Text)
        |> Option.defaultValue ""

    member this.ValueWithUnitText =
        let unit = 
            this.ComponentUnit |> Option.map (fun oa -> oa.NameText)
        let v = this.ValueText
        match unit with
        | Some u    -> sprintf "%s %s" v u
        | None      -> v

    member this.MapCategory(f : OntologyAnnotation -> OntologyAnnotation) =
        {this with ComponentType = Option.map f this.ComponentType}

    member this.SetCategory(c : OntologyAnnotation) =
        {this with ComponentType = Some c}
