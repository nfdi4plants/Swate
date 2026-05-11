namespace Swate.Components.Metadata

open System
open Fable.Core
open Feliz
open ARCtrl
open Swate.Components

[<Erase; Mangle(false)>]
type RunMetadata =

    [<ReactComponent(true)>]
    static member RunMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (run: ArcRun, setRun: ArcRun -> unit) =
        LayoutComponents.Section [
            LayoutComponents.BoxedField(
                "Run Metadata",
                content = [
                    FormComponents.TextInput(run.Identifier, (fun _ -> ()), label = "Identifier", disabled = true)
                    FormComponents.TextInput(
                        defaultArg run.Title "",
                        (fun value ->
                            run.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setRun run
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg run.Description "",
                        (fun value ->
                            run.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setRun run
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.OntologyAnnotationInput(
                        run.MeasurementType,
                        (fun annotation ->
                            run.MeasurementType <- annotation
                            setRun run
                        ),
                        label = "Measurement Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        run.TechnologyType,
                        (fun annotation ->
                            run.TechnologyType <- annotation
                            setRun run
                        ),
                        label = "Technology Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        run.TechnologyPlatform,
                        (fun annotation ->
                            run.TechnologyPlatform <- annotation
                            setRun run
                        ),
                        label = "Technology Platform"
                    )
                    FormComponents.CollectionOfStrings(
                        run.WorkflowIdentifiers,
                        (fun identifiers ->
                            run.WorkflowIdentifiers <- identifiers
                            setRun run
                        ),
                        label = "Workflow Identifiers"
                    )
                    FormComponents.PersonsInput(
                        run.Performers,
                        (fun persons ->
                            run.Performers <- persons
                            setRun run
                        ),
                        label = "Performers"
                    )
                    FormComponents.CommentsInput(
                        run.Comments,
                        (fun comments ->
                            run.Comments <- comments
                            setRun run
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]