namespace MainComponents.Metadata

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl.ISA
open Shared


module Helper =
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

    let addButton (clickEvent: MouseEvent -> unit) =
        Html.div [
            prop.classes ["is-flex"; "is-justify-content-center"]
            prop.children [
                Bulma.button.button [
                    Bulma.button.isOutlined
                    Bulma.color.isInfo
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

type FormComponents =

    [<ReactComponent>]
    static member TextInput (input: string, label: string, setter: string -> unit, ?fullwidth: bool, ?removebutton: MouseEvent -> unit) =
        let fullwidth = defaultArg fullwidth false
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input)
        let debounceStorage, setdebounceStorage = React.useState(newDebounceStorage)
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
                                Bulma.input.text [
                                    prop.valueOrDefault state
                                    prop.onChange(fun (e: string) ->
                                        setState e
                                        debouncel debounceStorage label 1000 setLoading setter e
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
    static member InputSequence<'A>(inputs: 'A [], empty: 'A, label: string, setter: 'A [] -> unit, inputComponent: 'A * string * ('A -> unit) * (MouseEvent -> unit) -> ReactElement) =
        let texts = React.useRef (inputs)
        React.useEffect((fun _ -> texts.current <- inputs), [|box inputs|])
        Bulma.field.div [
            Bulma.label label
            Bulma.field.div [
                Html.orderedList [
                    yield! texts.current
                    |> Array.mapi (fun i x ->
                        Html.li [
                            inputComponent(
                                x, "",
                                (fun oa -> 
                                    texts.current.[i] <- oa
                                    texts.current |> setter 
                                ),
                                (fun _ ->  
                                    texts.current <- Array.removeAt i texts.current 
                                    texts.current |> setter
                                )
                            )
                        ]
                    )
                ]
            ]
            Helper.addButton (fun _ ->
                texts.current <- Array.append texts.current [|empty|] 
                texts.current |> setter
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
                            if placeholder.IsSome then prop.placeholder placeholder.Value
                            prop.valueOrDefault state
                            prop.onChange(fun (e: string) ->
                                setState e
                                debouncel debounceStorage label 1000 setLoading setter e
                            )
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationInput (oa: OntologyAnnotation, label: string, setter: OntologyAnnotation -> unit, ?showTextLabels: bool, ?removebutton: MouseEvent -> unit) =
        let showTextLabels = defaultArg showTextLabels true
        let oa = React.useRef(Helper.OntologyAnnotationMutable.fromOntologyAnnotation oa) 
        let hasLabel = label <> ""
        Bulma.field.div [ 
            if hasLabel then Bulma.label label
            Bulma.field.div [
                prop.classes ["is-flex"; "is-flex-direction-row"; "is-justify-content-space-between"]
                prop.children [
                    Html.div [
                        prop.classes ["form-container"; if removebutton.IsSome then "pr-2"]
                        prop.children [
                            FormComponents.TextInput(
                                Option.defaultValue "" oa.current.Name,
                                (if showTextLabels then $"Term Name" else ""),
                                (fun s -> 
                                    let s = if s = "" then None else Some s
                                    oa.current.Name <- s 
                                    oa.current.ToOntologyAnnotation() |> setter),
                                fullwidth = true
                            )
                            FormComponents.TextInput(
                                Option.defaultValue "" oa.current.TSR,
                                (if showTextLabels then $"TSR" else ""),
                                (fun s -> 
                                    let s = if s = "" then None else Some s
                                    oa.current.TSR <- s 
                                    oa.current.ToOntologyAnnotation() |> setter),
                                fullwidth = true
                            )
                            FormComponents.TextInput(
                                Option.defaultValue "" oa.current.TAN,
                                (if showTextLabels then $"TAN" else ""),
                                (fun s -> 
                                    let s = if s = "" then None else Some s
                                    oa.current.TAN <- s 
                                    oa.current.ToOntologyAnnotation() |> setter),
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
            (fun (a,b,c,d) -> FormComponents.OntologyAnnotationInput(a,b,c,removebutton=d,?showTextLabels=showTextLabels))
        )
        //let oas = React.useRef (oas)
        //Bulma.field.div [
        //    Bulma.label label
        //    Html.orderedList [
        //        yield! oas.current
        //        |> Array.mapi (fun i role ->
        //            Html.li [
        //                FormComponents.OntologyAnnotationInput(
        //                    role, "",
        //                    (fun oa -> 
        //                        oas.current.[i] <- oa
        //                        oas.current |> setter 
        //                    ), 
        //                    showTextLabels = false, 
        //                    removebutton=(fun _ ->  
        //                        oas.current <- Array.removeAt i oas.current 
        //                        oas.current |> setter
        //                    )
        //                )
        //            ]
        //        )
        //    ]
        //    Helper.addButton (fun _ ->
        //        oas.current <- Array.append oas.current [|OntologyAnnotation.empty|] 
        //        oas.current |> setter
        //    )
        //]


    [<ReactComponent>]
    static member PersonInput(person': Person, setter: Person -> unit, ?deletebutton: MouseEvent -> unit) =
        let isExtended, setIsExtended = React.useState(false)
        // Must use `React.useRef` do this. Otherwise simultanios updates will overwrite each other
        let person = React.useRef(Helper.PersonMutable.fromPerson person') 
        React.useEffect((fun _ -> person.current <- Helper.PersonMutable.fromPerson person'), [|box person|])
        let fn = Option.defaultValue "" person.current.FirstName 
        let ln = Option.defaultValue "" person.current.LastName
        let mi = Option.defaultValue "" person.current.MidInitials
        let nameStr = 
            let x = $"{fn} {mi} {ln}".Trim()
            if x = "" then "<name>" else x
        let orcid = Option.defaultValue "<orcid>" person.current.ORCID
        let createPersonFieldTextInput(field: string option, label, personSetter: string option -> unit) =
            FormComponents.TextInput(
                field |> Option.defaultValue "",
                label,
                (fun s -> 
                    let s = if s = "" then None else Some s
                    personSetter s 
                    person.current.ToPerson() |> setter),
                fullwidth=true
            )
        let countFilledFieldsString (person:Fable.React.IRefValue<Helper.PersonMutable>) =
            let fields = [
                person.current.FirstName
                person.current.LastName
                person.current.MidInitials
                person.current.ORCID
                person.current.Address
                person.current.Affiliation
                person.current.EMail
                person.current.Phone
                person.current.Fax
                person.current.Roles |> Option.map (fun _ -> "")
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
                            prop.text (countFilledFieldsString person)
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
                    Bulma.field.div [
                        Html.div [
                            prop.classes ["form-container"]
                            prop.children [
                                createPersonFieldTextInput(person.current.FirstName, "First Name", fun s -> person.current.FirstName <- s)
                                createPersonFieldTextInput(person.current.LastName, "Last Name", fun s -> person.current.LastName <- s)
                            ]
                        ]
                    ]
                    Bulma.field.div [
                        Html.div [
                            prop.classes ["form-container"]
                            prop.children [
                                createPersonFieldTextInput(person.current.MidInitials, "Mid Initials", fun s -> person.current.MidInitials <- s)
                                createPersonFieldTextInput(person.current.ORCID, "ORCID", fun s -> person.current.ORCID <- s)
                            ]
                        ]
                    ]
                    Bulma.field.div [
                        Html.div [
                            prop.classes ["form-container"]
                            prop.children [
                                createPersonFieldTextInput(person.current.Affiliation, "Affiliation", fun s -> person.current.Affiliation <- s)
                                createPersonFieldTextInput(person.current.Address, "Address", fun s -> person.current.Address <- s)
                            ]
                        ]
                    ]
                    Bulma.field.div [
                        Html.div [
                            prop.classes ["form-container"]
                            prop.children [
                                createPersonFieldTextInput(person.current.EMail, "Email", fun s -> person.current.EMail <- s)
                                createPersonFieldTextInput(person.current.Phone, "Phone", fun s -> person.current.Phone <- s)
                                createPersonFieldTextInput(person.current.Fax, "Fax", fun s -> person.current.Fax <- s)
                            ]
                        ]
                    ]
                    FormComponents.OntologyAnnotationsInput(
                        Option.defaultValue [||] person.current.Roles,
                        "Roles",
                        (fun oas -> 
                            let oas = if oas = [||] then None else Some oas
                            person.current.Roles <- oas
                            person.current.ToPerson() |> setter
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