module Components.Metadata.Run

open Swate.Components.Shared
open Feliz
open Feliz.DaisyUI
open ARCtrl
open Swate.Components.Shared
open Components
open Components.Forms

[<ReactComponent>]
let Main
    (run: ArcRun, setArcRun: ArcRun -> unit, model: Model.Model)
    =
    Generic.Section [
        Generic.BoxedField(
            "Run Metadata",
            content = [
                FormComponents.TextInput(
                    run.Identifier,
                    //(fun v ->
                    //    let nextRun = IdentifierSetters.setAssayIdentifier v run
                    //    setArcRun nextRun),
                    (fun _ -> ()), //Have to implement setRunIdentifier in ARCtrl
                    "Identifier",
                    validator = {|
                        fn = (fun s -> ARCtrl.Helper.Identifier.tryCheckValidCharacters s)
                        msg = "Invalid Identifier"
                    |},
                    disabled = Generic.isDisabledInARCitect model.PersistentStorageState.Host,
                    classes = "swt:w-full"
                )
                FormComponents.OntologyAnnotationInput(
                    run.MeasurementType,
                    (fun oa ->
                        run.MeasurementType <- oa
                        setArcRun <| run),
                    "Measurement Type"
                )
                FormComponents.OntologyAnnotationInput(
                    run.TechnologyType,
                    (fun oa ->
                        run.TechnologyType <- oa
                        setArcRun <| run),
                    "Technology Type"
                )
                FormComponents.OntologyAnnotationInput(
                    run.TechnologyPlatform,
                    (fun oa ->
                        run.TechnologyPlatform <- oa
                        setArcRun <| run),
                    "Technology Platform"
                )
                FormComponents.CollectionOfStrings(
                    run.WorkflowIdentifiers,
                    "Workflow Identifiers"
                )
                FormComponents.PersonsInput(
                    run.Performers,
                    (fun persons ->
                        run.Performers <- persons
                        setArcRun run),
                    model.PersistentStorageState.IsARCitect,
                    "Performers"
                )
                FormComponents.CommentsInput(
                    run.Comments,
                    (fun comments ->
                        run.Comments <- ResizeArray comments
                        setArcRun run),
                    "Comments"
                )
            ]
        )
    ]