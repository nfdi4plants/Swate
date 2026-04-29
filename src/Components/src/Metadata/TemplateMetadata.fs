namespace Swate.Components.Metadata

open Fable.Core
open Feliz
open ARCtrl
open Swate.Components.Metadata

[<Erase; Mangle(false)>]
type TemplateMetadata =

    [<ReactComponent(true)>]
    static member TemplateMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (template: ARCtrl.Template, setTemplate: ARCtrl.Template -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Template Metadata",
                content = [
                    FormComponents.TextInput(
                        template.Id.ToString(),
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    FormComponents.TextInput(
                        template.Name,
                        (fun value ->
                            template.Name <- value
                            setTemplate template
                        ),
                        label = "Name"
                    )
                    FormComponents.TextInput(
                        template.Description,
                        (fun value ->
                            template.Description <- value
                            setTemplate template
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.TextInput(
                        template.Organisation.ToString(),
                        (fun value ->
                            template.Organisation <- Organisation.ofString value
                            setTemplate template
                        ),
                        label = "Organisation"
                    )
                    FormComponents.TextInput(
                        template.Version,
                        (fun value ->
                            template.Version <- value
                            setTemplate template
                        ),
                        label = "Version"
                    )
                    FormComponents.DateTimeInput(
                        template.LastUpdated,
                        (fun value ->
                            template.LastUpdated <- value
                            setTemplate template
                        ),
                        label = "Last Updated"
                    )
                    FormComponents.OntologyAnnotationsInput(
                        template.Tags,
                        (fun annotations ->
                            template.Tags <- annotations
                            setTemplate template
                        ),
                        label = "Tags"
                    )
                    FormComponents.OntologyAnnotationsInput(
                        template.EndpointRepositories,
                        (fun annotations ->
                            template.EndpointRepositories <- annotations
                            setTemplate template
                        ),
                        label = "Endpoint Repositories"
                    )
                    FormComponents.PersonsInput(
                        template.Authors,
                        (fun persons ->
                            template.Authors <- persons
                            setTemplate template
                        ),
                        label = "Authors"
                    )
                ]
            )
        ]