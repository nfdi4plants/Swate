namespace Components.Forms

open System
open Feliz
open Feliz.DaisyUI

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl
open Fetch
open ARCtrl.Json
open Swate.Components
open Components
open Swate.Components.Shared

module private API =

    module Null =
        let defaultValue (def: 'A) (x: 'A) = if isNull x then def else x

    let requestAsJson (url) = promise {
        let! response = fetch url [ requestHeaders [ Accept "application/json" ] ]
        let! json = response.json ()
        return Some json
    }

    let private createAuthorString (authors: Person[]) =
        authors
        |> Array.map (fun x -> $"{x.FirstName} {x.LastName}")
        |> String.concat ", "

    let private createAffiliationString (org_department: (string * string)[]) : string option =
        if org_department.Length = 0 then
            None
        else
            org_department
            |> Array.map (fun (org, department) -> $"{org}, {department}")
            |> String.concat ";"
            |> Some

    let requestByORCID (orcid: string) =
        let url = $"https://pub.orcid.org/v3.0/{orcid}/record"

        promise {
            let! json = requestAsJson url
            let name: string = json?person?name?("given-names")?value
            let lastName: string = json?person?name?("family-name")?value
            let emails: obj[] = json?person?emails?email
            let email = if emails.Length = 0 then None else Some(emails.[0]?email)
            let groups: obj[] = json?("activities-summary")?employments?("affiliation-group")

            let groupsParsed =
                groups
                |> Array.choose (fun json ->
                    let summaries: obj[] = json?summaries

                    let summary =
                        summaries
                        |> Array.tryHead
                        |> Option.map (fun s0 ->
                            let s = s0?("employment-summary")
                            let department = s?("department-name") |> Null.defaultValue ""
                            let org = s?organization?name |> Null.defaultValue ""
                            org, department)

                    summary)
                |> createAffiliationString

            let person =
                Person.create (
                    orcid = orcid,
                    lastName = lastName,
                    firstName = name,
                    ?email = email,
                    ?affiliation = groupsParsed
                )

            return person
        }


    let requestByPubMedID (id: string) =
        let url = @"https://api.ncbi.nlm.nih.gov/lit/ctxp/v1/pubmed/?format=csl&id=" + id

        promise {
            let! json = requestAsJson url
            let doi: string = json?DOI
            let pmid: string = json?PMID

            let authors: Person[] = [|
                for pj in json?author do
                    Person.create (LastName = pj?family, FirstName = pj?given)
            |]

            let authorString = createAuthorString authors
            let title = json?title

            let publication =
                Publication.create (pmid, doi, authorString, title, TermCollection.Published)

            return publication
        }

    let requestByDOI_FromPubMed (doi: string) =
        /// https://academia.stackexchange.com/questions/67103/is-there-any-api-service-to-retrieve-abstract-of-a-journal-article
        let url_pubmed =
            $"https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed&term={doi}&retmode=JSON"

        promise {
            let! json = requestAsJson url_pubmed
            let errorList = json?esearchresult?errorlist

            if isNull errorList then
                let idList: string[] = json?esearchresult?idlist
                let pubmedID = if idList.Length <> 1 then None else Some idList.[0]
                return pubmedID
            else
                return None
        }

    let requestByDOI (doi: string) =
        let url_crossref = $"https://api.crossref.org/works/{doi}"

        promise {
            let! json = requestAsJson url_crossref
            let titles: string[] = json?message?title
            let title = if titles.Length = 0 then None else Some titles.[0]

            let authors: Person[] = [|
                for pj in json?message?author do
                    let affiliationsJson: obj[] = pj?affiliation

                    let affiliations: string[] = [|
                        for aff in affiliationsJson do
                            yield aff?("name")
                    |]

                    let affString = affiliations |> String.concat ", "

                    Person.create (
                        ORCID = pj?ORCID,
                        LastName = pj?family,
                        FirstName = pj?given,
                        affiliation = affString
                    )
            |]

            let! pubmedId = requestByDOI_FromPubMed doi
            let authorString = createAuthorString authors

            let publication =
                Publication.create (
                    ?pubMedID = pubmedId,
                    doi = doi,
                    authors = authorString,
                    ?title = title,
                    status = TermCollection.Published
                )

            return publication
        }

    let start (call: 't -> Fable.Core.JS.Promise<'a>) (args: 't) (success) (fail) =
        call args |> Promise.either success fail |> Promise.start

module private Helper =

    let addButton (clickEvent: MouseEvent -> unit) =
        Daisy.button.button [
            prop.text "+"
            button.wide
            prop.onClick clickEvent
            prop.className "btn-accent btn-outline"
        ]

    let deleteButton (clickEvent: MouseEvent -> unit) =
        Html.div [
            prop.className "grow-0"
            prop.children [
                Daisy.button.button [ button.error; prop.text "Delete"; prop.onClick clickEvent ]
            ]
        ]

    let readOnlyFormElement (v: string option, label: string) =
        let v = defaultArg v "-"

        Daisy.formControl [
            prop.children [
                Daisy.label [ Daisy.labelText label ]
                Daisy.input [
                    input.bordered
                    prop.disabled true
                    prop.readOnly true
                    prop.valueOrDefault v
                ]
            ]
        ]

    let cardFormGroup (content: ReactElement list) =
        Html.div [
            prop.className "grid @md/main:grid-cols-2 @xl/main:grid-flow-col gap-4 not-prose"
            prop.children content
        ]

    let personModal (person: Person, confirm, back) =
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop []
                Daisy.modalBox.div [
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
                        prop.className "flex justify-end gap-4"
                        prop.style [ style.gap (length.rem 1) ]
                        prop.children [
                            Daisy.button.button [ prop.text "back"; button.outline; prop.onClick back ]
                            Daisy.button.button [ button.success; prop.text "confirm"; prop.onClick confirm ]
                        ]
                    ]
                ]
            ]
        ]

    let PersonsModal (existingPersons: ResizeArray<Person>, externalPersons: Person[], select: Person -> unit, back) =
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop []
                Daisy.modalBox.div [
                    prop.className "max-h-[80%] overflow-y-hidden flex flex-col space-y-2"
                    prop.children [
                        Html.div [
                            prop.className "space-y-2 overflow-y-auto max-h-fit overflow-x-auto"
                            prop.children [
                                Html.table [
                                    prop.className "table"
                                    prop.children [
                                        Html.thead [
                                            Html.tr [
                                                Html.th [] // Select
                                                Html.th "Name"
                                                Html.th "Affiliation"
                                                Html.th "Orcid"
                                                Html.th "Address"
                                                Html.th "Contact"
                                                Html.th "Roles"
                                                Html.th "Comments"
                                            ]
                                        ]
                                        Html.tbody [
                                            for person in externalPersons do
                                                let isSelected =
                                                    existingPersons |> Seq.exists (fun x -> x.Equals person)

                                                Html.tr [
                                                    Html.td [
                                                        Html.button [
                                                            prop.className "btn btn-primary"
                                                            prop.disabled isSelected
                                                            prop.text "Add"
                                                            prop.onClick (fun _ ->
                                                                if not isSelected then
                                                                    select person)
                                                        ]
                                                    ]
                                                    Html.td [
                                                        prop.className "no-wrap"
                                                        prop.text (
                                                            [ person.FirstName; person.MidInitials; person.LastName ]
                                                            |> List.choose id
                                                            |> String.concat " "
                                                        )
                                                    ]
                                                    Html.td (person.Affiliation |> Option.defaultValue "")
                                                    Html.td (person.ORCID |> Option.defaultValue "")
                                                    Html.td (person.Address |> Option.defaultValue "")
                                                    Html.td (
                                                        [ person.EMail; person.Phone; person.Fax ]
                                                        |> List.choose id
                                                        |> String.concat "; "
                                                    )
                                                    Html.td [
                                                        prop.title (
                                                            person.Roles
                                                            |> Seq.map _.ToJsonString()
                                                            |> String.concat "; "
                                                        )
                                                        prop.text (
                                                            person.Roles |> Seq.map _.NameText |> String.concat "; "
                                                        )
                                                    ]
                                                    Html.td (
                                                        person.Comments
                                                        |> Seq.map _.toJsonString()
                                                        |> String.concat "; "
                                                    )
                                                ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "flex justify-end gap-4"
                            prop.style [ style.gap (length.rem 1) ]
                            prop.children [
                                Daisy.button.button [ prop.text "back"; button.outline; prop.onClick back ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let publicationModal (pub: Publication, confirm, back) =
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop []
                Daisy.modalBox.form [
                    Html.div [ readOnlyFormElement (pub.Title, "Title") ]
                    Html.div [
                        readOnlyFormElement (pub.DOI, "DOI")
                        readOnlyFormElement (pub.PubMedID, "PubMedID")
                    ]
                    Html.div [ readOnlyFormElement (pub.Authors, "Authors") ]
                    Html.div [ readOnlyFormElement (pub.Status |> Option.map _.ToString(), "Status") ]
                    Html.div [
                        prop.className "is-flex is-justify-content-flex-end"
                        prop.style [ style.gap (length.rem 1) ]
                        prop.children [
                            Daisy.button.button [ prop.text "back"; prop.onClick back ]
                            Daisy.button.button [ button.success; prop.text "confirm"; prop.onClick confirm ]
                        ]
                    ]
                ]
            ]
        ]

    let errorModal (error: exn, back) =
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick back ]
                Daisy.modalBox.div [
                    prop.className "bg-transparent p-0 border-0"
                    prop.children [
                        Daisy.alert [
                            alert.error
                            prop.children [
                                Components.DeleteButton(props = [| prop.onClick back |])
                                Html.div error.Message
                            ]
                        ]
                    ]
                ]
            ]
        ]

open JsBindings

type FormComponents =

    [<ReactComponent>]
    static member InputSequenceElement(key: string, id: string, listComponent: ReactElement) =
        let sortable = JsBindings.DndKit.useSortable ({| id = id |})

        let style = {|
            transform = DndKit.CSS.Transform.toString (sortable.transform)
            transition = sortable.transition
        |}

        Html.div [
            prop.ref sortable.setNodeRef
            prop.id id
            for attr in Object.keys sortable.attributes do
                prop.custom (attr, sortable.attributes.get attr)
            prop.className "flex flex-row gap-2"
            prop.custom ("style", style)
            prop.children [
                Html.span [
                    for listener in Object.keys sortable.listeners do
                        prop.custom (listener, sortable.listeners.get listener)
                    prop.className "cursor-grab flex items-center"
                    prop.children [ Html.i [ prop.className "fa-solid fa-arrows-up-down fa-lg" ] ]
                ]
                Html.div [ prop.className "grow"; prop.children listComponent ]
            ]
        ]

    /// <summary>
    /// A rather complicated function. A generic list container for form components.
    ///
    /// Uses dnd-kit to allow drag and drop reordering of the list.
    /// </summary>
    /// <param name="inputs"></param>
    /// <param name="constructor"></param>
    /// <param name="setter"></param>
    /// <param name="inputComponent"></param>
    /// <param name="label"></param>
    [<ReactComponent>]
    static member InputSequence<'A>
        (
            inputs: ResizeArray<'A>,
            constructor: unit -> 'A,
            setter: ResizeArray<'A> -> unit,
            inputComponent: 'A * ('A -> unit) * (MouseEvent -> unit) -> ReactElement,
            inputEquality: 'A -> 'A -> bool,
            ?label: string,
            ?extendedElements: ReactElement
        ) =
        // dnd-kit requires an id for each element in the list.
        // The id is used to keep track of the order of the elements in the list.
        // Because most of our classes do not have a unique id, we generate a new guid for each element in the list.
        let sensors = DndKit.useSensors [| DndKit.useSensor (DndKit.PointerSensor) |]

        /// This is a list of guids that are used to keep track of the order of the elements in the list.
        /// We use "React.useMemo" to keep the guids stable unless items are added/removed or reorder happens.
        /// Without this children would be rerendered on every change (e.g. expanded publications close on publication change).
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
        let getIndexFromId (id: string) = guids.FindIndex(fun x -> x = Guid(id))

        let handleDragEnd =
            fun (event: DndKit.IDndKitEvent) ->
                let active = event.active
                let over = event.over

                if (active.id <> over.id) then
                    let oldIndex = getIndexFromId (active.id)
                    let newIndex = getIndexFromId (over.id)
                    DndKit.arrayMove (inputs, oldIndex, newIndex) |> setter

                ()

        let equalityFunc
            (props1: 'A * ('A -> unit) * (MouseEvent -> unit))
            (props2: 'A * ('A -> unit) * (MouseEvent -> unit))
            =
            let (a1, _, _) = props1
            let (a2, _, _) = props2
            inputEquality a1 a2
        // let memoizeInputs = React.memo (
        //     inputComponent,
        //     areEqual = equalityFunc
        // )
        Html.div [
            prop.className "space-y-2"
            prop.children [
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
                                    prop.className "space-y-2"
                                    prop.children [
                                        for i in 0 .. (inputs.Count - 1) do
                                            let item = inputs.[i]
                                            let id = mkId i

                                            FormComponents.InputSequenceElement(
                                                id,
                                                id,
                                                (inputComponent (
                                                    item,
                                                    (fun v ->
                                                        inputs.[i] <- v
                                                        inputs |> setter),
                                                    (fun _ ->
                                                        inputs.RemoveAt i
                                                        inputs |> setter)
                                                ))
                                            )
                                    ]
                                ]
                        )
                )
                Html.div [
                    prop.className "flex justify-center w-full mt-2"
                    prop.children [
                        Helper.addButton (fun _ ->
                            inputs.Add(constructor ())
                            inputs |> setter)
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
            ?validator: {| fn: string -> bool; msg: string |},
            ?placeholder: string,
            ?isarea: bool,
            ?isJoin,
            ?disabled
        ) =
        let disabled = defaultArg disabled false
        let isJoin = defaultArg isJoin false
        let loading, setLoading = React.useState (false)
        let isValid, setIsValid = React.useState (true)
        let ref = React.useInputRef ()
        let debounceSetter = React.useDebouncedCallback (setValue)

        React.useEffect (
            (fun () ->
                if ref.current.IsSome then
                    setLoading false
                    setIsValid true
                    ref.current.Value.value <- value),
            [| box value |]
        )

        let onChange =
            fun (e: string) ->
                if validator.IsSome then
                    let isValid = validator.Value.fn e
                    setIsValid isValid

                    if isValid then
                        setLoading true
                        debounceSetter e
                else
                    setLoading true
                    debounceSetter e

        Html.div [
            prop.className "grow not-prose"
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                Daisy.label [
                    prop.className [
                        if isarea.IsSome && isarea.Value then
                            "textarea"
                        else
                            "input"
                        "flex items-center gap-2"
                        if isJoin then
                            "join-item"
                    ]
                    prop.children [
                        match isarea with
                        | Some true ->
                            Daisy.textarea [
                                prop.disabled disabled
                                prop.readOnly disabled
                                prop.className "grow ghost"
                                if placeholder.IsSome then
                                    prop.placeholder placeholder.Value
                                prop.ref ref
                                prop.onChange onChange
                            ]
                        | _ ->
                            Html.input [
                                prop.disabled disabled
                                prop.readOnly disabled
                                prop.className "truncate w-full"
                                if placeholder.IsSome then
                                    prop.placeholder placeholder.Value
                                prop.ref ref
                                prop.onChange onChange
                            ]
                        Daisy.loading [
                            if not loading then
                                prop.className "invisible"
                        ]
                    ]
                ]
                if not isValid then
                    let txt = validator |> Option.map _.msg |> Option.defaultValue "Invalid input."
                    Html.p [ prop.className "text-error text-sm mt-1"; prop.text txt ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationInput
        (
            input: OntologyAnnotation option,
            setter: OntologyAnnotation option -> unit,
            ?label: string,
            ?parent: OntologyAnnotation,
            ?rmv: MouseEvent -> unit
        ) =
        let portal = React.useElementRef ()
        let renderer = fun _ c -> React.fragment [ c ]

        let portalObj =
            portal.current |> Option.map (fun p -> PortalTermDropdown(p, renderer))

        Html.div [
            prop.className "space-y-2"
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                Html.div [
                    prop.ref portal
                    prop.className "w-full flex gap-2 relative"
                    prop.children [
                        TermSearch.TermSearch(
                            (fun term -> term |> Option.map OntologyAnnotation.fromTerm |> setter),
                            (input |> Option.map _.ToTerm()),
                            ?parentId = (parent |> Option.map _.TermAccessionShort),
                            ?portalTermDropdown = portalObj,
                            showDetails = true,
                            advancedSearch = !^true,
                            fullwidth = true
                        )
                        if rmv.IsSome then
                            Helper.deleteButton rmv.Value
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationsInput
        (oas: ResizeArray<OntologyAnnotation>, setter: ResizeArray<OntologyAnnotation> -> unit, ?label: string, ?parent)
        =
        FormComponents.InputSequence(
            oas,
            OntologyAnnotation.empty,
            setter,
            (fun (v, setV, rmv) ->
                FormComponents.OntologyAnnotationInput(
                    Some v,
                    (fun t -> t |> Option.defaultValue (OntologyAnnotation.empty ()) |> setV),
                    ?parent = parent,
                    rmv = rmv
                )),
            (fun oa1 oa2 -> oa1.Equals oa2),
            ?label = label
        )

    [<ReactComponent>]
    static member PersonRequestInput(orcid: string option, doisetter, searchsetter: Person -> unit, ?label: string) =
        let orcid = defaultArg orcid ""
        let state, setState = React.useState (GenericApiState<Person>.Idle)
        let resetState = fun _ -> setState GenericApiState.Idle

        Html.div [
            prop.className "grow cursor-auto"
            prop.children [
                match state with
                | GenericApiState.Ok p ->
                    Helper.personModal (
                        p,
                        (fun _ ->
                            searchsetter p
                            resetState ()),
                        resetState
                    )
                | GenericApiState.Error e -> Helper.errorModal (e, resetState)
                | GenericApiState.Loading -> Modals.Loading.Modal(rmv = resetState)
                | _ -> Html.none
                if label.IsSome then
                    Generic.FieldTitle label.Value
                Daisy.join [
                    prop.className "w-full"
                    prop.children [
                        FormComponents.TextInput(orcid, doisetter, placeholder = "xxxx-xxxx-xxxx-xxxx", isJoin = true)
                        Daisy.button.button [
                            join.item
                            button.info
                            prop.text "Search"
                            prop.onClick (fun _ ->
                                setState GenericApiState.Loading
                                // setState <| API.Request.Error (new Exception("Not implemented"))
                                // setState <| (API.Request.Ok (Person.create(orcid=orcid,firstName="John",lastName="Doe")))
                                API.start
                                    API.requestByORCID
                                    orcid
                                    (GenericApiState.Ok >> setState)
                                    (GenericApiState.Error >> setState))
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member PersonInput(input: Person, setter: Person -> unit, ?rmv: MouseEvent -> unit) =
        let nameStr =
            let fn = Option.defaultValue "" input.FirstName
            let ln = Option.defaultValue "" input.LastName
            let mi = Option.defaultValue "" input.MidInitials
            let x = $"{fn} {mi} {ln}".Trim()
            if x = "" then "<name>" else x

        let orcid = Option.defaultValue "<orcid>" input.ORCID

        let updatePersonField =
            fun s personSetter input ->
                let s = if s = "" then None else Some s
                personSetter input s
                input |> setter

        let createPersonFieldTextInput (field: string option, label, personSetter: Person -> string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                (fun s -> updatePersonField s personSetter input),
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
                if person.Roles.Count > 0 then Some "roles" else None // just for count. Value does not matter
            ]

            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"

        Generic.Collapse [ // title
            Generic.CollapseTitle(nameStr, orcid, countFilledFieldsString input)
        ] [ // content
            Helper.cardFormGroup [
                createPersonFieldTextInput (input.FirstName, "First Name", fun input s -> input.FirstName <- s)
                createPersonFieldTextInput (input.LastName, "Last Name", fun input s -> input.LastName <- s)
            ]
            Helper.cardFormGroup [
                createPersonFieldTextInput (input.MidInitials, "Mid Initials", fun input s -> input.MidInitials <- s)
                FormComponents.PersonRequestInput(
                    input.ORCID,
                    (fun s ->
                        let s = if s = "" then None else Some s
                        input.ORCID <- s
                        input |> setter),
                    (fun s -> setter s),
                    "ORCID"
                )
            ]
            Helper.cardFormGroup [
                createPersonFieldTextInput (input.Affiliation, "Affiliation", fun input s -> input.Affiliation <- s)
                createPersonFieldTextInput (input.Address, "Address", fun input s -> input.Address <- s)
            ]
            createPersonFieldTextInput (input.EMail, "Email", fun input s -> input.EMail <- s)
            Helper.cardFormGroup [
                createPersonFieldTextInput (input.Phone, "Phone", fun input s -> input.Phone <- s)
                createPersonFieldTextInput (input.Fax, "Fax", fun input s -> input.Fax <- s)
            ]
            FormComponents.OntologyAnnotationsInput(
                input.Roles,
                (fun oas ->
                    input.Roles <- oas
                    input |> setter),
                "Roles",
                parent = TermCollection.PersonRoleWithinExperiment
            )
            if rmv.IsSome then
                Helper.deleteButton rmv.Value
        ]

    [<ReactComponent>]
    static member PersonsInput
        (persons: ResizeArray<Person>, setter: ResizeArray<Person> -> unit, ?isARCitect: bool, ?label: string)
        =
        let isARCitect = defaultArg isARCitect false

        let (externalPersons: GenericApiState<Person[]>), setExternalPersons =
            React.useState (GenericApiState.Idle)

        let extendedElements =
            match isARCitect with
            | true ->
                React.fragment [
                    Html.div [
                        prop.className "flex justify-center"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-primary btn-wide"
                                prop.text "Import Persons"
                                prop.onClick (fun _ ->
                                    promise {
                                        setExternalPersons GenericApiState.Loading
                                        let! personsJson = Model.ARCitect.api.RequestPersons()

                                        let persons =
                                            personsJson
                                            |> Array.map ARCtrl.Person.fromJsonString
                                            |> Array.sortBy _.LastName

                                        GenericApiState.Ok persons |> setExternalPersons
                                    }
                                    |> Promise.catch (fun e -> GenericApiState.Error e |> setExternalPersons)
                                    |> Promise.start)
                            ]
                        ]
                    ]
                    match externalPersons with
                    | GenericApiState.Idle -> Html.none
                    | GenericApiState.Error e ->
                        Helper.errorModal (e, (fun _ -> setExternalPersons GenericApiState.Idle))
                    | GenericApiState.Loading ->
                        Modals.Loading.Modal(rmv = (fun _ -> setExternalPersons GenericApiState.Idle))
                    | GenericApiState.Ok externalPersons ->
                        Helper.PersonsModal(
                            persons,
                            externalPersons,
                            (fun person ->
                                persons.Add(person)
                                persons |> setter),
                            (fun _ -> setExternalPersons GenericApiState.Idle)
                        )
                ]
                |> Some
            | false -> None

        FormComponents.InputSequence(
            persons,
            Person,
            setter,
            (fun (v, setV, rmv) -> FormComponents.PersonInput(v, setV, rmv)),
            (fun person1 person2 -> person1.Equals person2),
            ?label = label,
            ?extendedElements = extendedElements
        )


    [<ReactComponent>]
    static member DateTimeInput(input_: string, setter: string -> unit, ?label: string) =
        let ref = React.useInputRef ()
        let debounceSetter = React.useDebouncedCallback (fun s -> setter s)

        React.useEffect (
            (fun () ->
                if ref.current.IsSome then
                    ref.current.Value.value <- input_),
            [| box input_ |]
        )

        let onChange = fun (e: string) -> debounceSetter e

        Html.div [
            prop.className "grow"
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                Daisy.input [
                    input.bordered
                    prop.type'.dateTimeLocal
                    prop.ref ref
                    prop.onChange (fun (e: System.DateTime) ->
                        let dtString = e.ToString("yyyy-MM-ddTHH:mm")
                        onChange dtString)
                ]
            ]
        ]

    static member DateTimeInput(input: System.DateTime, setter: System.DateTime -> unit, ?label: string) =
        FormComponents.DateTimeInput(
            input.ToString("yyyy-MM-ddTHH:mm"),
            (fun (s: string) -> setter (System.DateTime.Parse(s))),
            ?label = label
        )

    static member GUIDInput(input: Guid, setter: Guid -> unit, ?label: string) =
        //let regex = System.Text.RegularExpressions.Regex(@"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$")
        //let unmask (s:string) = s |> String.filter(fun c -> c <> '_' && c <> '-')
        //let mask (s:string) =
        //    s.PadRight(32,'_').[0..31]
        //    |> fun padded -> sprintf "%s-%s-%s-%s-%s" padded.[0..7] padded.[8..11] padded.[12..15] padded.[16..19] padded.[20..31]
        FormComponents.TextInput(
            input.ToString(),
            (fun s -> System.Guid.Parse s |> setter),
            ?label = label,
            validator = {|
                fn = Guid.TryParse >> fst
                msg =
                    "Guid should contain 32 digits with 4 dashes following: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx. Using numbers 0 through 9 and letters A through F."
            |}
        )

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
        let state, setState = React.useState (GenericApiState<Publication>.Idle)
        let resetState = fun _ -> setState GenericApiState.Idle

        Html.div [
            prop.className "grow"
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                //if state.IsSome || error.IsSome then
                match state with
                | GenericApiState.Ok pub ->
                    Helper.publicationModal (
                        pub,
                        (fun _ ->
                            searchsetter pub
                            resetState ()),
                        resetState
                    )
                | GenericApiState.Error e -> Helper.errorModal (e, resetState)
                | GenericApiState.Loading -> Modals.Loading.Modal(rmv = resetState)
                | _ -> Html.none
                Daisy.join [
                    prop.className "w-full"
                    prop.children [
                        FormComponents.TextInput(id, doisetter, isJoin = true)
                        Daisy.button.button [
                            button.info
                            join.item
                            prop.text "Search"
                            prop.onClick (fun _ ->
                                setState GenericApiState.Loading

                                API.start
                                    searchAPI
                                    id
                                    (GenericApiState.Ok >> setState)
                                    (GenericApiState.Error >> setState))
                        ]
                    ]
                ]
            ]
        ]

    static member DOIInput(id: string option, doisetter, searchsetter: Publication -> unit, ?label: string) =
        FormComponents.PublicationRequestInput(
            id,
            API.requestByDOI, //"10.3390/ijms24087444"//"10.3390/ijms2408741d"//
            doisetter,
            searchsetter,
            ?label = label
        )

    static member PubMedIDInput(id: string option, doisetter, searchsetter: Publication -> unit, ?label: string) =
        FormComponents.PublicationRequestInput(id, API.requestByPubMedID, doisetter, searchsetter, ?label = label)

    static member CommentInput(comment: Comment, setter: Comment -> unit, ?label: string, ?rmv: MouseEvent -> unit) =
        Html.div [
            prop.children [
                if label.IsSome then
                    Daisy.label [ Daisy.labelText label.Value ]
                Html.div [
                    prop.className "flex flex-row gap-2 relative"
                    prop.children [
                        FormComponents.TextInput(
                            comment.Name |> Option.defaultValue "",
                            (fun s ->
                                comment.Name <- if s = "" then None else Some s
                                comment |> setter),
                            placeholder = "comment name"
                        )
                        FormComponents.TextInput(
                            comment.Value |> Option.defaultValue "",
                            (fun s ->
                                comment.Value <- if s = "" then None else Some s
                                comment |> setter),
                            placeholder = "comment"
                        )
                        if rmv.IsSome then
                            Helper.deleteButton rmv.Value
                    ]
                ]
            ]
        ]

    static member CommentsInput(comments: ResizeArray<Comment>, setter: ResizeArray<Comment> -> unit, ?label) =
        FormComponents.InputSequence(
            comments,
            Comment,
            setter,
            (fun (v, setV, rmv) -> FormComponents.CommentInput(v, setV, rmv = rmv)),
            (fun c1 c2 -> c1.Equals c2),
            ?label = label
        )

    [<ReactComponent>]
    static member PublicationInput(input: Publication, setter: Publication -> unit, ?rmv: MouseEvent -> unit) =
        let title = Option.defaultValue "<title>" input.Title
        let doi = Option.defaultValue "<doi>" input.DOI

        let createFieldTextInput (field: string option, label, publicationSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                (fun s ->
                    let s = if s = "" then None else Some s
                    publicationSetter s
                    input |> setter),
                label
            )

        let countFilledFieldsString () =
            let fields = [
                input.PubMedID
                input.DOI
                input.Title
                input.Authors
                input.Status |> Option.map (fun _ -> "")
            ]

            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"

        Generic.Collapse [ Generic.CollapseTitle(title, doi, countFilledFieldsString ()) ] [
            createFieldTextInput (input.Title, "Title", fun s -> input.Title <- s)
            Helper.cardFormGroup [
                FormComponents.PubMedIDInput(
                    input.PubMedID,
                    (fun s ->
                        let s = if s = "" then None else Some s
                        input.PubMedID <- s
                        input |> setter),
                    (fun pub -> setter pub),
                    "PubMed Id"
                )
                FormComponents.DOIInput(
                    input.DOI,
                    (fun s ->
                        let s = if s = "" then None else Some s
                        input.DOI <- s
                        input |> setter),
                    (fun pub -> setter pub),
                    "DOI"
                )
            ]
            createFieldTextInput (input.Authors, "Authors", fun s -> input.Authors <- s)
            FormComponents.OntologyAnnotationInput(
                input.Status,
                (fun s ->
                    input.Status <- s
                    input |> setter),
                "Status",
                parent = TermCollection.PublicationStatus
            )
            FormComponents.CommentsInput(
                input.Comments,
                (fun c ->
                    input.Comments <- ResizeArray(c)
                    input |> setter),
                "Comments"
            )
            if rmv.IsSome then
                Helper.deleteButton rmv.Value
        ]

    static member PublicationsInput
        (input: ResizeArray<Publication>, setter: ResizeArray<Publication> -> unit, label: string)
        =
        FormComponents.InputSequence(
            input,
            Publication,
            setter,
            (fun (a, b, c) -> FormComponents.PublicationInput(a, b, rmv = c)),
            (fun p1 p2 -> p1.Equals p2),
            label
        )

    [<ReactComponent>]
    static member OntologySourceReferenceInput
        (input: OntologySourceReference, setter: OntologySourceReference -> unit, ?deletebutton: MouseEvent -> unit)
        =
        let name = Option.defaultValue "<name>" input.Name
        let version = Option.defaultValue "<version>" input.Version

        let createFieldTextInput (field: string option, label, setFunction: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                (fun s ->
                    s |> Option.whereNot System.String.IsNullOrWhiteSpace |> setFunction
                    input |> setter),
                label
            )

        let countFilledFieldsString () =
            let fields = [
                input.Name
                input.File
                input.Version
                input.Description
                if input.Comments.Count > 0 then Some "comments" else None // just for count. Value does not matter
            ]

            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"

        Generic.Collapse [ Generic.CollapseTitle(name, version, countFilledFieldsString ()) ] [
            createFieldTextInput (input.Name, "Name", fun s -> input.Name <- s)
            Helper.cardFormGroup [
                createFieldTextInput (input.Version, "Version", fun s -> input.Version <- s)
                createFieldTextInput (input.File, "File", fun s -> input.File <- s)
            ]
            FormComponents.TextInput(
                Option.defaultValue "" input.Description,
                (fun s ->
                    input.Description <- s |> Option.whereNot System.String.IsNullOrWhiteSpace
                    input |> setter),
                "Description",
                isarea = true
            )
            FormComponents.CommentsInput(
                input.Comments,
                (fun c ->
                    input.Comments <- c
                    input |> setter),
                "Comments"
            )
            if deletebutton.IsSome then
                Helper.deleteButton deletebutton.Value
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
            (fun (a, b, c) -> FormComponents.OntologySourceReferenceInput(a, b, c)),
            (fun o1 o2 -> o1.Equals o2),
            label
        )