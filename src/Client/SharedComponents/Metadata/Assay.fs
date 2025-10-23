module Components.Metadata.Assay

open Swate.Components.Shared
open Feliz
open Feliz.DaisyUI
open ARCtrl
open Swate.Components.Shared
open Components
open Components.Forms

[<ReactComponent>]
let Main (assay: ArcAssay, setArcAssay: ArcAssay -> unit, model: Model.Model) =
    Generic.Section [
        Generic.BoxedField(
            "Assay Metadata",
            content = [
                FormComponents.TextInput(
                    assay.Identifier,
                    (fun v ->
                        let nextAssay = IdentifierSetters.setAssayIdentifier v assay
                        setArcAssay nextAssay
                    ),
                    "Identifier",
                    validator = {|
                        fn = (fun s -> ARCtrl.Helper.Identifier.tryCheckValidCharacters s)
                        msg = "Invalid Identifier"
                    |},
                    disabled = Generic.isDisabledInARCitect model.PersistentStorageState.Host,
                    classes = "swt:w-full"
                )
                FormComponents.TextInput(
                    defaultArg assay.Title "",
                    (fun v ->
                        assay.Title <- Option.whereNot System.String.IsNullOrWhiteSpace v
                        setArcAssay <| assay
                    ),
                    "Title",
                    classes = "swt:w-full"
                )
                FormComponents.TextInput(
                    defaultArg assay.Description "",
                    (fun v ->
                        assay.Description <- Option.whereNot System.String.IsNullOrWhiteSpace v
                        setArcAssay <| assay
                    ),
                    "Description",
                    classes = "swt:w-full",
                    isarea = true
                )
                FormComponents.OntologyAnnotationInput(
                    assay.MeasurementType,
                    (fun oa ->
                        assay.MeasurementType <- oa
                        setArcAssay <| assay
                    ),
                    "Measurement Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyType,
                    (fun oa ->
                        assay.TechnologyType <- oa
                        setArcAssay <| assay
                    ),
                    "Technology Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyPlatform,
                    (fun oa ->
                        assay.TechnologyPlatform <- oa
                        setArcAssay <| assay
                    ),
                    "Technology Platform"
                )
                FormComponents.PersonsInput(
                    assay.Performers,
                    (fun persons ->
                        assay.Performers <- persons
                        setArcAssay assay
                    ),
                    model.PersistentStorageState.IsARCitect,
                    "Performers"
                )
                FormComponents.CommentsInput(
                    assay.Comments,
                    (fun comments ->
                        assay.Comments <- ResizeArray comments
                        setArcAssay assay
                    ),
                    "Comments"
                )
            ]
        )
    ]