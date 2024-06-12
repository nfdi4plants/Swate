namespace Protocol

open Shared
open Model
open Messages.Protocol
open Messages

open Feliz
open Feliz.Bulma

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

open ARCtrl

type TemplateFilterConfig = {
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
        CommunityFilter         = Model.Protocol.CommunityFilter.OnlyCurated
        TagFilterIsAnd          = true
        Searchfield             = SearchFields.Name
    }

module ComponentAux = 

    let curatedOrganisationNames = [
        "dataplant"
        "nfdi4plants"
    ]

    [<LiteralAttribute>]
    let SearchFieldId = "template_searchfield_main"

    let queryField (model:Model) (state: TemplateFilterConfig) (setState: TemplateFilterConfig -> unit) =
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

    let tagQueryField (model:Model) (state: TemplateFilterConfig) (setState: TemplateFilterConfig -> unit) =
        let allTags = model.ProtocolState.Templates |> Seq.collect (fun x -> x.Tags) |> Seq.distinct |> Seq.filter (fun x -> state.ProtocolFilterTags |> List.contains x |> not ) |> Array.ofSeq
        let allErTags = model.ProtocolState.Templates |> Seq.collect (fun x -> x.EndpointRepositories) |> Seq.distinct |> Seq.filter (fun x -> state.ProtocolFilterErTags |> List.contains x |> not ) |> Array.ofSeq
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

    let communitySelectField (model: Messages.Model) (state: TemplateFilterConfig) setState =
        let communityNames = 
            model.ProtocolState.Templates 
            |> Array.choose (fun t -> Model.Protocol.CommunityFilter.CommunityFromOrganisation t.Organisation) 
            |> Array.distinct |> List.ofArray
        let options = 
            [
                Model.Protocol.CommunityFilter.All
                Model.Protocol.CommunityFilter.OnlyCurated
            ]@communityNames
        Html.div [
            Bulma.label "Select community"
            Bulma.control.div [
                Bulma.control.isExpanded 
                prop.children [
                    Bulma.select [
                        prop.value (state.CommunityFilter.ToStringRdb())
                        prop.onChange(fun (e: Browser.Types.Event) ->
                            let filter = Model.Protocol.CommunityFilter.fromString e.target?value
                            if state.CommunityFilter <> filter then
                                {state with CommunityFilter = filter} |> setState
                        )
                        prop.children [
                            for option in options do
                                Html.option [
                                    //prop.selected (state.CommunityFilter = option)
                                    prop.value (option.ToStringRdb())                                   
                                    prop.text (option.ToStringRdb())                                   
                                ]
                        ]
                    ]
                ]
            ]
        ]

    let TagRemovableElement (tag:OntologyAnnotation) (color: IReactProperty) (rmv: unit -> unit) =
        Bulma.control.div [
            Bulma.tags [
                prop.style [style.flexWrap.nowrap]
                Bulma.tags.hasAddons
                prop.children [
                    Bulma.tag [color; prop.style [style.borderWidth 0]; prop.text tag.NameText; prop.title tag.TermAccessionShort]
                    Bulma.tag [
                        Bulma.tag.isDelete
                        prop.onClick (fun _ -> rmv())
                    ]
                ]
            ]
        ]

    let SwitchElement (tagIsFilterAnd: bool) (setFilter: bool -> unit) =
        Html.div [
            prop.style [style.marginLeft length.auto]
            prop.children [
                Bulma.button.button [
                    Bulma.button.isSmall
                    prop.onClick (fun _ -> setFilter (not tagIsFilterAnd))
                    prop.title (if tagIsFilterAnd then "Templates contain all tags." else "Templates contain at least one tag.")
                    prop.text (if tagIsFilterAnd then "And" else "Or")
                ]
            ]
        ]

    let TagDisplayField (model:Model) (state: TemplateFilterConfig) (setState: TemplateFilterConfig -> unit) =
        Html.div [
            prop.className "is-flex"
            prop.children [
                Bulma.field.div [
                    Bulma.field.isGroupedMultiline
                    prop.style [style.display.flex; style.flexGrow 1; style.gap (length.rem 0.5); style.flexWrap.wrap; style.flexDirection.row]
                    prop.children [
                        for selectedTag in state.ProtocolFilterErTags do
                            let rmv = fun () -> {state with ProtocolFilterErTags = state.ProtocolFilterErTags |> List.except [selectedTag]} |> setState
                            TagRemovableElement selectedTag Bulma.color.isLink rmv
                        for selectedTag in state.ProtocolFilterTags do
                            let rmv = fun () -> {state with ProtocolFilterTags = state.ProtocolFilterTags |> List.except [selectedTag]} |> setState
                            TagRemovableElement selectedTag Bulma.color.isInfo rmv
                    ]
                ]
                // tag filter (AND or OR) 
                let filtersetter = fun b -> setState {state with TagFilterIsAnd = b}
                SwitchElement state.TagFilterIsAnd filtersetter
            ]
        ]

    let fileSortElements (model:Model) (state: TemplateFilterConfig) (setState: TemplateFilterConfig -> unit) =

        Html.div [
            prop.style [style.marginBottom(length.rem 0.75); style.display.flex; style.flexDirection.column]
            prop.children [
                Bulma.field.div [
                    prop.className "template-filter-container"
                    prop.children [
                        queryField model state setState
                        tagQueryField model state setState
                        communitySelectField model state setState
                    ]
                ]
                // Only show the tag list and tag filter (AND or OR) if any tag exists
                if state.ProtocolFilterErTags <> [] || state.ProtocolFilterTags <> [] then
                    Bulma.field.div [
                        TagDisplayField model state setState
                    ]
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
    let createAuthorsStringHelper (authors: ResizeArray<Person>) = authors |> Seq.map createAuthorStringHelper |> String.concat ", "

    let protocolElement i (template:ARCtrl.Template) (isShown:bool) (setIsShown: bool -> unit) (model:Model) dispatch  =
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
                    Html.td [
                        prop.text template.Name
                        prop.key $"{i}_{template.Id}_name"
                    ]
                    Html.td [
                        prop.key $"{i}_{template.Id}_tag"
                        prop.children [
                            if curatedOrganisationNames |> List.contains (template.Organisation.ToString().ToLower()) then
                                curatedTag
                            else
                                communitytag
                        ]
                    ]
                    //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ a [ OnClick (fun e -> e.stopPropagation()); Href prot.DocsLink; Target "_Blank"; Title "docs" ] [Fa.i [Fa.Size Fa.Fa2x ; Fa.Regular.FileAlt] []] ]
                    Html.td [ prop.key $"{i}_{template.Id}_version"; prop.style [style.textAlign.center; style.verticalAlign.middle]; prop.text template.Version ]
                    //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str (string template.Used) ]
                    Html.td [
                        prop.key $"{i}_{template.Id}_button"
                        prop.children [Bulma.icon [Html.i [prop.className "fa-solid fa-chevron-down"]] ]
                    ]
                ]
            ]
            Html.tr [
                Html.td [
                    prop.key $"{i}_{template.Id}_description"
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

module FilterHelper =
    open ComponentAux

    let sortTableBySearchQuery (searchfield: SearchFields) (searchQuery: string) (protocol: ARCtrl.Template []) =
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
                            let scores = template.Authors |> Seq.filter (fun author -> 
                                (createAuthorStringHelper author).ToLower().Contains query'
                                || (author.ORCID.IsSome && author.ORCID.Value = query)
                            )
                            if Seq.isEmpty scores then 0.0 else 1.0
                    score, template
                )
                |> Array.filter (fun (score,_) -> score > 0.2)
                |> Array.sortByDescending fst
                |> Array.map snd
            scoredTemplate
        else
            protocol
    let filterTableByTags tags ertags tagfilter (templates: ARCtrl.Template []) =
        if tags <> [] || ertags <> [] then
            let tagArray = tags@ertags |> ResizeArray
            let filteredTemplates = ResizeArray templates |> ARCtrl.Templates.filterByOntologyAnnotation(tagArray, tagfilter)
            Array.ofSeq filteredTemplates
        else
            templates
    let filterTableByCommunityFilter communityfilter (protocol:ARCtrl.Template []) =
        match communityfilter with
        | Protocol.CommunityFilter.All              -> protocol
        | Protocol.CommunityFilter.OnlyCurated      -> protocol |> Array.filter (fun x -> x.Organisation.IsOfficial())
        | Protocol.CommunityFilter.Community name   -> protocol |> Array.filter (fun x -> x.Organisation.ToString() = name)

open Feliz
open System
open ComponentAux


type Search =

    static member InfoField() =
        Bulma.field.div [
            Bulma.content [
                Html.b "Search for templates."
                Html.span " For more information you can look "
                Html.a [ prop.href Shared.URLs.SwateWiki; prop.target "_Blank"; prop.text "here"]
                Html.span ". If you find any problems with a template or have other suggestions you can contact us "
                Html.a [ prop.href URLs.Helpdesk.UrlTemplateTopic; prop.target "_Blank"; prop.text "here"]
                Html.span "."
            ]
            Bulma.content [
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

    static member filterTemplates(templates: ARCtrl.Template [], config: TemplateFilterConfig) =
        if templates.Length = 0 then [||] else
            templates
            |> Array.ofSeq
            |> FilterHelper.filterTableByTags config.ProtocolFilterTags config.ProtocolFilterErTags config.TagFilterIsAnd 
            |> FilterHelper.filterTableByCommunityFilter config.CommunityFilter
            |> FilterHelper.sortTableBySearchQuery config.Searchfield config.ProtocolSearchQuery
            |> Array.sortBy (fun template -> template.Name, template.Organisation)

    [<ReactComponent>]
    static member FileSortElement(model, config, configSetter: TemplateFilterConfig -> unit) =
        fileSortElements model config configSetter

    [<ReactComponent>]
    static member Component (templates, model:Model, dispatch, ?maxheight: Styles.ICssUnit) =
        let maxheight = defaultArg maxheight (length.px 600)
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