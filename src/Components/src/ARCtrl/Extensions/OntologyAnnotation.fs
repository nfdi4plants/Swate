[<AutoOpen>]
module ARCtrl.OntologyAnnotationExtensions

open ARCtrl
open Swate.Components

module OntologyAnnotationHelper =

    [<Literal>]
    let DescriptionCommentKey = "description"

    [<Literal>]
    let IsObsoleteCommentKey = "isObsolete"

type OntologyAnnotation with
    static member empty() = OntologyAnnotation.create ()

    member this.hasName = not (System.String.IsNullOrWhiteSpace this.NameText)

    member this.hasTermMetadata =
        Option.exists (System.String.IsNullOrWhiteSpace >> not) this.TermSourceREF
        && Option.exists (System.String.IsNullOrWhiteSpace >> not) this.TermAccessionNumber

    member this.NameEquals(other: OntologyAnnotation) =
        this.hasName
        && other.hasName
        && System.String.Equals(this.NameText, other.NameText, System.StringComparison.OrdinalIgnoreCase)

    static member from(term: Shared.Database.Term) =
        let comments =
            ResizeArray [
                if System.String.IsNullOrWhiteSpace term.Description |> not then
                    Comment(OntologyAnnotationHelper.DescriptionCommentKey, term.Description)
                if term.IsObsolete then
                    Comment(OntologyAnnotationHelper.IsObsoleteCommentKey, term.IsObsolete.ToString())
            ]

        OntologyAnnotation(term.Name, term.FK_Ontology, term.Accession, comments)
