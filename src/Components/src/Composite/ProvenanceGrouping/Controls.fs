namespace Swate.Components.Composite.ProvenanceGrouping

open System
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
open Swate.Components.Composite.TermSearch
open Swate.Components.Composite.TermSearch.Types
open Swate.Components.Composite.ProvenanceGrouping.Helper

type private DraftValueKind =
    | DraftText
    | DraftInteger
    | DraftFloat
    | DraftTerm

module private ControlsHelper =

    let sideName side =
        match side with
        | ProvenanceSide.Input -> "input"
        | ProvenanceSide.Output -> "output"

    let kindForValue value =
        match value with
        | ProvenanceValue.Text _ -> DraftText
        | ProvenanceValue.Integer _ -> DraftInteger
        | ProvenanceValue.Float _ -> DraftFloat
        | ProvenanceValue.Term _ -> DraftTerm

    let kindName kind =
        match kind with
        | DraftText -> "Text"
        | DraftInteger -> "Integer"
        | DraftFloat -> "Float"
        | DraftTerm -> "Term"

    let kindFromName value =
        match value with
        | "Integer" -> DraftInteger
        | "Float" -> DraftFloat
        | "Term" -> DraftTerm
        | _ -> DraftText

    let textForValue value =
        match value with
        | ProvenanceValue.Text text -> text
        | ProvenanceValue.Integer value -> string value
        | ProvenanceValue.Float value -> string value
        | ProvenanceValue.Term term -> term.Name

    let termForValue value =
        match value with
        | ProvenanceValue.Term term -> Some term
        | _ -> None

    let tryValue kind (text: string) term =
        match kind with
        | DraftText -> Some(ProvenanceValue.Text text)
        | DraftInteger ->
            match Int32.TryParse text with
            | true, value -> Some(ProvenanceValue.Integer value)
            | _ -> None
        | DraftFloat ->
            match Double.TryParse text with
            | true, value when not (Double.IsNaN value || Double.IsInfinity value) ->
                Some(ProvenanceValue.Float value)
            | _ -> None
        | DraftTerm -> term |> Option.map ProvenanceValue.Term

    let toTermSearchTerm (term: ProvenanceTerm) =
        Term(name = term.Name, ?id = term.TermAccession, ?source = term.TermSource)

    let fromTermSearchTerm (term: Term) =
        term.name
        |> Option.map (fun name ->
            {
                Name = name
                TermSource = term.source
                TermAccession = term.id
            })

    let endpointKindName kind =
        match kind with
        | ProvenanceIOKind.Source -> "Source"
        | ProvenanceIOKind.Sample -> "Sample"
        | ProvenanceIOKind.Data -> "Data"
        | ProvenanceIOKind.Material -> "Material"
        | ProvenanceIOKind.FreeText _ -> "FreeText"
        | ProvenanceIOKind.Unknown -> "Sample"

    let propertyKindName kind =
        match kind with
        | ProvenancePropertyKind.Characteristic -> "Characteristic"
        | ProvenancePropertyKind.Factor -> "Factor"
        | ProvenancePropertyKind.Parameter -> "Parameter"
        | ProvenancePropertyKind.Component -> "Component"

    let propertyKindFromName value =
        match value with
        | "Characteristic" -> ProvenancePropertyKind.Characteristic
        | "Factor" -> ProvenancePropertyKind.Factor
        | "Component" -> ProvenancePropertyKind.Component
        | _ -> ProvenancePropertyKind.Parameter

[<Erase; Mangle(false)>]
type Controls =

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
    static member private PropertySwapButton
        (
            side: ProvenanceSide,
            header: ProvenancePropertyHeader,
            onSwitch: ProvenancePropertyHeader -> unit,
            ?debug: bool
        ) =
        let sideName = ControlsHelper.sideName side

        Html.button [
            prop.type'.button
            prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:btn-square"
            prop.ariaLabel $"Move {header.Category.Name} grouping from {sideName}"
            if defaultArg debug false then prop.testId $"provenance-property-drag-{side}-{header.Category.Name}"
            prop.onClick (fun _ -> onSwitch header)
            prop.children [ Html.i [ prop.className "swt:iconify swt:fluent--arrow-swap-20-regular swt:size-4" ] ]
        ]

    [<ReactComponent>]
    static member private PropertyRailItem
        (
            side: ProvenanceSide,
            header: ProvenancePropertyHeader,
            active: GroupingAssignment list,
            canSwitch: bool,
            onToggleSide: ProvenancePropertyHeader -> unit,
            onToggleBoth: ProvenancePropertyHeader -> unit,
            onSwitch: ProvenancePropertyHeader -> unit,
            ?debug: bool,
            ?key: string
        ) =
        let draggable = DndKit.useDraggable ({| id = propertyDragId side header |})
        let sideScope =
            match side with
            | ProvenanceSide.Input -> GroupingScope.Input
            | ProvenanceSide.Output -> GroupingScope.Output
        let sideSelected =
            active |> List.exists (fun assignment -> assignment.Key.Header = header && assignment.Scope = sideScope)
        let bothSelected =
            active |> List.exists (fun assignment -> assignment.Key.Header = header && assignment.Scope = GroupingScope.Both)
        let sideName = ControlsHelper.sideName side

        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.className "swt:flex swt:items-center swt:gap-1"
            prop.children [
                Html.button [
                    prop.type'.button
                    if canSwitch then
                        prop.ref draggable.setNodeRef
                        yield! prop.spread (!!draggable.attributes)
                        yield! prop.spread (!!draggable.listeners)
                    prop.className [
                        "swt:btn swt:btn-sm swt:grow swt:justify-start"
                        if sideSelected then "swt:btn-primary" else "swt:btn-outline"
                        if canSwitch && draggable.isDragging then "swt:opacity-50"
                    ]
                    if defaultArg debug false then prop.testId $"provenance-property-{side}-{header.Category.Name}"
                    prop.onClick (fun _ -> onToggleSide header)
                    prop.text header.Category.Name
                ]
                Html.button [
                    prop.type'.button
                    prop.className [
                        "swt:btn swt:btn-sm swt:btn-square"
                        if bothSelected then "swt:btn-primary" else "swt:btn-outline"
                    ]
                    if defaultArg debug false then prop.testId $"provenance-property-both-{side}-{header.Category.Name}"
                    prop.ariaLabel $"Group {header.Category.Name} on both sides"
                    prop.onClick (fun _ -> onToggleBoth header)
                    prop.children [ Html.i [ prop.className "swt:iconify swt:fluent--link-multiple-20-regular swt:size-4" ] ]
                ]
                if canSwitch then
                    Controls.PropertySwapButton(side, header, onSwitch, ?debug = debug)
                else
                    Html.button [
                        prop.type'.button
                        prop.disabled true
                        prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:btn-square"
                        prop.ariaLabel $"Move {header.Category.Name} grouping from {sideName}"
                        if defaultArg debug false then prop.testId $"provenance-property-drag-{side}-{header.Category.Name}"
                        prop.children [ Html.i [ prop.className "swt:iconify swt:fluent--arrow-swap-20-regular swt:size-4" ] ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member PropertyRail
        (
            side: ProvenanceSide,
            headers: ProvenancePropertyHeader list,
            active: GroupingAssignment list,
            onToggleSide: ProvenancePropertyHeader -> unit,
            onToggleBoth: ProvenancePropertyHeader -> unit,
            onSwitch: ProvenancePropertyHeader -> unit,
            canSwitch: ProvenancePropertyHeader -> bool,
            ?debug: bool
        ) =
        let droppable = DndKit.useDroppable ({| id = propertyRailDropId side |})

        Html.aside [
            prop.ref droppable.setNodeRef
            prop.className [
                "swt:flex swt:flex-col swt:gap-2 swt:min-w-44"
                if droppable.isOver then "swt:ring-2 swt:ring-primary swt:rounded"
            ]
            if defaultArg debug false then prop.testId $"provenance-property-rail-{side}"
            prop.children [
                Html.h3 [ prop.className "swt:text-sm swt:font-semibold swt:text-primary"; prop.text "Properties" ]
                for header in headers do
                    Controls.PropertyRailItem(
                        side,
                        header,
                        active,
                        canSwitch header,
                        onToggleSide,
                        onToggleBoth,
                        onSwitch,
                        debug = defaultArg debug false,
                        key = propertyHeaderIdentity header)
            ]
        ]

    [<ReactComponent>]
    static member EditValuePopover
        (
            propertyValue: ProvenancePropertyValue,
            onApply: ProvenanceValue -> ProvenanceTerm option -> unit,
            ?debug: bool
        ) =
        let category = propertyValue.Header.Category.Name
        let kind = ControlsHelper.kindForValue propertyValue.Value
        let value, setValue = React.useState (ControlsHelper.textForValue propertyValue.Value)
        let term, setTerm = React.useState (ControlsHelper.termForValue propertyValue.Value)
        let nextValue = ControlsHelper.tryValue kind value term
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
                        nextValue |> Option.iter (fun updated -> onApply updated propertyValue.Unit))
                    prop.children [
                        Html.label [ prop.htmlFor inputId; prop.className "swt:label"; prop.text $"{category} value" ]
                        match kind with
                        | DraftTerm ->
                            TermSearch.TermSearch(
                                term |> Option.map ControlsHelper.toTermSearchTerm,
                                (fun next -> setTerm (next |> Option.bind ControlsHelper.fromTermSearchTerm)))
                        | _ ->
                            Html.input [
                                prop.id inputId
                                prop.ariaLabel $"{category} value"
                                prop.className "swt:input swt:input-bordered swt:input-sm"
                                prop.value value
                                prop.onChange setValue
                            ]
                        if nextValue.IsNone then
                            Html.p [ prop.className "swt:text-xs swt:text-error"; prop.text "Enter a valid value." ]
                        Html.button [
                            prop.type'.submit
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.disabled nextValue.IsNone
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
            header: ProvenancePropertyHeader option,
            onCreate: CreateLoadedPropertyValueCommand -> unit,
            ?debug: bool
        ) =
        let propertyKind, setPropertyKind =
            React.useState (header |> Option.map (fun known -> known.Kind) |> Option.defaultValue ProvenancePropertyKind.Parameter)
        let categoryTerm, setCategoryTerm = React.useState (header |> Option.map (fun known -> known.Category))
        let kind, setKind = React.useState DraftText
        let value, setValue = React.useState ""
        let term, setTerm = React.useState (None: ProvenanceTerm option)
        let unit', setUnit = React.useState (None: ProvenanceTerm option)
        let nextHeader =
            match header, categoryTerm with
            | Some known, _ -> Some known
            | None, Some category when not (String.IsNullOrWhiteSpace category.Name) ->
                Some { Kind = propertyKind; Category = category }
            | _ -> None
        let category =
            nextHeader
            |> Option.map (fun next -> next.Category.Name)
            |> Option.defaultValue "Property"
        let nextValue = ControlsHelper.tryValue kind value term
        let canCreate = nextHeader.IsSome && nextValue.IsSome

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-outline swt:btn-xs"
                    prop.text (if header.IsSome then $"Add {category} value" else "Add new property")
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()
                        match nextHeader, nextValue with
                        | Some createdHeader, Some created ->
                            onCreate {
                                Target = target
                                CopiedFrom = None
                                Header = createdHeader
                                Value = created
                                Unit = unit'
                            }
                        | _ -> ())
                    prop.children [
                        if header.IsNone then
                            Html.label [ prop.className "swt:label"; prop.text "Property kind" ]
                            Html.select [
                                prop.ariaLabel "Property kind"
                                prop.className "swt:select swt:select-bordered swt:select-sm"
                                prop.value (ControlsHelper.propertyKindName propertyKind)
                                prop.onChange (ControlsHelper.propertyKindFromName >> setPropertyKind)
                                prop.children [
                                    Html.option [ prop.value "Characteristic"; prop.text "Characteristic" ]
                                    Html.option [ prop.value "Factor"; prop.text "Factor" ]
                                    Html.option [ prop.value "Parameter"; prop.text "Parameter" ]
                                    Html.option [ prop.value "Component"; prop.text "Component" ]
                                ]
                            ]
                            Html.label [ prop.className "swt:label"; prop.text "Property category" ]
                            TermSearch.TermSearch(
                                categoryTerm |> Option.map ControlsHelper.toTermSearchTerm,
                                (fun next -> setCategoryTerm (next |> Option.bind ControlsHelper.fromTermSearchTerm)))
                        Html.label [ prop.className "swt:label"; prop.text "Value type" ]
                        Html.select [
                            prop.ariaLabel "Value type"
                            prop.className "swt:select swt:select-bordered swt:select-sm"
                            prop.value (ControlsHelper.kindName kind)
                            prop.onChange (ControlsHelper.kindFromName >> setKind)
                            prop.children [
                                Html.option [ prop.value "Text"; prop.text "Text" ]
                                Html.option [ prop.value "Integer"; prop.text "Integer" ]
                                Html.option [ prop.value "Float"; prop.text "Float" ]
                                Html.option [ prop.value "Term"; prop.text "Term" ]
                            ]
                        ]
                        Html.label [ prop.className "swt:label"; prop.text $"{category} value" ]
                        match kind with
                        | DraftTerm ->
                            TermSearch.TermSearch(
                                term |> Option.map ControlsHelper.toTermSearchTerm,
                                (fun next -> setTerm (next |> Option.bind ControlsHelper.fromTermSearchTerm)))
                        | _ ->
                            Html.input [ prop.ariaLabel $"{category} value"; prop.className "swt:input swt:input-bordered swt:input-sm"; prop.value value; prop.onChange setValue ]
                        Html.label [ prop.className "swt:label"; prop.text "Unit" ]
                        TermSearch.TermSearch(
                            unit' |> Option.map ControlsHelper.toTermSearchTerm,
                            (fun next -> setUnit (next |> Option.bind ControlsHelper.fromTermSearchTerm)))
                        if not canCreate then
                            Html.p [ prop.className "swt:text-xs swt:text-error"; prop.text "Enter a valid value." ]
                        Html.button [ prop.type'.submit; prop.disabled (not canCreate); prop.className "swt:btn swt:btn-primary swt:btn-sm"; prop.text "Add value" ]
                    ]
                ],
            ?debug = (if defaultArg debug false then Some $"provenance-add-value-{category}" else None)
        )

    [<ReactComponent>]
    static member ValueChip
        (
            propertyValue: ProvenancePropertyValue,
            onApply: ProvenanceValue -> ProvenanceTerm option -> unit,
            ?debug: bool,
            ?key: string
        ) =
        let draggable = DndKit.useDraggable ({| id = valueDragId propertyValue.Id |})

        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.ref draggable.setNodeRef
            prop.className [
                "swt:flex swt:items-center swt:gap-1 swt:rounded swt:bg-base-200 swt:px-2 swt:py-1 swt:text-xs"
                if draggable.isDragging then "swt:opacity-50"
            ]
            if defaultArg debug false then prop.testId $"provenance-value-{propertyValue.Id}"
            let label = $"{propertyValue.Header.Category.Name}: {formatValue propertyValue.Value propertyValue.Unit}"
            prop.children [
                Html.span [ prop.text label ]
                Html.button [
                    prop.type'.button
                    yield! prop.spread (!!draggable.attributes)
                    yield! prop.spread (!!draggable.listeners)
                    prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:btn-square"
                    prop.ariaLabel $"Drag {propertyValue.Header.Category.Name} value"
                    if defaultArg debug false then prop.testId $"provenance-drag-value-{propertyValue.Id}"
                    prop.children [ Html.i [ prop.className "swt:iconify swt:fluent--re-order-dots-vertical-20-regular swt:size-4" ] ]
                ]
                Controls.EditValuePopover(propertyValue, onApply, ?debug = debug)
            ]
        ]

    [<ReactComponent>]
    static member AddEndpointPopover
        (
            side: ProvenanceSide,
            defaultKind: ProvenanceIOKind,
            onCreate: CreateLoadedSetCommand -> unit,
            ?debug: bool,
            ?key: string
        ) =
        let sideName = ControlsHelper.sideName side
        let name, setName = React.useState ""
        let kind, setKind = React.useState defaultKind
        let initialFreeText =
            match defaultKind with
            | ProvenanceIOKind.FreeText text -> text
            | _ -> ""
        let freeText, setFreeText = React.useState initialFreeText

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
                        onCreate { Side = side; Header = endpointHeader side kind; Name = name })
                    prop.children [
                        Html.label [ prop.className "swt:label"; prop.text "Endpoint name" ]
                        Html.input [
                            prop.ariaLabel "Endpoint name"
                            prop.className "swt:input swt:input-bordered swt:input-sm"
                            prop.value name
                            prop.onChange setName
                        ]
                        Html.label [ prop.className "swt:label"; prop.text "Kind" ]
                        Html.select [
                            prop.ariaLabel "Kind"
                            prop.className "swt:select swt:select-bordered swt:select-sm"
                            prop.value (ControlsHelper.endpointKindName kind)
                            prop.onChange (fun (value: string) ->
                                match value with
                                | "Source" -> setKind ProvenanceIOKind.Source
                                | "Sample" -> setKind ProvenanceIOKind.Sample
                                | "Data" -> setKind ProvenanceIOKind.Data
                                | "Material" -> setKind ProvenanceIOKind.Material
                                | "FreeText" -> setKind (ProvenanceIOKind.FreeText freeText)
                                | _ -> setKind ProvenanceIOKind.Sample)
                            prop.children [
                                Html.option [ prop.value "Source"; prop.text "Source" ]
                                Html.option [ prop.value "Sample"; prop.text "Sample" ]
                                Html.option [ prop.value "Data"; prop.text "Data" ]
                                Html.option [ prop.value "Material"; prop.text "Material" ]
                                Html.option [ prop.value "FreeText"; prop.text "Custom header" ]
                            ]
                        ]
                        match kind with
                        | ProvenanceIOKind.FreeText _ ->
                            Html.label [ prop.className "swt:label"; prop.text "Endpoint header" ]
                            Html.input [
                                prop.ariaLabel "Endpoint header"
                                prop.className "swt:input swt:input-bordered swt:input-sm"
                                prop.value freeText
                                prop.onChange (fun text ->
                                    setFreeText text
                                    setKind (ProvenanceIOKind.FreeText text))
                            ]
                        | _ -> Html.none
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
