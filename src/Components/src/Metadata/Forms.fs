namespace Swate.Components.Metadata

open System
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open ARCtrl

open Swate.Components
open Swate.Components.Metadata.JsBindings

[<RequireQualifiedAccess>]
type AsyncState<'T> =
    | Idle
    | Loading
    | Ok of 'T
    | Error of exn

module Helper =

    let addButton (clickEvent: MouseEvent -> unit) =
        Html.button [
            prop.className "swt:btn swt:btn-info"
            prop.text "+"
            prop.onClick clickEvent
        ]

    let deleteButton (clickEvent: MouseEvent -> unit) =
        Html.button [
            prop.className "swt:btn swt:btn-error swt:grow-0"
            prop.text "Delete"
            prop.onClick clickEvent
        ]

    let cardFormGroup (content: ReactElement list) =
        Html.div [
            prop.className "swt:grid swt:@md/main:grid-cols-2 swt:@xl/main:grid-flow-col swt:gap-4 not-prose"
            prop.children content
        ]

type FormComponents =

    [<ReactComponent>]
    static member InputSequenceElement(key: string, id: string, listComponent: ReactElement) =
        let sortable = JsBindings.DndKit.useSortable ({| id = id |})

        let style = {|
            transform = DndKit.CSS.Transform.toString sortable.transform
            transition = sortable.transition
        |}

        Html.div [
            prop.ref sortable.setNodeRef
            prop.id id
            for attribute in Object.keys sortable.attributes do
                prop.custom (attribute, sortable.attributes.get attribute)
            prop.className "swt:flex swt:flex-row swt:gap-2"
            prop.custom ("style", style)
            prop.children [
                Html.span [
                    for listener in Object.keys sortable.listeners do
                        prop.custom (listener, sortable.listeners.get listener)
                    prop.className "swt:cursor-grab swt:flex swt:items-center"
                    prop.children [ Icons.ArrowUpDown() ]
                ]
                Html.div [ prop.className "swt:grow"; prop.children listComponent ]
            ]
        ]

    [<ReactComponent>]
    static member InputSequence<'T>
        (
            inputs: ResizeArray<'T>,
            constructor: unit -> 'T,
            setter: ResizeArray<'T> -> unit,
            inputComponent: 'T * ('T -> unit) * (MouseEvent -> unit) -> ReactElement,
            ?validator: ResizeArray<'T> -> Result<unit, string>,
            ?label: string,
            ?extendedElements: ReactElement
        ) =
        let sensors = DndKit.useSensors [| DndKit.useSensor DndKit.PointerSensor |]
        let error, setError = React.useState (None: string option)

        let guids =
            React.useMemo (
                (fun () ->
                    ResizeArray [
                        for _ in inputs do
                            Guid.NewGuid()
                    ]),
                [| box inputs.Count |]
            )

        let mkId index = guids.[index].ToString()

        let getIndexFromId (id: string) =
            guids.FindIndex(fun guid -> guid = Guid id)

        let previousValidInputs = React.useRef inputs

        let validateSetter next =
            match validator with
            | Some validate ->
                match validate next with
                | Ok() ->
                    previousValidInputs.current <- next
                    setter next
                | Error message ->
                    setter previousValidInputs.current
                    setError (Some $"Validation Error: {message}")
            | None ->
                previousValidInputs.current <- next
                setter next

        let handleDragEnd (event: DndKit.IDndKitEvent) =
            let active = event.active
            let over = event.over

            if isNull over |> not && active.id <> over.id then
                let oldIndex = getIndexFromId active.id
                let newIndex = getIndexFromId over.id

                if oldIndex >= 0 && newIndex >= 0 then
                    DndKit.arrayMove (inputs, oldIndex, newIndex) |> validateSetter

        Html.div [
            prop.className "swt:space-y-2"
            prop.children [
                BaseModal.ErrorModalObsolete(error.IsSome, (fun _ -> setError None), error |> Option.defaultValue "")
                if label.IsSome then
                    Generic.FieldTitle label.Value
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
                                        for index in 0 .. (inputs.Count - 1) do
                                            let item = inputs.[index]
                                            let id = mkId index

                                            FormComponents.InputSequenceElement(
                                                id,
                                                id,
                                                inputComponent (
                                                    item,
                                                    (fun updated ->
                                                        inputs.[index] <- updated
                                                        validateSetter inputs),
                                                    (fun _ ->
                                                        inputs.RemoveAt index
                                                        validateSetter inputs)
                                                )
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
                            validateSetter inputs)
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member TextInput
        (
            value: string,
            setValue: string -> unit,
            ?label: string,
            ?validator: string -> Result<unit, string>,
            ?placeholder: string,
            ?isArea: bool,
            ?isJoin: bool,
            ?disabled: bool,
            ?rmv: MouseEvent -> unit,
            ?classes: string
        ) =
        TextInput.Entry(
            value,
            setValue,
            ?label = label,
            ?validator = validator,
            ?placeholder = placeholder,
            isArea = defaultArg isArea false,
            isJoin = defaultArg isJoin false,
            disabled = defaultArg disabled false,
            ?rmv = rmv,
            ?classes = classes
        )

    [<ReactComponent>]
    static member OntologyAnnotationInput
        (
            input: OntologyAnnotation option,
            setter: OntologyAnnotation option -> unit,
            ?label: string,
            ?parent: OntologyAnnotation,
            ?rmv: MouseEvent -> unit
        ) =
        let startedChange = React.useRef false
        let term, setTerm = React.useState (input |> Option.map _.ToTerm())

        let setTermWrapper =
            React.useCallback (fun (nextTerm: Term option) ->
                setTerm nextTerm
                startedChange.current <- true)

        let debouncedTerm = React.useDebounce (term, 300)

        React.useEffect (
            (fun () ->
                if startedChange.current then
                    setter (debouncedTerm |> Option.map OntologyAnnotation.from)

                startedChange.current <- false),
            [| box debouncedTerm |]
        )

        React.useEffect (
            (fun () ->
                setTerm (input |> Option.map _.ToTerm())
                startedChange.current <- false),
            [| box input |]
        )

        Html.div [
            prop.className "swt:space-y-2 swt:grow"
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                Html.div [
                    prop.className "swt:w-full swt:flex swt:gap-2 swt:relative"
                    prop.children [
                        TermSearch.TermSearch.Init(
                            term,
                            setTermWrapper,
                            ?parentId = (parent |> Option.map _.TermAccessionShort),
                            classNames = TermSearchStyle(Fable.Core.U2.Case1 "swt:w-full")
                        )
                        if rmv.IsSome then
                            Helper.deleteButton rmv.Value
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationsInput
        (input: ResizeArray<OntologyAnnotation>, setter: ResizeArray<OntologyAnnotation> -> unit, ?label: string, ?parent)
        =
        FormComponents.InputSequence(
            input,
            OntologyAnnotation.empty,
            setter,
            (fun (value, setValue, remove) ->
                FormComponents.OntologyAnnotationInput(
                    Some value,
                    (fun next -> next |> Option.defaultValue (OntologyAnnotation.empty ()) |> setValue),
                    ?parent = parent,
                    rmv = remove
                )),
            ?label = label
        )

    [<ReactComponent>]
    static member PersonInput(input: Person, setter: Person -> unit, ?rmv: MouseEvent -> unit) =
        let nameText =
            let firstName = Option.defaultValue "" input.FirstName
            let lastName = Option.defaultValue "" input.LastName
            let midInitials = Option.defaultValue "" input.MidInitials
            let fullName = $"{firstName} {midInitials} {lastName}".Trim()
            if fullName = "" then "<name>" else fullName

        let orcid = Option.defaultValue "<orcid>" input.ORCID

        let updatePersonField (value: string) (personSetter: Person -> string option -> unit) =
            let value = if value = "" then None else Some value
            personSetter input value
            setter input

        let createPersonFieldTextInput (field: string option, label: string, personSetter: Person -> string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                (fun value -> updatePersonField value personSetter),
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

            let total = fields.Length
            let filled = fields |> List.choose id |> List.length
            $"{filled}/{total}"

        Generic.Collapse
            [ Generic.CollapseTitle(nameText, orcid, countFilledFieldsString input) ]
            [
                Helper.cardFormGroup [
                    createPersonFieldTextInput (input.FirstName, "First Name", fun person value -> person.FirstName <- value)
                    createPersonFieldTextInput (input.LastName, "Last Name", fun person value -> person.LastName <- value)
                ]
                Helper.cardFormGroup [
                    createPersonFieldTextInput (input.MidInitials, "Mid Initials", fun person value -> person.MidInitials <- value)
                    createPersonFieldTextInput (input.ORCID, "ORCID", fun person value -> person.ORCID <- value)
                ]
                Helper.cardFormGroup [
                    createPersonFieldTextInput (input.Affiliation, "Affiliation", fun person value -> person.Affiliation <- value)
                    createPersonFieldTextInput (input.Address, "Address", fun person value -> person.Address <- value)
                ]
                createPersonFieldTextInput (input.EMail, "Email", fun person value -> person.EMail <- value)
                Helper.cardFormGroup [
                    createPersonFieldTextInput (input.Phone, "Phone", fun person value -> person.Phone <- value)
                    createPersonFieldTextInput (input.Fax, "Fax", fun person value -> person.Fax <- value)
                ]
                FormComponents.OntologyAnnotationsInput(
                    input.Roles,
                    (fun roles ->
                        input.Roles <- roles
                        setter input),
                    "Roles",
                    parent = TermCollection.PersonRoleWithinExperiment
                )
                if rmv.IsSome then
                    Helper.deleteButton rmv.Value
            ]

    [<ReactComponent>]
    static member PersonsInput
        (
            persons: ResizeArray<Person>,
            setter: ResizeArray<Person> -> unit,
            ?isARCitect: bool,
            ?label: string,
            ?onImportPersons: unit -> JS.Promise<Person[]>
        ) =
        let importState, setImportState = React.useState (AsyncState<Person[]>.Idle)
        let showImportButton = defaultArg isARCitect false || onImportPersons.IsSome

        let importPersons () =
            match onImportPersons with
            | None -> setImportState (AsyncState.Error (exn "No import callback configured."))
            | Some load ->
                promise {
                    setImportState AsyncState.Loading
                    let! imported = load ()

                    persons.AddRange(imported)
                    setter persons
                    setImportState (AsyncState.Ok imported)
                }
                |> Promise.catch (fun err -> setImportState (AsyncState.Error err))
                |> Promise.start

        let extendedElements =
            if showImportButton then
                Some(
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:items-center swt:gap-2"
                        prop.children [
                            Html.button [
                                prop.className "swt:btn swt:btn-primary swt:btn-wide"
                                prop.text "Import Persons"
                                prop.disabled (onImportPersons.IsNone || importState = AsyncState.Loading)
                                prop.onClick (fun _ -> importPersons ())
                            ]
                            match importState with
                            | AsyncState.Loading ->
                                Html.span [ prop.className "swt:loading swt:loading-spinner swt:loading-sm" ]
                            | AsyncState.Error err ->
                                Html.span [ prop.className "swt:text-error swt:text-sm"; prop.text err.Message ]
                            | AsyncState.Ok loaded ->
                                Html.span [
                                    prop.className "swt:text-success swt:text-sm"
                                    prop.text $"Imported {loaded.Length} persons."
                                ]
                            | AsyncState.Idle -> Html.none
                        ]
                    ]
                )
            else
                None

        FormComponents.InputSequence(
            persons,
            Person,
            setter,
            (fun (value, setValue, remove) -> FormComponents.PersonInput(value, setValue, remove)),
            ?label = label,
            ?extendedElements = extendedElements
        )

    [<ReactComponent>]
    static member DateTimeInput(inputValue: string, setter: string -> unit, ?label: string) =
        let inputRef = React.useInputRef ()
        let debouncedSetter = React.useDebouncedCallback setter

        React.useEffect (
            (fun () ->
                if inputRef.current.IsSome then
                    inputRef.current.Value.value <- inputValue),
            [| box inputValue |]
        )

        Html.div [
            prop.className "swt:grow"
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                Html.input [
                    prop.className "swt:input"
                    prop.type'.dateTimeLocal
                    prop.ref inputRef
                    prop.onChange (fun (dateValue: DateTime) ->
                        dateValue.ToString("yyyy-MM-ddTHH:mm") |> debouncedSetter)
                ]
            ]
        ]

    static member DateTimeInput(inputValue: DateTime, setter: DateTime -> unit, ?label: string) =
        FormComponents.DateTimeInput(
            inputValue.ToString("yyyy-MM-ddTHH:mm"),
            (fun value -> DateTime.Parse(value) |> setter),
            ?label = label
        )

    static member CommentInput
        (
            comment: Comment,
            setter: Comment -> unit,
            ?label: string,
            ?rmv: MouseEvent -> unit,
            ?keyValidator: string -> Result<unit, string>
        ) =
        Html.div [
            prop.children [
                if label.IsSome then
                    Html.label [ prop.className "swt:label"; prop.text label.Value ]
                Html.div [
                    prop.className "swt:flex swt:flex-row swt:gap-2 swt:relative"
                    prop.children [
                        FormComponents.TextInput(
                            comment.Name |> Option.defaultValue "",
                            (fun value ->
                                comment.Name <- (if value = "" then None else Some value)
                                setter comment),
                            placeholder = "comment name",
                            ?validator = keyValidator
                        )
                        FormComponents.TextInput(
                            comment.Value |> Option.defaultValue "",
                            (fun value ->
                                comment.Value <- (if value = "" then None else Some value)
                                setter comment),
                            placeholder = "comment"
                        )
                        if rmv.IsSome then
                            Helper.deleteButton rmv.Value
                    ]
                ]
            ]
        ]

    static member CommentsInput(comments: ResizeArray<Comment>, setter: ResizeArray<Comment> -> unit, ?label: string) =
        let keyValidator (name: string) =
            let isDuplicate =
                comments
                |> Seq.exists (fun comment -> comment.Name.IsSome && comment.Name.Value = name)

            if isDuplicate then
                Error "Comment names must be unique."
            else
                Ok()

        FormComponents.InputSequence(
            comments,
            Comment,
            setter,
            (fun (value, setValue, remove) ->
                FormComponents.CommentInput(value, setValue, rmv = remove, keyValidator = keyValidator)),
            ?label = label
        )

    static member CollectionOfStrings(values: ResizeArray<string>, setValues, ?label: string) =
        FormComponents.InputSequence(
            values,
            (fun () -> ""),
            setValues,
            (fun (value, setValue, remove) -> FormComponents.TextInput(value, setValue, rmv = remove)),
            ?label = label
        )

    [<ReactComponent>]
    static member PublicationInput(publication: Publication, setter: Publication -> unit, ?rmv: MouseEvent -> unit) =
        let title = Option.defaultValue "<title>" publication.Title
        let doi = Option.defaultValue "<doi>" publication.DOI

        let createFieldTextInput
            (field: string option, label: string, publicationSetter: string option -> unit)
            =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                (fun value ->
                    let value = if value = "" then None else Some value
                    publicationSetter value
                    setter publication),
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

            let total = fields.Length
            let filled = fields |> List.choose id |> List.length
            $"{filled}/{total}"

        Generic.Collapse
            [ Generic.CollapseTitle(title, doi, countFilledFieldsString ()) ]
            [
                createFieldTextInput (publication.Title, "Title", fun value -> publication.Title <- value)
                Helper.cardFormGroup [
                    createFieldTextInput (publication.PubMedID, "PubMed ID", fun value -> publication.PubMedID <- value)
                    createFieldTextInput (publication.DOI, "DOI", fun value -> publication.DOI <- value)
                ]
                createFieldTextInput (publication.Authors, "Authors", fun value -> publication.Authors <- value)
                FormComponents.OntologyAnnotationInput(
                    publication.Status,
                    (fun value ->
                        publication.Status <- value
                        setter publication),
                    "Status",
                    parent = TermCollection.PublicationStatus
                )
                FormComponents.CommentsInput(
                    publication.Comments,
                    (fun comments ->
                        publication.Comments <- ResizeArray comments
                        setter publication),
                    "Comments"
                )
                if rmv.IsSome then
                    Helper.deleteButton rmv.Value
            ]

    static member PublicationsInput
        (publications: ResizeArray<Publication>, setter: ResizeArray<Publication> -> unit, label: string)
        =
        FormComponents.InputSequence(
            publications,
            Publication,
            setter,
            (fun (value, setValue, remove) -> FormComponents.PublicationInput(value, setValue, rmv = remove)),
            label = label
        )

    [<ReactComponent>]
    static member OntologySourceReferenceInput
        (
            input: OntologySourceReference,
            setter: OntologySourceReference -> unit,
            ?deleteButton: MouseEvent -> unit
        ) =
        let name = Option.defaultValue "<name>" input.Name
        let version = Option.defaultValue "<version>" input.Version

        let toOptionalString (value: string) =
            if String.IsNullOrWhiteSpace value then None else Some value

        let createFieldTextInput
            (field: string option, label: string, setField: string option -> unit)
            =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                (fun value ->
                    setField (toOptionalString value)
                    setter input),
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

            let total = fields.Length
            let filled = fields |> List.choose id |> List.length
            $"{filled}/{total}"

        Generic.Collapse
            [ Generic.CollapseTitle(name, version, countFilledFieldsString ()) ]
            [
                createFieldTextInput (input.Name, "Name", fun value -> input.Name <- value)
                Helper.cardFormGroup [
                    createFieldTextInput (input.Version, "Version", fun value -> input.Version <- value)
                    createFieldTextInput (input.File, "File", fun value -> input.File <- value)
                ]
                FormComponents.TextInput(
                    Option.defaultValue "" input.Description,
                    (fun value ->
                        input.Description <- toOptionalString value
                        setter input),
                    "Description",
                    isArea = true
                )
                FormComponents.CommentsInput(
                    input.Comments,
                    (fun comments ->
                        input.Comments <- comments
                        setter input),
                    "Comments"
                )
                if deleteButton.IsSome then
                    Helper.deleteButton deleteButton.Value
            ]

    static member OntologySourceReferencesInput
        (
            input: ResizeArray<OntologySourceReference>,
            setter: ResizeArray<OntologySourceReference> -> unit,
            label: string
        ) =
        FormComponents.InputSequence(
            input,
            OntologySourceReference,
            setter,
            (fun (value, setValue, remove) ->
                FormComponents.OntologySourceReferenceInput(value, setValue, deleteButton = remove)),
            label = label
        )
