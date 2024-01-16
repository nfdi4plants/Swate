namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update
open Fable.Core

[<AttachMembers>]
type OntologySourceReference =
    {
        Description : string option
        File : string option
        Name : string option
        Version : string option
        Comments : Comment [] option
    }

    static member make description file name version comments  =
        {

            Description = description
            File        = file
            Name        = name
            Version     = version
            Comments    = comments
        }

    static member create(?Description,?File,?Name,?Version,?Comments) : OntologySourceReference =
        OntologySourceReference.make Description File Name Version Comments

    static member empty =
        OntologySourceReference.create()

    /// If an ontology source reference with the given name exists in the list, returns it
    static member tryGetByName (name : string) (ontologies : OntologySourceReference list) =
        List.tryFind (fun (t : OntologySourceReference) -> t.Name = Some name) ontologies

    /// If an ontology source reference with the given name exists in the list, returns true
    static member existsByName (name : string) (ontologies : OntologySourceReference list) =
        List.exists (fun (t : OntologySourceReference) -> t.Name = Some name) ontologies

    /// Adds the given ontology source reference to the investigation  
    static member add (ontologySourceReference : OntologySourceReference) (ontologies : OntologySourceReference list) =
        List.append ontologies [ontologySourceReference]

    /// Updates all ontology source references for which the predicate returns true with the given ontology source reference values
    static member updateBy (predicate : OntologySourceReference -> bool) (updateOption : UpdateOptions) (ontologySourceReference : OntologySourceReference) (ontologies : OntologySourceReference list) =
        if List.exists predicate ontologies then
            ontologies
            |> List.map (fun t -> if predicate t then updateOption.updateRecordType t ontologySourceReference else t) 
        else 
            ontologies

    /// If an ontology source reference with the same name as the given name exists in the investigation, updates it with the given ontology source reference
    static member updateByName (updateOption:UpdateOptions) (ontologySourceReference : OntologySourceReference) (ontologies:OntologySourceReference list) =
        OntologySourceReference.updateBy (fun t -> t.Name = ontologySourceReference.Name) updateOption ontologySourceReference ontologies


    /// If a ontology source reference with the given name exists in the list, removes it
    static member removeByName (name : string) (ontologies : OntologySourceReference list) = 
        List.filter (fun (t : OntologySourceReference) -> t.Name = Some name |> not) ontologies

    /// Returns comments of ontology source ref
    static member getComments (ontology : OntologySourceReference) =
        ontology.Comments

    /// Applies function f on comments in ontology source ref
    static member mapComments (f : Comment [] -> Comment []) (ontology : OntologySourceReference) =
        { ontology with 
            Comments = Option.mapDefault [||] f ontology.Comments}

    /// Replaces comments in ontology source ref by given comment list
    static member setComments (ontology : OntologySourceReference) (comments : Comment []) =
        { ontology with
            Comments = Some comments }

    member this.Copy() =
        let nextComments = this.Comments |> Option.map (Array.map (fun c -> c.Copy()))
        OntologySourceReference.make this.Description this.File this.Name this.Version nextComments