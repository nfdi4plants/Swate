namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update

type MaterialAttribute = 
    {
        ID : URI option
        CharacteristicType : OntologyAnnotation option
    
    }
        
    static member make id characteristicType =
        {
            ID = id
            CharacteristicType = characteristicType     
        }

    static member create (?Id,?CharacteristicType) : MaterialAttribute =
        MaterialAttribute.make Id CharacteristicType

    static member empty =
        MaterialAttribute.create()

    /// Create a ISAJson MaterialAttribute from ISATab string entries
    static member fromString (term:string, source:string, accession:string, ?comments : Comment []) =
        let oa = OntologyAnnotation.fromString (term, source, accession, ?comments = comments)
        MaterialAttribute.make None (Option.fromValueWithDefault OntologyAnnotation.empty oa)

    /// Get ISATab string entries from an ISAJson MaterialAttribute object
    static member toString (ma : MaterialAttribute) =
        ma.CharacteristicType |> Option.map OntologyAnnotation.toString |> Option.defaultValue {|TermName = ""; TermAccessionNumber = ""; TermSourceREF = ""|}  

    /// Returns the name of the characteristic as string
    member this.NameText =
        this.CharacteristicType
        |> Option.map (fun oa -> oa.NameText)
        |> Option.defaultValue ""

    /// Returns the name of the characteristic as string
    member this.TryNameText =
        this.CharacteristicType
        |> Option.bind (fun oa -> oa.Name)

    member this.MapCategory(f : OntologyAnnotation -> OntologyAnnotation) =
        {this with CharacteristicType = Option.map f this.CharacteristicType}

    member this.SetCategory(c : OntologyAnnotation) =
        {this with CharacteristicType = Some c}

    interface IISAPrintable with
        member this.Print() =
            this.ToString()
        member this.PrintCompact() =
            "OA " + this.NameText

    /// Returns the name of the characteristic as string if it exists
    static member tryGetNameText (ma : MaterialAttribute) =
        ma.NameText

    /// Returns the name of the characteristic as string
    static member getNameText (ma : MaterialAttribute) =
        ma.TryNameText

    /// Returns true if the given name matches the name of the characteristic
    static member nameEqualsString (name : string) (ma : MaterialAttribute) =
        ma.NameText = name