[<AutoOpen>]
module ARCtrl.OntologyAnnotationExtensions

open ARCtrl
open Swate.Components
open Swate.Components.Shared

type OntologyAnnotation with

    static member private DescriptionCommentKey = "description"
    static member private IsObsoleteCommentKey = "isObsolete"
    static member empty() = OntologyAnnotation.create ()

    static member from(term: Swate.Components.Types.Term) =
        let comments =
            ResizeArray [
                if term.description.IsSome then
                    Comment(OntologyAnnotation.DescriptionCommentKey, term.description.Value)
                if term.isObsolete.IsSome then
                    Comment(OntologyAnnotation.IsObsoleteCommentKey, term.isObsolete.Value.ToString())
            ]

        OntologyAnnotation(?name = term.name, ?tsr = term.source, ?tan = term.id, comments = comments)

    static member from(term: Shared.Database.Term) =
        let comments =
            ResizeArray [
                if System.String.IsNullOrWhiteSpace term.Description |> not then
                    Comment(OntologyAnnotation.DescriptionCommentKey, term.Description)
                if term.IsObsolete then
                    Comment(OntologyAnnotation.IsObsoleteCommentKey, term.IsObsolete.ToString())
            ]

        OntologyAnnotation(term.Name, term.FK_Ontology, term.Accession, comments)

    member this.ToTerm() =
        let href =
            this.TermAccessionOntobeeUrl |> Option.whereNot System.String.IsNullOrWhiteSpace

        let description =
            this.Comments
            |> Seq.tryFind (fun c -> c.Name = Some OntologyAnnotation.DescriptionCommentKey)
            |> Option.bind (fun c -> c.Value)

        let isObsolete =
            this.Comments
            |> Seq.tryFind (fun c -> c.Name = Some OntologyAnnotation.IsObsoleteCommentKey)
            |> Option.bind (fun c -> c.Value)
            |> Option.map System.Boolean.Parse

        Swate.Components.Types.Term(
            ?name = this.Name,
            ?source = this.TermSourceREF,
            ?id = this.TermAccessionNumber,
            ?href = href,
            ?description = description,
            ?isObsolete = isObsolete
        )