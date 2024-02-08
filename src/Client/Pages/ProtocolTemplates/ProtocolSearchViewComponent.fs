namespace Protocol

open Shared
open TemplateTypes
open Model
open Messages.Protocol
open Messages

open Feliz
open Feliz.Bulma

module ComponentAux = 

    let curatedOrganisationNames = [
        "dataplant"
        "nfdi4plants"
    ]

    /// Fields of Template that can be searched
    [<RequireQualifiedAccess>]
    type SearchFields =
    | Name
    | Organisation
    | Authors

        static member private ofFieldString (str:string) =
            let str = str.ToLower()
            match str with
            | "/o" | "/org"             -> Some Organisation
            | "/a" | "/authors"         -> Some Authors
            | "/n" | "/reset" | "/e"    -> Some Name
            | _ -> None

        member this.toStr =
            match this with
            | Name          -> "/name"
            | Organisation  -> "/org"
            | Authors       -> "/auth"

        member this.toNameRdb =
            match this with
            | Name          -> "template name"
            | Organisation  -> "organisation"
            | Authors       -> "authors"

        static member GetOfQuery(query:string) =
            SearchFields.ofFieldString query

    open ARCtrl.ISA

    type ProtocolViewState = {
            ProtocolSearchQuery     : string
            ProtocolTagSearchQuery  : string
            ProtocolFilterTags      : OntologyAnnotation list
            ProtocolFilterErTags    : OntologyAnnotation list
            CommunityFilter         : Model.Protocol.CommunityFilter
            TagFilterIsAnd          : bool
            Searchfield             : SearchFields
    } with
        static member init () = {
            ProtocolSearchQuery     = ""
            ProtocolTagSearchQuery  = ""
            ProtocolFilterTags      = []
            ProtocolFilterErTags    = []
            CommunityFilter         = Model.Protocol.CommunityFilter.All
            TagFilterIsAnd          = true
            Searchfield             = SearchFields.Name
        }

    [<LiteralAttribute>]
    let SearchFieldId = "template_searchfield_main"

    let queryField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
        Html.div [
            Bulma.label $"Search by {state.Searchfield.toNameRdb}"
            let hasSearchAddon = state.Searchfield <> SearchFields.Name
            Bulma.field.div [
                if hasSearchAddon then Bulma.field.hasAddons
                prop.children [
                    if hasSearchAddon then
                        Bulma.control.div [
                            Bulma.button.a [ Bulma.button.isStatic; prop.text state.Searchfield.toStr]
                        ]
                    Bulma.control.div [
                        Bulma.control.hasIconsRight
                        prop.children [
                            Bulma.input.text [
                                prop.style [style.minWidth 200]
                                prop.placeholder $".. {state.Searchfield.toNameRdb}"
                                prop.id SearchFieldId
                                Bulma.color.isPrimary
                                prop.valueOrDefault state.ProtocolSearchQuery
                                prop.onChange (fun (e: string) ->
                                    let query = e
                                    // if query starts with "/" expect intend to search by different field
                                    if query.StartsWith "/" then
                                        let searchField = SearchFields.GetOfQuery query
                                        if searchField.IsSome then
                                            {state with Searchfield = searchField.Value; ProtocolSearchQuery = ""} |> setState
                                            //let inp = Browser.Dom.document.getElementById SearchFieldId
                                    // if query starts NOT with "/" update query
                                    else
                                        {
                                            state with
                                                ProtocolSearchQuery = query
                                        }
                                        |> setState
                                )
                            ]
                            Bulma.icon [Bulma.icon.isSmall; Bulma.icon.isRight; prop.children (Html.i [prop.className "fa-solid fa-search"])]
                        ]
                    ]
                ]
            ]
        ]

    let tagQueryField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
        let allTags = model.ProtocolState.Templates |> Array.collect (fun x -> x.Tags) |> Array.distinct |> Array.filter (fun x -> state.ProtocolFilterTags |> List.contains x |> not )
        let allErTags = model.ProtocolState.Templates |> Array.collect (fun x -> x.EndpointRepositories) |> Array.distinct |> Array.filter (fun x -> state.ProtocolFilterErTags |> List.contains x |> not )
        let hitTagList, hitErTagList =
            if state.ProtocolTagSearchQuery <> ""
            then
                let queryBigram = state.ProtocolTagSearchQuery |> Shared.SorensenDice.createBigrams 
                let getMatchingTags (allTags: OntologyAnnotation []) =
                    allTags
                    |> Array.map (fun x ->
                        x.NameText
                        |> Shared.SorensenDice.createBigrams
                        |> Shared.SorensenDice.calculateDistance queryBigram
                        , x
                    )
                    |> Array.filter (fun x -> fst x >= 0.3 || (snd x).TermAccessionShort = state.ProtocolTagSearchQuery)
                    |> Array.sortByDescending fst
                    |> Array.map snd
                let sortedTags = getMatchingTags allTags
                let sortedErTags = getMatchingTags allErTags
                sortedTags, sortedErTags
            else
                [||], [||]
        Html.div [
            Bulma.label "Search for tags"
            Bulma.control.div [
                Bulma.control.hasIconsRight
                prop.children [
                    Bulma.input.text [
                        prop.style [style.minWidth 150]
                        prop.placeholder ".. protocol tag"
                        Bulma.color.isPrimary
                        prop.valueOrDefault state.ProtocolTagSearchQuery
                        prop.onChange (fun (e:string) ->
                            {state with ProtocolTagSearchQuery = e} |> setState
                        )
                    ]
                    Bulma.icon [
                        Bulma.icon.isSmall; Bulma.icon.isRight
                        Html.i [prop.className "fa-solid fa-search"] |> prop.children
                    ]
                    // Pseudo dropdown
                    Bulma.box [
                        prop.style [
                            style.position.absolute
                            style.width(length.perc 100)
                            style.zIndex 10
                            if hitTagList |> Array.isEmpty && hitErTagList |> Array.isEmpty then style.display.none
                        ]
                        prop.children [
                            if hitErTagList <> [||] then
                                Bulma.label "Endpoint Repositories"
                                Bulma.tags [
                                    for tagSuggestion in hitErTagList do
                                        yield
                                            Bulma.tag [
                                                prop.className "clickableTag"
                                                Bulma.color.isLink
                                                prop.onClick (fun _ ->
                                                    let nextState = {
                                                        state with
                                                            ProtocolFilterErTags = tagSuggestion::state.ProtocolFilterErTags
                                                            ProtocolTagSearchQuery = ""
                                                    }
                                                    setState nextState
                                                )
                                                prop.title tagSuggestion.TermAccessionShort
                                                prop.text tagSuggestion.NameText
                                            ]
                                ]
                            if hitTagList <> [||] then
                                Bulma.label "Tags"
                                Bulma.tags [
                                    for tagSuggestion in hitTagList do
                                        yield
                                            Bulma.tag [
                                                prop.className "clickableTag"
                                                Bulma.color.isInfo
                                                prop.onClick (fun _ ->
                                                    let nextState = {
                                                            state with
                                                                ProtocolFilterTags = tagSuggestion::state.ProtocolFilterTags
                                                                ProtocolTagSearchQuery = ""
                                                        }
                                                    setState nextState
                                                    //AddProtocolTag tagSuggestion |> ProtocolMsg |> dispatch
                                                )
                                                prop.title tagSuggestion.TermAccessionShort
                                                prop.text tagSuggestion.NameText
                                            ]
                                ]
                        ]
                    ]
                ]
            ]
        ]

    open Fable.Core.JsInterop

    let communitySelectField (model) (state: ProtocolViewState) setState =
        let options = [
            Model.Protocol.CommunityFilter.All
            Model.Protocol.CommunityFilter.OnlyCurated
            Model.Protocol.CommunityFilter.OnlyCommunity
        ]
        Html.div [
            Bulma.label "Search for tags"
            Bulma.control.div [
                Bulma.control.isExpanded 
                prop.children [
                    Bulma.select [
                        prop.onChange(fun (e: Browser.Types.Event) ->
                            let filter = Model.Protocol.CommunityFilter.fromString e.target?value
                            {state with CommunityFilter = filter} |> setState
                        )
                        prop.children [
                            for option in options do
                                Html.option [
                                    prop.selected (state.CommunityFilter = option)
                                    prop.value (option.ToStringRdb())                                   
                                    prop.text (option.ToStringRdb())                                   
                                ]
                        ]
                    ]
                ]
            ]
        ]

    let tagDisplayField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
        Bulma.columns [
            Bulma.columns.isMobile
            prop.children [
                Bulma.column [
                    Bulma.field.div [
                        Bulma.field.isGroupedMultiline
                        prop.children [
                            for selectedTag in state.ProtocolFilterErTags do
                                yield Bulma.control.div [
                                    Bulma.tags [
                                        Bulma.tags.hasAddons
                                        prop.children [
                                            Bulma.tag [Bulma.color.isLink; prop.style [style.borderWidth 0]; prop.text selectedTag.NameText]
                                            Bulma.delete [
                                                prop.className "clickableTagDelete"
                                                prop.onClick (fun _ ->
                                                    {state with ProtocolFilterErTags = state.ProtocolFilterErTags |> List.except [selectedTag]} |> setState
                                                    //RemoveProtocolErTag selectedTag |> ProtocolMsg |> dispatch
                                                )
                                            ]
                                        ]
                                    ]
                                ]
                            for selectedTag in state.ProtocolFilterTags do
                                yield Bulma.control.div [
                                    Bulma.tags [
                                        Bulma.tags.hasAddons
                                        prop.children [
                                            Bulma.tag [Bulma.color.isInfo; prop.style [style.borderWidth 0]; prop.text selectedTag.NameText]
                                            Bulma.delete [
                                                prop.className "clickableTagDelete"
                                                //Tag.Color IsWarning;
                                                prop.onClick (fun _ ->
                                                        {state with ProtocolFilterTags = state.ProtocolFilterTags |> List.except [selectedTag]} |> setState
                                                        //RemoveProtocolTag selectedTag |> ProtocolMsg |> dispatch
                                                )
                                            ]
                                        ]
                                    ]
                                ]
                        ]
                    ]
                ]
                // tag filter (AND or OR) 
                Bulma.column [
                    Bulma.column.isNarrow
                    prop.title (if state.TagFilterIsAnd then "Templates contain all tags." else "Templates contain at least one tag.")
                    Switch.checkbox [
                        Bulma.color.isDark
                        prop.style [style.userSelect.none]
                        switch.isOutlined
                        switch.isSmall
                        prop.id "switch-2"
                        prop.isChecked state.TagFilterIsAnd
                        prop.onChange (fun (e:bool) ->
                            {state with TagFilterIsAnd = not state.TagFilterIsAnd} |> setState
                            //UpdateTagFilterIsAnd (not state.TagFilterIsAnd) |> ProtocolMsg |> dispatch
                        )
                        prop.children (if state.TagFilterIsAnd then Html.b "And" else Html.b "Or")
                    ] |> prop.children
                ]
            ]
        ]

    let fileSortElements (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =

        Html.div [
            prop.style [style.marginBottom(length.rem 0.75); style.display.flex]
            prop.children [
                Html.div [
                    prop.className "template-filter-container"
                    prop.children [
                        queryField model state setState
                        tagQueryField model state setState
                        communitySelectField model state setState
                    ]
                ]
                // Only show the tag list and tag filter (AND or OR) if any tag exists
                if state.ProtocolFilterErTags <> [] || state.ProtocolFilterTags <> [] then
                    tagDisplayField model state setState
            ]
        ]

    let curatedTag = Bulma.tag [prop.text "curated"; Bulma.color.isSuccess]
    let communitytag = Bulma.tag [prop.text "community"; Bulma.color.isWarning]
    let curatedCommunityTag =
        Bulma.tag [
            prop.style [style.custom("background", "linear-gradient(90deg, rgba(31,194,167,1) 50%, rgba(255,192,0,1) 50%)")]
            Bulma.color.isSuccess
            prop.children [
                Html.span [prop.style [style.marginRight (length.em 0.75)]; prop.text "cur"]
                Html.span [prop.style [style.marginLeft (length.em 0.75); style.color "rgba(0, 0, 0, 0.7)"]; prop.text "com"]  
            ]
        ]

    let createAuthorStringHelper (author: Person) = 
        let mi = if author.MidInitials.IsSome then author.MidInitials.Value else ""
        $"{author.FirstName} {mi} {author.LastName}"
    let createAuthorsStringHelper (authors: Person []) = authors |> Array.map createAuthorStringHelper |> String.concat ", "

    let protocolElement i (template:ARCtrl.Template.Template) (isShown:bool) (setIsShown: bool -> unit) (model:Model) dispatch  =
        [
            Html.tr [
                prop.key $"{i}_{template.Id}"
                prop.classes [ "nonSelectText"; if isShown then "hoverTableEle"]
                prop.style [
                    style.cursor.pointer; style.userSelect.none;
                ]
                prop.onClick (fun e ->
                    e.preventDefault()
                    setIsShown (not isShown)
                    //if isActive then
                    //    UpdateDisplayedProtDetailsId None |> ProtocolMsg |> dispatch
                    //else
                    //    UpdateDisplayedProtDetailsId (Some i) |> ProtocolMsg |> dispatch
                )
                prop.children [
                    Html.td template.Name
                    Html.td (
                        if curatedOrganisationNames |> List.contains (template.Organisation.ToString().ToLower()) then
                            curatedTag
                        else
                            communitytag
                    )
                    //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ a [ OnClick (fun e -> e.stopPropagation()); Href prot.DocsLink; Target "_Blank"; Title "docs" ] [Fa.i [Fa.Size Fa.Fa2x ; Fa.Regular.FileAlt] []] ]
                    Html.td [ prop.style [style.textAlign.center; style.verticalAlign.middle]; prop.text template.Version ]
                    //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str (string template.Used) ]
                    Html.td (
                        Bulma.icon [Html.i [prop.className "fa-solid fa-chevron-down"]]
                    )
                ]
            ]
            Html.tr [
                Html.td [
                    prop.style [
                        style.padding 0
                        if isShown then
                            style.borderBottom (2, borderStyle.solid, "black")
                        else
                            style.display.none
                    ]
                    prop.colSpan 4
                    prop.children [
                        Bulma.box [
                            prop.style [style.borderRadius 0]
                            prop.children [
                                Html.div [
                                    Html.div template.Description
                                    Html.div [
                                        Html.div [ Html.b "Author: "; Html.span (createAuthorsStringHelper template.Authors) ]
                                        Html.div [ Html.b "Created: "; Html.span (template.LastUpdated.ToString("yyyy/MM/dd")) ]
                                    ]
                                    Html.div [
                                        Html.div [ Html.b "Organisation: "; Html.span (template.Organisation.ToString()) ]
                                    ]
                                ]
                                Bulma.tags [
                                    for tag in template.EndpointRepositories do
                                        yield
                                            Bulma.tag [Bulma.color.isLink; prop.text tag.NameText; prop.title tag.TermAccessionShort]
                                ]
                                Bulma.tags [
                                    for tag in template.Tags do
                                        yield
                                            Bulma.tag [Bulma.color.isInfo; prop.text tag.NameText; prop.title tag.TermAccessionShort]
                                ]
                                Bulma.button.a [
                                    prop.onClick (fun _ -> SelectProtocol template |> ProtocolMsg |> dispatch)
                                    Bulma.button.isFullWidth; Bulma.color.isSuccess
                                    prop.text "select"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let RefreshButton (model:Messages.Model) dispatch =
        Bulma.button.button [
            Bulma.button.isSmall
            prop.onClick (fun _ -> Messages.Protocol.GetAllProtocolsForceRequest |> ProtocolMsg |> dispatch)
            prop.children [
                Bulma.icon [Html.i [prop.className "fa-solid fa-arrows-rotate"]]
            ]
        ]

    //let CommunityFilterDropdownItem (filter:Protocol.CommunityFilter) (child: ReactElement) (state:ProtocolViewState) (setState: ProtocolViewState -> unit) =
    //    Bulma.dropdownItem.a [
    //        prop.onClick(fun e ->
    //                e.preventDefault();
    //                {state with CommunityFilter = filter} |> setState
    //                //UpdateCommunityFilter filter |> ProtocolMsg |> dispatch
    //            )
    //        prop.children child
    //    ]

    //let CommunityFilterElement (state:ProtocolViewState) (setState: ProtocolViewState -> unit) =
    //    Bulma.dropdown [
    //        Bulma.dropdown.isHoverable
    //        prop.children [
    //            Bulma.dropdownTrigger [
    //                Bulma.button.button [
    //                    Bulma.button.isSmall; Bulma.button.isOutlined; Bulma.color.isWhite;
    //                    prop.style [style.padding 0]
    //                    match state.CommunityFilter with
    //                    | Protocol.CommunityFilter.All -> curatedCommunityTag
    //                    | Protocol.CommunityFilter.OnlyCommunity -> communitytag
    //                    | Protocol.CommunityFilter.OnlyCurated -> curatedTag
    //                    |> prop.children
    //                ]
    //            ]
    //            Bulma.dropdownMenu [
    //                prop.style [style.minWidth.unset; style.fontWeight.normal]
    //                Bulma.dropdownContent [
    //                    CommunityFilterDropdownItem Protocol.CommunityFilter.All curatedCommunityTag state setState
    //                    CommunityFilterDropdownItem Protocol.CommunityFilter.OnlyCurated curatedTag state setState
    //                    CommunityFilterDropdownItem Protocol.CommunityFilter.OnlyCommunity communitytag state setState
    //                ] |> prop.children
    //            ]
    //        ]
    //    ]

open Feliz
open System
open ComponentAux


type Search =

    static member InfoField() =
        Bulma.field.div [
                Bulma.help [
                    Html.b "Search for templates."
                    Html.span " For more information you can look "
                    Html.a [ prop.href Shared.URLs.SwateWiki; prop.target "_Blank"; prop.text "here"]
                    Html.span ". If you find any problems with a template or have other suggestions you can contact us "
                    Html.a [ prop.href URLs.Helpdesk.UrlTemplateTopic; prop.target "_Blank"; prop.text "here"]
                    Html.span "."
                ]
                Bulma.help [
                    Html.span "You can search by template name, organisation and authors. Just type:"
                    Bulma.content [
                        Html.ul [
                            Html.li [Html.code "/a"; Html.span " to search authors."]
                            Html.li [Html.code "/o"; Html.span " to search organisations."]
                            Html.li [Html.code "/n"; Html.span " to search template names."]
                        ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member FileSortElement(templates, setter, model, dispatch) =
        let templates = model.ProtocolState.Templates
        let sortTableBySearchQuery searchfield (searchQuery: string) (protocol: ARCtrl.Template.Template []) =
            let query = searchQuery.Trim()
            // Only search if field is not empty and does not start with "/".
            // If it starts with "/" and does not match SearchFields then it will never trigger search
            // As soon as it matches SearchFields it will be removed and can become 'query <> ""'
            if query <> "" && query.StartsWith("/") |> not
            then
                let queryBigram = query |> Shared.SorensenDice.createBigrams
                let createScore (str:string) =
                    str
                    |> Shared.SorensenDice.createBigrams
                    |> Shared.SorensenDice.calculateDistance queryBigram
                let scoredTemplate =
                    protocol
                    |> Array.map (fun template ->
                        let score =
                            match searchfield with
                            | SearchFields.Name          ->
                                createScore template.Name
                            | SearchFields.Organisation  ->
                                createScore (template.Organisation.ToString())
                            | SearchFields.Authors       ->
                                let query' = query.ToLower()
                                let scores = template.Authors |> Array.filter (fun author -> 
                                    (createAuthorStringHelper author).ToLower().Contains query'
                                    || (author.ORCID.IsSome && author.ORCID.Value = query)
                                )
                                if Array.isEmpty scores then 0.0 else 1.0
                        score, template
                    )
                    |> Array.filter (fun (score,_) -> score > 0.2)
                    |> Array.sortByDescending fst
                    |> Array.map snd
                scoredTemplate
            else
                protocol
        let filterTableByTags tags ertags tagfilter (protocol:ARCtrl.Template.Template []) =
            if tags <> [] || ertags <> [] then
                protocol |> Array.filter (fun x ->
                    let tags' = Array.append x.Tags x.EndpointRepositories |> Array.distinct
                    let filterTags = tags@ertags |> List.distinct
                    Seq.except filterTags tags'
                        |> fun filteredTags ->
                            // if we want to filter by tag with AND, all tags must match
                            if tagfilter then
                                Seq.length filteredTags = tags'.Length - filterTags.Length
                            // if we want to filter by tag with OR, at least one tag must match
                            else
                                Seq.length filteredTags < tags'.Length
                )
            else
                protocol
        let filterTableByCommunityFilter communityfilter (protocol:ARCtrl.Template.Template []) =
            match communityfilter with
            | Protocol.CommunityFilter.All          -> protocol
            | Protocol.CommunityFilter.OnlyCurated   -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToString().ToLower()) curatedOrganisationNames)
            | Protocol.CommunityFilter.OnlyCommunity -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToString().ToLower()) curatedOrganisationNames |> not)

        let state, setState = React.useState(ProtocolViewState.init)
        let propagateOutside = fun () ->
            let sortedTable =
                if templates.Length = 0 then [||] else
                    model.ProtocolState.Templates
                    |> filterTableByTags state.ProtocolFilterTags state.ProtocolFilterErTags state.TagFilterIsAnd 
                    |> filterTableByCommunityFilter state.CommunityFilter
                    |> sortTableBySearchQuery state.Searchfield state.ProtocolSearchQuery
                    |> Array.sortBy (fun template -> template.Name, template.Organisation)
            setter sortedTable
        React.useEffect(propagateOutside, [|box state|])

        fileSortElements model state setState

    [<ReactComponent>]
    static member Component (templates, model:Model, dispatch, ?maxheight: Styles.ICssUnit) =
        let maxheight = defaultArg maxheight (length.px 600)
        let isEmpty = templates |> isNull || templates |> Array.isEmpty
        let showIds, setShowIds = React.useState(fun _ -> [])
        Html.div [
            prop.style [style.overflow.auto; style.maxHeight maxheight]
            prop.children [
                Bulma.table [
                    Bulma.table.isFullWidth
                    Bulma.table.isStriped
                    prop.className "tableFixHead"
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th "Template Name"
                                //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Documentation"      ]
                                Html.th "Community"//[CommunityFilterElement state setState]
                                Html.th "Template Version"
                                //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Uses"               ]
                                Html.th [
                                    RefreshButton model dispatch
                                ]
                            ]
                        ]
                        Html.tbody [
                            match model.ProtocolState.Loading with
                            | true ->
                                Html.tr [
                                    Html.td [
                                        prop.colSpan 4
                                        prop.style [style.textAlign.center]
                                        prop.children [
                                            Bulma.icon [ 
                                                Bulma.icon.isMedium
                                                prop.children [
                                                    Html.i [prop.className "fa-solid fa-spinner fa-spin fa-lg"]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            | false ->
                                match templates with
                                | [||] ->
                                    Html.tr [ Html.td "Empty" ]
                                | _ ->
                                    for i in 0 .. templates.Length-1 do
                                        let isShown = showIds |> List.contains i
                                        let setIsShown (show: bool) = 
                                            if show then i::showIds |> setShowIds else showIds |> List.filter (fun x -> x <> i) |> setShowIds
                                        yield!
                                            protocolElement i templates.[i] isShown setIsShown model dispatch 
                        ]
                    ]
                ]
            ]
        ]