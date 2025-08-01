namespace Protocol

open Model
open Messages.Protocol
open Messages

open Feliz
open Feliz.DaisyUI

open Modals

open FileImport

open Swate.Components
open Swate.Components.Shared

/// Fields of Template that can be searched
[<RequireQualifiedAccess>]
type SearchFields =
    | Name
    | Organisation
    | Authors

    static member private ofFieldString(str: string) =
        let str = str.ToLower()

        match str with
        | "/o"
        | "/org" -> Some Organisation
        | "/a"
        | "/authors" -> Some Authors
        | "/n"
        | "/reset"
        | "/e" -> Some Name
        | _ -> None

    member this.toStr =
        match this with
        | Name -> "/name"
        | Organisation -> "/org"
        | Authors -> "/auth"

    member this.toNameRdb =
        match this with
        | Name -> "template name"
        | Organisation -> "organisation"
        | Authors -> "authors"

    static member GetOfQuery(query: string) = SearchFields.ofFieldString query

open ARCtrl

type TemplateFilterConfig = {
    ProtocolSearchQuery: string
    ProtocolTagSearchQuery: string
    ProtocolFilterTags: OntologyAnnotation list
    ProtocolFilterErTags: OntologyAnnotation list
    CommunityFilter: Model.Protocol.CommunityFilter
    TagFilterIsAnd: bool
    Searchfield: SearchFields
} with

    static member init() = {
        ProtocolSearchQuery = ""
        ProtocolTagSearchQuery = ""
        ProtocolFilterTags = []
        ProtocolFilterErTags = []
        CommunityFilter = Model.Protocol.CommunityFilter.OnlyCurated
        TagFilterIsAnd = true
        Searchfield = SearchFields.Name
    }

module ComponentAux =

    let ErBadgeColor = badge.primary
    let TagBadgeColor = badge.accent

    let curatedOrganisationNames = [ "dataplant"; "nfdi4plants" ]

    [<LiteralAttribute>]
    let SearchFieldId = "template_searchfield_main"

    let queryField (model: Model) (state: TemplateFilterConfig) (setState: TemplateFilterConfig -> unit) =
        Html.div [
            Html.p $"Search by {state.Searchfield.toNameRdb}"
            let hasSearchAddon = state.Searchfield <> SearchFields.Name

            //Daisy.join [
            Html.div [
                prop.className "swt:join swt:w-full"
                prop.children [
                    if hasSearchAddon then
                        //Daisy.button.a [
                        Html.button [
                            prop.className "swt:btn swt:btn-disabled swt:join-item swt:w-full swt:!text-base-content swt:!border-primary"
                            prop.readOnly true
                            prop.text state.Searchfield.toStr
                        ]
                    //Daisy.label [
                    Html.label [
                        prop.className "swt:label swt:join-item swt:input swt:input-sm swt:input-primary swt:flex swt:items-center swt:w-full"
                        prop.children [
                            Html.input [
                                prop.placeholder $".. {state.Searchfield.toNameRdb}"
                                prop.id SearchFieldId
                                prop.valueOrDefault state.ProtocolSearchQuery
                                prop.onChange (fun (e: string) ->
                                    let query = e
                                    // if query starts with "/" expect intend to search by different field
                                    if query.StartsWith "/" then
                                        let searchField = SearchFields.GetOfQuery query

                                        if searchField.IsSome then
                                            {
                                                state with
                                                    Searchfield = searchField.Value
                                                    ProtocolSearchQuery = ""
                                            }
                                            |> setState
                                    //let inp = Browser.Dom.document.getElementById SearchFieldId
                                    // if query starts NOT with "/" update query
                                    else
                                        {
                                            state with
                                                ProtocolSearchQuery = query
                                        }
                                        |> setState)
                            ]
                            Icons.MagnifyingClass()
                        ]
                    ]
                ]
            ]
        ]

    let Tag
        (
            tag: OntologyAnnotation,
            color: IReactProperty,
            isRemovable: bool,
            onclick: (Browser.Types.MouseEvent -> unit) option
        ) =
        //Daisy.badge [
        Html.div [
            color
            prop.className [
                "swt:badge"
                if onclick.IsSome then
                    "swt:cursor-pointer"
                "swt:text-nowrap"
            ]
            if onclick.IsSome then
                prop.onClick (onclick.Value)
            prop.children [
                if isRemovable then
                    Svg.svg [
                        svg.xmlns "http://www.w3.org/2000/svg"
                        svg.fill "none"
                        svg.viewBox (0, 0, 24, 24)
                        svg.className "swt:inline-block swt:h-4 swt:w-4 swt:stroke-current"
                        svg.children [
                            Svg.path [
                                svg.strokeLineCap "round"
                                svg.strokeLineJoin "round"
                                svg.strokeWidth 2
                                svg.d "M6 18L18 6M6 6l12 12"
                            ]
                        ]
                    ]
                Html.div tag.NameText
            ]
            prop.title tag.TermAccessionShort
        ]

    let TagContainer
        (
            tagList: OntologyAnnotation seq,
            title: string option,
            updateToggle: (OntologyAnnotation -> unit) option,
            badgeColor
        ) =
        React.fragment [
            if title.IsSome then
                //Daisy.divider title.Value
                Html.div [
                    prop.className "swt:divider"
                    prop.text title.Value
                ]
            Html.div [
                prop.className "swt:flex swt:flex-wrap swt:gap-2"
                prop.children [
                    for tagSuggestion in tagList do
                        Tag(
                            tagSuggestion,
                            badgeColor,
                            false,
                            updateToggle |> Option.map (fun f -> fun _ -> f tagSuggestion)
                        )
                ]
            ]
        ]

    let tagQueryField (model: Model) (state: TemplateFilterConfig) (setState: TemplateFilterConfig -> unit) =
        let allTags =
            model.ProtocolState.Templates
            |> Seq.collect (fun x -> x.Tags)
            |> Seq.distinct
            |> Seq.filter (fun x -> state.ProtocolFilterTags |> List.contains x |> not)
            |> Array.ofSeq

        let allErTags =
            model.ProtocolState.Templates
            |> Seq.collect (fun x -> x.EndpointRepositories)
            |> Seq.distinct
            |> Seq.filter (fun x -> state.ProtocolFilterErTags |> List.contains x |> not)
            |> Array.ofSeq

        let hitTagList, hitErTagList =
            if state.ProtocolTagSearchQuery <> "" then
                let queryBigram =
                    state.ProtocolTagSearchQuery
                    |> Swate.Components.Shared.SorensenDice.createBigrams

                let getMatchingTags (allTags: OntologyAnnotation[]) =
                    allTags
                    |> Array.map (fun oa ->
                        oa.NameText
                        |> Swate.Components.Shared.SorensenDice.createBigrams
                        |> Swate.Components.Shared.SorensenDice.calculateDistance queryBigram,
                        oa)
                    |> Array.filter (fun x -> fst x >= 0.3 || (snd x).TermAccessionShort = state.ProtocolTagSearchQuery)
                    |> Array.sortByDescending fst
                    |> Array.map snd

                let sortedTags = getMatchingTags allTags
                let sortedErTags = getMatchingTags allErTags
                sortedTags, sortedErTags
            else
                [||], [||]

        Html.div [
            Html.p "Search for tags"
            Html.div [
                prop.className "swt:relative"
                prop.children [
                    //Daisy.label [
                    Html.label [
                        prop.className "swt:label swt:input swt:input-sm swt:input-primary swt:flex swt:w-full swt:items-center"
                        prop.children [
                            Html.input [
                                prop.placeholder ".. protocol tag"
                                prop.valueOrDefault state.ProtocolTagSearchQuery
                                prop.onChange (fun (e: string) ->
                                    {
                                        state with
                                            ProtocolTagSearchQuery = e
                                    }
                                    |> setState)
                            ]
                            Icons.MagnifyingClass()
                        // Pseudo dropdown
                        ]
                    ]
                    Html.div [
                        prop.className "swt:absolute swt:bg-base-300 swt:shadow-lg rounded-md swt:w-full swt:z-10 swt:p-2 swt:text-base-content"
                        prop.style [
                            if hitTagList |> Array.isEmpty && hitErTagList |> Array.isEmpty then
                                style.display.none
                        ]
                        prop.children [
                            if hitErTagList <> [||] then
                                let updateToggle =
                                    (fun tagSuggestion ->
                                        let nextState = {
                                            state with
                                                ProtocolFilterErTags = tagSuggestion :: state.ProtocolFilterErTags
                                                ProtocolTagSearchQuery = ""
                                        }

                                        setState nextState)

                                TagContainer(
                                    hitErTagList,
                                    Some "Endpoint Repositories",
                                    Some updateToggle,
                                    ErBadgeColor
                                )
                            if hitTagList <> [||] then
                                let updateToggle =
                                    (fun tagSuggestion ->
                                        let nextState = {
                                            state with
                                                ProtocolFilterTags = tagSuggestion :: state.ProtocolFilterTags
                                                ProtocolTagSearchQuery = ""
                                        }

                                        setState nextState)

                                TagContainer(hitTagList, Some "Tags", Some updateToggle, TagBadgeColor)
                        ]
                    ]
                ]
            ]
        ]

    open Fable.Core.JsInterop

    let communitySelectField (model: Model) (state: TemplateFilterConfig) setState =
        let communityNames =
            model.ProtocolState.Templates
            |> Array.choose (fun t -> Model.Protocol.CommunityFilter.CommunityFromOrganisation t.Organisation)
            |> Array.distinct
            |> List.ofArray

        let options =
            [
                Model.Protocol.CommunityFilter.All
                Model.Protocol.CommunityFilter.OnlyCurated
            ]
            @ communityNames

        Html.div [
            Html.p "Select community"
            //Daisy.select [
            Html.select [
                prop.className "swt:select swt:select-sm swt:select-primary swt:w-full"
                prop.value (state.CommunityFilter.ToStringRdb())
                prop.onChange (fun (e: Browser.Types.Event) ->
                    let filter = Model.Protocol.CommunityFilter.fromString e.target?value

                    if state.CommunityFilter <> filter then
                        { state with CommunityFilter = filter } |> setState)
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

    let SwitchElement (tagIsFilterAnd: bool) (setFilter: bool -> unit) =
        Html.div [
            prop.style [ style.marginLeft length.auto ]
            prop.children [
                //Daisy.button.button [
                Html.button [
                    prop.className "swt:btn swt:btn-sm"
                    prop.title (
                        if tagIsFilterAnd then
                            "Templates contain all tags."
                        else
                            "Templates contain at least one tag."
                    )
                    prop.text (if tagIsFilterAnd then "And" else "Or")
                    prop.onClick (fun _ -> setFilter (not tagIsFilterAnd))
                ]
            ]
        ]

    let TagDisplayField (model: Model) (state: TemplateFilterConfig) (setState: TemplateFilterConfig -> unit) =
        Html.div [
            prop.className "swt:flex"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-2"
                    prop.children [
                        for selectedTag in state.ProtocolFilterErTags do
                            let rmv =
                                fun _ ->
                                    {
                                        state with
                                            ProtocolFilterErTags =
                                                state.ProtocolFilterErTags |> List.except [ selectedTag ]
                                    }
                                    |> setState

                            Tag(selectedTag, ErBadgeColor, true, Some rmv)
                        for selectedTag in state.ProtocolFilterTags do
                            let rmv =
                                fun _ ->
                                    {
                                        state with
                                            ProtocolFilterTags = state.ProtocolFilterTags |> List.except [ selectedTag ]
                                    }
                                    |> setState

                            Tag(selectedTag, TagBadgeColor, true, Some rmv)
                    ]
                ]
                // tag filter (AND or OR)
                let filtersetter = fun b -> setState { state with TagFilterIsAnd = b }
                SwitchElement state.TagFilterIsAnd filtersetter
            ]
        ]

    let curatedTag =
        //Daisy.badge [
        Html.div [
            prop.className "swt:badge swt:badge-primary"
            prop.text "curated"
        ]
    let communitytag =
        //Daisy.badge [
        Html.div [
            prop.className "swt:badge swt:badge-warning"
            prop.text "community"
        ]

    let curatedCommunityTag =
        //Daisy.badge [
        Html.div [
            prop.className "swt:badge swt:badge-success"
            prop.style [
                style.custom ("background", "linear-gradient(90deg, rgba(31,194,167,1) 50%, rgba(255,192,0,1) 50%)")
            ]
            prop.children [
                Html.span [ prop.style [ style.marginRight (length.em 0.75) ]; prop.text "cur" ]
                Html.span [
                    prop.style [ style.marginLeft (length.em 0.75); style.color "rgba(0, 0, 0, 0.7)" ]
                    prop.text "com"
                ]
            ]
        ]

    let createAuthorStringHelper (author: Person) =
        let mi =
            if author.MidInitials.IsSome then
                author.MidInitials.Value
            else
                ""

        $"{author.FirstName} {mi} {author.LastName}"

    let createAuthorsStringHelper (authors: ResizeArray<Person>) =
        authors |> Seq.map createAuthorStringHelper |> String.concat ", "

    let protocolElement
        i
        (template: ARCtrl.Template)
        (isShown: bool)
        (setIsShown: bool -> unit)
        (model: Model)
        dispatch
        =
        [
            Html.tr [
                prop.key $"{i}_{template.Id}"
                prop.className [
                    "swt:base-content swt:cursor-pointer swt:hover:bg-base-200"
                    if List.contains template model.ProtocolState.TemplatesSelected then
                        "swt:bg-base-200 swt:shadow-lg"

                ]
                prop.onClick (fun e ->
                    e.preventDefault ()
                    setIsShown (not isShown))
                prop.children [
                    Html.td [ prop.text template.Name; prop.key $"{i}_{template.Id}_name" ]
                    Html.td [
                        prop.key $"{i}_{template.Id}_tag"
                        prop.children [
                            if
                                curatedOrganisationNames
                                |> List.contains (template.Organisation.ToString().ToLower())
                            then
                                curatedTag
                            else
                                communitytag
                        ]
                    ]
                    //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ a [ OnClick (fun e -> e.stopPropagation()); Href prot.DocsLink; Target "_Blank"; Title "docs" ] [Fa.i [Fa.Size Fa.Fa2x ; Fa.Regular.FileAlt] []] ]
                    Html.td [
                        prop.key $"{i}_{template.Id}_version"
                        prop.style [ style.textAlign.center; style.verticalAlign.middle ]
                        prop.text template.Version
                    ]
                    //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str (string template.Used) ]
                    Html.td [
                        prop.key $"{i}_{template.Id}_button"
                        prop.children [ Icons.ChevronDown() ]
                    ]
                ]
            ]
            Html.tr [
                Html.td [
                    prop.key $"{i}_{template.Id}_description"
                    prop.style [
                        if isShown then
                            style.borderBottom (2, borderStyle.solid, "black")
                        else
                            style.display.none
                    ]
                    prop.colSpan 4
                    prop.children [
                        Html.div [
                            prop.className "swt:prose swt:max-w-none swt:p-3 swt:flex swt:flex-col swt:gap-2"
                            prop.children [
                                Html.div [
                                    Html.div template.Description
                                    Html.div [
                                        Html.div [
                                            Html.b "Author: "
                                            Html.span (createAuthorsStringHelper template.Authors)
                                        ]
                                        Html.div [
                                            Html.b "Created: "
                                            Html.span (template.LastUpdated.ToString("yyyy/MM/dd"))
                                        ]
                                    ]
                                    Html.div [
                                        Html.div [
                                            Html.b "Organisation: "
                                            Html.span (template.Organisation.ToString())
                                        ]
                                    ]
                                ]
                                TagContainer(template.EndpointRepositories, None, None, ErBadgeColor)
                                TagContainer(template.Tags, None, None, TagBadgeColor)
                            ]
                        ]
                        Html.div [
                            prop.className "swt:flex swt:justify-center swt:gap-2"
                            prop.children [
                                if List.contains template model.ProtocolState.TemplatesSelected then
                                    let templates = model.ProtocolState.TemplatesSelected |> Array.ofSeq

                                    let templateIndex =
                                        Array.findIndex (fun selectedTemplate -> selectedTemplate = template) templates

                                    //Daisy.button.a [
                                    Html.button [
                                        prop.className "swt:btn swt:btn-primary swt:btn-sm"
                                        prop.onClick (fun _ ->
                                            setIsShown (not isShown)

                                            let newTemplatesSelected =
                                                List.removeAt templateIndex model.ProtocolState.TemplatesSelected

                                            SelectProtocols newTemplatesSelected |> ProtocolMsg |> dispatch)
                                        prop.text "remove"
                                    ]
                                else
                                    //Daisy.button.a [
                                    Html.button [
                                        prop.className "swt:btn swt:btn-primary swt:btn-sm"
                                        prop.onClick (fun _ ->
                                            setIsShown (not isShown)
                                            AddProtocol template |> ProtocolMsg |> dispatch)
                                        prop.text "select"
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let RefreshButton (model: Model) dispatch =
        Daisy.button.button [
            button.sm
            prop.onClick (fun _ -> Messages.Protocol.GetAllProtocolsForceRequest |> ProtocolMsg |> dispatch)
            prop.children [ Icons.ArrowsRotate() ]
        ]

module FilterHelper =

    open ComponentAux

    let sortTableBySearchQuery (searchfield: SearchFields) (searchQuery: string) (protocol: ARCtrl.Template[]) =
        let query = searchQuery.Trim()
        // Only search if field is not empty and does not start with "/".
        // If it starts with "/" and does not match SearchFields then it will never trigger search
        // As soon as it matches SearchFields it will be removed and can become 'query <> ""'
        if query <> "" && query.StartsWith("/") |> not then
            let query = query.ToLower()
            let queryBigram = query |> Swate.Components.Shared.SorensenDice.createBigrams

            let createScore (str: string) =
                str
                |> Swate.Components.Shared.SorensenDice.createBigrams
                |> Swate.Components.Shared.SorensenDice.calculateDistance queryBigram
            // https://github.com/nfdi4plants/Swate/issues/490
            let adjustScore (compareString: string) (score: float) =
                let compareString = compareString.ToLower()

                if compareString.StartsWith query then score + 0.5
                elif compareString.Contains query then score + 0.3
                else score

            let scoredTemplate =
                protocol
                |> Array.map (fun template ->
                    let score =
                        match searchfield with
                        | SearchFields.Name ->
                            let s = template.Name
                            createScore s |> adjustScore s
                        | SearchFields.Organisation ->
                            let s = (template.Organisation.ToString())
                            createScore s |> adjustScore s
                        | SearchFields.Authors ->
                            let scores =
                                template.Authors
                                |> Seq.filter (fun author ->
                                    (createAuthorStringHelper author).ToLower().Contains query
                                    || (author.ORCID.IsSome && author.ORCID.Value = query))

                            if Seq.isEmpty scores then 0.0 else 1.0

                    score, template)
                |> Array.filter (fun (score, _) -> score > 0.3)
                |> Array.sortByDescending fst
                |> fun y ->
                    for score, x in y do
                        log (score, x.Name)

                    y
                |> Array.map snd

            scoredTemplate
        else
            protocol

    let filterTableByTags tags ertags tagfilter (templates: ARCtrl.Template[]) =
        if tags <> [] || ertags <> [] then
            let tagArray = tags @ ertags |> ResizeArray

            let filteredTemplates =
                ResizeArray templates
                |> ARCtrl.Templates.filterByOntologyAnnotation (tagArray, tagfilter)

            Array.ofSeq filteredTemplates
        else
            templates

    let filterTableByCommunityFilter communityfilter (protocol: ARCtrl.Template[]) =
        match communityfilter with
        | Protocol.CommunityFilter.All -> protocol
        | Protocol.CommunityFilter.OnlyCurated -> protocol |> Array.filter (fun x -> x.Organisation.IsOfficial())
        | Protocol.CommunityFilter.Community name ->
            protocol |> Array.filter (fun x -> x.Organisation.ToString() = name)

open ComponentAux

type Search =

    static member InfoField() =
        Html.div [
            prop.className "swt:prose-sm swt:prose-p:m-1 swt:prose-ul:mt-1 swt:max-w-none"
            prop.children [
                Html.p [
                    Html.b "Search for templates."
                    Html.text " For more information you can look "
                    Html.a [
                        prop.href Swate.Components.Shared.URLs.SWATE_WIKI
                        prop.target "_Blank"
                        prop.text "here"
                    ]
                    Html.text ". If you find any problems with a template or have other suggestions you can contact us "
                    Html.a [
                        prop.href URLs.Helpdesk.UrlTemplateTopic
                        prop.target "_Blank"
                        prop.className [ "swt:link" ]
                        prop.text "here"
                    ]
                    Html.text "."
                ]
                Html.p "You can search by template name, organisation and authors. Just type:"
                Html.ul [
                    Html.li [ Html.code "/a"; Html.span " to search authors." ]
                    Html.li [ Html.code "/o"; Html.span " to search organisations." ]
                    Html.li [ Html.code "/n"; Html.span " to search template names." ]
                ]
            ]
        ]

    static member filterTemplates(templates: ARCtrl.Template[], config: TemplateFilterConfig) =
        if templates.Length = 0 then
            [||]
        else
            templates
            |> Array.ofSeq
            |> Array.sortBy (fun template -> template.Name, template.Organisation)
            |> FilterHelper.filterTableByTags
                config.ProtocolFilterTags
                config.ProtocolFilterErTags
                config.TagFilterIsAnd
            |> FilterHelper.filterTableByCommunityFilter config.CommunityFilter
            |> FilterHelper.sortTableBySearchQuery config.Searchfield config.ProtocolSearchQuery

    [<ReactComponent>]
    static member FileSortElement(model, config, configSetter: TemplateFilterConfig -> unit, ?classes: string) =
        React.fragment [
            Html.div [
                prop.className [
                    "swt:grid swt:grid-cols-1 swt:gap-2 swt:px-2"
                    if classes.IsSome then
                        classes.Value
                ]
                prop.children [
                    queryField model config configSetter
                    tagQueryField model config configSetter
                    communitySelectField model config configSetter
                ]
            ]
            // Only show the tag list and tag filter (AND or OR) if any tag exists
            if config.ProtocolFilterErTags <> [] || config.ProtocolFilterTags <> [] then
                Html.div [ TagDisplayField model config configSetter ]
        ]

    static member SelectTemplatesButton(model: Model.Model, dispatch) =
        Html.div [
            prop.className "swt:flex swt:justify-center swt:gap-2"
            prop.children [
                //Daisy.button.a [
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-wide"
                        if model.ProtocolState.TemplatesSelected.Length > 0 then
                            "swt:btn-primary"
                        else
                            "swt:btn-disabled"
                    ]
                    prop.text "Add templates"
                    prop.onClick (fun _ ->
                        SelectProtocols model.ProtocolState.TemplatesSelected |> ProtocolMsg |> dispatch)
                ]
            ]
        ]

    [<ReactComponent>]
    static member Component(templates, model: Model, dispatch, ?maxheight: Styles.ICssUnit) =
        let maxheight = defaultArg maxheight (length.px 600)
        let showIds, setShowIds = React.useState (fun _ -> [])

        Html.div [
            prop.style [ style.maxHeight maxheight ]
            prop.className "swt:shrink swt:overflow-y-auto"
            prop.children [
                Html.table [
                    prop.className "swt:table swt:table-zebra swt:table-pinCols"
                    // prop.className "tableFixHead"
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th "Template Name"
                                //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Documentation"      ]
                                Html.th "Community" //[CommunityFilterElement state setState]
                                Html.th "Template Version"
                                //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Uses"               ]
                                Html.th [ RefreshButton model dispatch ]
                            ]
                        ]
                        Html.tbody [
                            match model.ProtocolState.Loading with
                            | true ->
                                Html.tr [
                                    Html.td [
                                        prop.colSpan 4
                                        prop.style [ style.textAlign.center ]
                                        prop.children [ Icons.SpinningSpinner() ]
                                    ]
                                ]
                            | false ->
                                match templates with
                                | [||] -> Html.tr [ Html.td "Empty" ]
                                | _ ->
                                    for i in 0 .. templates.Length - 1 do
                                        let isShown = showIds |> List.contains i

                                        let setIsShown (show: bool) =
                                            if show then
                                                i :: showIds |> setShowIds
                                            else
                                                showIds |> List.filter (fun id -> id <> i) |> setShowIds

                                        yield! protocolElement i templates.[i] isShown setIsShown model dispatch
                        ]
                    ]
                ]
            ]
        ]