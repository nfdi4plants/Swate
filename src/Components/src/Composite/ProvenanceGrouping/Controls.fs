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

type private DraftValueKind =
    | DraftText
    | DraftInteger
    | DraftFloat
    | DraftTerm

/// Converts provenance sides into lower-case text used in labels and test ids.
module private SideLabels =

    let sideName side =
        match side with
        | ProvenanceSide.Input -> "input"
        | ProvenanceSide.Output -> "output"

/// Converts between draft form state and typed provenance values.
module private ValueDrafts =

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

/// Maps provenance terms to the TermSearch component shape and back.
module private TermSearchMapping =

    let toTermSearchTerm (term: ProvenanceTerm) =
        Term(name = term.Name, ?id = term.TermAccession, ?source = term.TermSource)

    let fromTermSearchTerm (term: Term) =
        term.name
        |> Option.map (fun name -> {
            Name = name
            TermSource = term.source
            TermAccession = term.id
        })

/// Creates editor-owned generic provenance kinds for user-created values.
module private KindNames =

    let editorProperty =
        ProvenanceKind.create "editor:property" "Property"

    let endpointFromLabel (label: string) =
        let trimmed = label.Trim()

        if System.String.IsNullOrWhiteSpace trimmed then
            ProvenanceKind.create "editor:endpoint" "Endpoint"
        else
            ProvenanceKind.create $"editor:endpoint:{System.Uri.EscapeDataString trimmed}" trimmed

[<Erase; Mangle(false)>]
type Controls =

    [<ReactComponent>]
    static member LayerTabs
        (session: ProvenanceSession, onSelect: ProvenancePairId -> unit, onAddLayer: unit -> unit, ?debug: bool)
        =
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
                            if pairId = session.ActivePairId then
                                "swt:btn-primary"
                            else
                                "swt:btn-outline"
                        ]
                        if defaultArg debug false then
                            prop.testId $"provenance-pair-{pairId}"
                        prop.onClick (fun _ -> onSelect pairId)
                        prop.text $"{left.Label} -> {right.Label}"
                    ]
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                    if defaultArg debug false then
                        prop.testId "provenance-add-layer"
                    prop.onClick (fun _ -> onAddLayer ())
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--add-square-multiple-20-regular swt:size-4"
                        ]
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
        let sideName = SideLabels.sideName side

        Html.button [
            prop.type'.button
            prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:btn-square"
            prop.ariaLabel $"Move {header.Category.Name} grouping from {sideName}"
            if defaultArg debug false then
                prop.testId $"provenance-property-drag-{side}-{header.Category.Name}"
            prop.onPointerUp (fun _ -> onSwitch header)
            prop.onClick (fun _ -> onSwitch header)
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--arrow-swap-20-regular swt:size-4"
                ]
            ]
        ]

    [<ReactComponent>]
    static member private PropertyRailItem
        (
            side: ProvenanceSide,
            header: ProvenancePropertyHeader,
            propertyValues: ProvenancePropertyValue list,
            active: GroupingAssignment list,
            canSwitch: bool,
            expanded: bool,
            onToggleSide: ProvenancePropertyHeader -> unit,
            onToggleBoth: ProvenancePropertyHeader -> unit,
            onSwitch: ProvenancePropertyHeader -> unit,
            onToggleExpanded: ProvenancePropertyHeader -> unit,
            onAddValue: ProvenancePropertyHeader -> ProvenanceValue -> ProvenanceTerm option -> unit,
            ?debug: bool,
            ?key: string
        ) =
        let draggable =
            DndKit.useDraggable (
                {|
                    id = DragDrop.propertyDragId side header
                |}
            )

        let sideScope =
            match side with
            | ProvenanceSide.Input -> GroupingScope.Input
            | ProvenanceSide.Output -> GroupingScope.Output

        let sideSelected =
            active
            |> List.exists (fun assignment -> assignment.Key.Header = header && assignment.Scope = sideScope)

        let bothSelected =
            active
            |> List.exists (fun assignment -> assignment.Key.Header = header && assignment.Scope = GroupingScope.Both)

        let sideName = SideLabels.sideName side

        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.className "swt:flex swt:flex-col swt:gap-1"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-1"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            if canSwitch then
                                prop.ref draggable.setNodeRef
                                yield! prop.spread (!!draggable.attributes)
                                yield! prop.spread (!!draggable.listeners)
                            prop.className [
                                "swt:btn swt:btn-sm swt:grow swt:min-w-0 swt:justify-start"
                                if sideSelected then
                                    "swt:btn-primary"
                                else
                                    "swt:btn-outline"
                                if canSwitch then
                                    yield! Styles.draggableButtonClasses draggable.isDragging
                            ]
                            if defaultArg debug false then
                                prop.testId $"provenance-property-{side}-{header.Category.Name}"
                            prop.onClick (fun _ -> onToggleSide header)
                            prop.children [
                                Html.span [
                                    prop.className "swt:grow swt:min-w-0 swt:truncate swt:text-left"
                                    prop.text header.Category.Name
                                ]
                            ]
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:btn-square"
                            if defaultArg debug false then
                                prop.testId $"provenance-property-expand-{side}-{header.Category.Name}"
                            prop.ariaLabel (
                                if expanded then
                                    $"Collapse {header.Category.Name} values"
                                else
                                    $"Expand {header.Category.Name} values"
                            )
                            prop.onClick (fun _ -> onToggleExpanded header)
                            prop.children [
                                Html.i [
                                    prop.className [
                                        "swt:iconify swt:size-4"
                                        if expanded then
                                            "swt:fluent--chevron-up-20-regular"
                                        else
                                            "swt:fluent--chevron-down-20-regular"
                                    ]
                                ]
                            ]
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:btn-sm swt:btn-square"
                                if bothSelected then
                                    "swt:btn-primary"
                                else
                                    "swt:btn-outline"
                            ]
                            if defaultArg debug false then
                                prop.testId $"provenance-property-both-{side}-{header.Category.Name}"
                            prop.ariaLabel $"Group {header.Category.Name} on both sides"
                            prop.onClick (fun _ -> onToggleBoth header)
                            prop.children [
                                Html.i [
                                    prop.className "swt:iconify swt:fluent--link-multiple-20-regular swt:size-4"
                                ]
                            ]
                        ]
                        if canSwitch then
                            Controls.PropertySwapButton(side, header, onSwitch, ?debug = debug)
                        else
                            Html.button [
                                prop.type'.button
                                prop.disabled true
                                prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:btn-square"
                                prop.ariaLabel $"Move {header.Category.Name} grouping from {sideName}"
                                if defaultArg debug false then
                                    prop.testId $"provenance-property-drag-{side}-{header.Category.Name}"
                                prop.children [
                                    Html.i [
                                        prop.className "swt:iconify swt:fluent--arrow-swap-20-regular swt:size-4"
                                    ]
                                ]
                            ]
                    ]
                ]
                if expanded then
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1 swt:pl-2"
                        if defaultArg debug false then
                            prop.testId $"provenance-property-values-{side}-{header.Category.Name}"
                        prop.children [
                            for propertyValue in propertyValues do
                                Controls.ValueChip(
                                    propertyValue,
                                    draggable = true,
                                    showHeader = false,
                                    ?debug = debug,
                                    key = DragDrop.propertyValueIdentity propertyValue
                                )
                            Controls.AddValuePopover(
                                Some header,
                                (fun addedHeader value unit -> onAddValue addedHeader value unit),
                                label = "Add value",
                                ?debug = debug
                            )
                        ]
                    ]
                elif propertyValues.IsEmpty then
                    Html.none
                else
                    Html.none
            ]
        ]

    [<ReactComponent>]
    static member PropertyRail
        (
            side: ProvenanceSide,
            headers: ProvenancePropertyHeader list,
            active: GroupingAssignment list,
            valuesForHeader: ProvenancePropertyHeader -> ProvenancePropertyValue list,
            isExpanded: ProvenancePropertyHeader -> bool,
            onToggleSide: ProvenancePropertyHeader -> unit,
            onToggleBoth: ProvenancePropertyHeader -> unit,
            onSwitch: ProvenancePropertyHeader -> unit,
            onToggleExpanded: ProvenancePropertyHeader -> unit,
            onAddValue: ProvenancePropertyHeader -> ProvenanceValue -> ProvenanceTerm option -> unit,
            canSwitch: ProvenancePropertyHeader -> bool,
            ?debug: bool
        ) =
        let droppable =
            DndKit.useDroppable (
                {|
                    id = DragDrop.propertyRailDropId side
                |}
            )

        Html.aside [
            prop.ref droppable.setNodeRef
            prop.className [
                "swt:flex swt:flex-col swt:gap-2 swt:min-w-44"
                if droppable.isOver then
                    "swt:ring-2 swt:ring-primary swt:rounded"
            ]
            if defaultArg debug false then
                prop.testId $"provenance-property-rail-{side}"
            prop.children [
                Html.h3 [
                    prop.className "swt:text-sm swt:font-semibold swt:text-primary"
                    prop.text "Properties"
                ]
                for header in headers do
                    Controls.PropertyRailItem(
                        side,
                        header,
                        valuesForHeader header,
                        active,
                        canSwitch header,
                        isExpanded header,
                        onToggleSide,
                        onToggleBoth,
                        onSwitch,
                        onToggleExpanded,
                        onAddValue,
                        debug = defaultArg debug false,
                        key = DragDrop.propertyHeaderIdentity header
                    )
                Controls.AddValuePopover(None, onAddValue, label = "Add property", ?debug = debug)
            ]
        ]

    [<ReactComponent>]
    static member EditValuePopover
        (propertyValue: ProvenancePropertyValue, onApply: ProvenanceValue -> ProvenanceTerm option -> unit, ?debug: bool) =
        let category = propertyValue.Header.Category.Name
        let kind = ValueDrafts.kindForValue propertyValue.Value

        let value, setValue =
            React.useState (ValueDrafts.textForValue propertyValue.Value)

        let term, setTerm =
            React.useState (ValueDrafts.termForValue propertyValue.Value)

        let nextValue = ValueDrafts.tryValue kind value term
        let inputId = $"provenance-edit-{category}"

        Popover.Simple(
            ?debug =
                (if defaultArg debug false then
                     Some $"provenance-edit-{category}"
                 else
                     None),
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                    if defaultArg debug false then
                        prop.testId $"provenance-edit-trigger-{category}"
                    prop.ariaLabel $"Edit {category}"
                    prop.text $"Edit {category}"
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()
                        nextValue |> Option.iter (fun updated -> onApply updated propertyValue.Unit)
                    )
                    prop.children [
                        Html.label [
                            prop.htmlFor inputId
                            prop.className "swt:label"
                            prop.text $"{category} value"
                        ]
                        match kind with
                        | DraftTerm ->
                            TermSearch.TermSearch(
                                term |> Option.map TermSearchMapping.toTermSearchTerm,
                                (fun next ->
                                    setTerm (next |> Option.bind TermSearchMapping.fromTermSearchTerm)
                                )
                            )
                        | _ ->
                            Html.input [
                                prop.id inputId
                                prop.ariaLabel $"{category} value"
                                prop.className "swt:input swt:input-bordered swt:input-sm"
                                prop.value value
                                prop.onChange setValue
                            ]
                        if nextValue.IsNone then
                            Html.p [
                                prop.className "swt:text-xs swt:text-error"
                                prop.text "Enter a valid value."
                            ]
                        Html.button [
                            prop.type'.submit
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.disabled nextValue.IsNone
                            if defaultArg debug false then
                                prop.testId "provenance-apply-value"
                            prop.text "Apply value"
                        ]
                    ]
                ]
        )

    [<ReactComponent>]
    static member AddValuePopover
        (
            header: ProvenancePropertyHeader option,
            onCreate: ProvenancePropertyHeader -> ProvenanceValue -> ProvenanceTerm option -> unit,
            ?label: string,
            ?debug: bool
        ) : ReactElement =
        let propertyKind =
            header
            |> Option.map (fun known -> known.Kind)
            |> Option.defaultValue KindNames.editorProperty

        let categoryTerm, setCategoryTerm =
            React.useState (header |> Option.map (fun known -> known.Category))

        let kind, setKind = React.useState DraftText
        let value, setValue = React.useState ""
        let term, setTerm = React.useState (None: ProvenanceTerm option)
        let unit', setUnit = React.useState (None: ProvenanceTerm option)

        let nextHeader =
            match header, categoryTerm with
            | Some known, _ -> Some known
            | None, Some category when not (String.IsNullOrWhiteSpace category.Name) ->
                Some {
                    Kind = propertyKind
                    Category = category
                }
            | _ -> None

        let category =
            nextHeader
            |> Option.map (fun next -> next.Category.Name)
            |> Option.defaultValue "Property"

        let nextValue = ValueDrafts.tryValue kind value term
        let canCreate = nextHeader.IsSome && nextValue.IsSome

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className Styles.addPropertyValueButtonClasses
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--add-20-regular swt:size-5 swt:shrink-0"
                        ]
                        Html.span [
                            prop.text (
                                label
                                |> Option.defaultValue (if header.IsSome then "Add value" else "Add property")
                            )
                        ]
                    ]
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()

                        match nextHeader, nextValue with
                        | Some createdHeader, Some created -> onCreate createdHeader created unit'
                        | _ -> ()
                    )
                    prop.children [
                        if header.IsNone then
                            Html.label [
                                prop.className "swt:label"
                                prop.text "Property category"
                            ]

                            TermSearch.TermSearch(
                                categoryTerm |> Option.map TermSearchMapping.toTermSearchTerm,
                                (fun next ->
                                    setCategoryTerm (
                                        next |> Option.bind TermSearchMapping.fromTermSearchTerm
                                    )
                                )
                            )
                        Html.label [ prop.className "swt:label"; prop.text "Value type" ]
                        Html.select [
                            prop.ariaLabel "Value type"
                            prop.className "swt:select swt:select-bordered swt:select-sm"
                            prop.value (ValueDrafts.kindName kind)
                            prop.onChange (ValueDrafts.kindFromName >> setKind)
                            prop.children [
                                Html.option [ prop.value "Text"; prop.text "Text" ]
                                Html.option [ prop.value "Integer"; prop.text "Integer" ]
                                Html.option [ prop.value "Float"; prop.text "Float" ]
                                Html.option [ prop.value "Term"; prop.text "Term" ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label"
                            prop.text $"{category} value"
                        ]
                        match kind with
                        | DraftTerm ->
                            TermSearch.TermSearch(
                                term |> Option.map TermSearchMapping.toTermSearchTerm,
                                (fun next ->
                                    setTerm (next |> Option.bind TermSearchMapping.fromTermSearchTerm)
                                )
                            )
                        | _ ->
                            Html.input [
                                prop.ariaLabel $"{category} value"
                                prop.className "swt:input swt:input-bordered swt:input-sm"
                                prop.value value
                                prop.onChange setValue
                            ]
                        Html.label [ prop.className "swt:label"; prop.text "Unit" ]
                        TermSearch.TermSearch(
                            unit' |> Option.map TermSearchMapping.toTermSearchTerm,
                            (fun next ->
                                setUnit (next |> Option.bind TermSearchMapping.fromTermSearchTerm)
                            )
                        )
                        if not canCreate then
                            Html.p [
                                prop.className "swt:text-xs swt:text-error"
                                prop.text "Enter a valid value."
                            ]
                        Html.button [
                            prop.type'.submit
                            prop.disabled (not canCreate)
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.text (if header.IsSome then "Add value" else "Add property")
                        ]
                    ]
                ],
            ?debug =
                (if defaultArg debug false then
                     Some $"provenance-add-value-{category}"
                 else
                     None)
        )

    [<ReactComponent>]
    static member ValueLabel(propertyValue: ProvenancePropertyValue, ?debug: bool, ?key: string) : ReactElement =
        let label =
            $"{propertyValue.Header.Category.Name}: {Formatting.formatValue propertyValue.Value propertyValue.Unit}"

        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.className
                "swt:flex swt:items-center swt:gap-1 swt:rounded swt:bg-base-200 swt:px-2 swt:py-1 swt:text-xs"
            if defaultArg debug false then
                prop.testId $"provenance-value-{propertyValue.Id}"
            prop.children [ Html.span [ prop.text label ] ]
        ]

    [<ReactComponent>]
    static member ValueChip
        (propertyValue: ProvenancePropertyValue, ?draggable: bool, ?showHeader: bool, ?debug: bool, ?key: string)
        : ReactElement =
        let canDrag = defaultArg draggable true
        let showHeader = defaultArg showHeader true

        let drag =
            DndKit.useDraggable (
                {|
                    id = DragDrop.valueDragId propertyValue.Id
                |}
            )

        let text = Formatting.formatValue propertyValue.Value propertyValue.Unit

        let label =
            if showHeader then
                $"{propertyValue.Header.Category.Name}: {text}"
            else
                text

        Html.button [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.type'.button
            if canDrag then
                prop.ref drag.setNodeRef
                yield! prop.spread (!!drag.attributes)
                yield! prop.spread (!!drag.listeners)
            prop.className [
                yield! Styles.propertyValueButtonClasses drag.isDragging
                if not canDrag then
                    "swt:cursor-default"
            ]
            if defaultArg debug false then
                prop.testId $"provenance-value-{propertyValue.Id}"
            prop.ariaLabel $"Drag {propertyValue.Header.Category.Name} value"
            prop.children [
                Html.span [
                    prop.className "swt:min-w-0 swt:truncate"
                    prop.text label
                ]
            ]
        ]

    [<ReactComponent>]
    static member ValueDragPreview
        (propertyValue: ProvenancePropertyValue, ?showHeader: bool, ?debug: bool)
        : ReactElement =
        let showHeader = defaultArg showHeader true
        let text = Formatting.formatValue propertyValue.Value propertyValue.Unit

        let label =
            if showHeader then
                $"{propertyValue.Header.Category.Name}: {text}"
            else
                text

        Html.div [
            prop.className Styles.propertyValueOverlayClasses
            if defaultArg debug false then
                prop.testId "provenance-drag-overlay-value"
            prop.children [
                Html.span [
                    prop.className "swt:min-w-0 swt:truncate"
                    prop.text label
                ]
            ]
        ]

    [<ReactComponent>]
    static member AddEndpointPopover
        (
            side: ProvenanceSide,
            defaultKind: ProvenanceKind,
            onCreate: CreateLoadedSetCommand -> unit,
            ?debug: bool,
            ?key: string
        ) =
        let sideName = SideLabels.sideName side
        let name, setName = React.useState ""
        let endpointLabel, setEndpointLabel = React.useState (ProvenanceKind.displayName defaultKind)

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-outline swt:btn-sm"
                    if defaultArg debug false then
                        prop.testId $"provenance-add-{sideName}-trigger"
                    prop.ariaLabel $"Add {sideName}"
                    prop.text $"Add {sideName}"
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()

                        onCreate {
                            Side = side
                            Header =
                                endpointLabel
                                |> KindNames.endpointFromLabel
                                |> Endpoints.endpointHeader side
                            Name = name
                        }
                    )
                    prop.children [
                        Html.label [ prop.className "swt:label"; prop.text "Endpoint name" ]
                        Html.input [
                            prop.ariaLabel "Endpoint name"
                            prop.className "swt:input swt:input-bordered swt:input-sm"
                            prop.value name
                            prop.onChange setName
                        ]
                        Html.label [ prop.className "swt:label"; prop.text "Endpoint kind" ]
                        Html.input [
                            prop.ariaLabel "Endpoint kind"
                            prop.className "swt:input swt:input-bordered swt:input-sm"
                            prop.value endpointLabel
                            prop.onChange setEndpointLabel
                        ]
                        Html.button [
                            prop.type'.submit
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            if defaultArg debug false then
                                prop.testId $"provenance-create-{sideName}"
                            prop.text "Create endpoint"
                        ]
                    ]
                ],
            ?debug =
                (if defaultArg debug false then
                     Some $"provenance-add-{sideName}"
                 else
                     None)
        )
