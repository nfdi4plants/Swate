namespace Swate.Components.Metadata

open System
open Fable.Core
open Feliz
open ARCtrl
open Swate.Components
open Swate.Components.Metadata.FormComponents

[<Erase; Mangle(false)>]
type InvestigationMetadata =

    [<ReactComponent(true)>]
    static member InvestigationMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit) =
        LayoutComponents.Section [
            LayoutComponents.BoxedField(
                "Investigation Metadata",
                content = [
                    TextInput.TextInput(
                        investigation.Identifier,
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    TextInput.TextInput(
                        defaultArg investigation.Title "",
                        (fun value ->
                            investigation.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setInvestigation investigation
                        ),
                        label = "Title"
                    )
                    TextInput.TextInput(
                        defaultArg investigation.Description "",
                        (fun value ->
                            investigation.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setInvestigation investigation
                        ),
                        label = "Description",
                        isArea = true
                    )
                    PersonsInput.PersonsInput(
                        investigation.Contacts,
                        (fun persons ->
                            investigation.Contacts <- persons
                            setInvestigation investigation
                        ),
                        label = "Contacts"
                    )
                    PublicationsInput.PublicationsInput(
                        investigation.Publications,
                        (fun publications ->
                            investigation.Publications <- publications
                            setInvestigation investigation
                        ),
                        label = "Publications"
                    )
                    DateTimeInput.DateTimeInput(
                        defaultArg investigation.SubmissionDate "",
                        (fun value ->
                            investigation.SubmissionDate <-
                                if String.IsNullOrWhiteSpace value then None else Some value

                            setInvestigation investigation
                        ),
                        label = "Submission Date"
                    )
                    DateTimeInput.DateTimeInput(
                        defaultArg investigation.PublicReleaseDate "",
                        (fun value ->
                            investigation.PublicReleaseDate <-
                                if String.IsNullOrWhiteSpace value then None else Some value

                            setInvestigation investigation
                        ),
                        label = "Public Release Date"
                    )
                    OntologySourceReferencesInput.OntologySourceReferencesInput(
                        investigation.OntologySourceReferences,
                        (fun references ->
                            investigation.OntologySourceReferences <- references
                            setInvestigation investigation
                        ),
                        label = "Ontology Source References"
                    )
                    CommentsInput.CommentsInput(
                        investigation.Comments,
                        (fun comments ->
                            investigation.Comments <- comments
                            setInvestigation investigation
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]