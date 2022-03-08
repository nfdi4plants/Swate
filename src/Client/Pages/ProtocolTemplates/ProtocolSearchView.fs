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

let curatedOrganisationNames = [
    "dataplant"
    "nfdi4plants"
]

let breadcrumbEle (model:Model) dispatch =
    Breadcrumb.breadcrumb [Breadcrumb.HasArrowSeparator][
        Breadcrumb.item [][
            a [
                OnClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
            ][
                str (Routing.Route.Protocol.toStringRdbl)
            ]
        ];
        Breadcrumb.item [
            Breadcrumb.Item.IsActive true
        ][
            a [
                Style [Color model.SiteStyleState.ColorMode.Text]
                OnClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
            ][
                str Routing.Route.ProtocolSearch.toStringRdbl
            ]
        ]
    ]

let sortButton icon msg =
    Button.a [
        Button.IsOutlined
        Button.Color IsPrimary
        Button.OnClick msg
    ][
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
                    x
                    |> Shared.SorensenDice.createBigrams
                    |> Shared.SorensenDice.calculateDistance queryBigram
                    , x
                )
                |> Array.filter (fun x -> fst x >= 0.3)
                |> Array.sortByDescending fst
                |> Array.map snd
            sortedTags, sortedErTags
        else
            [||], [||]
    div [ Style [MarginBottom "0.75rem"] ][
        Columns.columns [Columns.IsMobile; Columns.Props [Style [MarginBottom "0";]]] [
            Column.column [ ] [
                Label.label [Label.Size IsSmall; Label.Props [Style [Color model.SiteStyleState.ColorMode.Text]]] [str "Search by protocol name"]
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
                Label.label [Label.Size IsSmall; Label.Props [Style [Color model.SiteStyleState.ColorMode.Text]]] [str "Search for tags"]
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
                        //Border "0.5px solid"
                        if hitTagList |> Array.isEmpty then Display DisplayOptions.None
                    ]]] [
                        Label.label [][str "Endpoint Repositories"]
                        Tag.list [][
                            for tagSuggestion in hitErTagList do
                                yield
                                    Tag.tag [
                                        Tag.CustomClass "clickableTag"
                                        Tag.Color IsLink
                                        Tag.Props [ OnClick (fun e -> AddProtocolErTag tagSuggestion |> ProtocolMsg |> dispatch) ]
                                    ][
                                        str tagSuggestion
                                    ]
                        ]
                        Label.label [][str "Tags"]
                        Tag.list [][
                            for tagSuggestion in hitTagList do
                                yield
                                    Tag.tag [
                                        Tag.CustomClass "clickableTag"
                                        Tag.Color IsInfo
                                        Tag.Props [ OnClick (fun e -> AddProtocolTag tagSuggestion |> ProtocolMsg |> dispatch) ]
                                    ][
                                        str tagSuggestion
                                    ]
                        ]
                    ]
                ]
            ]
        ]
        Field.div [Field.IsGroupedMultiline][
            for selectedTag in model.ProtocolState.ProtocolFilterErTags do
                yield
                    Control.div [ ] [
                        Tag.list [Tag.List.HasAddons][
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
                        Tag.list [Tag.List.HasAddons][
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
                    Tag.tag [Tag.Color IsSuccess ][str "curated"]
                else
                    Tag.tag [Tag.Color IsWarning ][str "community"]
            ]
            //td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ a [ OnClick (fun e -> e.stopPropagation()); Href prot.DocsLink; Target "_Blank"; Title "docs" ] [Fa.i [Fa.Size Fa.Fa2x ; Fa.Regular.FileAlt][]] ]
            td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str prot.Version ]
            td [ Style [TextAlign TextAlignOptions.Center; VerticalAlign "middle"] ] [ str (string prot.Used) ]
            td [][
                Icon.icon [][
                    Fa.i [Fa.Solid.ChevronDown][]
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
                Box.box' [Props [Style [BorderRadius "0px"; yield! ExcelColors.colorControlInArray model.SiteStyleState.ColorMode]]][
                    Columns.columns [][
                        Column.column [][
                            Text.div [][
                                str prot.Description
                                div [][
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
                                        b [] [str "Author: "]
                                        str (prot.Authors.Replace(",",", "))
                                    ]
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]]] [
                                        b [][str "Created: "]
                                        str (prot.LastUpdated.ToString("yyyy/MM/dd"))
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Tag.list [][
                        for tag in prot.Er_Tags do
                            yield
                                Tag.tag [Tag.Color IsLink][
                                    str tag
                                ]
                    ]
                    Tag.list [][
                        for tag in prot.Tags do
                            yield
                                Tag.tag [Tag.Color IsInfo][
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
        if model.ProtocolState.ProtocolFilterTags |> List.isEmpty |> not then
            protocol |> Array.filter (fun x ->
                let protTagSet = x.Tags |> Set.ofArray
                let filterTags = model.ProtocolState.ProtocolFilterTags |> Set.ofList
                Set.intersect protTagSet filterTags |> fun intersectSet -> intersectSet.Count = filterTags.Count
            )
        else
            protocol

    let filterTableByErTags (protocol:Template []) =
        if model.ProtocolState.ProtocolFilterErTags |> List.isEmpty |> not then
            protocol |> Array.filter (fun x ->
                let protTagSet = x.Er_Tags |> Set.ofArray
                let filterTags = model.ProtocolState.ProtocolFilterErTags |> Set.ofList
                Set.intersect protTagSet filterTags |> fun intersectSet -> intersectSet.Count = filterTags.Count
            )
        else
            protocol

    let sortedTable =
        model.ProtocolState.ProtocolsAll
        |> filterTableByTags
        |> sortTableBySearchQuery
        |> filterTableByErTags

    mainFunctionContainer [
        Field.div [][
            Help.help [][
                b [][str "Search for protocol templates."]
                str " For more information you can look "
                a [ Href @"https://github.com/nfdi4plants/SWATE_templates/wiki"; Target "_Blank" ][str "here"]
                str ". If you find any problems with a protocol or have other suggestions you can contact us "
                a [ Href @"https://github.com/nfdi4plants/SWATE_templates/issues/new/choose"; Target "_Blank" ] [str "here"]
                str "."
            ]
        ]
        fileSortElements model dispatch
        Table.table [
            Table.IsFullWidth
            Table.IsStriped
            Table.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text]]
        ] [
            thead [][
                tr [][
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text] ][ str "Protocol Name"      ]
                    //th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ][ str "Documentation"      ]
                    th [][]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ][ str "Protocol Version"   ]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Center] ][ str "Uses"               ]
                    th [ Style [ Color model.SiteStyleState.ColorMode.Text] ][]
                ]
            ]
            tbody [][
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
            Help.help [Help.Color IsDanger][str "No Protocols were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

        if isLoading then
            CustomComponents.Loading.loadingModal

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Search the database for a protocol template you want to use."]

        if not isEmpty then
            protocolElementContainer model dispatch
    ]