namespace ARCitISA.Components.Page.Metadata.FormComponents.PersonsInput

open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open ARCtrl

open ARCitISA.Components
open ARCitISA.Components.Primitive.LayoutComponents
open ARCitISA.Components.Page.Metadata.FormComponents
open ARCitISA.Components.Page.Metadata.FormComponents.PersonsInput.Types

module private PersonsInputTypes =

    [<RequireQualifiedAccess>]
    type AsyncState<'T> =
        | Idle
        | Loading
        | Ok of 'T
        | Error of exn

open PersonsInputTypes

module private PersonsInputHelper =

    let toOptionalString (value: string) = if value = "" then None else Some value

    let nameQuery (firstName: string) (lastName: string) = $"{firstName} {lastName}".Trim()

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

[<Erase; Mangle(false)>]
type PersonsInput =

    [<ReactComponent>]
    static member private PersonInput(input: Person, setter: Person -> unit, ?rmv: MouseEvent -> unit) =
        // Mode plus the ORCID and name queries, taken from the live field drafts when a search opens.
        let searchRequest, setSearchRequest =
            React.useState<(SearchMode * string * string) option> None

        let fieldRevision, setFieldRevision = React.useState 0
        let firstNameDraft = React.useRef (defaultArg input.FirstName "")
        let lastNameDraft = React.useRef (defaultArg input.LastName "")
        let orcidDraft = React.useRef (defaultArg input.ORCID "")

        let trackDraft (draft: IRefValue<string>) =
            fun (value: string) -> draft.current <- value

        let nameText =
            let firstName = Option.defaultValue "" input.FirstName
            let lastName = Option.defaultValue "" input.LastName
            let midInitials = Option.defaultValue "" input.MidInitials
            let fullName = $"{firstName} {midInitials} {lastName}".Trim()
            if fullName = "" then "<name>" else fullName

        let orcid = Option.defaultValue "<orcid>" input.ORCID

        let updatePersonField (value: string) (personSetter: Person -> string option -> unit) =
            let nextValue = PersonsInputHelper.toOptionalString value
            personSetter input nextValue
            setter input

        let createPersonFieldTextInput
            (field: string option, label: string, personSetter: Person -> string option -> unit)
            =
            TextInput.TextInput(
                field |> Option.defaultValue "",
                (fun value -> updatePersonField value personSetter),
                label
            )

        let createSearchableField
            (
                field: string option,
                label: string,
                personSetter: Person -> string option -> unit,
                mode: SearchMode,
                draft: IRefValue<string>
            ) =
            TextInput.TextInput(
                defaultArg field "",
                (fun value -> updatePersonField value personSetter),
                label,
                onDraftChange = trackDraft draft,
                trailingAction =
                    (fun value ->
                        let actionLabel =
                            if mode = SearchMode.Orcid then
                                "Search by ORCID"
                            else
                                "Search by name"

                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-square swt:btn-ghost swt:shrink-0"
                            prop.ariaLabel actionLabel
                            prop.title actionLabel
                            prop.onClick (fun _ ->
                                // value is this field's own draft. The other fields come from their tracked drafts.
                                let orcidQuery, nameQuery =
                                    if mode = SearchMode.Orcid then
                                        value,
                                        PersonsInputHelper.nameQuery firstNameDraft.current lastNameDraft.current
                                    else
                                        orcidDraft.current, PersonsInputHelper.nameQuery firstNameDraft.current value

                                setSearchRequest (Some(mode, orcidQuery, nameQuery))
                            )
                            prop.children [
                                Html.span [
                                    prop.ariaHidden true
                                    prop.className "swt:iconify swt:fluent--search-20-regular swt:size-5"
                                ]
                            ]
                        ]
                    )
            )

        LayoutComponents.Collapse [
            LayoutComponents.CollapseTitle(nameText, orcid, PersonsInputHelper.countFilledFieldsString input)
        ] [
            match searchRequest with
            | Some(mode, orcidQuery, nameQuery) ->
                PersonSearchDialog.PersonSearchDialog(
                    orcidQuery,
                    nameQuery,
                    mode,
                    (fun result ->
                        setter (Lookup.mergePerson input result)
                        setFieldRevision (fieldRevision + 1)
                    ),
                    (fun () -> setSearchRequest None)
                )
            | None -> ()
            Html.div [
                prop.key fieldRevision
                // Collapse spaces only its direct children, so this wrapper spaces the fields itself.
                prop.className "swt:space-y-4"
                prop.children [
                    Helpers.cardFormGroup [
                        TextInput.TextInput(
                            defaultArg input.FirstName "",
                            (fun value -> updatePersonField value (fun person value -> person.FirstName <- value)),
                            "First Name",
                            onDraftChange = trackDraft firstNameDraft
                        )
                        createSearchableField (
                            input.LastName,
                            "Last Name",
                            (fun person value -> person.LastName <- value),
                            SearchMode.Name,
                            lastNameDraft
                        )
                    ]
                    Helpers.cardFormGroup [
                        createPersonFieldTextInput (
                            input.MidInitials,
                            "Mid Initials",
                            fun person value -> person.MidInitials <- value
                        )
                        createSearchableField (
                            input.ORCID,
                            "ORCID",
                            (fun person value -> person.ORCID <- value),
                            SearchMode.Orcid,
                            orcidDraft
                        )
                    ]
                    Helpers.cardFormGroup [
                        createPersonFieldTextInput (
                            input.Affiliation,
                            "Affiliation",
                            fun person value -> person.Affiliation <- value
                        )
                        createPersonFieldTextInput (
                            input.Address,
                            "Address",
                            fun person value -> person.Address <- value
                        )
                    ]
                    createPersonFieldTextInput (input.EMail, "Email", fun person value -> person.EMail <- value)
                    Helpers.cardFormGroup [
                        createPersonFieldTextInput (input.Phone, "Phone", fun person value -> person.Phone <- value)
                        createPersonFieldTextInput (input.Fax, "Fax", fun person value -> person.Fax <- value)
                    ]
                    OntologyAnnotationInput.OntologyAnnotationsInput(
                        input.Roles,
                        (fun roles ->
                            input.Roles <- roles
                            setter input
                        ),
                        "Roles",
                        parent = TermCollection.PersonRoleWithinExperiment
                    )
                ]
            ]
            if rmv.IsSome then
                Helpers.deleteButton rmv.Value
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
            | None -> setImportState (AsyncState.Error(exn "No import callback configured."))
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
                                Html.span [
                                    prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                                ]
                            | AsyncState.Error err ->
                                Html.span [
                                    prop.className "swt:text-error swt:text-sm"
                                    prop.text err.Message
                                ]
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

        InputSequence.InputSequence(
            persons,
            Person,
            setter,
            (fun (value, setValue, remove) -> PersonsInput.PersonInput(value, setValue, remove)),
            ?label = label,
            ?extendedElements = extendedElements
        )
