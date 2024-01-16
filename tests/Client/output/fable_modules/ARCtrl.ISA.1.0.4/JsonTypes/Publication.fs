namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update
open Fable.Core

[<AttachMembers>]
type Publication = 
    {
        PubMedID : URI option
        DOI : string option
        Authors : string option
        Title : string option
        Status : OntologyAnnotation option
        Comments : Comment [] option
    }

    static member make pubMedID doi authors title status comments =
        {
            PubMedID    = pubMedID
            DOI         = doi
            Authors     = authors
            Title       = title
            Status      = status
            Comments    = comments
        }

    static member create (?PubMedID,?Doi,?Authors,?Title,?Status,?Comments) : Publication =
       Publication.make PubMedID Doi Authors Title Status Comments

    static member empty =
        Publication.create()

    /// Adds the given publication to the publications  
    static member add (publications : Publication list) (publication : Publication) =
        List.append publications [publication]

    /// Returns true, if a publication with the given doi exists in the investigation
    static member existsByDoi (doi : string) (publications : Publication list) =
        List.exists (fun p -> p.DOI = Some doi) publications

    /// Returns true, if a publication with the given pubmedID exists in the investigation
    static member existsByPubMedID (pubMedID : string) (publications : Publication list) =
        List.exists (fun p -> p.PubMedID = Some pubMedID) publications

    /// If a publication with the given doi exists in the investigation, returns it
    static member tryGetByDoi doi (publications:Publication list) =
        publications
        |> List.tryFind (fun publication -> publication.DOI = Some doi)

    /// Updates all publications for which the predicate returns true with the given publication values
    static member updateBy (predicate : Publication -> bool) (updateOption : UpdateOptions) (publication : Publication) (publications : Publication list) =
        if List.exists predicate publications then
            publications
            |> List.map (fun p -> if predicate p then updateOption.updateRecordType p publication else p) 
        else 
            publications

    /// Updates all protocols with the same DOI as the given publication with its values
    static member updateByDOI (updateOption : UpdateOptions) (publication : Publication) (publications : Publication list) =
        Publication.updateBy (fun p -> p.DOI = publication.DOI) updateOption publication publications

    /// Updates all protocols with the same pubMedID as the given publication with its values
    static member updateByPubMedID (updateOption : UpdateOptions) (publication : Publication) (publications : Publication list) =
        Publication.updateBy (fun p -> p.PubMedID = publication.PubMedID) updateOption publication publications

    /// If a publication with the given doi exists in the investigation, removes it from the investigation
    static member removeByDoi (doi : string) (publications : Publication list) = 
        List.filter (fun p -> p.DOI = Some doi |> not) publications

    /// If a publication with the given pubMedID exists in the investigation, removes it
    static member removeByPubMedID (pubMedID : string) (publications : Publication list) = 
        List.filter (fun p -> p.PubMedID = Some pubMedID |> not) publications

    /// Status

    /// Returns publication status of a publication
    static member getStatus (publication : Publication) =
        publication.Status

    /// Applies function f on publication status of a publication
    static member mapStatus (f : OntologyAnnotation -> OntologyAnnotation) (publication : Publication) =
        { publication with 
            Status = Option.map f publication.Status}

    /// Replaces publication status of a publication by given publication status
    static member setStatus (publication : Publication) (status : OntologyAnnotation) =
        { publication with
            Status = Some status }

    // Comments
    
    /// Returns comments of a protocol
    static member getComments (publication : Publication) =
        publication.Comments
    
    /// Applies function f on comments of a protocol
    static member mapComments (f : Comment [] -> Comment []) (publication : Publication) =
        { publication with 
            Comments = Option.mapDefault [||] f publication.Comments}
    
    /// Replaces comments of a protocol by given comment list
    static member setComments (publication : Publication) (comments : Comment []) =
        { publication with
            Comments = Some comments }


    //

    member this.Copy() =
        let nextComments = this.Comments |> Option.map (Array.map (fun c -> c.Copy()))
        Publication.make this.PubMedID this.DOI this.Authors this.Title this.Status nextComments