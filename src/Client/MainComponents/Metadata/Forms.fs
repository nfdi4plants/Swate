namespace MainComponents.Metadata

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl
open Shared
open Fetch
open ARCtrl.Json

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
    //type PersonMutable(?firstname, ?lastname, ?midinitials, ?orcid, ?address, ?affiliation, ?email, ?phone, ?fax, ?roles) =
    //    member val FirstName : string option = firstname with get, set
    //    member val LastName : string option = lastname with get, set
    //    member val MidInitials : string option = midinitials with get, set
    //    member val ORCID : string option = orcid with get, set
    //    member val Address : string option = address with get, set
    //    member val Affiliation : string option = affiliation with get, set
    //    member val EMail : string option = email with get, set
    //    member val Phone : string option = phone with get, set
    //    member val Fax : string option = fax with get, set
    //    member val Roles : OntologyAnnotation [] option = roles with get, set
        
    //    static member fromPerson(person:Person) =
    //        PersonMutable(
    //            ?firstname=person.FirstName, 
    //            ?lastname=person.LastName, 
    //            ?midinitials=person.MidInitials,
    //            ?orcid=person.ORCID,
    //            ?address=person.Address,
    //            ?affiliation=person.Affiliation,
    //            ?email=person.EMail,
    //            ?phone=person.Phone,
    //            ?fax=person.Fax,
    //            ?roles=person.Roles
    //        )

    //    member this.ToPerson() =
    //        Person.create(
    //            ?FirstName=this.FirstName, 
    //            ?LastName=this.LastName, 
    //            ?MidInitials=this.MidInitials,
    //            ?ORCID=this.ORCID,
    //            ?Address=this.Address,
    //            ?Affiliation=this.Affiliation,
    //            ?Email=this.EMail,
    //            ?Phone=this.Phone,
    //            ?Fax=this.Fax,
    //            ?Roles=this.Roles
    //        )

    //type OntologyAnnotationMutable(?name,?tsr,?tan) =
    //    member val Name : string option = name with get, set
    //    member val TSR : string option = tsr with get, set
    //    member val TAN : string option = tan with get, set

    //    static member fromOntologyAnnotation(oa: OntologyAnnotation) =
    //        let name = if oa.NameText = "" then None else Some oa.NameText
    //        OntologyAnnotationMutable(?name=name, ?tsr=oa.TermSourceREF, ?tan=oa.TermAccessionNumber)

    //    member this.ToOntologyAnnotation() =
    //        OntologyAnnotation.fromString(?termName=this.Name,?tsr=this.TSR,?tan=this.TAN)

    //type PublicationMutable(?pubmedid: string, ?doi: string, ?authors: string, ?title: string, ?status: OntologyAnnotation, ?comments: Comment []) =
    //    member val PubmedId = pubmedid with get, set
    //    member val Doi = doi with get, set
    //    member val Authors = authors with get, set
    //    member val Title = title with get, set
    //    member val Status = status with get, set
    //    member val Comments = comments with get, set

    //    static member fromPublication(pub:Publication) =
    //        PublicationMutable(
    //            ?pubmedid=pub.PubMedID,
    //            ?doi=pub.DOI,
    //            ?authors=pub.Authors,
    //            ?title=pub.Title,
    //            ?status=pub.Status,
    //            ?comments=pub.Comments
    //        )

    //    member this.ToPublication() =
    //        Publication.create(
    //            ?PubMedID=this.PubmedId,
    //            ?Doi=this.Doi,
    //            ?Authors=this.Authors,
    //            ?Title=this.Title,
    //            ?Status=this.Status,
    //            ?Comments=this.Comments
    //        )

    //type FactorMutable(?name,?factortype,?comments) =
    //    member val Name = name with get, set
    //    member val FactorType = factortype with get, set
    //    member val Comments = comments with get, set

    //    static member fromFactor(f:Factor) =
    //        FactorMutable(
    //            ?name=f.Name,
    //            ?factortype=f.FactorType,
    //            ?comments=f.Comments
    //        )
    //    member this.ToFactor() =
    //        Factor.create(
    //            ?Name=this.Name,
    //            ?FactorType=this.FactorType,
    //            ?Comments=this.Comments
    //        )

    //type OntologySourceReferenceMutable(?name,?description,?file,?version,?comments) =
    //    member val Name = name with get, set
    //    member val Description = description with get, set
    //    member val File = file with get, set
    //    member val Version = version with get, set
    //    member val Comments = comments with get, set

    //    static member fromOntologySourceReference(o:OntologySourceReference) =
    //        OntologySourceReferenceMutable(
    //            ?name=o.Name,
    //            ?description= o.Description,
    //            ?file=o.File,
    //            ?version=o.Version,
    //            ?comments=o.Comments
    //        )
    //    member this.ToOntologySourceReference() =
    //        OntologySourceReference.create(
    //            ?Name=this.Name,
    //            ?Description=this.Description,
    //            ?File=this.File,
    //            ?Version=this.Version,
    //            ?Comments=this.Comments
    //        )

    let addButton (clickEvent: MouseEvent -> unit) =
        Html.div [
            prop.classes ["is-flex"; "is-justify-content-center"]
            prop.children [
                Bulma.button.button [
                    prop.text "+"
                    prop.onClick clickEvent
                ]
            ]
        ]

    let deleteButton (clickEvent: MouseEvent -> unit) =
        Html.div [
            prop.classes ["is-flex"; "is-justify-content-flex-end"]
            prop.children [
                Bulma.button.button [
                    Bulma.button.isOutlined
                    Bulma.color.isDanger
                    prop.text "Delete"
                    prop.onClick clickEvent
                ]
            ]
        ]

    let cardFormGroup (formComponents: ReactElement list) = 
        Bulma.field.div [
            Html.div [
                prop.classes ["form-container"]
                prop.children formComponents
            ]
        ]

    let readOnlyFormElement(v: string option, label: string) =
        let v = defaultArg v "-"
        Bulma.field.div [
            prop.className "is-flex is-flex-direction-column is-flex-grow-1"
            prop.children [
                Bulma.label label
                Bulma.control.div [
                    Bulma.control.isExpanded
                    prop.children [
                        Bulma.input.text [
                            prop.readOnly true
                            prop.valueOrDefault v
                        ]
                    ]
                ]
            ]
        ]

    let personModal (person: Person, confirm, back) =
        Bulma.modal [
            Bulma.modal.isActive
            prop.children [
                Bulma.modalBackground []
                Bulma.modalClose []
                Bulma.modalContent [
                    Bulma.container [
                        prop.className "p-1"
                        prop.children [
                            Bulma.box [
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
                                Bulma.field.div [
                                    prop.className "is-flex is-justify-content-flex-end"
                                    prop.style [style.gap (length.rem 1)]
                                    prop.children [
                                        Bulma.button.button [
                                            prop.text "back"
                                            prop.onClick back
                                        ]
                                        Bulma.button.button [
                                            Bulma.color.isSuccess
                                            prop.text "confirm"
                                            prop.onClick confirm
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let publicationModal (pub: Publication, confirm, back) =
        Bulma.modal [
            Bulma.modal.isActive
            prop.children [
                Bulma.modalBackground []
                Bulma.modalClose []
                Bulma.modalContent [
                    Bulma.container [
                        prop.className "p-1"
                        prop.children [
                            Bulma.box [
                                cardFormGroup [
                                    readOnlyFormElement(pub.Title, "Title")
                                ]
                                cardFormGroup [
                                    readOnlyFormElement(pub.DOI, "DOI")
                                    readOnlyFormElement(pub.PubMedID, "PubMedID")
                                ]
                                cardFormGroup [
                                    readOnlyFormElement(pub.Authors, "Authors")
                                ]
                                cardFormGroup [
                                    readOnlyFormElement(pub.Status |> Option.map _.ToString(), "Status")
                                ]
                                Bulma.field.div [
                                    prop.className "is-flex is-justify-content-flex-end"
                                    prop.style [style.gap (length.rem 1)]
                                    prop.children [
                                        Bulma.button.button [
                                            prop.text "back"
                                            prop.onClick back
                                        ]
                                        Bulma.button.button [
                                            Bulma.color.isSuccess
                                            prop.text "confirm"
                                            prop.onClick confirm
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let errorModal (error: exn, back) =
        Bulma.modal [
            Bulma.modal.isActive
            prop.children [
                Bulma.modalBackground [prop.onClick back]
                Bulma.modalClose [prop.onClick back]
                Bulma.modalContent [
                    Bulma.notification [
                        Bulma.color.isDanger
                        prop.children [
                            Bulma.delete [prop.onClick back]
                            Html.div error.Message
                        ]
                    ]
                ]
            ]
        ]
        

type FormComponents =

    [<ReactComponent>]
    static member TextInput (input: string, label: string, setter: string -> unit, ?fullwidth: bool, ?removebutton: MouseEvent -> unit, ?isarea) =
        let isarea = defaultArg isarea false
        let inputFormElement : (IReactProperty list -> ReactElement) = if isarea then Bulma.textarea else Bulma.input.text
        let fullwidth = defaultArg fullwidth false
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input)
        let debounceStorage = React.useRef(newDebounceStorage())
        React.useEffect((fun () -> setState input), dependencies=[|box input|])
        Bulma.field.div [
            prop.style [if fullwidth then style.flexGrow 1]
            prop.children [
                if label <> "" then Bulma.label label
                Bulma.field.div [
                    Bulma.field.hasAddons
                    prop.children [
                        Bulma.control.div [
                            if loading then Bulma.control.isLoading
                            prop.style [if fullwidth then style.flexGrow 1]
                            prop.children [
                                inputFormElement [
                                    prop.valueOrDefault state
                                    prop.onChange(fun (e: string) ->
                                        setState e
                                        debouncel debounceStorage.current label 1000 setLoading setter e
                                    )
                                ]
                            ]
                        ]
                        if removebutton.IsSome then
                            Bulma.control.div [
                                Bulma.button.span [
                                    prop.text "X"
                                    prop.onClick removebutton.Value
                                    Bulma.color.isDanger
                                    Bulma.button.isOutlined
                                ]
                            ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member InputSequence<'A>(inputs': 'A [], empty: 'A, label: string, setter: 'A [] -> unit, inputComponent: 'A * string * ('A -> unit) * (MouseEvent -> unit) -> ReactElement) =
        let state, setState = React.useState (ResizeArray(collection=inputs'))
        React.useEffect((fun _ -> setState <| ResizeArray(inputs')), [|box inputs'|])
        Bulma.field.div [
            Bulma.label label
            Bulma.field.div [
                Html.orderedList [
                    for i in 0 .. state.Count - 1 do
                        let input = state.[i]
                        Html.li [
                            inputComponent(
                                input, "",
                                (fun oa -> 
                                    state.[i] <- oa
                                    state |> Array.ofSeq |> setter 
                                ),
                                (fun _ ->  
                                    state.RemoveAt i
                                    state |> Array.ofSeq |> setter
                                )
                            )
                        ]
                ]
            ]
            Helper.addButton (fun _ ->
                state.Add empty 
                state |> Array.ofSeq |> setter
            )
        ]

    [<ReactComponent>]
    static member TextInputs (texts: string [], label: string, setter: string [] -> unit, ?placeholder: string, ?fullwidth: bool) =
        FormComponents.InputSequence<string>(
            texts,
            "",
            label,
            setter,
            fun (a,b,c,d) -> FormComponents.TextInput(a,b,c,fullwidth=true,removebutton=d)
        )

    [<ReactComponent>]
    static member DateTimeInput (input: string, label: string, setter: string -> unit, ?placeholder: string, ?fullwidth: bool) =
        let fullwidth = defaultArg fullwidth false
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input)
        let debounceStorage, setdebounceStorage = React.useState(newDebounceStorage)
        React.useEffect((fun () -> setState input), dependencies=[|box input|])
        Bulma.field.div [
            prop.style [if fullwidth then style.flexGrow 1]
            prop.children [
                if label <> "" then Bulma.label label
                Bulma.control.div [
                    if loading then Bulma.control.isLoading
                    prop.children [
                        Bulma.input.datetimeLocal [
                            prop.valueOrDefault state
                            prop.onChange(fun (e: System.DateTime) ->
                                let dtString = e.ToString("yyyy-MM-ddThh:mm")
                                setState dtString
                                debouncel debounceStorage label 1000 setLoading setter dtString
                            )
                        ]
                    ]
                ]
            ]
        ]

    static member DateTimeInput (input: System.DateTime, label: string, setter: System.DateTime -> unit, ?fullwidth: bool) =
        FormComponents.DateTimeInput(
            input.ToString("yyyy-MM-ddThh:mm"),
            label,
            (fun (s: string) ->
                setter (System.DateTime.Parse(s))),
            ?fullwidth=fullwidth
        )

    [<ReactComponent>]
    static member GUIDInput (input: System.Guid, label: string, setter: string -> unit, ?placeholder: string, ?fullwidth: bool) =
        let fullwidth = defaultArg fullwidth false
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input.ToString())
        let isValid, setIsValid = React.useState(true)
        let debounceStorage, setdebounceStorage = React.useState(newDebounceStorage)
        React.useEffect((fun () -> setState <| input.ToString()), dependencies=[|box input|])
        let regex = System.Text.RegularExpressions.Regex(@"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$")
        //let unmask (s:string) = s |> String.filter(fun c -> c <> '_' && c <> '-')
        //let mask (s:string) = 
        //    s.PadRight(32,'_').[0..31]
        //    |> fun padded -> sprintf "%s-%s-%s-%s-%s" padded.[0..7] padded.[8..11] padded.[12..15] padded.[16..19] padded.[20..31]
        Bulma.field.div [
            prop.style [if fullwidth then style.flexGrow 1]
            prop.children [
                if label <> "" then Bulma.label label
                Bulma.control.div [
                    Bulma.control.hasIconsRight
                    if loading then Bulma.control.isLoading
                    prop.children [
                        Bulma.input.text [
                            prop.pattern (regex)
                            prop.required true
                            if not isValid then Bulma.color.isDanger
                            if placeholder.IsSome then prop.placeholder placeholder.Value
                            prop.valueOrDefault state
                            prop.onChange(fun (s: string) ->
                                let nextValid = regex.IsMatch(s.Trim())
                                setIsValid nextValid
                                setState s
                                if nextValid then
                                    debouncel debounceStorage label 200 setLoading setter s
                            )
                        ]
                        if isValid then Bulma.icon [
                            Bulma.icon.isRight
                            Bulma.icon.isSmall
                            Bulma.color.isSuccess
                            prop.children [Html.i [prop.className "fas fa-check"]]
                        ]
                    ]
                ]
                Html.small "Guid should contain 32 digits with 4 dashes following: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx. Allowed are a-f, A-F and numbers."
            ]
        ]

    [<ReactComponent>]
    static member PublicationRequestInput (id: string option,searchAPI: string -> Fable.Core.JS.Promise<Publication>, doisetter, searchsetter: Publication -> unit, ?label:string) =
        let id = defaultArg id ""
        let state, setState = React.useState(API.Request<Publication>.Idle)
        let resetState = fun _ -> setState API.Request.Idle
        Bulma.field.div [
            prop.className "is-flex-grow-1"
            prop.children [
                if label.IsSome then Bulma.label label.Value
                Bulma.field.div [
                    Bulma.field.hasAddons
                    prop.children [
                        //if state.IsSome || error.IsSome then
                        match state with
                        | API.Request.Ok pub -> Helper.publicationModal(pub,(fun _ -> searchsetter pub; resetState()), resetState)
                        | API.Request.Error e -> Helper.errorModal(e, resetState)
                        | API.Request.Loading -> Modals.Loading.loadingModal
                        | _ -> Html.none
                        Bulma.control.div [
                            Bulma.control.isExpanded
                            prop.children [
                                FormComponents.TextInput(
                                    id,
                                    "",
                                    setter=doisetter,
                                    fullwidth=true
                                )
                            ]
                        ]
                        Bulma.control.div [
                            Bulma.button.button [
                                Bulma.color.isInfo
                                prop.text "Search"
                                prop.onClick (fun _ ->
                                    //API.requestByORCID ("0000-0002-8510-6810") |> Promise.start
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
        ]

    [<ReactComponent>]
    static member PersonRequestInput (orcid: string option, doisetter, searchsetter: Person -> unit, ?label:string) =
        let orcid = defaultArg orcid ""
        let state, setState = React.useState(API.Request<Person>.Idle)
        let resetState = fun _ -> setState API.Request.Idle
        Bulma.field.div [
            prop.className "is-flex-grow-1"
            prop.children [
                if label.IsSome then Bulma.label label.Value
                Bulma.field.div [
                    Bulma.field.hasAddons
                    prop.children [
                        //if state.IsSome || error.IsSome then
                        match state with
                        | API.Request.Ok p -> Helper.personModal (p, (fun _ -> searchsetter p; resetState()), resetState)
                        | API.Request.Error e -> Helper.errorModal(e, resetState)
                        | API.Request.Loading -> Modals.Loading.loadingModal
                        | _ -> Html.none
                        Bulma.control.div [
                            Bulma.control.isExpanded
                            prop.children [
                                FormComponents.TextInput(
                                    orcid,
                                    "",
                                    setter=doisetter,
                                    fullwidth=true
                                )
                            ]
                        ]
                        Bulma.control.div [
                            Bulma.button.button [
                                Bulma.color.isInfo
                                prop.text "Search"
                                prop.onClick (fun _ ->
                                    //API.requestByORCID ("0000-0002-8510-6810") |> Promise.start
                                    setState API.Request.Loading
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
        ]

    [<ReactComponent>]
    static member DOIInput (id: string option, doisetter, searchsetter: Publication -> unit, ?label:string) =
        FormComponents.PublicationRequestInput(
            id,
            API.requestByDOI,//"10.3390/ijms24087444"//"10.3390/ijms2408741d"//
            doisetter,
            searchsetter,
            ?label=label
        )

    [<ReactComponent>]
    static member PubMedIDInput (id: string option, doisetter, searchsetter: Publication -> unit, ?label:string) =
        FormComponents.PublicationRequestInput(
            id,
            API.requestByPubMedID,             
            doisetter,
            searchsetter,
            ?label=label
        )

    [<ReactComponent>]
    static member OntologyAnnotationInput (input: OntologyAnnotation, setter: OntologyAnnotation -> unit, ?label: string, ?showTextLabels: bool, ?removebutton: MouseEvent -> unit, ?parent: OntologyAnnotation) =
        let showTextLabels = defaultArg showTextLabels true
        let state, setState = React.useState(input)
        let element = React.useElementRef()
        React.useEffect((fun () -> setState input), dependencies=[|box input|])
        Bulma.field.div [ 
            //if label.IsSome then Bulma.label label.Value
            Bulma.field.div [
                //prop.ref element
                prop.style [style.position.relative]
                prop.classes ["is-flex"; "is-flex-direction-row"; "is-justify-content-space-between"]
                prop.children [
                    Html.div [
                        prop.classes ["form-container"; if removebutton.IsSome then "pr-2"]
                        prop.children [
                            Bulma.field.div [
                                prop.style [style.flexGrow 1]
                                prop.children [
                                    let label = defaultArg label "Term Name" 
                                    Bulma.label label
                                    let innersetter = 
                                        fun (oaOpt: OntologyAnnotation option) -> 
                                            if oaOpt.IsSome then 
                                                setter oaOpt.Value
                                                setState oaOpt.Value
                                    Components.TermSearch.Input(
                                        innersetter,
                                        input=state,
                                        fullwidth=true,
                                        ?portalTermSelectArea=element.current,
                                        ?parent=parent,
                                        debounceSetter=1000
                                    )                                
                                ]
                            ]
                            Html.div [
                                prop.classes ["form-input-term-search-positioner"]
                                prop.ref element
                            ]
                            FormComponents.TextInput(
                                Option.defaultValue "" state.TermSourceREF,
                                (if showTextLabels then $"TSR" else ""),
                                (fun s -> 
                                    let s = if s = "" then None else Some s
                                    state.TermSourceREF <- s 
                                    state |> setter),
                                fullwidth = true
                            )
                            FormComponents.TextInput(
                                Option.defaultValue "" state.TermAccessionNumber,
                                (if showTextLabels then $"TAN" else ""),
                                (fun s -> 
                                    let s = if s = "" then None else Some s
                                    state.TermAccessionNumber <- s 
                                    state |> setter),
                                fullwidth = true
                            )
                        ]
                    ]
                    if removebutton.IsSome then
                        Html.div [
                            if showTextLabels then Bulma.label [prop.style [style.color("transparent")]; prop.text "rmv"]
                            Bulma.button.button [
                                prop.text "X"
                                prop.onClick removebutton.Value
                                Bulma.color.isDanger
                                Bulma.button.isOutlined
                            ]
                        ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationsInput (oas: OntologyAnnotation [], label: string, setter: OntologyAnnotation [] -> unit, ?showTextLabels: bool, ?parent: OntologyAnnotation) =
        FormComponents.InputSequence(
            oas, (OntologyAnnotation.empty()), label, setter, 
            (fun (a,b,c,d) -> FormComponents.OntologyAnnotationInput(a,c,label=b,removebutton=d,?showTextLabels=showTextLabels, ?parent=parent))
        )

    [<ReactComponent>]
    static member PersonInput(input: Person, setter: Person -> unit, ?deletebutton: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        // Must use `React.useRef` do this. Otherwise simultanios updates will overwrite each other
        let state, setState = React.useState(input) 
        React.useEffect((fun _ -> setState input), [|box input|])
        let fn = Option.defaultValue "" state.FirstName 
        let ln = Option.defaultValue "" state.LastName
        let mi = Option.defaultValue "" state.MidInitials
        let nameStr = 
            let x = $"{fn} {mi} {ln}".Trim()
            if x = "" then "<name>" else x
        let orcid = Option.defaultValue "<orcid>" state.ORCID
        let createPersonFieldTextInput(field: string option, label, personSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                label,
                (fun s -> 
                    let s = if s = "" then None else Some s
                    personSetter s 
                    state |> setter),
                fullwidth=true
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
        Bulma.card [
            Bulma.cardHeader [
                Bulma.cardHeaderTitle.div [
                    //prop.classes ["is-align-items-flex-start"]
                    prop.children [
                        Html.div [
                            Bulma.title.h5 nameStr
                            Bulma.subtitle.h6 orcid
                        ]
                        Html.div [
                            prop.style [style.custom("marginLeft", "auto")]
                            prop.text (countFilledFieldsString state)
                        ]
                    ]
                ]
                Bulma.cardHeaderIcon.a [
                    prop.onClick (fun _ -> not isExtended |> setIsExtended)
                    prop.children [
                        Bulma.icon [Html.i [prop.classes ["fas"; "fa-angle-down"]]]
                    ]
                ]
            ]
            Bulma.cardContent [
                prop.classes [if not isExtended then "is-hidden"]
                prop.children [
                    Helper.cardFormGroup [
                        createPersonFieldTextInput(state.FirstName, "First Name", fun s -> state.FirstName <- s)
                        createPersonFieldTextInput(state.LastName, "Last Name", fun s -> state.LastName <- s)
                    ]
                    Helper.cardFormGroup [
                        createPersonFieldTextInput(state.MidInitials, "Mid Initials", fun s -> state.MidInitials <- s)
                        FormComponents.PersonRequestInput(
                            state.ORCID,
                            (fun s -> 
                                let s = if s = "" then None else Some s
                                state.ORCID <- s
                                state |> setter),
                                (fun s -> setter s),
                                "ORCID"
                        )
                    ]
                    Helper.cardFormGroup [
                        createPersonFieldTextInput(state.Affiliation, "Affiliation", fun s -> state.Affiliation <- s)
                        createPersonFieldTextInput(state.Address, "Address", fun s -> state.Address <- s)
                    ]
                    Helper.cardFormGroup [
                        createPersonFieldTextInput(state.EMail, "Email", fun s -> state.EMail <- s)
                        createPersonFieldTextInput(state.Phone, "Phone", fun s -> state.Phone <- s)
                        createPersonFieldTextInput(state.Fax, "Fax", fun s -> state.Fax <- s)
                    ]
                    FormComponents.OntologyAnnotationsInput(
                        Array.ofSeq state.Roles,
                        "Roles",
                        (fun oas -> 
                            state.Roles <- ResizeArray(oas)
                            state |> setter
                        ),
                        showTextLabels = false,
                        parent=Shared.TermCollection.PersonRoleWithinExperiment
                    )
                    if deletebutton.IsSome then
                        Helper.deleteButton deletebutton.Value
                ]
            ]
        ]

    static member PersonsInput (persons: Person [], label: string, setter: Person [] -> unit) =
        Bulma.field.div [
            Bulma.label label
            Bulma.field.div [
                yield! persons
                |> Array.mapi (fun i person ->
                    let personsSetter = fun p -> 
                        persons.[i] <- p
                        setter persons 
                    let rmv = fun _ ->
                        persons |> Array.removeAt i |> setter
                    FormComponents.PersonInput (person, personsSetter, rmv)
                )
            ]
            Helper.addButton (fun _ ->
                let newPersons = Array.append persons [|Person.create()|]
                setter newPersons
            )
        ]

    static member CommentInput (comment: Comment, label: string, setter: Comment -> unit, ?showTextLabels: bool, ?removebutton: MouseEvent -> unit) =
        let showTextLabels = defaultArg showTextLabels true
        Bulma.field.div [ 
            if label <> "" then Bulma.label label
            Html.div [
                prop.classes ["form-container"]
                prop.children [
                    FormComponents.TextInput(
                        comment.Name |> Option.defaultValue "",
                        (if showTextLabels then $"Term Name" else ""),
                        (fun s ->
                            comment.Name <- if s = "" then None else Some s
                            comment |> setter),
                        fullwidth = true
                    )
                    FormComponents.TextInput(
                        comment.Value |> Option.defaultValue "",
                        (if showTextLabels then $"TSR" else ""),
                        (fun s -> 
                            comment.Value <- if s = "" then None else Some s
                            comment |> setter),
                        fullwidth = true
                    )
                    if removebutton.IsSome then
                        Bulma.button.button [
                            prop.text "X"
                            prop.onClick removebutton.Value
                            Bulma.color.isDanger
                            Bulma.button.isOutlined
                        ]
                ]
            ]
        ]

    static member CommentsInput(comments: Comment [], label, setter: Comment [] -> unit) =
        Bulma.field.div [
            if label <> "" then Bulma.label label
            Bulma.field.div [
                yield! comments
                |> Array.mapi (fun i comment ->
                    let commentSetter = fun c -> 
                        comments.[i] <- c
                        setter comments 
                    let rmv = fun _ -> comments |> Array.removeAt i |> setter
                    FormComponents.CommentInput (comment,"", commentSetter, false, rmv)
                )
            ]
            Helper.addButton (fun _ ->
                let newComment = Array.append comments [|Comment.create()|]
                setter newComment
            )
        ]

    [<ReactComponent>]
    static member PublicationInput(input: Publication, setter: Publication -> unit, ?deletebutton: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        // Must use `React.useRef` do this. Otherwise simultanios updates will overwrite each other
        let state, setState = React.useState(input) 
        React.useEffect((fun _ -> setState input), [|box input|])
        let title = Option.defaultValue "<title>" state.Title
        let doi = Option.defaultValue "<doi>" state.DOI
        let createPersonFieldTextInput(field: string option, label, publicationSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                label,
                (fun s -> 
                    let s = if s = "" then None else Some s
                    publicationSetter s 
                    state |> setter),
                fullwidth=true
            )
        let countFilledFieldsString () =
            let fields = [
                state.PubMedID
                state.DOI
                state.Title
                state.Authors
                state.Status |> Option.map (fun _ -> "")
            ]
            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"
        Bulma.card [
            Bulma.cardHeader [
                Bulma.cardHeaderTitle.div [
                    //prop.classes ["is-align-items-flex-start"]
                    prop.children [
                        Html.div [
                            Bulma.title.h5 title
                            Bulma.subtitle.h6 doi
                        ]
                        Html.div [
                            prop.style [style.custom("marginLeft", "auto")]
                            prop.text (countFilledFieldsString ())
                        ]
                    ]
                ]
                Bulma.cardHeaderIcon.a [
                    prop.onClick (fun _ -> not isExtended |> setIsExtended)
                    prop.children [
                        Bulma.icon [Html.i [prop.classes ["fas"; "fa-angle-down"]]]
                    ]
                ]
            ]
            Bulma.cardContent [
                prop.classes [if not isExtended then "is-hidden"]
                prop.children [
                    createPersonFieldTextInput(state.Title, "Title", fun s -> state.Title <- s)
                    Helper.cardFormGroup [
                        FormComponents.PubMedIDInput(
                            state.PubMedID,
                            (fun s -> 
                                let s = if s = "" then None else Some s
                                state.PubMedID <- s
                                state |> setter),
                            (fun pub -> setter pub),
                            "PubMed Id"
                        )
                        FormComponents.DOIInput(
                            state.DOI, 
                            (fun s -> 
                                let s = if s = "" then None else Some s
                                state.DOI <- s
                                state |> setter),
                            (fun pub -> setter pub),
                            "DOI"
                        )
                    ]
                    createPersonFieldTextInput(state.Authors, "Authors", fun s -> state.Authors <- s)
                    FormComponents.OntologyAnnotationInput(
                        Option.defaultValue (OntologyAnnotation.empty()) state.Status, 
                        (fun s -> 
                            state.Status <- if s = (OntologyAnnotation.empty()) then None else Some s
                            state |> setter
                        ),
                        "Status",
                        parent=Shared.TermCollection.PublicationStatus
                    )
                    FormComponents.CommentsInput(
                        Array.ofSeq state.Comments, 
                        "Comments", 
                        (fun c -> 
                            state.Comments <- ResizeArray(c)
                            state |> setter
                        )
                    )
                    if deletebutton.IsSome then
                        Helper.deleteButton deletebutton.Value
                ]
            ]
        ]

    static member PublicationsInput(input: Publication [], label: string, setter: Publication [] -> unit) =
        FormComponents.InputSequence(
            input,
            Publication.create(),
            label,
            setter,
            (fun (a,b,c,d) -> FormComponents.PublicationInput(a,c,deletebutton=d))
        )

    [<ReactComponent>]
    static member OntologySourceReferenceInput(input: OntologySourceReference, setter: OntologySourceReference -> unit, ?deletebutton: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        // Must use `React.useRef` do this. Otherwise simultanios updates will overwrite each other
        let state, setState = React.useState(input) 
        React.useEffect((fun _ -> setState input), [|box input|])
        let name = Option.defaultValue "<name>" state.Name
        let version = Option.defaultValue "<version>" state.Version
        let createFieldTextInput(field: string option, label, personSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                label,
                (fun s -> 
                    let s = if s = "" then None else Some s
                    personSetter s 
                    state |> setter),
                fullwidth=true
            )
        let countFilledFieldsString () =
            let fields = [
                state.Name
                state.File
                state.Version
                state.Description
                if state.Comments.Count > 0 then Some "comments" else None // just for count. Value does not matter
            ]
            let all = fields.Length
            let filled = fields |> List.choose id |> _.Length
            $"{filled}/{all}"
        Bulma.card [
            Bulma.cardHeader [
                Bulma.cardHeaderTitle.div [
                    //prop.classes ["is-align-items-flex-start"]
                    prop.children [
                        Html.div [
                            Bulma.title.h5 name
                            Bulma.subtitle.h6 version
                        ]
                        Html.div [
                            prop.style [style.custom("marginLeft", "auto")]
                            prop.text (countFilledFieldsString ())
                        ]
                    ]
                ]
                Bulma.cardHeaderIcon.a [
                    prop.onClick (fun _ -> not isExtended |> setIsExtended)
                    prop.children [
                        Bulma.icon [Html.i [prop.classes ["fas"; "fa-angle-down"]]]
                    ]
                ]
            ]
            Bulma.cardContent [
                prop.classes [if not isExtended then "is-hidden"]
                prop.children [
                    createFieldTextInput(state.Name, "Name", fun s -> state.Name <- s)
                    Helper.cardFormGroup [ 
                        createFieldTextInput(state.Version, "Version", fun s -> state.Version <- s)
                        createFieldTextInput(state.File, "File", fun s -> state.File <- s)
                    ]
                    FormComponents.TextInput(
                        Option.defaultValue "" state.Description,
                        "Description",
                        (fun s -> 
                            let s = if s = "" then None else Some s
                            state.Description <- s
                            state |> setter),
                        fullwidth=true,
                        isarea=true
                    )
                    FormComponents.CommentsInput(
                        Array.ofSeq state.Comments, 
                        "Comments", 
                        (fun c -> 
                            state.Comments <- ResizeArray(c)
                            state |> setter
                        )
                    )
                    if deletebutton.IsSome then
                        Helper.deleteButton deletebutton.Value
                ]
            ]
        ]

    static member OntologySourceReferencesInput(input: OntologySourceReference [], label: string, setter: OntologySourceReference [] -> unit) =
        FormComponents.InputSequence(
            input,
            OntologySourceReference.create(),
            label,
            setter,
            (fun (a,b,c,d) -> FormComponents.OntologySourceReferenceInput(a,c,deletebutton=d))
        )