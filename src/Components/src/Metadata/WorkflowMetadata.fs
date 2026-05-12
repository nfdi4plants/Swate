namespace Swate.Components.Metadata

open System
open Fable.Core
open Feliz
open ARCtrl
open Swate.Components

[<Erase; Mangle(false)>]
type WorkflowMetadata =

    [<ReactComponent(true)>]
    static member WorkflowMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (workflow: ArcWorkflow, setWorkflow: ArcWorkflow -> unit) =
        LayoutComponents.Section [
            LayoutComponents.BoxedField(
                "Workflow Metadata",
                content = [
                    FormComponents.TextInput(workflow.Identifier, (fun _ -> ()), label = "Identifier", disabled = true)
                    FormComponents.TextInput(
                        defaultArg workflow.Title "",
                        (fun value ->
                            workflow.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg workflow.Description "",
                        (fun value ->
                            workflow.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.TextInput(
                        defaultArg workflow.Version "",
                        (fun value ->
                            workflow.Version <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "Version"
                    )
                    FormComponents.OntologyAnnotationInput(
                        workflow.WorkflowType,
                        (fun annotation ->
                            workflow.WorkflowType <- annotation
                            setWorkflow workflow
                        ),
                        label = "Workflow Type"
                    )
                    FormComponents.TextInput(
                        defaultArg workflow.URI "",
                        (fun value ->
                            workflow.URI <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "URI"
                    )
                    FormComponents.PersonsInput(
                        workflow.Contacts,
                        (fun persons ->
                            workflow.Contacts <- persons
                            setWorkflow workflow
                        ),
                        label = "Contacts"
                    )
                    FormComponents.CommentsInput(
                        workflow.Comments,
                        (fun comments ->
                            workflow.Comments <- comments
                            setWorkflow workflow
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]