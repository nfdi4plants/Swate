namespace MainComponents.Metadata

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl.ISA
open Shared
open Fetch

module private API =
    let requestAsJson (url) =
        promise {
            let! response = fetch url [
                requestHeaders [Accept "application/json"]
            ]
            let! json = response.json()
            return Some json
        }

    let requestByORCID (orcID: string) =
        let url = $"https://pub.orcid.org/v3.0/{orcID}/record"
        promise {
            let! json = requestAsJson url 
            let name: string = json?person?name?("given-names")?value
            let lastName: string = json?person?name?("family-name")?value
            let emails: obj [] = json?person?emails?email
            let email = if emails.Length = 0 then None else Some (emails.[0]?email)
            log(name, lastName, email)
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
                    Person.create(ORCID=pj?ORCID, LastName=pj?family, FirstName=pj?given, Affiliation=affString)
            |]
            let! pubmedId = requestByDOI_FromPubMed doi
            let authorString = authors |> Array.map (fun x -> $"{x.FirstName} {x.LastName}") |> String.concat ", "
            let publication = Publication.create(?PubMedID=pubmedId, Doi=doi, Authors=authorString, ?Title=title)
            return publication
        }

    let start (call: Fable.Core.JS.Promise<'a>, success, fail) =
        call
        |> Promise.either 
            success
            fail
        |> Promise.start

module private Helper =
    type PersonMutable(?firstname, ?lastname, ?midinitials, ?orcid, ?address, ?affiliation, ?email, ?phone, ?fax, ?roles) =
        member val FirstName : string option = firstname with get, set
        member val LastName : string option = lastname with get, set
        member val MidInitials : string option = midinitials with get, set
        member val ORCID : string option = orcid with get, set
        member val Address : string option = address with get, set
        member val Affiliation : string option = affiliation with get, set
        member val EMail : string option = email with get, set
        member val Phone : string option = phone with get, set
        member val Fax : string option = fax with get, set
        member val Roles : OntologyAnnotation [] option = roles with get, set
        
        static member fromPerson(person:Person) =
            PersonMutable(
                ?firstname=person.FirstName, 
                ?lastname=person.LastName, 
                ?midinitials=person.MidInitials,
                ?orcid=person.ORCID,
                ?address=person.Address,
                ?affiliation=person.Affiliation,
                ?email=person.EMail,
                ?phone=person.Phone,
                ?fax=person.Fax,
                ?roles=person.Roles
            )

        member this.ToPerson() =
            Person.create(
                ?FirstName=this.FirstName, 
                ?LastName=this.LastName, 
                ?MidInitials=this.MidInitials,
                ?ORCID=this.ORCID,
                ?Address=this.Address,
                ?Affiliation=this.Affiliation,
                ?Email=this.EMail,
                ?Phone=this.Phone,
                ?Fax=this.Fax,
                ?Roles=this.Roles
            )

    type OntologyAnnotationMutable(?name,?tsr,?tan) =
        member val Name : string option = name with get, set
        member val TSR : string option = tsr with get, set
        member val TAN : string option = tan with get, set

        static member fromOntologyAnnotation(oa: OntologyAnnotation) =
            let name = if oa.NameText = "" then None else Some oa.NameText
            OntologyAnnotationMutable(?name=name, ?tsr=oa.TermSourceREF, ?tan=oa.TermAccessionNumber)

        member this.ToOntologyAnnotation() =
            OntologyAnnotation.fromString(?termName=this.Name,?tsr=this.TSR,?tan=this.TAN)

    type PublicationMutable(?pubmedid: string, ?doi: string, ?authors: string, ?title: string, ?status: OntologyAnnotation, ?comments: Comment []) =
        member val PubmedId = pubmedid with get, set
        member val Doi = doi with get, set
        member val Authors = authors with get, set
        member val Title = title with get, set
        member val Status = status with get, set
        member val Comments = comments with get, set

        static member fromPublication(pub:Publication) =
            PublicationMutable(
                ?pubmedid=pub.PubMedID,
                ?doi=pub.DOI,
                ?authors=pub.Authors,
                ?title=pub.Title,
                ?status=pub.Status,
                ?comments=pub.Comments
            )

        member this.ToPublication() =
            Publication.create(
                ?PubMedID=this.PubmedId,
                ?Doi=this.Doi,
                ?Authors=this.Authors,
                ?Title=this.Title,
                ?Status=this.Status,
                ?Comments=this.Comments
            )

    type FactorMutable(?name,?factortype,?comments) =
        member val Name = name with get, set
        member val FactorType = factortype with get, set
        member val Comments = comments with get, set

        static member fromFactor(f:Factor) =
            FactorMutable(
                ?name=f.Name,
                ?factortype=f.FactorType,
                ?comments=f.Comments
            )
        member this.ToFactor() =
            Factor.create(
                ?Name=this.Name,
                ?FactorType=this.FactorType,
                ?Comments=this.Comments
            )

    type OntologySourceReferenceMutable(?name,?description,?file,?version,?comments) =
        member val Name = name with get, set
        member val Description = description with get, set
        member val File = file with get, set
        member val Version = version with get, set
        member val Comments = comments with get, set

        static member fromOntologySourceReference(o:OntologySourceReference) =
            OntologySourceReferenceMutable(
                ?name=o.Name,
                ?description= o.Description,
                ?file=o.File,
                ?version=o.Version,
                ?comments=o.Comments
            )
        member this.ToOntologySourceReference() =
            OntologySourceReference.create(
                ?Name=this.Name,
                ?Description=this.Description,
                ?File=this.File,
                ?Version=this.Version,
                ?Comments=this.Comments
            )

    let addButton (clickEvent: MouseEvent -> unit) =
        Html.div [
            prop.classes ["is-flex"; "is-justify-content-center"]
            prop.children [
                Bulma.button.button [
                    prop.className "is-ghost"
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
    static member OntologyAnnotationInput (input: OntologyAnnotation, setter: OntologyAnnotation -> unit, ?label: string, ?showTextLabels: bool, ?removebutton: MouseEvent -> unit) =
        let showTextLabels = defaultArg showTextLabels true
        let state, setState = React.useState(Helper.OntologyAnnotationMutable.fromOntologyAnnotation input)
        let element = React.useElementRef()
        React.useEffect((fun () -> setState <| Helper.OntologyAnnotationMutable.fromOntologyAnnotation input), dependencies=[|box input|])
        Bulma.field.div [ 
            if label.IsSome then Bulma.label label.Value
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
                                    if showTextLabels then Bulma.label $"Term Name"
                                    let innersetter = 
                                        fun (oaOpt: OntologyAnnotation option) -> 
                                            if oaOpt.IsSome then 
                                                setter oaOpt.Value
                                                setState (Helper.OntologyAnnotationMutable.fromOntologyAnnotation oaOpt.Value)
                                    Components.TermSearch.Input(
                                        innersetter,
                                        input=state.ToOntologyAnnotation(),
                                        fullwidth=true,
                                        ?portalTermSelectArea=element.current,
                                        debounceSetter=1000
                                    )                                
                                ]
                            ]
                            Html.div [
                                prop.classes ["form-input-term-search-positioner"]
                                prop.ref element
                            ]
                            FormComponents.TextInput(
                                Option.defaultValue "" state.TSR,
                                (if showTextLabels then $"TSR" else ""),
                                (fun s -> 
                                    let s = if s = "" then None else Some s
                                    state.TSR <- s 
                                    state.ToOntologyAnnotation() |> setter),
                                fullwidth = true
                            )
                            FormComponents.TextInput(
                                Option.defaultValue "" state.TAN,
                                (if showTextLabels then $"TAN" else ""),
                                (fun s -> 
                                    let s = if s = "" then None else Some s
                                    state.TAN <- s 
                                    state.ToOntologyAnnotation() |> setter),
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
    static member OntologyAnnotationsInput (oas: OntologyAnnotation [], label: string, setter: OntologyAnnotation [] -> unit, ?showTextLabels: bool) =
        FormComponents.InputSequence(
            oas, OntologyAnnotation.empty, label, setter, 
            (fun (a,b,c,d) -> FormComponents.OntologyAnnotationInput(a,c,label=b,removebutton=d,?showTextLabels=showTextLabels))
        )

    [<ReactComponent>]
    static member PersonInput(input: Person, setter: Person -> unit, ?deletebutton: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        // Must use `React.useRef` do this. Otherwise simultanios updates will overwrite each other
        let state, setState = React.useState(Helper.PersonMutable.fromPerson input) 
        React.useEffect((fun _ -> setState <| Helper.PersonMutable.fromPerson input), [|box input|])
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
                    state.ToPerson() |> setter),
                fullwidth=true
            )
        let countFilledFieldsString (person: Helper.PersonMutable) =
            let fields = [
                state.FirstName
                state.LastName
                state.MidInitials
                state.ORCID
                state.Address
                state.Affiliation
                state.EMail
                state.Phone
                state.Fax
                state.Roles |> Option.map (fun _ -> "")
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
                    Bulma.button.button [
                        prop.text "Get test person"
                        prop.onClick (fun _ ->
                            //API.requestByORCID ("0000-0002-8510-6810") |> Promise.start
                            API.requestByDOI ("10.3390/ijms24087444") |> Promise.start
                            //API.requestByDOI_FromPubMed ("10.3390/ijms24087444") |> Promise.start
                            //let api = API.requestByDOI ("10.3390/ijms2408741d") //error
                        )
                    ]
                    Helper.cardFormGroup [
                        createPersonFieldTextInput(state.FirstName, "First Name", fun s -> state.FirstName <- s)
                        createPersonFieldTextInput(state.LastName, "Last Name", fun s -> state.LastName <- s)
                    ]
                    Helper.cardFormGroup [
                        createPersonFieldTextInput(state.MidInitials, "Mid Initials", fun s -> state.MidInitials <- s)
                        createPersonFieldTextInput(state.ORCID, "ORCID", fun s -> state.ORCID <- s)
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
                        Option.defaultValue [||] state.Roles,
                        "Roles",
                        (fun oas -> 
                            let oas = if oas = [||] then None else Some oas
                            state.Roles <- oas
                            state.ToPerson() |> setter
                        ),
                        showTextLabels = false
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
                        (fun s -> {comment with Name = if s = "" then None else Some s} |> setter),
                        fullwidth = true
                    )
                    FormComponents.TextInput(
                        comment.Value |> Option.defaultValue "",
                        (if showTextLabels then $"TSR" else ""),
                        (fun s -> {comment with Value = if s = "" then None else Some s} |> setter),
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
        let state, setState = React.useState(Helper.PublicationMutable.fromPublication input) 
        React.useEffect((fun _ -> setState <| Helper.PublicationMutable.fromPublication input), [|box input|])
        let title = Option.defaultValue "<title>" state.Title
        let doi = Option.defaultValue "<doi>" state.Doi
        let createPersonFieldTextInput(field: string option, label, personSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                label,
                (fun s -> 
                    let s = if s = "" then None else Some s
                    personSetter s 
                    state.ToPublication() |> setter),
                fullwidth=true
            )
        let countFilledFieldsString () =
            let fields = [
                state.PubmedId
                state.Doi
                state.Title
                state.Authors
                state.Comments |> Option.map (fun _ -> "")
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
                        createPersonFieldTextInput(state.PubmedId, "PubMed Id", fun s -> state.PubmedId <- s)
                        createPersonFieldTextInput(state.Doi, "DOI", fun s -> state.Doi <- s)
                        Bulma.field.div [
                            Bulma.label [
                                prop.style [style.opacity 0; style.userSelect.none]
                                prop.text "-"
                            ]
                            Bulma.button.a [
                                Bulma.button.isOutlined
                                Bulma.color.isInfo
                                prop.text "Search"
                            ]
                        ]
                    ]
                    createPersonFieldTextInput(state.Authors, "Authors", fun s -> state.Authors <- s)
                    FormComponents.OntologyAnnotationInput(
                        Option.defaultValue OntologyAnnotation.empty state.Status, 
                        (fun s -> 
                            state.Status <- if s = OntologyAnnotation.empty then None else Some s
                            state.ToPublication() |> setter
                        ),
                        "Status"
                    )
                    FormComponents.CommentsInput(
                        Option.defaultValue [||] state.Comments, 
                        "Comments", 
                        (fun c -> 
                            state.Comments <- if c = [||] then None else Some c
                            state.ToPublication() |> setter
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
    static member FactorInput(input: Factor, setter: Factor -> unit, ?deletebutton: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        // Must use `React.useRef` do this. Otherwise simultanios updates will overwrite each other
        let state, setState = React.useState(Helper.FactorMutable.fromFactor input) 
        React.useEffect((fun _ -> setState <| Helper.FactorMutable.fromFactor input), [|box input|])
        let name = Option.defaultValue "<name>" state.Name
        let type' = Option.defaultValue "<type>" (state.FactorType |> Option.map (fun x -> x.NameText))
        let createFieldTextInput(field: string option, label, personSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                label,
                (fun s -> 
                    let s = if s = "" then None else Some s
                    personSetter s 
                    state.ToFactor() |> setter),
                fullwidth=true
            )
        let countFilledFieldsString () =
            let fields = [
                state.Name
                state.FactorType |> Option.map (fun _ -> "")
                state.Comments |> Option.map (fun _ -> "")
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
                            Bulma.subtitle.h6 type'
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
                    FormComponents.OntologyAnnotationInput(
                        Option.defaultValue OntologyAnnotation.empty state.FactorType, 
                        (fun s -> 
                            state.FactorType <- if s = OntologyAnnotation.empty then None else Some s
                            state.ToFactor() |> setter
                        ),
                        "Status"
                    )
                    FormComponents.CommentsInput(
                        Option.defaultValue [||] state.Comments, 
                        "Comments", 
                        (fun c -> 
                            state.Comments <- if c = [||] then None else Some c
                            state.ToFactor() |> setter
                        )
                    )
                    if deletebutton.IsSome then
                        Helper.deleteButton deletebutton.Value
                ]
            ]
        ]

    static member FactorsInput(input: Factor [], label: string, setter: Factor [] -> unit) =
        FormComponents.InputSequence(
            input,
            Factor.create(),
            label,
            setter,
            (fun (a,b,c,d) -> FormComponents.FactorInput(a,c,deletebutton=d))
        )

    [<ReactComponent>]
    static member OntologySourceReferenceInput(input: OntologySourceReference, setter: OntologySourceReference -> unit, ?deletebutton: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        // Must use `React.useRef` do this. Otherwise simultanios updates will overwrite each other
        let state, setState = React.useState(Helper.OntologySourceReferenceMutable.fromOntologySourceReference input) 
        React.useEffect((fun _ -> setState <| Helper.OntologySourceReferenceMutable.fromOntologySourceReference input), [|box input|])
        let name = Option.defaultValue "<name>" state.Name
        let version = Option.defaultValue "<version>" state.Version
        let createFieldTextInput(field: string option, label, personSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                label,
                (fun s -> 
                    let s = if s = "" then None else Some s
                    personSetter s 
                    state.ToOntologySourceReference() |> setter),
                fullwidth=true
            )
        let countFilledFieldsString () =
            let fields = [
                state.Name
                state.File
                state.Version
                state.Description
                state.Comments |> Option.map (fun _ -> "")
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
                            state.ToOntologySourceReference() |> setter),
                        fullwidth=true,
                        isarea=true
                    )
                    FormComponents.CommentsInput(
                        Option.defaultValue [||] state.Comments, 
                        "Comments", 
                        (fun c -> 
                            state.Comments <- if c = [||] then None else Some c
                            state.ToOntologySourceReference() |> setter
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