module Protocol.Component

open Shared
open TemplateTypes
open Model
open Messages.Protocol
open Messages

open Feliz
open Feliz.Bulma

let private curatedOrganisationNames = [
    "dataplant"
    "nfdi4plants"
]

/// Fields of Template that can be searched
[<RequireQualifiedAccess>]
type private SearchFields =
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

type private ProtocolViewState = {
        DisplayedProtDetailsId  : int option
        ProtocolSearchQuery     : string
        ProtocolTagSearchQuery  : string
        ProtocolFilterTags      : OntologyAnnotation list
        ProtocolFilterErTags    : OntologyAnnotation list
        CuratedCommunityFilter  : Model.Protocol.CuratedCommunityFilter
        TagFilterIsAnd          : bool
        Searchfield             : SearchFields
} with
    static member init () = {
        ProtocolSearchQuery     = ""
        ProtocolTagSearchQuery  = ""
        ProtocolFilterTags      = []
        ProtocolFilterErTags    = []
        CuratedCommunityFilter  = Model.Protocol.CuratedCommunityFilter.Both
        TagFilterIsAnd          = true
        DisplayedProtDetailsId  = None
        Searchfield             = SearchFields.Name
    }

[<LiteralAttribute>]
let private SearchFieldId = "template_searchfield_main"

let private queryField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
    Bulma.column  [
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
                                            DisplayedProtDetailsId = None
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

let private tagQueryField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
    let allTags = model.ProtocolState.ProtocolsAll |> Array.collect (fun x -> x.Tags) |> Array.distinct |> Array.filter (fun x -> state.ProtocolFilterTags |> List.contains x |> not )
    let allErTags = model.ProtocolState.ProtocolsAll |> Array.collect (fun x -> x.EndpointRepositories) |> Array.distinct |> Array.filter (fun x -> state.ProtocolFilterErTags |> List.contains x |> not )
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
    Bulma.column [
        Bulma.label "Search for tags"
        Bulma.control.div [
            Bulma.control.hasIconsRight
            prop.children [
                Bulma.input.text [
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
                                                        DisplayedProtDetailsId = None
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
                                                            DisplayedProtDetailsId = None
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

let private tagDisplayField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
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

let private fileSortElements (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =

    Html.div [
        prop.style [style.marginBottom(length.rem 0.75)]
        prop.children [
            Bulma.columns [
                Bulma.columns.isMobile; prop.style [style.marginBottom 0]
                prop.children [
                    queryField model state setState
                    tagQueryField model state setState
                ]
            ]
            // Only show the tag list and tag filter (AND or OR) if any tag exists
            if state.ProtocolFilterErTags <> [] || state.ProtocolFilterTags <> [] then
                tagDisplayField model state setState
        ]
    ]

let private curatedTag = Bulma.tag [prop.text "curated"; Bulma.color.isSuccess]
let private communitytag = Bulma.tag [prop.text "community"; Bulma.color.isWarning]
let private curatedCommunityTag =
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

let private protocolElement i (template:ARCtrl.Template.Template) (model:Model) (state:ProtocolViewState) dispatch (setState: ProtocolViewState -> unit) =
    let isActive =
        match state.DisplayedProtDetailsId with
        | Some id when id = i ->
            true
        | _ ->
            false
    [
        Html.tr [
            prop.key $"{i}_{template.Id}"
            prop.classes [ "nonSelectText"; if isActive then "hoverTableEle"]
            prop.style [
                style.cursor.pointer; style.userSelect.none;
            ]
            prop.onClick (fun e ->
                e.preventDefault()
                { state with
                    DisplayedProtDetailsId = if isActive then None else Some i }
                |> setState
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
                    if isActive then
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

let private curatedCommunityFilterDropdownItem (filter:Protocol.CuratedCommunityFilter) (child: ReactElement) (state:ProtocolViewState) (setState: ProtocolViewState -> unit) =
    Bulma.dropdownItem.a [
        prop.onClick(fun e ->
                e.preventDefault();
                {state with CuratedCommunityFilter = filter} |> setState
                //UpdateCuratedCommunityFilter filter |> ProtocolMsg |> dispatch
            )
        prop.children child
    ]

let private curatedCommunityFilterElement (state:ProtocolViewState) (setState: ProtocolViewState -> unit) =
    Bulma.dropdown [
        Bulma.dropdown.isHoverable
        prop.children [
            Bulma.dropdownTrigger [
                Bulma.button.button [
                    Bulma.button.isSmall; Bulma.button.isOutlined; Bulma.color.isWhite;
                    prop.style [style.padding 0]
                    match state.CuratedCommunityFilter with
                    | Protocol.CuratedCommunityFilter.Both -> curatedCommunityTag
                    | Protocol.CuratedCommunityFilter.OnlyCommunity -> communitytag
                    | Protocol.CuratedCommunityFilter.OnlyCurated -> curatedTag
                    |> prop.children
                ]
            ]
            Bulma.dropdownMenu [
                prop.style [style.minWidth.unset; style.fontWeight.normal]
                Bulma.dropdownContent [
                    curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.Both curatedCommunityTag state setState
                    curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.OnlyCurated curatedTag state setState
                    curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.OnlyCommunity communitytag state setState
                ] |> prop.children
            ]
        ]
    ]

open Feliz
open System

[<ReactComponent>]
let ProtocolContainer (model:Model) dispatch =

    let state, setState = React.useState(ProtocolViewState.init)

    let sortTableBySearchQuery (protocol: ARCtrl.Template.Template []) =
        let query = state.ProtocolSearchQuery.Trim()
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
                        match state.Searchfield with
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
                |> Array.filter (fun (score,_) -> score > 0.1)
                |> Array.sortByDescending fst
                |> Array.map snd
            scoredTemplate
        else
            protocol
    let filterTableByTags (protocol:ARCtrl.Template.Template []) =
        if state.ProtocolFilterTags <> [] || state.ProtocolFilterErTags <> [] then
            protocol |> Array.filter (fun x ->
                let tags = Array.append x.Tags x.EndpointRepositories |> Array.distinct
                let filterTags = state.ProtocolFilterTags@state.ProtocolFilterErTags |> List.distinct
                Seq.except filterTags tags
                    |> fun filteredTags ->
                        // if we want to filter by tag with AND, all tags must match
                        if state.TagFilterIsAnd then
                            Seq.length filteredTags = tags.Length - filterTags.Length
                        // if we want to filter by tag with OR, at least one tag must match
                        else
                            Seq.length filteredTags < tags.Length
            )
        else
            protocol
    let filterTableByCuratedCommunityFilter (protocol:ARCtrl.Template.Template []) =
        match state.CuratedCommunityFilter with
        | Protocol.CuratedCommunityFilter.Both          -> protocol
        | Protocol.CuratedCommunityFilter.OnlyCurated   -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToString().ToLower()) curatedOrganisationNames)
        | Protocol.CuratedCommunityFilter.OnlyCommunity -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToString().ToLower()) curatedOrganisationNames |> not)

    let sortedTable =
        model.ProtocolState.ProtocolsAll
        |> filterTableByTags
        |> filterTableByCuratedCommunityFilter
        |> sortTableBySearchQuery
        |> Array.sortBy (fun template -> template.Name, template.Organisation)

    mainFunctionContainer [
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
        fileSortElements model state setState
        Bulma.table [
            Bulma.table.isFullWidth
            Bulma.table.isStriped
            prop.children [
                Html.thead [
                    Html.tr [
                        Html.th "Template Name"
                        //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Documentation"      ]
                        Html.th [curatedCommunityFilterElement state setState]
                        Html.th "Template Version"
                        //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Uses"               ]
                        Html.th Html.none
                    ]
                ]
                Html.tbody [
                    for i in 0 .. sortedTable.Length-1 do
                        yield!
                            protocolElement i sortedTable.[i] model state dispatch setState
                ]
            ]
        ]
    ]