module Protocol.Search

open System

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome

open Shared
open TemplateTypes

open Model
open Messages.Protocol
open Messages
open Fulma.Extensions.Wikiki

let curatedOrganisationNames = [
    "dataplant"
    "nfdi4plants"
]

let breadcrumbEle (model:Model) dispatch =
    Breadcrumb.breadcrumb [Breadcrumb.HasArrowSeparator] [
        Breadcrumb.item [] [
            a [
                OnClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
            ] [
                str (Routing.Route.Protocol.toStringRdbl)
            ]
        ];
        Breadcrumb.item [
            Breadcrumb.Item.IsActive true
        ] [
            a [
                Style [Color model.SiteStyleState.ColorMode.Text]
                OnClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
            ] [
                str Routing.Route.ProtocolSearch.toStringRdbl
            ]
        ]
    ]

let sortButton icon msg =
    Button.a [
        Button.IsOutlined
        Button.Color IsPrimary
        Button.OnClick msg
    ] [
        Fa.i [ Fa.Size Fa.FaLarge; icon ] [ ] 
    ]

let fileSortElements (model:Model) dispatch =
    let allTags = model.ProtocolState.ProtocolsAll |> Array.collect (fun x -> x.Tags) |> Array.distinct |> Array.filter (fun x -> model.ProtocolState.ProtocolFilterTags |> List.contains x |> not )
    let allErTags = model.ProtocolState.ProtocolsAll |> Array.collect (fun x -> x.Er_Tags) |> Array.distinct |> Array.filter (fun x -> model.ProtocolState.ProtocolFilterErTags |> List.contains x |> not )
    let hitTagList, hitErTagList =
        if model.ProtocolState.ProtocolTagSearchQuery <> ""
        then
            let queryBigram = model.ProtocolState.ProtocolTagSearchQuery |> Shared.SorensenDice.createBigrams 
            let sortedTags =
                allTags
                |> Array.map (fun x ->
                    x
                    |> Shared.SorensenDice.createBigrams
                    |> Shared.SorensenDice.calculateDistance queryBigram
                    , x
                )
                |> Array.filter (fun x -> fst x >= 0.3)
                |> Array.sortByDescending fst
                |> Array.map snd
            let sortedErTags =
                allErTags
                |> Array.map (fun x ->
                    let dist = 
                        x
                        |> Shared.SorensenDice.createBigrams
                        |> Shared.SorensenDice.calculateDistance queryBigram
                    dist , x
                )
                |> Array.filter (fun x -> fst x >= 0.3)
                |> Array.sortByDescending fst
                |> Array.map snd
            sortedTags, sortedErTags
        else
            [||], [||]
    div [ Style [MarginBottom "0.75rem"] ] [
        Columns.columns [Columns.IsMobile; Columns.Props [Style [MarginBottom "0";]]] [
            Column.column [ ] [
                Label.label [Label.Size IsSmall; Label.Props [Style [Color model.SiteStyleState.ColorMode.Text; MinWidth "91px"; WhiteSpace WhiteSpaceOptions.Nowrap]]] [str "Search by protocol name"]
                Control.div [
                    Control.HasIconRight
                ] [
                    Input.text [
                        Input.Placeholder ".. protocol name"
                        Input.Color IsPrimary
                        Input.ValueOrDefault model.ProtocolState.ProtocolNameSearchQuery
                        Input.OnChange (fun e -> UpdateProtocolNameSearchQuery e.Value |> ProtocolMsg |> dispatch)
                    ]
                    Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                        [ Fa.i [ Fa.Solid.Search ]
                            [ ] ] ]
            ]

            Column.column [ ] [
                Label.label [Label.Size IsSmall; Label.Props [Style [Color model.SiteStyleState.ColorMode.Text; MinWidth "91px"; WhiteSpace WhiteSpaceOptions.Nowrap]]] [str "Search for tags"]
                Control.div [
                    Control.HasIconRight
                ] [
                    Input.text [
                        Input.Placeholder ".. protocol tag"
                        Input.Color IsPrimary
                        Input.ValueOrDefault model.ProtocolState.ProtocolTagSearchQuery
                        Input.OnChange (fun e -> UpdateProtocolTagSearchQuery e.Value |> ProtocolMsg |> dispatch)
                    ]
                    Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                        [ Fa.i [ Fa.Solid.Search ]
                            [ ] ]
                    /// Pseudo dropdown
                    Box.box' [Props [Style [
                        yield! ExcelColors.colorControlInArray model.SiteStyleState.ColorMode
                        Position PositionOptions.Absolute
                        Width "100%"
                        ZIndex 10
                        if hitTagList |> Array.isEmpty && hitErTagList |> Array.isEmpty then Display DisplayOptions.None
                    ]]] [
                        if hitErTagList <> [||] then
                            Label.label [] [str "Endpoint Repositories"]
                            Tag.list [] [
                                for tagSuggestion in hitErTagList do
                                    yield
                                        Tag.tag [
                                            Tag.CustomClass "clickableTag"
                                            Tag.Color IsLink
                                            Tag.Props [ OnClick (fun _ -> AddProtocolErTag tagSuggestion |> ProtocolMsg |> dispatch) ]
                                        ] [
                                            str tagSuggestion
                                        ]
                            ]
                        if hitTagList <> [||] then
                            Label.label [] [str "Tags"]
                            Tag.list [] [
                                for tagSuggestion in hitTagList do
                                    yield
                                        Tag.tag [
                                            Tag.CustomClass "clickableTag"
                                            Tag.Color IsInfo
                                            Tag.Props [ OnClick (fun _ -> AddProtocolTag tagSuggestion |> ProtocolMsg |> dispatch) ]
                                        ] [
                                            str tagSuggestion
                                        ]
                            ]
                    ]
                ]
            ]
        ]
        /// Only show the tag list and tag filter (AND or OR) if any tag exists
        if model.ProtocolState.ProtocolFilterErTags <> [] || model.ProtocolState.ProtocolFilterTags <> [] then
            Columns.columns [Columns.IsMobile] [
                Column.column [] [
                    Field.div [Field.IsGroupedMultiline] [
                        for selectedTag in model.ProtocolState.ProtocolFilterErTags do
                            yield
                                Control.div [ ] [
                                    Tag.list [Tag.List.HasAddons] [
                                        Tag.tag [Tag.Color IsLink; Tag.Props [Style [Border "0px"]]] [str selectedTag]
                                        Tag.delete [
                                            Tag.CustomClass "clickableTagDelete"
                                            //Tag.Color IsWarning;
                                            Tag.Props [
                                                OnClick (fun _ -> RemoveProtocolErTag selectedTag |> ProtocolMsg |> dispatch)
                                            ]
                                        ] []
                                    ]
                                ]
                        for selectedTag in model.ProtocolState.ProtocolFilterTags do
                            yield
                                Control.div [ ] [
                                    Tag.list [Tag.List.HasAddons] [
                                        Tag.tag [Tag.Color IsInfo; Tag.Props [Style [Border "0px"]]] [str selectedTag]
                                        Tag.delete [
                                            Tag.CustomClass "clickableTagDelete"
                                            //Tag.Color IsWarning;
                                            Tag.Props [
                                                OnClick (fun _ -> RemoveProtocolTag selectedTag |> ProtocolMsg |> dispatch)
                                            ]
                                        ] []
                                    ]
                                ]
                        ]
                ]
                /// tag filter (AND or OR) 
                Column.column [
                    Column.Width (Screen.All, Column.IsNarrow)
                    Column.Props [Title (if model.ProtocolState.TagFilterIsAnd then "Templates contain all tags." else "Templates contain at least one tag.")]
                ] [
                    Switch.switchInline [
                        Switch.Color Color.IsDark
                        Switch.LabelProps [Style [UserSelect UserSelectOptions.None]]
                        Switch.IsOutlined
                        Switch.Size IsSmall
                        Switch.Id "switch-2"
                        Switch.Checked model.ProtocolState.TagFilterIsAnd
                        Switch.OnChange (fun _ ->
                            UpdateTagFilterIsAnd (not model.ProtocolState.TagFilterIsAnd) |> ProtocolMsg |> dispatch
                        )
                    ] [
                        if model.ProtocolState.TagFilterIsAnd then b [] [str "And"] else b [] [str "Or"]
                    ]
                ]
            ]
    ]

let private curatedTag = Tag.tag [Tag.Color IsSuccess] [str "curated"]
let private communitytag = Tag.tag [Tag.Color IsWarning] [str "community"]
let private curatedCommunityTag =
    Tag.tag [
        Tag.Props [Style [Background "linear-gradient(90deg, rgba(31,194,167,1) 50%, rgba(255,192,0,1) 50%)"]]
        Tag.Color IsSuccess
    ] [
        span [Style [MarginRight "0.75em"]] [str "cur"]
        span [Style [MarginLeft "0.75em"; Color "rgba(0, 0, 0, 0.7)"]] [str "com"]  
    ]

let protocolElement i (sortedTable:Template []) (model:Model) dispatch =
    let isActive =
        match model.ProtocolState.DisplayedProtDetailsId with
        | Some id when id = i ->
            true
        | _ ->
            false
    let prot = sortedTable.[i]
    [
        tr [
            if isActive then
                Class "nonSelectText"
            else
                Class "nonSelectText hoverTableEle"
            Style [
                Cursor "pointer"
                UserSelect UserSelectOptions.None
                if isActive then
                    BackgroundColor model.SiteStyleState.ColorMode.ElementBackground
                    Color "white"
            ]
            OnClick (fun e ->
                e.preventDefault()
                if isActive then
                    UpdateDisplayedProtDetailsId None |> ProtocolMsg |> dispatch
                else
                    UpdateDisplayedProtDetailsId (Some i) |> ProtocolMsg |> dispatch
            )

        ] [
            td [ ] [ str prot.Name ]
            td [ ] [
                if curatedOrganisationNames |> List.contains (prot.Organisation.ToLower()) then
                    curatedTag
                else
                    communitytag
            ]
            //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ a [ OnClick (fun e -> e.stopPropagation()); Href prot.DocsLink; Target "_Blank"; Title "docs" ] [Fa.i [Fa.Size Fa.Fa2x ; Fa.Regular.FileAlt] []] ]
            td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str prot.Version ]
            td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str (string prot.Used) ]
            td [] [
                Icon.icon [] [
                    Fa.i [Fa.Solid.ChevronDown] []
                ]
            ]

        ]
        tr [ ] [
            td [
                Style [
                    Padding "0"
                    if isActive then
                        BorderBottom (sprintf "2px solid %s" model.SiteStyleState.ColorMode.Accent)
                    if not isActive then
                        Display DisplayOptions.None
                ]
                ColSpan 5
            ] [
                Box.box' [Props [Style [BorderRadius "0px"; yield! ExcelColors.colorControlInArray model.SiteStyleState.ColorMode]]] [
                    Columns.columns [] [
                        Column.column [] [
                            Text.div [] [
                                str prot.Description
                                div [] [
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
                                        b [] [str "Author: "]
                                        str (prot.Authors.Replace(",",", "))
                                    ]
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]]] [
                                        b [] [str "Created: "]
                                        str (prot.LastUpdated.ToString("yyyy/MM/dd"))
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Tag.list [] [
                        for tag in prot.Er_Tags do
                            yield
                                Tag.tag [Tag.Color IsLink] [
                                    str tag
                                ]
                    ]
                    Tag.list [] [
                        for tag in prot.Tags do
                            yield
                                Tag.tag [Tag.Color IsInfo] [
                                    str tag
                                ]
                    ]
                    Button.a [
                        Button.OnClick (fun _ -> GetProtocolByIdRequest prot.Id |> ProtocolMsg |> dispatch)
                        Button.IsFullWidth; Button.Color IsSuccess
                    ] [str "select"]
                ]
            ]
        ]
    ]


let private curatedCommunityFilterDropdownItem (filter:Protocol.CuratedCommunityFilter) child (model:Model) dispatch =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            OnClick(fun e ->
                e.preventDefault();
                UpdateCuratedCommunityFilter filter |> ProtocolMsg |> dispatch
            )
        ]
    ] [ child ]

let private curatedCommunityFilterElement (model:Model) dispatch =
    Dropdown.dropdown [ Dropdown.IsHoverable ] [
        Dropdown.trigger [ ] [
            Button.button [ Button.Size IsSmall; Button.IsOutlined; Button.Color IsWhite; Button.Props [Style [Padding "0px"]] ] [
                match model.ProtocolState.CuratedCommunityFilter with
                | Protocol.CuratedCommunityFilter.Both -> curatedCommunityTag
                | Protocol.CuratedCommunityFilter.OnlyCommunity -> communitytag
                | Protocol.CuratedCommunityFilter.OnlyCurated -> curatedTag
            ]
        ]
        Dropdown.menu [ Props [Style [MinWidth "unset"; CSSProp.FontWeight "normal"]] ] [
            Dropdown.content [ ] [
                curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.Both curatedCommunityTag model dispatch
                curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.OnlyCurated curatedTag model dispatch
                curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.OnlyCommunity communitytag model dispatch
            ]
        ]
    ]

let protocolElementContainer (model:Model) dispatch =
    
    let sortTableBySearchQuery (protocol:Template []) =
        if model.ProtocolState.ProtocolNameSearchQuery <> ""
        then
            let queryBigram = model.ProtocolState.ProtocolNameSearchQuery |> Shared.SorensenDice.createBigrams 
            let bigrams =
                protocol
                |> Array.map (fun prot ->
                    let score =
                        prot.Name
                        |> Shared.SorensenDice.createBigrams
                        |> Shared.SorensenDice.calculateDistance queryBigram
                    score,prot
                )
                |> Array.filter (fun (score,prot) -> score > 0.1)
                |> Array.sortByDescending fst
                |> Array.map snd
            bigrams
        else
            protocol
    let filterTableByTags (protocol:Template []) =
        if model.ProtocolState.ProtocolFilterTags <> [] || model.ProtocolState.ProtocolFilterErTags <> [] then
            protocol |> Array.filter (fun x ->
                let tagSet = Set.union (x.Tags |> Set.ofArray) (x.Er_Tags |> Set.ofArray)
                let filterTags = Set.union (model.ProtocolState.ProtocolFilterTags |> Set.ofList) (model.ProtocolState.ProtocolFilterErTags |> Set.ofList)
                Set.intersect tagSet filterTags
                    |> fun intersectSet ->
                        /// if we want to filter by tag with AND, all tags must match
                        if model.ProtocolState.TagFilterIsAnd then
                            intersectSet.Count = filterTags.Count
                        /// if we want to filter by tag with OR, at least one tag must match
                        else
                            intersectSet.Count >= 1
            )
        else
            protocol

    let filterTableByCuratedCommunityFilter (protocol:Template []) =
        match model.ProtocolState.CuratedCommunityFilter with
        | Protocol.CuratedCommunityFilter.Both          -> protocol
        | Protocol.CuratedCommunityFilter.OnlyCurated   -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToLower()) curatedOrganisationNames)
        | Protocol.CuratedCommunityFilter.OnlyCommunity -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToLower()) curatedOrganisationNames |> not)

    let sortedTable =
        model.ProtocolState.ProtocolsAll
        |> filterTableByTags
        |> filterTableByCuratedCommunityFilter
        |> sortTableBySearchQuery

    mainFunctionContainer [
        Field.div [] [
            Help.help [] [
                b [] [str "Search for protocol templates."]
                str " For more information you can look "
                a [ Href Shared.URLs.SwateWiki; Target "_Blank" ] [str "here"]
                str ". If you find any problems with a template or have other suggestions you can contact us "
                a [ Href URLs.Helpdesk.UrlTemplateTopic; Target "_Blank" ] [str "here"]
                str "."
            ]
        ]
        fileSortElements model dispatch
        Table.table [
            Table.IsFullWidth
            Table.IsStriped
            Table.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text]]
        ] [
            thead [] [
                tr [] [
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text] ] [ str "Protocol Name"      ]
                    //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Documentation"      ]
                    th [] [curatedCommunityFilterElement model dispatch]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Protocol Version"   ]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Uses"               ]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text] ] []
                ]
            ]
            tbody [] [
                for i in 0 .. sortedTable.Length-1 do
                    yield!
                        protocolElement i sortedTable model dispatch
            ]
        ]
    ]

let protocolSearchViewComponent (model:Model) dispatch =
    let isEmpty = model.ProtocolState.ProtocolsAll |> isNull || model.ProtocolState.ProtocolsAll |> Array.isEmpty
    let isLoading = model.ProtocolState.Loading
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle model dispatch

        if isEmpty && not isLoading then
            Help.help [Help.Color IsDanger] [str "No Protocols were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

        if isLoading then
            CustomComponents.Loading.loadingModal

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Search the database for a protocol template you want to use."]

        if not isEmpty then
            protocolElementContainer model dispatch
    ]