namespace Swate.Components.Metadata

open System
open Fable.Core
open Feliz
open ARCtrl
open Swate.Components.Metadata

[<Erase; Mangle(false)>]
type StudyMetadata =

    [<ReactComponent(true)>]
    static member StudyMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (study: ArcStudy, setStudy: ArcStudy -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Study Metadata",
                content = [
                    FormComponents.TextInput(study.Identifier, (fun _ -> ()), label = "Identifier", disabled = true)
                    FormComponents.TextInput(
                        defaultArg study.Title "",
                        (fun value ->
                            study.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg study.Description "",
                        (fun value ->
                            study.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.PersonsInput(
                        study.Contacts,
                        (fun persons ->
                            study.Contacts <- persons
                            setStudy study
                        ),
                        label = "Contacts"
                    )
                    FormComponents.PublicationsInput(
                        study.Publications,
                        (fun publications ->
                            study.Publications <- publications
                            setStudy study
                        ),
                        label = "Publications"
                    )
                    FormComponents.DateTimeInput(
                        defaultArg study.SubmissionDate "",
                        (fun value ->
                            study.SubmissionDate <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Submission Date"
                    )
                    FormComponents.DateTimeInput(
                        defaultArg study.PublicReleaseDate "",
                        (fun value ->
                            study.PublicReleaseDate <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Public Release Date"
                    )
                    FormComponents.OntologyAnnotationsInput(
                        study.StudyDesignDescriptors,
                        (fun annotations ->
                            study.StudyDesignDescriptors <- annotations
                            setStudy study
                        ),
                        label = "Study Design Descriptors"
                    )
                    FormComponents.CommentsInput(
                        study.Comments,
                        (fun comments ->
                            study.Comments <- comments
                            setStudy study
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]