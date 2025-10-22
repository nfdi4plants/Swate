module Components.Metadata.Workflow

open Swate.Components.Shared
open Feliz
open Feliz.DaisyUI
open ARCtrl
open Swate.Components.Shared
open Components
open Components.Forms

[<ReactComponent>]
let Main
    (workflow: ArcWorkflow, setArcWorkflow: ArcWorkflow -> unit, model: Model.Model)
    =
    let versionPattern = @"^v[a-zA-Z0-9._\- ]+$"

    // Function to check if a string contains only valid characters
    let tryCheckValidCharacters (identifier: string) =
        match identifier with
        | Regex versionPattern _ -> true
        | _ -> false

    Generic.Section [
        Generic.BoxedField(
            "Workflow Metadata",
            content = [
                FormComponents.TextInput(
                    workflow.Identifier,
                    (fun _ -> ()),
                    //(fun v ->
                    //    let nextWorkflow = IdentifierSetters.setAssayIdentifier v workflow
                    //    setArcWorkflow nextWorkflow),
                    "Identifier",
                    validator = {|
                        fn = (fun s -> ARCtrl.Helper.Identifier.tryCheckValidCharacters s)
                        msg = "Invalid Identifier"
                    |},
                    disabled = true,
                    classes = "swt:w-full"
                )
                FormComponents.TextInput(
                    defaultArg workflow.Title "",
                    (fun v -> workflow.Title <- Some v
                    ),
                    "Title",
                    classes = "swt:w-full"
                )
                FormComponents.TextInput(
                    defaultArg workflow.Version "v",
                    (fun _ -> ()),
                    "Version",
                    validator = {|
                        fn = (fun s -> tryCheckValidCharacters s)
                        msg = "Invalid Version"
                    |},
                    classes = "swt:w-full"
                )
                FormComponents.OntologyAnnotationInput(
                    workflow.WorkflowType,
                    (fun oa ->
                        workflow.WorkflowType <- oa
                        setArcWorkflow <| workflow),
                    "Workflow Type"
                )
                FormComponents.TextInput(
                    defaultArg workflow.Description "",
                    (fun _ -> ()),
                    "Description",
                    classes = "swt:w-full"
                )
                FormComponents.SubWorkflowIdentifiers(
                    workflow.SubWorkflowIdentifiers,
                    "SubWorkflow Identifiers"
                )
                if workflow.Investigation.IsSome then
                    FormComponents.TextInput(
                        (if workflow.URI.IsSome then workflow.URI.Value.ToString() else ""),
                        (fun _ -> ()),
                        "Description",
                        classes = "swt:w-full"
                    )
                    FormComponents.TextInput(
                        workflow.SubWorkflowCount.ToString(),
                        (fun _ -> ()),
                        "Description",
                        classes = "swt:w-full"
                    )
                    FormComponents.SubWorkflowIdentifiers(
                        workflow.VacantSubWorkflowIdentifiers,
                        "Vacant SubWorkflow Identifiers"
                    )
                FormComponents.ParametersInput(
                    workflow.Parameters,
                    (fun parameter ->
                        workflow.Parameters <- parameter
                        setArcWorkflow workflow),
                    "Parameters"
                )
                FormComponents.ComponentsInput(
                    workflow.Components,
                    (fun workflowComponent ->
                        workflow.Components <- workflowComponent
                        setArcWorkflow workflow),
                    "Components"
                )
                FormComponents.PersonsInput(
                    workflow.Contacts,
                    (fun persons ->
                        workflow.Contacts <- persons
                        setArcWorkflow workflow),
                    model.PersistentStorageState.IsARCitect,
                    "Contacts"
                )
                FormComponents.CommentsInput(
                    workflow.Comments,
                    (fun comments ->
                        workflow.Comments <- ResizeArray comments
                        setArcWorkflow workflow),
                    "Comments"
                )
            ]
        )
    ]