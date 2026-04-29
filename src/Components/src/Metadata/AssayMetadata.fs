namespace Swate.Components.Metadata

open System
open Fable.Core
open Feliz
open ARCtrl
open Swate.Components.Metadata

[<Erase; Mangle(false)>]
type AssayMetadata =

    [<ReactComponent(true)>]
    static member AssayMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (assay: ArcAssay, setAssay: ArcAssay -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Assay Metadata",
                content = [
                    FormComponents.TextInput(assay.Identifier, (fun _ -> ()), label = "Identifier", disabled = true)
                    FormComponents.TextInput(
                        defaultArg assay.Title "",
                        (fun value ->
                            assay.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setAssay assay
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg assay.Description "",
                        (fun value ->
                            assay.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setAssay assay
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.OntologyAnnotationInput(
                        assay.MeasurementType,
                        (fun annotation ->
                            assay.MeasurementType <- annotation
                            setAssay assay
                        ),
                        label = "Measurement Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        assay.TechnologyType,
                        (fun annotation ->
                            assay.TechnologyType <- annotation
                            setAssay assay
                        ),
                        label = "Technology Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        assay.TechnologyPlatform,
                        (fun annotation ->
                            assay.TechnologyPlatform <- annotation
                            setAssay assay
                        ),
                        label = "Technology Platform"
                    )
                    FormComponents.PersonsInput(
                        assay.Performers,
                        (fun persons ->
                            assay.Performers <- persons
                            setAssay assay
                        ),
                        label = "Performers"
                    )
                    FormComponents.CommentsInput(
                        assay.Comments,
                        (fun comments ->
                            assay.Comments <- comments
                            setAssay assay
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]