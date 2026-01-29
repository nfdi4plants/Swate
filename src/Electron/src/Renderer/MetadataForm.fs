module Renderer.MetadataForms

open System
open Feliz
open Browser.Types
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Fable.Core.JsInterop
open Fetch
open Components.Forms
open Components.JsBindings

/// Generic API state for async operations (private to avoid shadowing Result cases)
type private ApiState<'a> =
    | Idle
    | Loading
    | Success of 'a
    | Failed of exn

module private Helper =

    let addButton (clickEvent: MouseEvent -> unit) =
        Html.button [
            prop.className "swt:btn swt:btn-info swt:btn-sm"
            prop.text "+"
            prop.onClick clickEvent
        ]

    let deleteButton (clickEvent: MouseEvent -> unit) =
        Html.button [
            prop.className "swt:btn swt:btn-error swt:btn-sm swt:grow-0"
            prop.text "Delete"
            prop.onClick clickEvent
        ]

    let fieldTitle (title: string) =
        Html.h5 [
            prop.className "swt:text-primary swt:font-semibold swt:mt-6 swt:mb-2"
            prop.text title
        ]

    let cardFormGroup (children: ReactElement list) =
        Html.div [
            prop.className "swt:grid swt:@md/main:grid-cols-2 swt:@xl/main:grid-flow-col swt:gap-4 not-prose"
            prop.children children
        ]

    let readOnlyFormElement (v: string option, label: string) =
        let v = defaultArg v "-"

        Html.div [
            prop.className "swt:fieldset"
            prop.children [
                Html.label [
                    prop.className "swt:label"
                    prop.children [
                        Html.span [ prop.className "swt:label-text"; prop.text label ]
                    ]
                ]
                Html.input [
                    prop.className "swt:input"
                    prop.disabled true
                    prop.readOnly true
                    prop.valueOrDefault v
                ]
            ]
        ]

    let personModal (person: Person, confirm, back) =
        Html.div [
            prop.className "swt:modal swt:modal-open"
            prop.children [
                Html.div [ prop.className "swt:modal-backdrop"; prop.onClick back ]
                Html.div [
                    prop.className "swt:modal-box"
                    prop.children [
                        cardFormGroup [
                            readOnlyFormElement (person.FirstName, "Given Name")
                            readOnlyFormElement (person.LastName, "Family Name")
                        ]
                        cardFormGroup [
                            readOnlyFormElement (person.EMail, "Email")
                            readOnlyFormElement (person.ORCID, "ORCID")
                        ]
                        cardFormGroup [ readOnlyFormElement (person.Affiliation, "Affiliation") ]
                        Html.div [
                            prop.className "swt:flex swt:justify-end swt:gap-4 swt:mt-4"
                            prop.children [
                                Html.button [
                                    prop.className "swt:btn swt:btn-outline"
                                    prop.text "back"
                                    prop.onClick back
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-success"
                                    prop.text "confirm"
                                    prop.onClick confirm
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let publicationModal (pub: Publication, confirm, back) =
        Html.div [
            prop.className "swt:modal swt:modal-open"
            prop.children [
                Html.div [ prop.className "swt:modal-backdrop"; prop.onClick back ]
                Html.div [
                    prop.className "swt:modal-box"
                    prop.children [
                        readOnlyFormElement (pub.Title, "Title")
                        cardFormGroup [
                            readOnlyFormElement (pub.DOI, "DOI")
                            readOnlyFormElement (pub.PubMedID, "PubMedID")
                        ]
                        readOnlyFormElement (pub.Authors, "Authors")
                        readOnlyFormElement (pub.Status |> Option.map _.ToString(), "Status")
                        Html.div [
                            prop.className "swt:flex swt:justify-end swt:gap-4 swt:mt-4"
                            prop.children [
                                Html.button [
                                    prop.className "swt:btn swt:btn-outline"
                                    prop.text "back"
                                    prop.onClick back
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-success"
                                    prop.text "confirm"
                                    prop.onClick confirm
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let errorModal (error: exn, back) =
        Html.div [
            prop.className "swt:modal swt:modal-open"
            prop.children [
                Html.div [ prop.className "swt:modal-backdrop"; prop.onClick back ]
                Html.div [
                    prop.className "swt:modal-box swt:bg-error swt:text-error-content"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:justify-between swt:items-start"
                            prop.children [
                                Html.div [ prop.className "swt:flex-1"; prop.text error.Message ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                    prop.text "×"
                                    prop.onClick back
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let loadingModal (back) =
        Html.div [
            prop.className "swt:modal swt:modal-open"
            prop.children [
                Html.div [ prop.className "swt:modal-backdrop"; prop.onClick back ]
                Html.div [
                    prop.className "swt:modal-box swt:flex swt:justify-center swt:items-center"
                    prop.children [
                        Html.span [
                            prop.className "swt:loading swt:loading-spinner swt:loading-lg"
                        ]
                    ]
                ]
            ]
        ]

/// Generic form layout components - matches Client's Generic module
type Generic =

    static member FieldTitle(title: string) =
        Html.h5 [
            prop.className "swt:text-primary swt:font-semibold swt:mt-6 swt:mb-2"
            prop.text title
        ]

    static member Collapse (title: ReactElement seq) (content: ReactElement seq) =
        Html.div [
            prop.className
                "swt:collapse swt:collapse-plus swt:grow swt:border swt:has-[:checked]:border-transparent swt:has-[:checked]:bg-base-200"
            prop.children [
                Html.input [ prop.type'.checkbox; prop.className "peer" ]
                Html.div [
                    prop.className
                        "swt:collapse-title swt:after:text-primary swt:@md/main:after:!size-4 swt:@md/main:after:text-xl swt:flex swt:gap-4"
                    prop.children title
                ]
                Html.div [
                    prop.className "swt:collapse-content swt:space-y-4 swt:cursor-default"
                    prop.children content
                ]
            ]
        ]

    static member CollapseTitle(title: string, subtitle: string, ?count: string) =
        React.Fragment [
            Html.div [
                Html.h5 [
                    prop.className "swt:text-md swt:font-semibold"
                    prop.text title
                ]
                Html.div [
                    prop.className "not-prose swt:text-xs swt:text-base-content/70"
                    prop.children [ Html.span [ prop.text subtitle ] ]
                ]
            ]
            if count.IsSome then
                Html.div [
                    prop.className "not-prose swt:flex swt:flex-col swt:ml-auto swt:items-center swt:justify-center"
                    prop.children [
                        Html.span [ prop.text "✏️" ] // Edit icon placeholder
                        Html.div [ prop.className "swt:text-sm"; prop.text (count.Value) ]
                    ]
                ]
        ]

/// Generic form helpers for Electron metadata editing
type FormHelpers =

    /// Debounced text input component
    [<ReactComponent>]
    static member TextInput
        (
            value: string,
            setValue: string -> unit,
            ?label: string,
            ?placeholder: string,
            ?isArea: bool,
            ?disabled: bool,
            ?classes: string,
            ?isJoin: bool,
            ?rmv: MouseEvent -> unit,
            ?validator: string -> Result<unit, string>
        ) =
        let isArea = defaultArg isArea false
        let disabled = defaultArg disabled false
        let isJoin = defaultArg isJoin false
        let startedChange = React.useRef false
        let tempValue, setTempValue = React.useState value
        let debouncedValue = React.useDebounce (tempValue, 300)
        let validationError, setValidationError = React.useState (None: string option)

        // Update parent when debounced value changes
        React.useEffect (
            (fun () ->
                if startedChange.current then
                    // Validate if validator provided
                    match validator with
                    | Some v ->
                        match v debouncedValue with
                        | Result.Ok() ->
                            setValidationError None
                            setValue debouncedValue
                        | Result.Error msg -> setValidationError (Some msg)
                    | None ->
                        setValidationError None
                        setValue debouncedValue

                    startedChange.current <- false
            ),
            [| box debouncedValue |]
        )

        // Sync with external value changes
        React.useEffect ((fun () -> setTempValue value), [| box value |])

        let handleChange =
            fun (s: string) ->
                setTempValue s
                startedChange.current <- true

        let inputClasses = [
            if isJoin then
                "swt:join-item"
            "swt:input swt:input-bordered swt:w-full"
            if validationError.IsSome then
                "swt:input-error"
            if classes.IsSome then
                classes.Value
        ]

        Html.div [
            prop.className (
                if isJoin then
                    "swt:grow swt:join-item"
                else
                    "swt:fieldset swt:grow"
            )
            prop.children [
                if label.IsSome && not isJoin then
                    Helper.fieldTitle label.Value
                if isArea then
                    Html.textarea [
                        prop.className [
                            "swt:textarea swt:textarea-bordered swt:w-full"
                            if validationError.IsSome then
                                "swt:textarea-error"
                            if classes.IsSome then
                                classes.Value
                        ]
                        prop.disabled disabled
                        prop.readOnly disabled
                        prop.valueOrDefault tempValue
                        prop.onChange handleChange
                        if placeholder.IsSome then
                            prop.placeholder placeholder.Value
                    ]
                else
                    Html.div [
                        prop.className "swt:flex swt:gap-2 swt:items-center swt:w-full"
                        prop.children [
                            Html.input [
                                prop.className inputClasses
                                prop.type'.text
                                prop.disabled disabled
                                prop.readOnly disabled
                                prop.valueOrDefault tempValue
                                prop.onChange handleChange
                                if placeholder.IsSome then
                                    prop.placeholder placeholder.Value
                            ]
                            if rmv.IsSome then
                                Helper.deleteButton rmv.Value
                        ]
                    ]
                if validationError.IsSome then
                    Html.p [
                        prop.className "swt:text-error swt:text-sm swt:mt-1"
                        prop.text validationError.Value
                    ]
            ]
        ]

    /// Date/time input component (for string dates)
    [<ReactComponent>]
    static member DateTimeInput(value: string, setValue: string -> unit, ?label: string) =
        let startedChange = React.useRef false
        let tempValue, setTempValue = React.useState value
        let debouncedValue = React.useDebounce (tempValue, 300)

        React.useEffect (
            (fun () ->
                if startedChange.current then
                    setValue debouncedValue
                    startedChange.current <- false
            ),
            [| box debouncedValue |]
        )

        React.useEffect ((fun () -> setTempValue value), [| box value |])

        Html.div [
            prop.className "swt:fieldset swt:grow"
            prop.children [
                if label.IsSome then
                    Helper.fieldTitle label.Value
                Html.input [
                    prop.className "swt:input swt:input-bordered swt:w-full"
                    prop.type'.date
                    prop.valueOrDefault tempValue
                    prop.onChange (fun (s: string) ->
                        setTempValue s
                        startedChange.current <- true
                    )
                ]
            ]
        ]

    /// Date/time input component (for DateTime values)
    static member DateTimeInput(value: System.DateTime, setValue: System.DateTime -> unit, ?label: string) =
        FormHelpers.DateTimeInput(
            value.ToString("yyyy-MM-ddTHH:mm"),
            (fun (s: string) -> setValue (System.DateTime.Parse(s))),
            ?label = label
        )

    /// DndKit InputSequence element with drag handle
    [<ReactComponent>]
    static member InputSequenceElement(key: string, id: string, listComponent: ReactElement) =
        let sortable = DndKit.useSortable ({| id = id |})

        let style = {|
            transform = DndKit.CSS.Transform.toString (sortable.transform)
            transition = sortable.transition
        |}

        Html.div [
            prop.ref sortable.setNodeRef
            prop.id id
            for attr in Object.keys sortable.attributes do
                prop.custom (attr, sortable.attributes.get attr)
            prop.className "swt:flex swt:flex-row swt:gap-2"
            prop.custom ("style", style)
            prop.children [
                Html.span [
                    for listener in Object.keys sortable.listeners do
                        prop.custom (listener, sortable.listeners.get listener)
                    prop.className "swt:cursor-grab swt:flex swt:items-center swt:text-base-content/50"
                    prop.children [
                        Html.text "⇅" // Drag handle icon
                    ]
                ]
                Html.div [ prop.className "swt:grow"; prop.children listComponent ]
            ]
        ]

    /// A generic list container with DndKit drag-and-drop reordering
    [<ReactComponent>]
    static member InputSequence<'A>
        (
            inputs: ResizeArray<'A>,
            constructor: unit -> 'A,
            setter: ResizeArray<'A> -> unit,
            inputComponent: 'A * ('A -> unit) * (MouseEvent -> unit) -> ReactElement,
            ?validator: ResizeArray<'A> -> Result<unit, string>,
            ?label: string,
            ?extendedElements: ReactElement
        ) =
        let sensors = DndKit.useSensors [| DndKit.useSensor (DndKit.PointerSensor) |]
        let error, setError = React.useState (None: string option)

        // Use guids to track element order for DndKit
        let guids =
            React.useMemo (
                (fun () ->
                    ResizeArray [
                        for _ in inputs do
                            Guid.NewGuid()
                    ]
                ),
                [| box inputs.Count |]
            )

        let mkId index = guids.[index].ToString()
        let getIndexFromId (id: string) = guids.FindIndex(fun x -> x = Guid(id))
        let tempInputs = React.useRef inputs

        let validateSetter =
            fun next ->
                match validator with
                | Some validatorFn ->
                    match validatorFn next with
                    | Result.Ok() ->
                        tempInputs.current <- next
                        setter next
                    | Result.Error msg ->
                        setter tempInputs.current
                        setError (Some <| sprintf "Validation Error: %s" msg)
                | None ->
                    tempInputs.current <- next
                    setter next

        let handleDragEnd =
            fun (event: DndKit.IDndKitEvent) ->
                let active = event.active
                let over = event.over

                if (active.id <> over.id) then
                    let oldIndex = getIndexFromId (active.id)
                    let newIndex = getIndexFromId (over.id)
                    DndKit.arrayMove (inputs, oldIndex, newIndex) |> validateSetter

                ()

        Html.div [
            prop.className "swt:space-y-2"
            prop.children [
                if error.IsSome then
                    Swate.Components.BaseModal.ErrorBaseModal(true, (fun _ -> setError None), error.Value)
                if label.IsSome then
                    Helper.fieldTitle label.Value
                if extendedElements.IsSome then
                    extendedElements.Value
                DndKit.DndContext(
                    sensors = sensors,
                    onDragEnd = handleDragEnd,
                    collisionDetection = DndKit.closestCenter,
                    children =
                        DndKit.SortableContext(
                            items = guids,
                            strategy = DndKit.verticalListSortingStrategy,
                            children =
                                Html.div [
                                    prop.className "swt:space-y-2"
                                    prop.children [
                                        for i in 0 .. (inputs.Count - 1) do
                                            let item = inputs.[i]
                                            let id = mkId i

                                            FormHelpers.InputSequenceElement(
                                                id,
                                                id,
                                                (inputComponent (
                                                    item,
                                                    (fun v ->
                                                        inputs.[i] <- v
                                                        inputs |> validateSetter
                                                    ),
                                                    (fun _ ->
                                                        inputs.RemoveAt i
                                                        inputs |> validateSetter
                                                    )
                                                ))
                                            )
                                    ]
                                ]
                        )
                )
                Html.div [
                    prop.className "swt:flex swt:justify-center swt:w-full swt:mt-2"
                    prop.children [
                        Helper.addButton (fun _ ->
                            inputs.Add(constructor ())
                            inputs |> validateSetter
                        )
                    ]
                ]
            ]
        ]

    /// ORCID lookup input with search button
    [<ReactComponent>]
    static member PersonRequestInput
        (id: string option, orcidsetter: string -> unit, searchsetter: Person -> unit, ?label: string)
        =
        let id = defaultArg id ""
        let state, setState = React.useState (ApiState<Person>.Idle)
        let resetState = fun _ -> setState ApiState.Idle

        Html.div [
            prop.className "swt:grow"
            prop.children [
                if label.IsSome then
                    Helper.fieldTitle label.Value
                match state with
                | ApiState.Success person ->
                    Helper.personModal (
                        person,
                        (fun _ ->
                            searchsetter person
                            resetState ()
                        ),
                        resetState
                    )
                | ApiState.Failed e -> Helper.errorModal (e, resetState)
                | ApiState.Loading -> Helper.loadingModal (resetState)
                | _ -> Html.none
                Html.div [
                    prop.className "swt:join swt:w-full"
                    prop.children [
                        FormHelpers.TextInput(id, orcidsetter, isJoin = true)
                        Html.button [
                            prop.className "swt:btn swt:btn-info swt:join-item"
                            prop.text "Search"
                            prop.onClick (fun _ ->
                                setState ApiState.Loading

                                API.start
                                    API.requestByORCID
                                    id
                                    (ApiState.Success >> setState)
                                    (ApiState.Failed >> setState)
                            )
                        ]
                    ]
                ]
            ]
        ]

    /// DOI/PubMed lookup input with search button
    [<ReactComponent>]
    static member PublicationRequestInput
        (
            id: string option,
            searchAPI: string -> Fable.Core.JS.Promise<Publication>,
            doisetter,
            searchsetter: Publication -> unit,
            ?label: string
        ) =
        let id = defaultArg id ""
        let state, setState = React.useState (ApiState<Publication>.Idle)
        let resetState = fun _ -> setState ApiState.Idle

        Html.div [
            prop.className "swt:grow"
            prop.children [
                if label.IsSome then
                    Helper.fieldTitle label.Value
                match state with
                | ApiState.Success pub ->
                    Helper.publicationModal (
                        pub,
                        (fun _ ->
                            searchsetter pub
                            resetState ()
                        ),
                        resetState
                    )
                | ApiState.Failed e -> Helper.errorModal (e, resetState)
                | ApiState.Loading -> Helper.loadingModal (resetState)
                | _ -> Html.none
                Html.div [
                    prop.className "swt:join swt:w-full"
                    prop.children [
                        FormHelpers.TextInput(id, doisetter, isJoin = true)
                        Html.button [
                            prop.className "swt:btn swt:btn-info swt:join-item"
                            prop.text "Search"
                            prop.onClick (fun _ ->
                                setState ApiState.Loading

                                API.start searchAPI id (ApiState.Success >> setState) (ApiState.Failed >> setState)
                            )
                        ]
                    ]
                ]
            ]
        ]

    static member DOIInput(id: string option, doisetter, searchsetter: Publication -> unit, ?label: string) =
        FormHelpers.PublicationRequestInput(id, API.requestByDOI, doisetter, searchsetter, ?label = label)

    static member PubMedIDInput(id: string option, doisetter, searchsetter: Publication -> unit, ?label: string) =
        FormHelpers.PublicationRequestInput(id, API.requestByPubMedID, doisetter, searchsetter, ?label = label)

    /// Ontology annotation input with TermSearch integration
    [<ReactComponent>]
    static member OntologyAnnotationInput
        (
            oa: OntologyAnnotation option,
            setOa: OntologyAnnotation option -> unit,
            ?label: string,
            ?rmv: MouseEvent -> unit,
            ?parent: OntologyAnnotation
        ) =
        let startedChange = React.useRef false
        let tempValue, setTempValue = React.useState (oa |> Option.map _.ToTerm())
        let debouncedValue = React.useDebounce (tempValue, 300)

        React.useEffect (
            (fun () ->
                if startedChange.current then
                    setOa (debouncedValue |> Option.map OntologyAnnotation.from)
                    startedChange.current <- false
            ),
            [| box debouncedValue |]
        )

        React.useEffect ((fun () -> setTempValue (oa |> Option.map _.ToTerm())), [| box oa |])

        let setTempValueWrapper =
            fun (t: Term option) ->
                setTempValue t
                startedChange.current <- true

        Html.div [
            prop.className "swt:space-y-2 swt:grow"
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                Html.div [
                    prop.className "swt:w-full swt:flex swt:gap-2 swt:relative"
                    prop.children [
                        TermSearch.TermSearch(
                            tempValue,
                            setTempValueWrapper,
                            classNames = TermSearchStyle(Fable.Core.U2.Case1 "swt:w-full"),
                            ?parentId = (parent |> Option.bind (fun p -> p.TermAccessionNumber))
                        )
                        if rmv.IsSome then
                            Helper.deleteButton rmv.Value
                    ]
                ]
            ]
        ]

    /// Multiple ontology annotations input (list with DndKit drag-drop)
    [<ReactComponent>]
    static member OntologyAnnotationsInput
        (
            oas: ResizeArray<OntologyAnnotation>,
            setter: ResizeArray<OntologyAnnotation> -> unit,
            ?label: string,
            ?parent: OntologyAnnotation
        ) =
        FormHelpers.InputSequence(
            oas,
            OntologyAnnotation.empty,
            setter,
            (fun (v, setV, rmv) ->
                FormHelpers.OntologyAnnotationInput(
                    Some v,
                    (fun newOa ->
                        match newOa with
                        | Some oa -> setV oa
                        | None -> setV (OntologyAnnotation.empty ())
                    ),
                    rmv = rmv,
                    ?parent = parent
                )
            ),
            ?label = label
        )

    /// Comment input component
    [<ReactComponent>]
    static member CommentInput
        (
            comment: Comment,
            setter: Comment -> unit,
            ?rmv: MouseEvent -> unit,
            ?keyValidator: string -> Result<unit, string>
        ) =
        Html.div [
            prop.className "swt:flex swt:flex-row swt:gap-2 swt:relative swt:items-end"
            prop.children [
                FormHelpers.TextInput(
                    comment.Name |> Option.defaultValue "",
                    (fun s ->
                        comment.Name <- if String.IsNullOrWhiteSpace s then None else Some s
                        setter comment
                    ),
                    placeholder = "comment name",
                    ?validator = keyValidator
                )
                FormHelpers.TextInput(
                    comment.Value |> Option.defaultValue "",
                    (fun s ->
                        comment.Value <- if String.IsNullOrWhiteSpace s then None else Some s
                        setter comment
                    ),
                    placeholder = "comment value"
                )
                if rmv.IsSome then
                    Helper.deleteButton rmv.Value
            ]
        ]

    /// Comments input (list with DndKit drag-drop)
    [<ReactComponent>]
    static member CommentsInput(comments: ResizeArray<Comment>, setter: ResizeArray<Comment> -> unit, ?label: string) =
        let keyValidator =
            fun (name: string) ->
                let isDuplicate =
                    comments
                    |> Seq.filter (fun c -> c.Name.IsSome && c.Name.Value = name)
                    |> Seq.length > 1

                if isDuplicate then
                    Result.Error "Comment names must be unique."
                else
                    Result.Ok()

        FormHelpers.InputSequence(
            comments,
            Comment,
            setter,
            (fun (v, setV, rmv) -> FormHelpers.CommentInput(v, setV, rmv = rmv, keyValidator = keyValidator)),
            ?label = label
        )

    /// Person input component (collapsible) with ORCID lookup
    [<ReactComponent>]
    static member PersonInput(person: Person, setter: Person -> unit, ?rmv: MouseEvent -> unit) =
        let nameStr =
            let fn = Option.defaultValue "" person.FirstName
            let ln = Option.defaultValue "" person.LastName
            let mi = Option.defaultValue "" person.MidInitials
            let x = $"{fn} {mi} {ln}".Trim()
            if x = "" then "<name>" else x

        let orcid = Option.defaultValue "<orcid>" person.ORCID

        let updatePersonField =
            fun s personSetter input ->
                let s = if s = "" then None else Some s
                personSetter input s
                input |> setter

        let createPersonFieldTextInput (field: string option, label, personSetter: Person -> string option -> unit) =
            FormHelpers.TextInput(
                field |> Option.defaultValue "",
                (fun s -> updatePersonField s personSetter person),
                label
            )

        let countFilledFieldsString (person: Person) =
            let fields = [
                person.FirstName
                person.LastName
                person.MidInitials
                person.ORCID
                person.Address
                person.Affiliation
                person.EMail
                person.Phone
                person.Fax
                if person.Roles.Count > 0 then Some "roles" else None
            ]

            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"

        Generic.Collapse [ // title
            Generic.CollapseTitle(nameStr, orcid, countFilledFieldsString person)
        ] [ // content
            Helper.cardFormGroup [
                createPersonFieldTextInput (person.FirstName, "First Name", fun input s -> input.FirstName <- s)
                createPersonFieldTextInput (person.LastName, "Last Name", fun input s -> input.LastName <- s)
            ]
            Helper.cardFormGroup [
                createPersonFieldTextInput (person.MidInitials, "Mid Initials", fun input s -> input.MidInitials <- s)
                FormHelpers.PersonRequestInput(
                    person.ORCID,
                    (fun s ->
                        let s = if s = "" then None else Some s
                        person.ORCID <- s
                        person |> setter
                    ),
                    (fun p -> setter p),
                    "ORCID"
                )
            ]
            Helper.cardFormGroup [
                createPersonFieldTextInput (person.Affiliation, "Affiliation", fun input s -> input.Affiliation <- s)
                createPersonFieldTextInput (person.Address, "Address", fun input s -> input.Address <- s)
            ]
            createPersonFieldTextInput (person.EMail, "Email", fun input s -> input.EMail <- s)
            Helper.cardFormGroup [
                createPersonFieldTextInput (person.Phone, "Phone", fun input s -> input.Phone <- s)
                createPersonFieldTextInput (person.Fax, "Fax", fun input s -> input.Fax <- s)
            ]
            FormHelpers.OntologyAnnotationsInput(
                person.Roles,
                (fun oas ->
                    person.Roles <- oas
                    person |> setter
                ),
                "Roles",
                parent = TermCollection.PersonRoleWithinExperiment
            )
            if rmv.IsSome then
                Helper.deleteButton rmv.Value
        ]

    /// Persons input (list with DndKit drag-drop)
    [<ReactComponent>]
    static member PersonsInput(persons: ResizeArray<Person>, setter: ResizeArray<Person> -> unit, ?label: string) =
        FormHelpers.InputSequence(
            persons,
            Person,
            setter,
            (fun (v, setV, rmv) -> FormHelpers.PersonInput(v, setV, rmv)),
            ?label = label
        )

    /// Publication input component (collapsible) with DOI/PubMed lookup
    [<ReactComponent>]
    static member PublicationInput(publication: Publication, setter: Publication -> unit, ?rmv: MouseEvent -> unit) =
        let title = Option.defaultValue "<title>" publication.Title
        let doi = Option.defaultValue "<doi>" publication.DOI

        let createFieldTextInput (field: string option, label, publicationSetter: string option -> unit) =
            FormHelpers.TextInput(
                field |> Option.defaultValue "",
                (fun s ->
                    let s = if s = "" then None else Some s
                    publicationSetter s
                    publication |> setter
                ),
                label
            )

        let countFilledFieldsString () =
            let fields = [
                publication.PubMedID
                publication.DOI
                publication.Title
                publication.Authors
                publication.Status |> Option.map (fun _ -> "")
            ]

            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"

        Generic.Collapse [
            Generic.CollapseTitle(title, doi, countFilledFieldsString ())
        ] [
            createFieldTextInput (publication.Title, "Title", fun s -> publication.Title <- s)
            Helper.cardFormGroup [
                FormHelpers.PubMedIDInput(
                    publication.PubMedID,
                    (fun s ->
                        let s = if s = "" then None else Some s
                        publication.PubMedID <- s
                        publication |> setter
                    ),
                    (fun pub -> setter pub),
                    "PubMed Id"
                )
                FormHelpers.DOIInput(
                    publication.DOI,
                    (fun s ->
                        let s = if s = "" then None else Some s
                        publication.DOI <- s
                        publication |> setter
                    ),
                    (fun pub -> setter pub),
                    "DOI"
                )
            ]
            createFieldTextInput (publication.Authors, "Authors", fun s -> publication.Authors <- s)
            FormHelpers.OntologyAnnotationInput(
                publication.Status,
                (fun s ->
                    publication.Status <- s
                    publication |> setter
                ),
                "Status",
                parent = TermCollection.PublicationStatus
            )
            FormHelpers.CommentsInput(
                publication.Comments,
                (fun c ->
                    publication.Comments <- ResizeArray(c)
                    publication |> setter
                ),
                "Comments"
            )
            if rmv.IsSome then
                Helper.deleteButton rmv.Value
        ]

    /// Publications input (list with DndKit drag-drop)
    [<ReactComponent>]
    static member PublicationsInput
        (input: ResizeArray<Publication>, setter: ResizeArray<Publication> -> unit, label: string)
        =
        FormHelpers.InputSequence(
            input,
            Publication,
            setter,
            (fun (a, b, c) -> FormHelpers.PublicationInput(a, b, rmv = c)),
            label = label
        )

    /// Ontology Source Reference input (collapsible)
    [<ReactComponent>]
    static member OntologySourceReferenceInput
        (input: OntologySourceReference, setter: OntologySourceReference -> unit, ?deletebutton: MouseEvent -> unit)
        =
        let name = Option.defaultValue "<name>" input.Name
        let version = Option.defaultValue "<version>" input.Version

        let createFieldTextInput (field: string option, label, setFunction: string option -> unit) =
            FormHelpers.TextInput(
                field |> Option.defaultValue "",
                (fun s ->
                    s |> Option.whereNot String.IsNullOrWhiteSpace |> setFunction
                    input |> setter
                ),
                label
            )

        let countFilledFieldsString () =
            let fields = [
                input.Name
                input.File
                input.Version
                input.Description
                if input.Comments.Count > 0 then Some "comments" else None
            ]

            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"

        Generic.Collapse [
            Generic.CollapseTitle(name, version, countFilledFieldsString ())
        ] [
            createFieldTextInput (input.Name, "Name", fun s -> input.Name <- s)
            Helper.cardFormGroup [
                createFieldTextInput (input.Version, "Version", fun s -> input.Version <- s)
                createFieldTextInput (input.File, "File", fun s -> input.File <- s)
            ]
            FormHelpers.TextInput(
                Option.defaultValue "" input.Description,
                (fun s ->
                    input.Description <- s |> Option.whereNot String.IsNullOrWhiteSpace
                    input |> setter
                ),
                "Description",
                isArea = true
            )
            FormHelpers.CommentsInput(
                input.Comments,
                (fun c ->
                    input.Comments <- c
                    input |> setter
                ),
                "Comments"
            )
            if deletebutton.IsSome then
                Helper.deleteButton deletebutton.Value
        ]

    /// Ontology Source References input (list with DndKit drag-drop)
    static member OntologySourceReferencesInput
        (
            input: ResizeArray<OntologySourceReference>,
            setter: ResizeArray<OntologySourceReference> -> unit,
            label: string
        ) =
        FormHelpers.InputSequence(
            input,
            OntologySourceReference,
            setter,
            (fun (a, b, c) -> FormHelpers.OntologySourceReferenceInput(a, b, c)),
            label = label
        )

    /// Collection of strings input with DndKit drag-drop
    static member CollectionOfStrings(identifiers: ResizeArray<string>, setIdentifiers, ?label: string) =
        FormHelpers.InputSequence(
            identifiers,
            (fun () -> ""),
            setIdentifiers,
            (fun (v, setV, rmv) -> FormHelpers.TextInput(v, setV, rmv = rmv)),
            ?label = label
        )

    /// Section wrapper for metadata forms
    static member Section(children: ReactElement list) =
        Html.section [
            prop.className "swt:container swt:py-2 swt:lg:py-8 swt:space-y-8"
            prop.children children
        ]

    /// Boxed field card wrapper
    static member BoxedField(title: string, ?description: string, ?content: ReactElement list) =
        Html.div [
            prop.className
                "swt:card swt:card-sm swt:space-y-6 swt:border-2 swt:border-base-300 swt:shadow-xl swt:bg-base
            swt:prose swt:prose-headings:text-primary swt:container swt:max-w-full swt:lg:max-w-[800px]"
            prop.children [
                Html.div [
                    prop.className "swt:card-body"
                    prop.children [
                        Html.div [
                            prop.children [
                                Html.h1 [ prop.className "swt:mt-0"; prop.text title ]
                                if description.IsSome then
                                    Html.p [
                                        prop.className "swt:text-sm swt:text-base-content/80"
                                        prop.text description.Value
                                    ]
                            ]
                        ]
                        if content.IsSome then
                            Html.div [
                                prop.className "swt:divide-y swt:divide-base-content/20"
                                prop.children (
                                    content.Value
                                    |> List.map (fun element ->
                                        Html.div [ prop.className "swt:py-2"; prop.children [ element ] ]
                                    )
                                )
                            ]
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let AssayMetadata (assay: ArcAssay, setAssay: ArcAssay -> unit) =
    FormHelpers.Section [
        FormHelpers.BoxedField(
            "Assay Metadata",
            content = [
                FormHelpers.TextInput(
                    assay.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg assay.Title "",
                    (fun v ->
                        assay.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setAssay assay
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg assay.Description "",
                    (fun v ->
                        assay.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setAssay assay
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.OntologyAnnotationInput(
                    assay.MeasurementType,
                    (fun oa ->
                        assay.MeasurementType <- oa
                        setAssay assay
                    ),
                    label = "Measurement Type"
                )
                FormHelpers.OntologyAnnotationInput(
                    assay.TechnologyType,
                    (fun oa ->
                        assay.TechnologyType <- oa
                        setAssay assay
                    ),
                    label = "Technology Type"
                )
                FormHelpers.OntologyAnnotationInput(
                    assay.TechnologyPlatform,
                    (fun oa ->
                        assay.TechnologyPlatform <- oa
                        setAssay assay
                    ),
                    label = "Technology Platform"
                )
                FormHelpers.PersonsInput(
                    assay.Performers,
                    (fun persons ->
                        assay.Performers <- persons
                        setAssay assay
                    ),
                    label = "Performers"
                )
                FormHelpers.CommentsInput(
                    assay.Comments,
                    (fun comments ->
                        assay.Comments <- comments
                        setAssay assay
                    ),
                    label = "Comments"
                )
            ]
        )
    ]

[<ReactComponent>]
let StudyMetadata (study: ArcStudy, setStudy: ArcStudy -> unit) =
    FormHelpers.Section [
        FormHelpers.BoxedField(
            "Study Metadata",
            content = [
                FormHelpers.TextInput(
                    study.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg study.Title "",
                    (fun v ->
                        study.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setStudy study
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg study.Description "",
                    (fun v ->
                        study.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setStudy study
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.PersonsInput(
                    study.Contacts,
                    (fun persons ->
                        study.Contacts <- persons
                        setStudy study
                    ),
                    label = "Contacts"
                )
                FormHelpers.PublicationsInput(
                    study.Publications,
                    (fun pubs ->
                        study.Publications <- pubs
                        setStudy study
                    ),
                    label = "Publications"
                )
                FormHelpers.DateTimeInput(
                    defaultArg study.SubmissionDate "",
                    (fun v ->
                        study.SubmissionDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setStudy study
                    ),
                    label = "Submission Date"
                )
                FormHelpers.DateTimeInput(
                    defaultArg study.PublicReleaseDate "",
                    (fun v ->
                        study.PublicReleaseDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setStudy study
                    ),
                    label = "Public Release Date"
                )
                FormHelpers.OntologyAnnotationsInput(
                    study.StudyDesignDescriptors,
                    (fun oas ->
                        study.StudyDesignDescriptors <- oas
                        setStudy study
                    ),
                    label = "Study Design Descriptors"
                )
                FormHelpers.CommentsInput(
                    study.Comments,
                    (fun comments ->
                        study.Comments <- comments
                        setStudy study
                    ),
                    label = "Comments"
                )
            ]
        )
    ]

[<ReactComponent>]
let InvestigationMetadata (investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit) =
    FormHelpers.Section [
        FormHelpers.BoxedField(
            "Investigation Metadata",
            content = [
                FormHelpers.TextInput(
                    investigation.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg investigation.Title "",
                    (fun v ->
                        investigation.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setInvestigation investigation
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg investigation.Description "",
                    (fun v ->
                        investigation.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setInvestigation investigation
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.PersonsInput(
                    investigation.Contacts,
                    (fun persons ->
                        investigation.Contacts <- persons
                        setInvestigation investigation
                    ),
                    label = "Contacts"
                )
                FormHelpers.PublicationsInput(
                    investigation.Publications,
                    (fun pubs ->
                        investigation.Publications <- pubs
                        setInvestigation investigation
                    ),
                    label = "Publications"
                )
                FormHelpers.DateTimeInput(
                    defaultArg investigation.SubmissionDate "",
                    (fun v ->
                        investigation.SubmissionDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setInvestigation investigation
                    ),
                    label = "Submission Date"
                )
                FormHelpers.DateTimeInput(
                    defaultArg investigation.PublicReleaseDate "",
                    (fun v ->
                        investigation.PublicReleaseDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setInvestigation investigation
                    ),
                    label = "Public Release Date"
                )
                FormHelpers.OntologySourceReferencesInput(
                    investigation.OntologySourceReferences,
                    (fun osrs ->
                        investigation.OntologySourceReferences <- osrs
                        setInvestigation investigation
                    ),
                    label = "Ontology Source References"
                )
                FormHelpers.CommentsInput(
                    investigation.Comments,
                    (fun comments ->
                        investigation.Comments <- comments
                        setInvestigation investigation
                    ),
                    label = "Comments"
                )
            ]
        )
    ]

[<ReactComponent>]
let RunMetadata (run: ArcRun, setRun: ArcRun -> unit) =
    FormHelpers.Section [
        FormHelpers.BoxedField(
            "Run Metadata",
            content = [
                FormHelpers.TextInput(
                    run.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg run.Title "",
                    (fun v ->
                        run.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setRun run
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg run.Description "",
                    (fun v ->
                        run.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setRun run
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.OntologyAnnotationInput(
                    run.MeasurementType,
                    (fun oa ->
                        run.MeasurementType <- oa
                        setRun run
                    ),
                    label = "Measurement Type"
                )
                FormHelpers.OntologyAnnotationInput(
                    run.TechnologyType,
                    (fun oa ->
                        run.TechnologyType <- oa
                        setRun run
                    ),
                    label = "Technology Type"
                )
                FormHelpers.OntologyAnnotationInput(
                    run.TechnologyPlatform,
                    (fun oa ->
                        run.TechnologyPlatform <- oa
                        setRun run
                    ),
                    label = "Technology Platform"
                )
                FormHelpers.CollectionOfStrings(
                    run.WorkflowIdentifiers,
                    (fun ids ->
                        run.WorkflowIdentifiers <- ids
                        setRun run
                    ),
                    label = "Workflow Identifiers"
                )
                FormHelpers.PersonsInput(
                    run.Performers,
                    (fun persons ->
                        run.Performers <- persons
                        setRun run
                    ),
                    label = "Performers"
                )
                FormHelpers.CommentsInput(
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

[<ReactComponent>]
let WorkflowMetadata (workflow: ArcWorkflow, setWorkflow: ArcWorkflow -> unit) =
    FormHelpers.Section [
        FormHelpers.BoxedField(
            "Workflow Metadata",
            content = [
                FormHelpers.TextInput(
                    workflow.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg workflow.Title "",
                    (fun v ->
                        workflow.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setWorkflow workflow
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg workflow.Description "",
                    (fun v ->
                        workflow.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setWorkflow workflow
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.TextInput(
                    defaultArg workflow.Version "",
                    (fun v ->
                        workflow.Version <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setWorkflow workflow
                    ),
                    label = "Version"
                )
                FormHelpers.OntologyAnnotationInput(
                    workflow.WorkflowType,
                    (fun oa ->
                        workflow.WorkflowType <- oa
                        setWorkflow workflow
                    ),
                    label = "Workflow Type"
                )
                FormHelpers.TextInput(
                    defaultArg workflow.URI "",
                    (fun v ->
                        workflow.URI <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setWorkflow workflow
                    ),
                    label = "URI"
                )
                FormHelpers.PersonsInput(
                    workflow.Contacts,
                    (fun persons ->
                        workflow.Contacts <- persons
                        setWorkflow workflow
                    ),
                    label = "Contacts"
                )
                FormHelpers.CommentsInput(
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

[<ReactComponent>]
let TemplateMetadata (template: Template, setTemplate: Template -> unit) =
    FormHelpers.Section [
        FormHelpers.BoxedField(
            "Template Metadata",
            content = [
                FormHelpers.TextInput(
                    template.Id.ToString(),
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    template.Name,
                    (fun v ->
                        template.Name <- v
                        setTemplate template
                    ),
                    label = "Name"
                )
                FormHelpers.TextInput(
                    template.Description,
                    (fun v ->
                        template.Description <- v
                        setTemplate template
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.TextInput(
                    template.Organisation.ToString(),
                    (fun v ->
                        template.Organisation <- Organisation.ofString v
                        setTemplate template
                    ),
                    label = "Organisation"
                )
                FormHelpers.TextInput(
                    template.Version,
                    (fun v ->
                        template.Version <- v
                        setTemplate template
                    ),
                    label = "Version"
                )
                FormHelpers.DateTimeInput(
                    template.LastUpdated,
                    (fun v ->
                        template.LastUpdated <- v
                        setTemplate template
                    ),
                    label = "Last Updated"
                )
                FormHelpers.OntologyAnnotationsInput(
                    template.Tags,
                    (fun oas ->
                        template.Tags <- oas
                        setTemplate template
                    ),
                    label = "Tags"
                )
                FormHelpers.OntologyAnnotationsInput(
                    template.EndpointRepositories,
                    (fun oas ->
                        template.EndpointRepositories <- oas
                        setTemplate template
                    ),
                    label = "Endpoint Repositories"
                )
                FormHelpers.PersonsInput(
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

[<ReactComponent>]
let DataMapMetadata (datamap: DataMap) =
    FormHelpers.Section [
        FormHelpers.BoxedField("DataMap", description = $"Data Contexts: {datamap.DataContexts.Count}")
    ]