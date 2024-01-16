module ARCtrl.ISA.ColumnIndex


let private tryInt (str:string) =
    match System.Int32.TryParse str with
    | true,int -> Some int
    | _ -> None

let orderName = "ColumnIndex"

let createOrderComment (index : int) =
    Comment.fromString orderName (string index)

let tryGetIndex (comments : Comment []) =
    comments 
    |> CommentArray.tryItem orderName 
    |> Option.bind tryInt

let setOntologyAnnotationIndex i (oa : OntologyAnnotation) =
    {
        oa with 
            Comments = 
                match oa.Comments with
                | Some cs -> Some <| Array.append [|createOrderComment i|] cs
                | None -> Some <| [|createOrderComment i|]
    }

let tryGetOntologyAnnotationIndex (oa : OntologyAnnotation) =
    oa.Comments |> Option.bind tryGetIndex

let tryGetParameterIndex (param : ProtocolParameter) =
    param.ParameterName 
    |> Option.bind (fun oa -> 
        oa.Comments |> Option.bind tryGetIndex
    )

let tryGetParameterColumnIndex (paramValue : ProcessParameterValue) =
    paramValue.Category 
    |> Option.bind tryGetParameterIndex

let tryGetFactorIndex (factor : Factor) =
    factor.FactorType 
    |> Option.bind (fun oa -> 
        oa.Comments |> Option.bind tryGetIndex
    )
      
let tryGetFactorColumnIndex (factorValue : FactorValue) =
    factorValue.Category 
    |> Option.bind tryGetFactorIndex

let tryGetCharacteristicIndex (characteristic : MaterialAttribute) =
    characteristic.CharacteristicType 
    |> Option.bind (fun oa -> 
        oa.Comments |> Option.bind tryGetIndex
    )
      
let tryGetCharacteristicColumnIndex (characteristicValue : MaterialAttributeValue) =
    characteristicValue.Category 
    |> Option.bind tryGetCharacteristicIndex

let tryGetComponentIndex (comp : Component) =
    comp.ComponentType 
    |> Option.bind (fun oa -> 
        oa.Comments |> Option.bind tryGetIndex
    )
      

[<AutoOpen>]
module ColumnIndexExtensions = 
    
    type OntologyAnnotation with

        /// Create a ISAJson Factor from ISATab string entries
        static member fromStringWithColumnIndex (name:string) (term:string) (source:string) (accession:string) valueIndex =
            Factor.fromString(name,term,source,accession,[|createOrderComment valueIndex|])

        static member getColumnIndex(f) = tryGetOntologyAnnotationIndex f |> Option.get

        member this.GetColumnIndex() = tryGetOntologyAnnotationIndex this |> Option.get

        static member tryGetColumnIndex(f) = tryGetOntologyAnnotationIndex f

        member this.TryGetColumnIndex() = tryGetOntologyAnnotationIndex this

        static member setColumnIndex i oa = setOntologyAnnotationIndex i oa

        member this.SetColumnIndex i = setOntologyAnnotationIndex i this

    type Factor with
        
        /// Create a ISAJson Factor from ISATab string entries
        static member fromStringWithColumnIndex (name:string) (term:string) (source:string) (accession:string) valueIndex =
            Factor.fromString(name,term,source,accession,[|createOrderComment valueIndex|])

        static member getColumnIndex(f) = tryGetFactorIndex f |> Option.get

        member this.GetColumnIndex() = tryGetFactorIndex this |> Option.get

        static member tryGetColumnIndex(f) = tryGetFactorIndex f

        member this.TryGetColumnIndex() = tryGetFactorIndex this

    type FactorValue with

        static member getColumnIndex(f) = tryGetFactorColumnIndex f |> Option.get

        member this.GetColumnIndex() = tryGetFactorColumnIndex this |> Option.get

        static member tryGetColumnIndex(f) = tryGetFactorColumnIndex f

        member this.TryGetColumnIndex() = tryGetFactorColumnIndex this

    type MaterialAttribute with
    
        /// Create a ISAJson characteristic from ISATab string entries
        static member fromStringWithColumnIndex (term:string) (source:string) (accession:string) valueIndex =
            MaterialAttribute.fromString(term,source,accession,[|createOrderComment valueIndex|])

        static member getColumnIndex(m) = tryGetCharacteristicIndex m |> Option.get

        member this.GetColumnIndex() = tryGetCharacteristicIndex this |> Option.get

        static member tryGetColumnIndex(m) = tryGetCharacteristicIndex m
        
        member this.TryGetColumnIndex() = tryGetCharacteristicIndex this

    type MaterialAttributeValue with
            
        static member getColumnIndex(m) = tryGetCharacteristicColumnIndex m |> Option.get

        member this.GetColumnIndex() = tryGetCharacteristicColumnIndex this |> Option.get

        static member tryGetColumnIndex(m) = tryGetCharacteristicColumnIndex m
            
        member this.TryGetColumnIndex() = tryGetCharacteristicColumnIndex this

    type ProtocolParameter with
    
        /// Create a ISAJson parameter from ISATab string entries
        static member fromStringWithColumnIndex (term:string) (source:string) (accession:string) valueIndex =
            ProtocolParameter.fromString(term,source,accession,[|createOrderComment valueIndex|])

        static member getColumnIndex(p) = tryGetParameterIndex p |> Option.get

        member this.GetColumnIndex() = tryGetParameterIndex this |> Option.get

        static member tryGetColumnIndex(p) = tryGetParameterIndex p
        
        member this.TryGetColumnIndex() = tryGetParameterIndex this

    type ProcessParameterValue with
    
        static member getColumnIndex(p) = tryGetParameterColumnIndex p |> Option.get

        member this.GetColumnIndex() = tryGetParameterColumnIndex this |> Option.get

        static member tryGetColumnIndex(p) = tryGetParameterColumnIndex p
        
        member this.TryGetColumnIndex() = tryGetParameterColumnIndex this
        
    type Component with 

        /// Create a ISAJson Factor from ISATab string entries
        static member fromStringWithColumnIndex (name:string) (term:string) (source:string) (accession:string) valueIndex =
            Component.fromString(name,term,source,accession,[|createOrderComment valueIndex|])

        static member getColumnIndex(f) = tryGetComponentIndex f |> Option.get

        member this.GetColumnIndex() = tryGetComponentIndex this |> Option.get

        static member tryGetColumnIndex(f) = tryGetComponentIndex f

        member this.TryGetColumnIndex() = tryGetComponentIndex this
