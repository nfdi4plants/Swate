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

module private API =

    [<RequireQualifiedAccess>]
    type Request<'A> =
    | Ok of 'A
    | Error of exn
    | Loading
    | Idle

    module Null =
        let defaultValue (def:'A) (x:'A) = if isNull x then def else x

    let requestAsJson (url) =
        promise {
            let! response = fetch url [
                requestHeaders [Accept "application/json"]
            ]
            let! json = response.json()
            return Some json
        }

    let private createAuthorString (authors: Person []) =
        authors |> Array.map (fun x -> $"{x.FirstName} {x.LastName}") |> String.concat ", "

    let private createAffiliationString (org_department: (string*string) []) : string option =
        if org_department.Length = 0 then
            None
        else
            org_department |> Array.map (fun (org,department) -> $"{org}, {department}") |> String.concat ";"
            |> Some

    let requestByORCID (orcid: string) =
        let url = $"https://pub.orcid.org/v3.0/{orcid}/record"
        promise {
            let! json = requestAsJson url
            let name: string = json?person?name?("given-names")?value
            let lastName: string = json?person?name?("family-name")?value
            let emails: obj [] = json?person?emails?email
            let email = if emails.Length = 0 then None else Some (emails.[0]?email)
            let groups: obj [] = json?("activities-summary")?employments?("affiliation-group")
            let groupsParsed =
                groups |> Array.choose (fun json ->
                    let summaries : obj [] = json?summaries
                    let summary =
                        summaries
                        |> Array.tryHead
                        |> Option.map (fun s0 ->
                            let s = s0?("employment-summary")
                            let department = s?("department-name") |> Null.defaultValue ""
                            let org = s?organization?name |> Null.defaultValue ""
                            org, department
                        )
                    summary
                )
                |> createAffiliationString
            let person = Person.create(orcid=orcid,lastName=lastName, firstName=name, ?email=email, ?affiliation=groupsParsed)
            return person
        }


    let requestByPubMedID (id: string) =
        let url = @"https://api.ncbi.nlm.nih.gov/lit/ctxp/v1/pubmed/?format=csl&id=" + id
        promise {
            let! json = requestAsJson url
            let doi: string = json?DOI
            let pmid: string = json?PMID
            let authors : Person [] =
                [|
                    for pj in json?author do
                        Person.create(LastName=pj?family, FirstName=pj?given)
                |]
            let authorString = createAuthorString authors
            let title = json?title
            let publication = Publication.create(pmid, doi, authorString, title, TermCollection.Published)
            return publication
        }

    let requestByDOI_FromPubMed (doi: string) =
        /// https://academia.stackexchange.com/questions/67103/is-there-any-api-service-to-retrieve-abstract-of-a-journal-article
        let url_pubmed = $"https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed&term={doi}&retmode=JSON"
        promise {
            let! json = requestAsJson url_pubmed
            let errorList = json?esearchresult?errorlist
            if isNull errorList then
                let idList : string [] = json?esearchresult?idlist
                let pubmedID = if idList.Length <> 1 then None else Some idList.[0]
                return pubmedID
            else
                return None
        }

    let requestByDOI (doi: string) =
        let url_crossref = $"https://api.crossref.org/works/{doi}"
        promise {
            let! json = requestAsJson url_crossref
            let titles: string [] = json?message?title
            let title = if titles.Length = 0 then None else Some titles.[0]
            let authors : Person [] = [|
                for pj in json?message?author do
                    let affiliationsJson: obj [] = pj?affiliation
                    let affiliations : string [] =
                        [|
                            for aff in affiliationsJson do
                                yield aff?("name")
                        |]
                    let affString = affiliations |> String.concat ", "
                    Person.create(ORCID=pj?ORCID, LastName=pj?family, FirstName=pj?given, affiliation=affString)
            |]
            let! pubmedId = requestByDOI_FromPubMed doi
            let authorString = createAuthorString authors
            let publication = Publication.create(?pubMedID=pubmedId, doi=doi, authors=authorString, ?title=title, status=TermCollection.Published)
            return publication
        }

    let start (call: 't -> Fable.Core.JS.Promise<'a>) (args:'t) (success) (fail) =
        call args
        |> Promise.either
            success
            fail
        |> Promise.start

module private Helper =

    let addButton (clickEvent: MouseEvent -> unit) =
        Daisy.button.button [
            prop.text "+"
            button.wide
            prop.onClick clickEvent
            button.ghost
            prop.className "text-accent"
        ]

    let deleteButton (clickEvent: MouseEvent -> unit) =
        Html.div [
            prop.className "grow-0"
            prop.children [
                Daisy.button.button [
                    button.error
                    prop.text "Delete"
                    prop.onClick clickEvent
                ]
            ]
        ]

    let readOnlyFormElement(v: string option, label: string) =
        let v = defaultArg v "-"
        Daisy.formControl [
            prop.children [
                Daisy.label [
                    Daisy.labelText label
                ]
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
                        readOnlyFormElement(person.FirstName, "Given Name")
                        readOnlyFormElement(person.LastName, "Family Name")
                    ]
                    cardFormGroup [
                        readOnlyFormElement(person.EMail, "Email")
                        readOnlyFormElement(person.ORCID, "ORCID")
                    ]
                    cardFormGroup [
                        readOnlyFormElement(person.Affiliation, "Affiliation")
                    ]
                    Html.div [
                        prop.className "flex justify-end gap-4"
                        prop.style [style.gap (length.rem 1)]
                        prop.children [
                            Daisy.button.button [
                                prop.text "back"
                                button.outline
                                prop.onClick back
                            ]
                            Daisy.button.button [
                                button.success
                                prop.text "confirm"
                                prop.onClick confirm
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
                    Html.div [
                        readOnlyFormElement(pub.Title, "Title")
                    ]
                    Html.div [
                        readOnlyFormElement(pub.DOI, "DOI")
                        readOnlyFormElement(pub.PubMedID, "PubMedID")
                    ]
                    Html.div [
                        readOnlyFormElement(pub.Authors, "Authors")
                    ]
                    Html.div [
                        readOnlyFormElement(pub.Status |> Option.map _.ToString(), "Status")
                    ]
                    Html.div [
                        prop.className "is-flex is-justify-content-flex-end"
                        prop.style [style.gap (length.rem 1)]
                        prop.children [
                            Daisy.button.button [
                                prop.text "back"
                                prop.onClick back
                            ]
                            Daisy.button.button [
                                button.success
                                prop.text "confirm"
                                prop.onClick confirm
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let errorModal (error: exn, back) =
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [prop.onClick back]
                Daisy.modalBox.div [
                    prop.className "bg-transparent p-0 border-0"
                    prop.children [
                        Daisy.alert [
                            alert.error
                            prop.children [
                                Components.DeleteButton(props=[|prop.onClick back|])
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
        let sortable = JsBindings.DndKit.useSortable({|id = id|})
        let style = {|
          transform = DndKit.CSS.Transform.toString(sortable.transform)
          transition = sortable.transition
        |}
        Html.div [
            prop.ref sortable.setNodeRef
            prop.id id
            for attr in Object.keys sortable.attributes do
                prop.custom(attr, sortable.attributes.get attr)
            prop.className "flex flex-row gap-2"
            prop.custom("style", style)
            prop.children [
                Html.span [
                    for listener in Object.keys sortable.listeners do
                        prop.custom(listener, sortable.listeners.get listener)
                    prop.className "cursor-grab flex items-center"
                    prop.children [
                        Html.i [ prop.className "fa-solid fa-arrows-up-down fa-lg" ]
                    ]
                ]
                Html.div [
                    prop.className "grow"
                    prop.children listComponent
                ]
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
    static member InputSequence<'A>(inputs: ResizeArray<'A>, constructor: unit -> 'A, setter: ResizeArray<'A> -> unit, inputComponent: 'A * ('A -> unit) * (MouseEvent -> unit) -> ReactElement, ?label: string) =
        // dnd-kit requires an id for each element in the list.
        // The id is used to keep track of the order of the elements in the list.
        // Because most of our classes do not have a unique id, we generate a new guid for each element in the list.
        let sensors = DndKit.useSensors [|
            DndKit.useSensor(DndKit.PointerSensor)
        |]
        /// This is guid is used to force a rerender of the list when the order of the elements changes.
        let OrderId = React.useRef (System.Guid.NewGuid())
        /// This is a list of guids that are used to keep track of the order of the elements in the list.
        /// We use "React.useMemo" to keep the guids stable unless items are added/removed or reorder happens.
        /// Without this children would be rerendered on every change (e.g. expanded publications close on publication change).
        let guids = React.useMemo (
            (fun () ->
                ResizeArray [for _ in inputs do Guid.NewGuid()]),
            [|box inputs.Count; box OrderId.current|]
        )
        let mkId index = guids.[index].ToString()
        let getIndexFromId (id:string) = guids.FindIndex (fun x -> x = Guid(id))
        let handleDragEnd = fun (event: DndKit.IDndKitEvent) ->
            let active = event.active
            let over = event.over
            if (active.id <> over.id) then
                let oldIndex = getIndexFromId (active.id)
                let newIndex = getIndexFromId (over.id)
                DndKit.arrayMove(inputs, oldIndex, newIndex)
                |> setter
                // trigger rerender
                OrderId.current <- System.Guid.NewGuid()
            ()
        Html.div [
            prop.children [
                if label.IsSome then
                    Generic.FieldTitle label.Value
                DndKit.DndContext(
                    sensors = sensors,
                    onDragEnd = handleDragEnd,
                    collisionDetection = DndKit.closestCenter,
                    children = [
                        DndKit.SortableContext(
                            items = guids,
                            strategy = DndKit.verticalListSortingStrategy,
                            children = ResizeArray [
                                Html.div [
                                    prop.className "space-y-2"
                                    prop.children [
                                        for i in 0 .. (inputs.Count-1) do
                                            let item = inputs.[i]
                                            let id = mkId i
                                            FormComponents.InputSequenceElement(
                                                id,
                                                id,
                                                (
                                                    inputComponent(
                                                        item,
                                                        (fun v ->
                                                            inputs.[i] <- v
                                                            inputs |> setter
                                                        ),
                                                        (fun _ ->
                                                            inputs.RemoveAt i
                                                            inputs |> setter
                                                        )
                                                    )
                                                )
                                            )
                                    ]
                                ]
                            ]
                        )
                    ]
                )
                Html.div [
                    prop.className "flex justify-center w-full mt-2"
                    prop.children [
                        Helper.addButton (fun _ ->
                            inputs.Add (constructor())
                            inputs |> setter
                        )
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member TextInput(value: string, setValue: string -> unit, ?label: string, ?validator: {| fn: string -> bool; msg: string |}, ?placeholder: string, ?isarea: bool, ?isJoin, ?disabled) =
        let disabled = defaultArg disabled false
        let isJoin = defaultArg isJoin false
        let loading, setLoading = React.useState(false)
        let isValid, setIsValid = React.useState(true)
        let ref = React.useInputRef()
        let debounceSetter = React.useDebouncedCallback(setValue)
        React.useEffect(
            (fun () ->
                if ref.current.IsSome then
                    setLoading false
                    setIsValid true
                    ref.current.Value.value <- value
            ),
            [|box value|]
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
                if label.IsSome then Generic.FieldTitle label.Value
                Daisy.label [
                    prop.className [
                        if isarea.IsSome && isarea.Value then
                            "textarea textarea-bordered"
                        else
                            "input input-bordered"
                        "flex items-center gap-2"
                        if isJoin then "join-item"
                    ]
                    prop.children [
                        match isarea with
                        | Some true ->
                            Daisy.textarea [
                                prop.disabled disabled
                                prop.readOnly disabled
                                prop.className "grow ghost"
                                if placeholder.IsSome then prop.placeholder placeholder.Value
                                prop.ref ref
                                prop.onChange onChange
                            ]
                        | _ ->
                            Html.input [
                                prop.disabled disabled
                                prop.readOnly disabled
                                prop.className "trunacte w-full"
                                if placeholder.IsSome then prop.placeholder placeholder.Value
                                prop.ref ref
                                prop.onChange onChange
                            ]
                        Daisy.loading [
                            if not loading then prop.className "invisible"
                        ]
                    ]
                ]
                if not isValid then
                    let txt = validator |> Option.map _.msg |> Option.defaultValue "Invalid input."
                    Html.p [
                        prop.className "text-error text-sm mt-1"
                        prop.text txt
                    ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationInput (input: OntologyAnnotation option, setter: OntologyAnnotation option -> unit, ?label: string, ?parent: OntologyAnnotation, ?rmv: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        let portal = React.useElementRef()
        Html.div [
            prop.className "space-y-2"
            prop.children [
                if label.IsSome then Generic.FieldTitle label.Value
                Html.div [
                    prop.ref portal
                    prop.className "w-full flex gap-2 relative"
                    prop.children [
                        TermSearch.TermSearch(
                            (fun term -> term |> Option.map OntologyAnnotation.fromTerm |> setter),
                            (input |> Option.map _.ToTerm()),
                            ?parentId = (parent |> Option.map _.TermAccessionShort),
                            portalTermSelectArea = portal,
                            showDetails = true,
                            advancedSearch = !^true,
                            fullwidth = true
                        )
                        Components.CollapseButton(isExtended, setIsExtended)
                        if rmv.IsSome then
                            Helper.deleteButton rmv.Value
                    ]
                ]
                if isExtended then
                    Html.div [
                        prop.className "flex flex-col @md/main:flex-row gap-2"
                        prop.children [
                            FormComponents.TextInput(
                                input|> (Option.bind _.TermSourceREF >> Option.defaultValue ""),
                                (fun (s: string) ->
                                    let s = s |> Option.whereNot String.IsNullOrWhiteSpace
                                    let input = input
                                    input |> Option.iter (fun x -> x.TermSourceREF <- s)
                                    input |> setter),
                                placeholder="term source ref"
                            )
                            FormComponents.TextInput(
                                input|> (Option.bind _.TermAccessionNumber >> Option.defaultValue ""),
                                (fun s0 ->
                                    let s = s0 |> Option.whereNot String.IsNullOrWhiteSpace
                                    input |> Option.iter (fun x -> x.TermAccessionNumber <- s)
                                    input |> setter
                                ),
                                placeholder="term accession number"
                            )
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationsInput (oas: ResizeArray<OntologyAnnotation>, setter: ResizeArray<OntologyAnnotation> -> unit, ?label: string, ?parent) =
        FormComponents.InputSequence(
            oas,
            OntologyAnnotation.empty,
            setter,
            (fun (v,setV,rmv) ->
                FormComponents.OntologyAnnotationInput(
                    Some v,
                    (fun t -> t |> Option.defaultValue (OntologyAnnotation.empty()) |> setV),
                    ?parent=parent,
                    rmv=rmv
                )
            ),
            ?label=label
        )

    [<ReactComponent>]
    static member PersonRequestInput (orcid: string option, doisetter, searchsetter: Person -> unit, ?label:string) =
        let orcid = defaultArg orcid ""
        let state, setState = React.useState(API.Request<Person>.Idle)
        let resetState = fun _ -> setState API.Request.Idle
        Html.div [
            prop.className "grow cursor-auto"
            prop.children [
                match state with
                | API.Request.Ok p -> Helper.personModal (p, (fun _ -> searchsetter p; resetState()), resetState)
                | API.Request.Error e -> Helper.errorModal(e, resetState)
                | API.Request.Loading -> Modals.Loading.Modal(rmv=resetState)
                | _ -> Html.none
                if label.IsSome then Generic.FieldTitle label.Value
                Daisy.join [
                    prop.className "w-full"
                    prop.children [
                        FormComponents.TextInput(
                            orcid,
                            doisetter,
                            placeholder="xxxx-xxxx-xxxx-xxxx",
                            isJoin=true
                        )
                        Daisy.button.button [
                            join.item
                            button.info
                            prop.text "Search"
                            prop.onClick (fun _ ->
                                setState API.Request.Loading
                                // setState <| API.Request.Error (new Exception("Not implemented"))
                                // setState <| (API.Request.Ok (Person.create(orcid=orcid,firstName="John",lastName="Doe")))
                                API.start
                                    API.requestByORCID
                                    orcid
                                    (API.Request.Ok >> setState)
                                    (API.Request.Error >> setState)
                            )
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member PersonInput(input: Person, setter: Person -> unit, ?rmv: MouseEvent -> unit) =
        log ("rerender", input)
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
        let createPersonFieldTextInput(field: string option, label, personSetter: Person -> string option -> unit) =
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
        Generic.Collapse
            // title
            [
                Generic.CollapseTitle(nameStr, orcid, countFilledFieldsString input)
            ]
            // content
            [
                Helper.cardFormGroup [
                    createPersonFieldTextInput(input.FirstName, "First Name", fun input s -> input.FirstName <- s)
                    createPersonFieldTextInput(input.LastName, "Last Name", fun input s -> input.LastName <- s)
                ]
                Helper.cardFormGroup [
                    createPersonFieldTextInput(input.MidInitials, "Mid Initials", fun input s -> input.MidInitials <- s)
                    FormComponents.PersonRequestInput(
                        input.ORCID,
                        (fun s ->
                            let s = if s = "" then None else Some s
                            input.ORCID <- s
                            input |> setter
                        ),
                        (fun s -> setter s),
                        "ORCID"
                    )
                ]
                Helper.cardFormGroup [
                    createPersonFieldTextInput(input.Affiliation, "Affiliation", fun input s -> input.Affiliation <- s)
                    createPersonFieldTextInput(input.Address, "Address", fun input s -> input.Address <- s)
                ]
                createPersonFieldTextInput(input.EMail, "Email", fun input s -> input.EMail <- s)
                Helper.cardFormGroup [
                    createPersonFieldTextInput(input.Phone, "Phone", fun input s -> input.Phone <- s)
                    createPersonFieldTextInput(input.Fax, "Fax", fun input s -> input.Fax <- s)
                ]
                FormComponents.OntologyAnnotationsInput(
                    input.Roles,
                    (fun oas ->
                        input.Roles <- oas
                        input |> setter
                    ),
                    "Roles",
                    parent=TermCollection.PersonRoleWithinExperiment
                )
                if rmv.IsSome then
                    Helper.deleteButton rmv.Value
            ]

    static member PersonsInput (persons: ResizeArray<Person>, setter: ResizeArray<Person> -> unit, ?label: string) =
        FormComponents.InputSequence(
            persons,
            Person,
            setter,
            (fun (v, setV, rmv) -> FormComponents.PersonInput(v, setV, rmv)),
            ?label=label
        )

    [<ReactComponent>]
    static member DateTimeInput (input_: string, setter: string -> unit, ?label: string) =
        let ref = React.useInputRef()
        let debounceSetter = React.useDebouncedCallback (fun s -> setter s)
        React.useEffect(
            (fun () ->
                if ref.current.IsSome then
                    ref.current.Value.value <- input_
            ),
            [|box input_|]
        )
        let onChange =
            fun (e: string) ->
                debounceSetter e
        Html.div [
            prop.className "grow"
            prop.children [
                if label.IsSome then Generic.FieldTitle label.Value
                Daisy.input [
                    input.bordered
                    prop.type'.dateTimeLocal
                    prop.ref ref
                    prop.onChange(fun (e: System.DateTime) ->
                        let dtString = e.ToString("yyyy-MM-ddTHH:mm")
                        onChange dtString
                    )
                ]
            ]
        ]

    static member DateTimeInput (input: System.DateTime, setter: System.DateTime -> unit, ?label: string) =
        FormComponents.DateTimeInput(
            input.ToString("yyyy-MM-ddTHH:mm"),
            (fun (s: string) ->
                setter (System.DateTime.Parse(s))),
            ?label=label
        )

    static member GUIDInput (input: Guid, setter: Guid -> unit, ?label: string) =
        //let regex = System.Text.RegularExpressions.Regex(@"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$")
        //let unmask (s:string) = s |> String.filter(fun c -> c <> '_' && c <> '-')
        //let mask (s:string) =
        //    s.PadRight(32,'_').[0..31]
        //    |> fun padded -> sprintf "%s-%s-%s-%s-%s" padded.[0..7] padded.[8..11] padded.[12..15] padded.[16..19] padded.[20..31]
        FormComponents.TextInput(
            input.ToString(),
            (fun s -> System.Guid.Parse s |> setter),
            ?label=label,
            validator={| fn = Guid.TryParse >> fst; msg = "Guid should contain 32 digits with 4 dashes following: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx. Using numbers 0 through 9 and letters A through F." |}
        )

    [<ReactComponent>]
    static member PublicationRequestInput (id: string option, searchAPI: string -> Fable.Core.JS.Promise<Publication>, doisetter, searchsetter: Publication -> unit, ?label:string) =
        let id = defaultArg id ""
        let state, setState = React.useState(API.Request<Publication>.Idle)
        let resetState = fun _ -> setState API.Request.Idle
        Html.div [
            prop.className "grow"
            prop.children [
                if label.IsSome then Generic.FieldTitle label.Value
                //if state.IsSome || error.IsSome then
                match state with
                | API.Request.Ok pub -> Helper.publicationModal(pub,(fun _ -> searchsetter pub; resetState()), resetState)
                | API.Request.Error e -> Helper.errorModal(e, resetState)
                | API.Request.Loading -> Modals.Loading.Modal(rmv=resetState)
                | _ -> Html.none
                Daisy.join [
                    prop.className "w-full"
                    prop.children [
                        FormComponents.TextInput(
                            id,
                            doisetter,
                            isJoin=true
                        )
                        Daisy.button.button [
                            button.info
                            join.item
                            prop.text "Search"
                            prop.onClick (fun _ ->
                                setState API.Request.Loading
                                API.start
                                    searchAPI
                                    id
                                    (API.Request.Ok >> setState)
                                    (API.Request.Error >> setState)
                            )
                        ]
                    ]
                ]
            ]
        ]

    static member DOIInput (id: string option, doisetter, searchsetter: Publication -> unit, ?label:string) =
        FormComponents.PublicationRequestInput(
            id,
            API.requestByDOI,//"10.3390/ijms24087444"//"10.3390/ijms2408741d"//
            doisetter,
            searchsetter,
            ?label=label
        )

    static member PubMedIDInput (id: string option, doisetter, searchsetter: Publication -> unit, ?label:string) =
        FormComponents.PublicationRequestInput(
            id,
            API.requestByPubMedID,
            doisetter,
            searchsetter,
            ?label=label
        )

    static member CommentInput (comment: Comment, setter: Comment -> unit, ?label: string, ?rmv: MouseEvent -> unit) =
        Html.div [
            prop.children [
                if label.IsSome then Daisy.label [Daisy.labelText label.Value]
                Html.div [
                    prop.className "flex flex-row gap-2 relative"
                    prop.children [
                        FormComponents.TextInput(
                            comment.Name |> Option.defaultValue "",
                            (fun s ->
                                comment.Name <- if s = "" then None else Some s
                                comment |> setter),
                            placeholder="comment name"
                        )
                        FormComponents.TextInput(
                            comment.Value |> Option.defaultValue "",
                            (fun s ->
                                comment.Value <- if s = "" then None else Some s
                                comment |> setter),
                            placeholder="comment"
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
            (fun (v, setV, rmv) -> FormComponents.CommentInput(v, setV, rmv=rmv)),
            ?label=label
        )

    [<ReactComponent>]
    static member PublicationInput(input: Publication, setter: Publication -> unit, ?rmv: MouseEvent -> unit) =
        let title = Option.defaultValue "<title>" input.Title
        let doi = Option.defaultValue "<doi>" input.DOI
        let createFieldTextInput(field: string option, label, publicationSetter: string option -> unit) =
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
        Generic.Collapse
            [
                Generic.CollapseTitle(title, doi, countFilledFieldsString ())
            ]
            [
                createFieldTextInput(input.Title, "Title", fun s -> input.Title <- s)
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
                createFieldTextInput(input.Authors, "Authors", fun s -> input.Authors <- s)
                FormComponents.OntologyAnnotationInput(
                    input.Status,
                    (fun s ->
                        input.Status <- s
                        input |> setter
                    ),
                    "Status",
                    parent=TermCollection.PublicationStatus
                )
                FormComponents.CommentsInput(
                    input.Comments,
                    (fun c ->
                        input.Comments <- ResizeArray(c)
                        input |> setter
                    ),
                    "Comments"
                )
                if rmv.IsSome then
                    Helper.deleteButton rmv.Value
            ]

    static member PublicationsInput(input: ResizeArray<Publication>, setter: ResizeArray<Publication> -> unit, label: string) =
        FormComponents.InputSequence(
            input,
            Publication,
            setter,
            (fun (a,b,c) -> FormComponents.PublicationInput(a,b,rmv=c)),
            label
        )

    [<ReactComponent>]
    static member OntologySourceReferenceInput(input: OntologySourceReference, setter: OntologySourceReference -> unit, ?deletebutton: MouseEvent -> unit) =
        let name = Option.defaultValue "<name>" input.Name
        let version = Option.defaultValue "<version>" input.Version
        let createFieldTextInput(field: string option, label, setFunction: string option -> unit) =
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
        Generic.Collapse
            [
                Generic.CollapseTitle(name, version, countFilledFieldsString ())
            ]
            [
                createFieldTextInput(input.Name, "Name", fun s -> input.Name <- s)
                Helper.cardFormGroup [
                    createFieldTextInput(input.Version, "Version", fun s -> input.Version <- s)
                    createFieldTextInput(input.File, "File", fun s -> input.File <- s)
                ]
                FormComponents.TextInput(
                    Option.defaultValue "" input.Description,
                    (fun s ->
                        input.Description <- s |> Option.whereNot System.String.IsNullOrWhiteSpace
                        input |> setter),
                    "Description",
                    isarea=true
                )
                FormComponents.CommentsInput(
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

    static member OntologySourceReferencesInput(input: ResizeArray<OntologySourceReference>, setter: ResizeArray<OntologySourceReference> -> unit, label: string) =
        FormComponents.InputSequence(
            input,
            OntologySourceReference,
            setter,
            (fun (a,b,c) -> FormComponents.OntologySourceReferenceInput(a,b,c)),
            label
        )