namespace Swate.Components.Page.ProvenanceGrouping

/// Formatting helpers for provenance values rendered in labels, chips, and sort keys.
module Formatting =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Grouping

    let formatValue value unit' = valueText value unit'

/// Editor-wide density, shared through context so nested cards and chips can
/// tighten their spacing without prop drilling.
module Density =

    open Fable.Core
    open Fable.Core.JsInterop
    open Feliz

    [<RequireQualifiedAccess>]
    type EditorDensity =
        | Comfortable
        | Compact

    let context = React.createContext (defaultValue = EditorDensity.Comfortable)

    [<ImportMember("react")>]
    let private createElement (comp: obj) (props: obj) (children: ReactElement) : ReactElement = jsNative

    /// Feliz 3 ships no contextProvider helper, so render the provider directly.
    let provider (value: EditorDensity) (children: ReactElement) : ReactElement =
        createElement !!context?Provider {| value = value |} children

/// CSS class builders shared by ProvenanceGrouping draggable cards, buttons, chips, and overlay previews.
module Styles =

    let dragIndicatorClasses isDragging = [
        "swt:transition swt:duration-150"
        if isDragging then
            "swt:ring-2 swt:ring-primary swt:border-primary swt:bg-primary/10 swt:shadow-md swt:opacity-80"
    ]

    let draggableButtonClasses isDragging = [
        "swt:cursor-grab swt:active:cursor-grabbing"
        yield! dragIndicatorClasses isDragging
    ]

    let draggableBoxClasses isDragging = [
        "swt:rounded-md swt:border swt:border-base-300 swt:bg-base-100 swt:shadow-sm"
        yield! draggableButtonClasses isDragging
    ]

    /// Value chips hug their content up to the property-header cap; the cap yields to
    /// the panel width when the rail gets narrow.
    let propertyValueButtonClasses density isDragging = [
        "swt:btn swt:btn-sm swt:btn-primary swt:w-fit swt:max-w-[min(18rem,100%)] swt:h-auto swt:justify-start swt:normal-case swt:font-medium swt:@max-xs/provenancePanel:px-2 swt:@max-xs/provenancePanel:text-[0.7rem]"
        match density with
        | Density.EditorDensity.Compact -> "swt:min-h-6 swt:px-2 swt:py-1 swt:text-[0.7rem]"
        | _ -> "swt:min-h-8 swt:px-3 swt:py-1.5 swt:text-xs"
        yield! draggableButtonClasses isDragging
    ]

    let propertyValueOverlayClasses = [
        "swt:btn swt:btn-sm swt:btn-primary swt:w-fit swt:max-w-[18rem] swt:min-h-8 swt:h-auto swt:justify-start swt:normal-case swt:px-3 swt:py-1.5 swt:text-xs swt:font-medium swt:pointer-events-none swt:shadow-lg swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100"
    ]

    let addPropertyValueButtonClasses = [
        "swt:btn swt:btn-sm swt:btn-outline swt:btn-primary swt:w-fit swt:max-w-full swt:min-h-8 swt:h-auto swt:justify-start swt:normal-case swt:px-3 swt:py-1.5 swt:text-xs swt:font-medium swt:@max-xs/provenancePanel:px-2 swt:@max-xs/provenancePanel:text-[0.7rem]"
    ]
