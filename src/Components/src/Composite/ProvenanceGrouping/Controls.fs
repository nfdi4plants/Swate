namespace Swate.Components.Composite.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Primitive.Buttons
open Swate.Components.Primitive.Popover
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Helper

[<Erase; Mangle(false)>]
type Controls =

    static member private SideName side =
        match side with
        | ProvenanceSide.Input -> "input"
        | ProvenanceSide.Output -> "output"

    [<ReactComponent>]
    static member LayerTabs(session: ProvenanceSession, onSelect: ProvenancePairId -> unit, onAddLayer: unit -> unit, ?debug: bool) =
        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
            prop.children [
                for pairId in session.PairOrder do
                    let pair = session.Pairs.[pairId]
                    let left = session.Layers |> List.find (fun layer -> layer.Id = pair.LeftLayerId)
                    let right = session.Layers |> List.find (fun layer -> layer.Id = pair.RightLayerId)
                    Html.button [
                        prop.className [
                            "swt:btn swt:btn-sm"
                            if pairId = session.ActivePairId then "swt:btn-primary" else "swt:btn-outline"
                        ]
                        if defaultArg debug false then prop.testId $"provenance-pair-{pairId}"
                        prop.onClick (fun _ -> onSelect pairId)
                        prop.text $"{left.Label} -> {right.Label}"
                    ]
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                    if defaultArg debug false then prop.testId "provenance-add-layer"
                    prop.onClick (fun _ -> onAddLayer ())
                    prop.children [
                        Html.i [ prop.className "swt:iconify swt:fluent--add-square-multiple-20-regular swt:size-4" ]
                        Html.span "Layer"
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member PropertyRail
        (
            side: ProvenanceSide,
            headers: ProvenancePropertyHeader list,
            active: GroupingKey list,
            onToggle: ProvenancePropertyHeader -> unit,
            ?debug: bool
        ) =
        Html.aside [
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-44"
            prop.children [
                Html.h3 [ prop.className "swt:text-sm swt:font-semibold swt:text-primary"; prop.text "Properties" ]
                for header in headers do
                    let selected = active |> List.exists (fun key -> key.Header = header)
                    Html.button [
                        prop.className [
                            "swt:btn swt:btn-sm swt:justify-start"
                            if selected then "swt:btn-primary" else "swt:btn-outline"
                        ]
                        if defaultArg debug false then prop.testId $"provenance-property-{side}-{header.Category.Name}"
                        prop.onClick (fun _ -> onToggle header)
                        prop.text header.Category.Name
                    ]
            ]
        ]

    [<ReactComponent>]
    static member EditValuePopover
        (
            header: ProvenancePropertyHeader,
            currentValue: ProvenanceValue,
            onApply: ProvenanceValue -> ProvenanceTerm option -> unit,
            ?debug: bool
        ) =
        let category = header.Category.Name
        let initialValue =
            match currentValue with
            | ProvenanceValue.Text text -> text
            | ProvenanceValue.Integer value -> string value
            | ProvenanceValue.Float value -> string value
            | ProvenanceValue.Term term -> term.Name
        let value, setValue = React.useState initialValue
        let inputId = $"provenance-edit-{category}"

        Popover.Simple(
            ?debug = (if defaultArg debug false then Some $"provenance-edit-{category}" else None),
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                    if defaultArg debug false then prop.testId $"provenance-edit-trigger-{category}"
                    prop.ariaLabel $"Edit {category}"
                    prop.text $"Edit {category}"
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()
                        onApply (ProvenanceValue.Text value) None)
                    prop.children [
                        Html.label [ prop.htmlFor inputId; prop.className "swt:label"; prop.text $"{category} value" ]
                        Html.input [
                            prop.id inputId
                            prop.ariaLabel $"{category} value"
                            prop.className "swt:input swt:input-bordered swt:input-sm"
                            prop.value value
                            prop.onChange setValue
                        ]
                        Html.button [
                            prop.type'.submit
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            if defaultArg debug false then prop.testId "provenance-apply-value"
                            prop.text "Apply value"
                        ]
                    ]
                ]
        )

    [<ReactComponent>]
    static member AddValuePopover
        (
            target: ProvenancePropertyTarget,
            header: ProvenancePropertyHeader,
            onCreate: CreateLoadedPropertyValueCommand -> unit,
            ?debug: bool
        ) =
        let category = header.Category.Name
        let value, setValue = React.useState ""

        Popover.Simple(
            trigger = Html.button [ prop.type'.button; prop.className "swt:btn swt:btn-outline swt:btn-xs"; prop.text $"Add {category} value" ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()
                        onCreate {
                            Target = target
                            CopiedFrom = None
                            Header = header
                            Value = ProvenanceValue.Text value
                            Unit = None
                        })
                    prop.children [
                        Html.label [ prop.className "swt:label"; prop.text $"{category} value" ]
                        Html.input [ prop.ariaLabel $"{category} value"; prop.className "swt:input swt:input-bordered swt:input-sm"; prop.value value; prop.onChange setValue ]
                        Html.button [ prop.type'.submit; prop.className "swt:btn swt:btn-primary swt:btn-sm"; prop.text "Add value" ]
                    ]
                ],
            ?debug = (if defaultArg debug false then Some $"provenance-add-value-{category}" else None)
        )

    [<ReactComponent>]
    static member ValueChip
        (
            propertyValue: ProvenancePropertyValue,
            onApply: ProvenanceValue -> ProvenanceTerm option -> unit,
            ?debug: bool
        ) =
        let draggable = DndKit.useDraggable ({| id = valueDragId propertyValue.Id |})

        Html.div [
            prop.ref draggable.setNodeRef
            yield! prop.spread (!!draggable.attributes)
            yield! prop.spread (!!draggable.listeners)
            prop.className [
                "swt:flex swt:items-center swt:gap-1 swt:rounded swt:bg-base-200 swt:px-2 swt:py-1 swt:text-xs"
                if draggable.isDragging then "swt:opacity-50"
            ]
            if defaultArg debug false then prop.testId $"provenance-value-{propertyValue.Id}"
            let label = $"{propertyValue.Header.Category.Name}: {formatValue propertyValue.Value propertyValue.Unit}"
            prop.children [
                Html.span [ prop.text label ]
                Controls.EditValuePopover(propertyValue.Header, propertyValue.Value, onApply, ?debug = debug)
            ]
        ]

    [<ReactComponent>]
    static member AddEndpointPopover
        (
            side: ProvenanceSide,
            header: ProvenanceIOHeader,
            onCreate: CreateLoadedSetCommand -> unit,
            ?debug: bool
        ) =
        let sideName = Controls.SideName side
        let name, setName = React.useState ""

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-outline swt:btn-sm"
                    if defaultArg debug false then prop.testId $"provenance-add-{sideName}-trigger"
                    prop.ariaLabel $"Add {sideName}"
                    prop.text $"Add {sideName}"
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()
                        onCreate { Side = side; Header = header; Name = name })
                    prop.children [
                        Html.label [ prop.className "swt:label"; prop.text "Endpoint name" ]
                        Html.input [
                            prop.ariaLabel "Endpoint name"
                            prop.className "swt:input swt:input-bordered swt:input-sm"
                            prop.value name
                            prop.onChange setName
                        ]
                        Html.button [
                            prop.type'.submit
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            if defaultArg debug false then prop.testId $"provenance-create-{sideName}"
                            prop.text "Create endpoint"
                        ]
                    ]
                ],
            ?debug = (if defaultArg debug false then Some $"provenance-add-{sideName}" else None)
        )
