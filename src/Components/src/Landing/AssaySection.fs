namespace Swate.Components.Landing

open Feliz
open Swate.Components.Metadata

[<RequireQualifiedAccess>]
module AssaySection =

    [<ReactComponent>]
    let Main(draft: LandingDraft, setDraft: LandingDraft -> unit) =
        Html.div [
            prop.className "swt:space-y-3"
            prop.children [
                FormComponents.OntologyAnnotationInput(
                    draft.MeasurementType,
                    (fun value -> setDraft { draft with MeasurementType = value }),
                    label = "Measurement Type"
                )
                FormComponents.OntologyAnnotationInput(
                    draft.TechnologyType,
                    (fun value -> setDraft { draft with TechnologyType = value }),
                    label = "Technology Type"
                )
                FormComponents.OntologyAnnotationInput(
                    draft.TechnologyPlatform,
                    (fun value -> setDraft { draft with TechnologyPlatform = value }),
                    label = "Technology Platform"
                )
            ]
        ]
