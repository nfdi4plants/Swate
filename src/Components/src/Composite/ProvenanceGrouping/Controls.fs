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
            | true, value -> Some(ProvenanceValue.Float value)
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
            header: ProvenancePropertyHeader,
            onCreate: CreateLoadedPropertyValueCommand -> unit,
            ?debug: bool
        ) =
        let category = header.Category.Name
        let kind, setKind = React.useState DraftText
        let value, setValue = React.useState ""
        let term, setTerm = React.useState (None: ProvenanceTerm option)
        let unit', setUnit = React.useState (None: ProvenanceTerm option)
        let nextValue = ControlsHelper.tryValue kind value term

        Popover.Simple(
            trigger = Html.button [ prop.type'.button; prop.className "swt:btn swt:btn-outline swt:btn-xs"; prop.text $"Add {category} value" ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()
                        nextValue
                        |> Option.iter (fun created ->
                            onCreate {
                                Target = target
                                CopiedFrom = None
                                Header = header
                                Value = created
                                Unit = unit'
                            }))
                    prop.children [
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
                        if nextValue.IsNone then
                            Html.p [ prop.className "swt:text-xs swt:text-error"; prop.text "Enter a valid value." ]
                        Html.button [ prop.type'.submit; prop.disabled nextValue.IsNone; prop.className "swt:btn swt:btn-primary swt:btn-sm"; prop.text "Add value" ]
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
            ?debug: bool
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
