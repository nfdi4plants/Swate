namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update
open Fable.Core

[<CustomEquality; NoComparison>]
[<AttachMembers>]
type OntologyAnnotation =
    {
        ID : URI option
        Name : string option
        TermSourceREF : string option
        TermAccessionNumber : URI option
        Comments : Comment [] option
    }

    static member make id name termSourceREF termAccessionNumber comments= 
        {
            ID = id
            Name = name 
            TermSourceREF = termSourceREF
            TermAccessionNumber = termAccessionNumber  
            Comments = comments
        }
    
    /// This function creates the type exactly as given. If you want a more streamlined approach use `OntologyAnnotation.fromString`.
    static member create(?Id,?Name,?TermSourceREF,?TermAccessionNumber,?Comments) : OntologyAnnotation =
        OntologyAnnotation.make Id Name TermSourceREF TermAccessionNumber Comments

    static member empty =
        OntologyAnnotation.create()


    member this.TANInfo = 
        match this.TermAccessionNumber with
        | Some v -> 
            Regex.tryParseTermAnnotation v
        | None -> None

    member this.NameText = 
        this.Name
        |> Option.defaultValue ""

    /// Returns the term source of the ontology as string
    member this.TermSourceREFString =       
        this.TermSourceREF
        |> Option.defaultValue ""

    /// Returns the term accession number of the ontology as string
    member this.TermAccessionString =       
        this.TermAccessionNumber
        |> Option.defaultValue ""

    /// Create a path in form of `http://purl.obolibrary.org/obo/MS_1000121` from it's Term Accession Source `MS` and Local Term Accession Number `1000121`. 
    static member createUriAnnotation (termSourceRef : string) (localTAN : string) =
        $"{Url.OntobeeOboPurl}{termSourceRef}_{localTAN}"

    ///<summary>
    /// Create a ISAJson Ontology Annotation value from ISATab string entries, will try to reduce `termAccessionNumber` with regex matching.
    ///
    /// Exmp. 1: http://purl.obolibrary.org/obo/GO_000001 --> GO:000001
    ///</summary>
    ///<param name="tsr">Term source reference</param>
    ///<param name="tan">Term accession number</param>
    static member fromString (?termName:string, ?tsr:string, ?tan:string, ?comments : Comment []) =

        OntologyAnnotation.make 
            None 
            termName 
            tsr
            tan
            comments

    /// Will always be created without `OntologyAnnotion.Name`
    static member fromTermAnnotation (termAnnotation : string) =
        termAnnotation
        |> Regex.tryParseTermAnnotation
        |> Option.get 
        |> fun r ->
            let accession = r.IDSpace + ":" + r.LocalID
            OntologyAnnotation.fromString ("", r.IDSpace, accession)

    /// Parses any value in `TermAccessionString` to term accession format "termsourceref:localtan". Exmp.: "MS:000001".
    ///
    /// If `TermAccessionString` cannot be parsed to this format, returns empty string!
    member this.TermAccessionShort = 
        match this.TANInfo with
        | Some id -> $"{id.IDSpace}:{id.LocalID}"
        | _ -> ""

    member this.TermAccessionOntobeeUrl = 
        match this.TANInfo with
        | Some id -> OntologyAnnotation.createUriAnnotation id.IDSpace id.LocalID
        | _ -> ""

    member this.TermAccessionAndOntobeeUrlIfShort = 
        match this.TermAccessionNumber with
        | Some tan -> 
            match tan with 
            | Regex.ActivePatterns.Regex Regex.Pattern.TermAnnotationShortPattern _ -> this.TermAccessionOntobeeUrl
            | _ -> tan
        | _ -> ""

    /// <summary>
    /// Get a ISATab string entries from an ISAJson Ontology Annotation object (name,source,accession)
    ///
    /// `asOntobeePurlUrl`: option to return term accession in Ontobee purl-url format (`http://purl.obolibrary.org/obo/MS_1000121`)
    /// </summary>
    static member toString (oa : OntologyAnnotation, ?asOntobeePurlUrlIfShort: bool) =
        let asOntobeePurlUrlIfShort = Option.defaultValue false asOntobeePurlUrlIfShort
        {|
            TermName = oa.Name |> Option.defaultValue ""
            TermSourceREF = oa.TermSourceREF |> Option.defaultValue ""
            TermAccessionNumber = 
                if asOntobeePurlUrlIfShort then
                    let url = oa.TermAccessionAndOntobeeUrlIfShort
                    if url = "" then 
                        oa.TermAccessionNumber |> Option.defaultValue ""
                    else
                        url
                else
                    oa.TermAccessionNumber |> Option.defaultValue ""
        |}

    interface IISAPrintable with
        member this.Print() =
            this.ToString()
        member this.PrintCompact() =
            "OA " + this.NameText

    override this.Equals other =
        match other with
        | :? OntologyAnnotation as oa -> (this :> System.IEquatable<_>).Equals oa
        | :? string as s ->           
            this.NameText = s
            || 
            this.TermAccessionShort = s
            ||
            this.TermAccessionOntobeeUrl = s
        | _ -> false

    override this.GetHashCode () = (this.NameText+this.TermAccessionShort).GetHashCode()

    interface System.IEquatable<OntologyAnnotation> with
        member this.Equals other =
            if this.TermAccessionNumber.IsSome && other.TermAccessionNumber.IsSome then
                other.TermAccessionShort = this.TermAccessionShort
                ||
                other.TermAccessionOntobeeUrl = this.TermAccessionOntobeeUrl
            elif this.Name.IsSome && other.Name.IsSome then
                other.NameText = this.NameText
            elif this.TermAccessionNumber.IsNone && other.TermAccessionNumber.IsNone && this.Name.IsNone && other.Name.IsNone then
                true
            else 
                false

    /// Returns the name of the ontology as string if it has a name
    static member tryGetName (oa : OntologyAnnotation) =
        oa.Name

    /// Returns the name of the ontology as string if it has a name
    static member getNameText (oa : OntologyAnnotation) =
        oa.NameText

    /// Returns true if the given name matches the name of the ontology annotation
    static member nameEqualsString (name : string) (oa : OntologyAnnotation) =
        oa.NameText = name

    /// If an ontology annotation with the given annotation value exists in the list, returns it
    static member tryGetByName (name : string) (annotations : OntologyAnnotation list) =
        List.tryFind (fun (d:OntologyAnnotation) -> d.Name = Some name) annotations

    /// If a ontology annotation with the given annotation value exists in the list, returns true
    static member existsByName (name : string) (annotations : OntologyAnnotation list) =
        List.exists (fun (d:OntologyAnnotation) -> d.Name = Some name) annotations

    /// Adds the given ontology annotation to the Study.StudyDesignDescriptors
    static member add (onotolgyAnnotations: OntologyAnnotation list) (onotolgyAnnotation : OntologyAnnotation) =
        List.append onotolgyAnnotations [onotolgyAnnotation]

    /// Updates all ontology annotations for which the predicate returns true with the given ontology annotations values
    static member updateBy (predicate : OntologyAnnotation -> bool) (updateOption : UpdateOptions) (design : OntologyAnnotation) (annotations : OntologyAnnotation list) =
        if List.exists predicate annotations then
            annotations
            |> List.map (fun d -> if predicate d then updateOption.updateRecordType d design else d)
        else 
            annotations

    /// If an ontology annotation with the same annotation value as the given annotation value exists in the list, updates it with the given ontology annotation
    static member updateByName (updateOption:UpdateOptions) (design : OntologyAnnotation) (annotations : OntologyAnnotation list) =
        OntologyAnnotation.updateBy (fun f -> f.Name = design.Name) updateOption design annotations

    /// If a ontology annotation with the annotation value exists in the list, removes it
    static member removeByName (name : string) (annotations : OntologyAnnotation list) = 
        List.filter (fun (d:OntologyAnnotation) -> d.Name = Some name |> not) annotations

    // Comments
    
    /// Returns comments of a ontology annotation
    static member getComments (annotation : OntologyAnnotation) =
        annotation.Comments
    
    /// Applies function f on comments of a ontology annotation
    static member mapComments (f : Comment [] -> Comment []) (annotation : OntologyAnnotation) =
        { annotation with 
            Comments = Option.mapDefault [||] f annotation.Comments}
    
    /// Replaces comments of a ontology annotation by given comment list
    static member setComments (annotation : OntologyAnnotation) (comments : Comment []) =
        { annotation with
            Comments = Some comments }

    member this.Copy() =
        let nextComments = this.Comments |> Option.map (Array.map (fun c -> c.Copy()))
        OntologyAnnotation.make this.ID this.Name this.TermSourceREF this.TermAccessionNumber nextComments