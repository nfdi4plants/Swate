module Protocol.Component

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
        | "/o" | "/org"     -> Some Organisation
        | "/a" | "/authors" -> Some Authors
        | "/n" | "/reset"   -> Some Name
        | _ -> None

    member this.toStr =
        match this with
        | Name          -> "/name"
        | Organisation  -> "/org"
        | Authors       -> "/auth"

    static member GetOfQuery(query:string) =
        SearchFields.ofFieldString query

type private ProtocolViewState = {
        DisplayedProtDetailsId  : int option
        ProtocolSearchQuery     : string
        ProtocolTagSearchQuery  : string
        ProtocolFilterTags      : string list
        ProtocolFilterErTags    : string list
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
    Column.column [ ] [
        Label.label [Label.Size IsSmall; Label.Props [Style [Color model.SiteStyleState.ColorMode.Text; MinWidth "91px"; WhiteSpace WhiteSpaceOptions.Nowrap]]] [str "Search by protocol name"]
        let hasSearchAddon = state.Searchfield <> SearchFields.Name
        Field.div [if hasSearchAddon then Field.HasAddons] [
            Control.div [
                Control.Props [Style [if not hasSearchAddon then Display DisplayOptions.None]]
            ] [
                Button.a [
                    Button.IsStatic true
                ] [ str state.Searchfield.toStr]
            ]
            Control.div [
                Control.HasIconRight
            ] [
                Input.text [
                    Input.Placeholder ".. protocol name"
                    Input.Id SearchFieldId
                    Input.Color IsPrimary
                    Input.ValueOrDefault state.ProtocolSearchQuery
                    Input.OnChange (fun e ->
                        let query = e.Value
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
                Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                    [ Fa.i [ Fa.Solid.Search ]
                        [ ] ]
            ]
        ]
    ]

let private tagQueryField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
    let allTags = model.ProtocolState.ProtocolsAll |> Array.collect (fun x -> x.Tags) |> Array.distinct |> Array.filter (fun x -> state.ProtocolFilterTags |> List.contains x |> not )
    let allErTags = model.ProtocolState.ProtocolsAll |> Array.collect (fun x -> x.Er_Tags) |> Array.distinct |> Array.filter (fun x -> state.ProtocolFilterErTags |> List.contains x |> not )
    let hitTagList, hitErTagList =
        if state.ProtocolTagSearchQuery <> ""
        then
            let queryBigram = state.ProtocolTagSearchQuery |> Shared.SorensenDice.createBigrams 
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
    Column.column [ ] [
        Label.label [Label.Size IsSmall; Label.Props [Style [Color model.SiteStyleState.ColorMode.Text; MinWidth "91px"; WhiteSpace WhiteSpaceOptions.Nowrap]]] [str "Search for tags"]
        Control.div [
            Control.HasIconRight
        ] [
            Input.text [
                Input.Placeholder ".. protocol tag"
                Input.Color IsPrimary
                Input.ValueOrDefault state.ProtocolTagSearchQuery
                Input.OnChange (fun e ->
                    {state with ProtocolTagSearchQuery = e.Value} |> setState
                    //UpdateProtocolTagSearchQuery e.Value |> ProtocolMsg |> dispatch
                )
            ]
            Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                [ Fa.i [ Fa.Solid.Search ]
                    [ ] ]
            // Pseudo dropdown
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
                                    Tag.Props [ OnClick (fun _ ->
                                        let nextState = {
                                            state with
                                                ProtocolFilterErTags = tagSuggestion::state.ProtocolFilterErTags
                                                ProtocolTagSearchQuery = ""
                                                DisplayedProtDetailsId = None
                                        }
                                        setState nextState
                                        //AddProtocolErTag tagSuggestion |> ProtocolMsg |> dispatch
                                    )
                                    ]
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
                                    Tag.Props [ OnClick (fun _ ->
                                        let nextState = {
                                                state with
                                                    ProtocolFilterTags = tagSuggestion::state.ProtocolFilterTags
                                                    ProtocolTagSearchQuery = ""
                                                    DisplayedProtDetailsId = None
                                            }
                                        setState nextState
                                        //AddProtocolTag tagSuggestion |> ProtocolMsg |> dispatch
                                    )]
                                ] [
                                    str tagSuggestion
                                ]
                    ]
            ]
        ]
    ]

let private tagDisplayField (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =
    Columns.columns [Columns.IsMobile] [
        Column.column [] [
            Field.div [Field.IsGroupedMultiline] [
                for selectedTag in state.ProtocolFilterErTags do
                    yield
                        Control.div [ ] [
                            Tag.list [Tag.List.HasAddons] [
                                Tag.tag [Tag.Color IsLink; Tag.Props [Style [Border "0px"]]] [str selectedTag]
                                Tag.delete [
                                    Tag.CustomClass "clickableTagDelete"
                                    //Tag.Color IsWarning;
                                    Tag.Props [
                                        OnClick (fun _ ->
                                            {state with ProtocolFilterErTags = state.ProtocolFilterErTags |> List.except [selectedTag]} |> setState
                                            //RemoveProtocolErTag selectedTag |> ProtocolMsg |> dispatch
                                        )
                                    ]
                                ] []
                            ]
                        ]
                for selectedTag in state.ProtocolFilterTags do
                    yield
                        Control.div [ ] [
                            Tag.list [Tag.List.HasAddons] [
                                Tag.tag [Tag.Color IsInfo; Tag.Props [Style [Border "0px"]]] [str selectedTag]
                                Tag.delete [
                                    Tag.CustomClass "clickableTagDelete"
                                    //Tag.Color IsWarning;
                                    Tag.Props [
                                        OnClick (fun _ ->
                                            {state with ProtocolFilterTags = state.ProtocolFilterTags |> List.except [selectedTag]} |> setState
                                            //RemoveProtocolTag selectedTag |> ProtocolMsg |> dispatch
                                        )
                                    ]
                                ] []
                            ]
                        ]
                ]
        ]
        // tag filter (AND or OR) 
        Column.column [
            Column.Width (Screen.All, Column.IsNarrow)
            Column.Props [Title (if state.TagFilterIsAnd then "Templates contain all tags." else "Templates contain at least one tag.")]
        ] [
            Switch.switchInline [
                Switch.Color Color.IsDark
                Switch.LabelProps [Style [UserSelect UserSelectOptions.None]]
                Switch.IsOutlined
                Switch.Size IsSmall
                Switch.Id "switch-2"
                Switch.Checked state.TagFilterIsAnd
                Switch.OnChange (fun _ ->
                    {state with TagFilterIsAnd = not state.TagFilterIsAnd} |> setState
                    //UpdateTagFilterIsAnd (not state.TagFilterIsAnd) |> ProtocolMsg |> dispatch
                )
            ] [
                if state.TagFilterIsAnd then b [] [str "And"] else b [] [str "Or"]
            ]
        ]
    ]

let private fileSortElements (model:Model) (state: ProtocolViewState) (setState: ProtocolViewState -> unit) =

    div [ Style [MarginBottom "0.75rem"] ] [
        Columns.columns [Columns.IsMobile; Columns.Props [Style [MarginBottom "0";]]] [
            
            queryField model state setState
            tagQueryField model state setState
        ]
        // Only show the tag list and tag filter (AND or OR) if any tag exists
        if state.ProtocolFilterErTags <> [] || state.ProtocolFilterTags <> [] then
            tagDisplayField model state setState
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

let private protocolElement i (template:Template) (model:Model) (state:ProtocolViewState) dispatch (setState: ProtocolViewState -> unit) =
    let isActive =
        match state.DisplayedProtDetailsId with
        | Some id when id = i ->
            true
        | _ ->
            false
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
                { state with
                    DisplayedProtDetailsId = if isActive then None else Some i }
                |> setState
                //if isActive then
                //    UpdateDisplayedProtDetailsId None |> ProtocolMsg |> dispatch
                //else
                //    UpdateDisplayedProtDetailsId (Some i) |> ProtocolMsg |> dispatch
            )

        ] [
            td [ ] [ str template.Name ]
            td [ ] [
                if curatedOrganisationNames |> List.contains (template.Organisation.ToLower()) then
                    curatedTag
                else
                    communitytag
            ]
            //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ a [ OnClick (fun e -> e.stopPropagation()); Href prot.DocsLink; Target "_Blank"; Title "docs" ] [Fa.i [Fa.Size Fa.Fa2x ; Fa.Regular.FileAlt] []] ]
            td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str template.Version ]
            td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str (string template.Used) ]
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
                                str template.Description
                                div [] [
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
                                        b [] [str "Author: "]
                                        str (template.Authors.Replace(",",", "))
                                    ]
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]]] [
                                        b [] [str "Created: "]
                                        str (template.LastUpdated.ToString("yyyy/MM/dd"))
                                    ]
                                ]
                                div [] [
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
                                        b [] [str "Organisation: "]
                                        str template.Organisation
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Tag.list [] [
                        for tag in template.Er_Tags do
                            yield
                                Tag.tag [Tag.Color IsLink] [
                                    str tag
                                ]
                    ]
                    Tag.list [] [
                        for tag in template.Tags do
                            yield
                                Tag.tag [Tag.Color IsInfo] [
                                    str tag
                                ]
                    ]
                    Button.a [
                        Button.OnClick (fun _ -> GetProtocolByIdRequest template.Id |> ProtocolMsg |> dispatch)
                        Button.IsFullWidth; Button.Color IsSuccess
                    ] [str "select"]
                ]
            ]
        ]
    ]

let private curatedCommunityFilterDropdownItem (filter:Protocol.CuratedCommunityFilter) child (state:ProtocolViewState) (setState: ProtocolViewState -> unit) =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            OnClick(fun e ->
                e.preventDefault();
                {state with CuratedCommunityFilter = filter} |> setState
                //UpdateCuratedCommunityFilter filter |> ProtocolMsg |> dispatch
            )
        ]
    ] [ child ]

let private curatedCommunityFilterElement (state:ProtocolViewState) (setState: ProtocolViewState -> unit) =
    Dropdown.dropdown [ Dropdown.IsHoverable ] [
        Dropdown.trigger [ ] [
            Button.button [ Button.Size IsSmall; Button.IsOutlined; Button.Color IsWhite; Button.Props [Style [Padding "0px"]] ] [
                match state.CuratedCommunityFilter with
                | Protocol.CuratedCommunityFilter.Both -> curatedCommunityTag
                | Protocol.CuratedCommunityFilter.OnlyCommunity -> communitytag
                | Protocol.CuratedCommunityFilter.OnlyCurated -> curatedTag
            ]
        ]
        Dropdown.menu [ Props [Style [MinWidth "unset"; CSSProp.FontWeight "normal"]] ] [
            Dropdown.content [ ] [
                curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.Both curatedCommunityTag state setState
                curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.OnlyCurated curatedTag state setState
                curatedCommunityFilterDropdownItem Protocol.CuratedCommunityFilter.OnlyCommunity communitytag state setState
            ]
        ]
    ]

open Feliz

[<ReactComponent>]
let ProtocolContainer (model:Model) dispatch =

    let state, setState = React.useState(ProtocolViewState.init)

    let sortTableBySearchQuery (protocol:Template []) =
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
                            createScore template.Organisation
                        | SearchFields.Authors       ->
                            let authors = template.Authors.Split([|','; ' '|], StringSplitOptions.TrimEntries)
                            let query = query.ToLower()
                            let scores = authors |> Array.filter (fun x -> x.ToLower().StartsWith query)
                            if Array.isEmpty scores then 0.0 else 1.0
                    score, template
                )
                |> Array.filter (fun (score,_) -> score > 0.1)
                |> Array.sortByDescending fst
                |> Array.map snd
            scoredTemplate
        else
            protocol
    let filterTableByTags (protocol:Template []) =
        if state.ProtocolFilterTags <> [] || state.ProtocolFilterErTags <> [] then
            protocol |> Array.filter (fun x ->
                let tagSet = Set.union (x.Tags |> Set.ofArray) (x.Er_Tags |> Set.ofArray)
                let filterTags = Set.union (state.ProtocolFilterTags |> Set.ofList) (state.ProtocolFilterErTags |> Set.ofList)
                Set.intersect tagSet filterTags
                    |> fun intersectSet ->
                        // if we want to filter by tag with AND, all tags must match
                        if state.TagFilterIsAnd then
                            intersectSet.Count = filterTags.Count
                        // if we want to filter by tag with OR, at least one tag must match
                        else
                            intersectSet.Count >= 1
            )
        else
            protocol
    let filterTableByCuratedCommunityFilter (protocol:Template []) =
        match state.CuratedCommunityFilter with
        | Protocol.CuratedCommunityFilter.Both          -> protocol
        | Protocol.CuratedCommunityFilter.OnlyCurated   -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToLower()) curatedOrganisationNames)
        | Protocol.CuratedCommunityFilter.OnlyCommunity -> protocol |> Array.filter (fun x -> List.contains (x.Organisation.ToLower()) curatedOrganisationNames |> not)

    let sortedTable =
        model.ProtocolState.ProtocolsAll
        |> filterTableByTags
        |> filterTableByCuratedCommunityFilter
        |> sortTableBySearchQuery
        |> Array.sortBy (fun template -> template.Name, template.Organisation)

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
        fileSortElements model state setState
        Table.table [
            Table.IsFullWidth
            Table.IsStriped
            Table.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text]]
        ] [
            thead [] [
                tr [] [
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text] ] [ str "Protocol Name"      ]
                    //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Documentation"      ]
                    th [] [curatedCommunityFilterElement state setState]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Protocol Version"   ]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ] [ str "Uses"               ]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text] ] []
                ]
            ]
            tbody [] [
                for i in 0 .. sortedTable.Length-1 do
                    yield!
                        protocolElement i sortedTable.[i] model state dispatch setState
            ]
        ]
    ]