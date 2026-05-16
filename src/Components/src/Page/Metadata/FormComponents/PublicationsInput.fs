namespace Swate.Components.Metadata.FormComponents

open Fable.Core
open Browser.Types
open Feliz
open ARCtrl

open Swate.Components

module private PublicationsInputHelper =

    let toOptionalString (value: string) =
        if value = "" then None else Some value

    let countFilledFieldsString (publication: Publication) =
        let fields = [
            publication.PubMedID
            publication.DOI
            publication.Title
            publication.Authors
            publication.Status |> Option.map (fun _ -> "")
        ]

        let total = fields.Length
        let filled = fields |> List.choose id |> List.length
        $"{filled}/{total}"

[<Erase; Mangle(false)>]
type PublicationsInput =

    [<ReactComponent>]
    static member private PublicationInput(publication: Publication, setter: Publication -> unit, ?rmv: MouseEvent -> unit) =
        let title = Option.defaultValue "<title>" publication.Title
        let doi = Option.defaultValue "<doi>" publication.DOI

        let createFieldTextInput
            (field: string option, label: string, publicationSetter: string option -> unit)
            =
            TextInput.TextInput(
                field |> Option.defaultValue "",
                (fun value ->
                    let nextValue = PublicationsInputHelper.toOptionalString value
                    publicationSetter nextValue
                    setter publication
                ),
                label
            )

        LayoutComponents.Collapse [
            LayoutComponents.CollapseTitle(title, doi, PublicationsInputHelper.countFilledFieldsString publication)
        ] [
            createFieldTextInput (publication.Title, "Title", fun value -> publication.Title <- value)
            Helpers.cardFormGroup [
                createFieldTextInput (publication.PubMedID, "PubMed ID", fun value -> publication.PubMedID <- value)
                createFieldTextInput (publication.DOI, "DOI", fun value -> publication.DOI <- value)
            ]
            createFieldTextInput (publication.Authors, "Authors", fun value -> publication.Authors <- value)
            OntologyAnnotationInput.OntologyAnnotationInput(
                publication.Status,
                (fun value ->
                    publication.Status <- value
                    setter publication
                ),
                "Status",
                parent = TermCollection.PublicationStatus
            )
            CommentsInput.CommentsInput(
                publication.Comments,
                (fun comments ->
                    publication.Comments <- ResizeArray comments
                    setter publication
                ),
                "Comments"
            )
            if rmv.IsSome then
                Helpers.deleteButton rmv.Value
        ]

    [<ReactComponent>]
    static member PublicationsInput
        (
            publications: ResizeArray<Publication>,
            setter: ResizeArray<Publication> -> unit,
            label: string
        ) =
        InputSequence.InputSequence(
            publications,
            Publication,
            setter,
            (fun (value, setValue, remove) -> PublicationsInput.PublicationInput(value, setValue, rmv = remove)),
            label = label
        )
