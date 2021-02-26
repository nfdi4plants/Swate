module ProtocolSearchView

open System

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome

open Shared

open Model
open Messages


let breadcrumbEle dispatch =
    Breadcrumb.breadcrumb [Breadcrumb.HasArrowSeparator][
        Breadcrumb.item [][
            a [
                OnClick (fun e -> UpdatePageState (Some Routing.Route.ProtocolInsert) |> dispatch)
            ][
                str (Routing.Route.ProtocolInsert.toStringRdbl)
            ]
        ]
        Breadcrumb.item [ Breadcrumb.Item.IsActive true ][
            a [
                OnClick (fun e -> UpdatePageState (Some Routing.Route.ProtocolInsert) |> dispatch)
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
    let allTags = model.ProtocolInsertState.ProtocolsAll |> Array.collect (fun x -> x.Tags) |> Array.distinct |> Array.filter (fun x -> model.ProtocolInsertState.ProtocolSearchTags |> List.contains x |> not )
    let hitTagList =
        if model.ProtocolInsertState.ProtocolTagSearchQuery <> ""
        then
            let queryBigram = model.ProtocolInsertState.ProtocolTagSearchQuery |> Shared.Suggestion.createBigrams 
            let bigrams =
                allTags
                |> Array.map (fun x ->
                    x
                    |> Shared.Suggestion.createBigrams
                    |> Shared.Suggestion.sorensenDice queryBigram
                    , x
                )
                |> Array.filter (fun x -> fst x >= 0.3)
                |> Array.sortByDescending fst
                |> Array.map snd
            bigrams
        else
            [||]
    div [ Style [MarginBottom "0.75rem"] ][
        Columns.columns [Columns.IsMobile; Columns.Props [Style [MarginBottom "0"]]] [
            Column.column [ ] [
                Label.label [Label.Size IsSmall] [str "Search by protocol name"]
                Control.div [
                    Control.HasIconRight
                ] [
                    Input.text [
                        Input.Placeholder ".. protocol name"
                        Input.Color IsPrimary
                        Input.ValueOrDefault model.ProtocolInsertState.ProtocolNameSearchQuery
                        Input.OnChange (fun e -> UpdateProtocolNameSearchQuery e.Value |> ProtocolInsert |> dispatch)
                    ]
                    Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                        [ Fa.i [ Fa.Solid.Search ]
                            [ ] ] ]
            ]

            Column.column [ ] [
                Label.label [Label.Size IsSmall] [str "Search for tags"]
                Control.div [
                    Control.HasIconRight
                ] [
                    Input.text [
                        Input.Placeholder ".. protocol tag"
                        Input.Color IsPrimary
                        Input.ValueOrDefault model.ProtocolInsertState.ProtocolTagSearchQuery
                        Input.OnChange (fun e -> UpdateProtocolTagSearchQuery e.Value |> ProtocolInsert |> dispatch)
                    ]
                    Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                        [ Fa.i [ Fa.Solid.Search ]
                            [ ] ]
                    /// Pseudo dropdown
                    Box.box' [Props [Style [
                        Position PositionOptions.Absolute
                        Width "100%"
                        Border "0.5px solid darkgrey"
                        if hitTagList |> Array.isEmpty then Display DisplayOptions.None
                    ]]] [
                        Tag.list [][
                            for tagSuggestion in hitTagList do
                                yield
                                    Tag.tag [
                                        Tag.CustomClass "clickableTag"
                                        Tag.Color IsInfo
                                        Tag.Props [ OnClick (fun e -> AddProtocolTag tagSuggestion |> ProtocolInsert |> dispatch) ]
                                    ][
                                        str tagSuggestion
                                    ]
                        ]
                    ]
                ]
            ]
        ]
        Field.div [Field.IsGroupedMultiline][
            for selectedTag in model.ProtocolInsertState.ProtocolSearchTags do
                yield
                    Control.div [ ] [
                        Tag.list [Tag.List.HasAddons][
                            Tag.tag [Tag.Color IsInfo; Tag.Props [Style [Border (sprintf "0.2px solid %s" NFDIColors.LightBlue.Base) ]]] [str selectedTag]
                            Tag.delete [
                                Tag.CustomClass "clickableTagDelete"
                                //Tag.Color IsWarning;
                                Tag.Props [
                                    OnClick (fun e -> RemoveProtocolTag selectedTag |> ProtocolInsert |> dispatch)
                                ]
                            ] []
                        ]
                    ]
        ]
    ]

let protocolElement i (sortedTable:ProtocolTemplate []) (model:Model) dispatch =
    let isActive =
        match model.ProtocolInsertState.DisplayedProtDetailsId with
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
                Class "nonSelectText validationTableEle"
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
                    UpdateDisplayedProtDetailsId None |> ProtocolInsert |> dispatch
                else
                    UpdateDisplayedProtDetailsId (Some i) |> ProtocolInsert |> dispatch
            )

        ] [
            td [ ] [ str prot.Name ]
            td [ ] [ Tag.tag [] [ a [ OnClick (fun e -> e.stopPropagation()); Href prot.DocsLink; Target "_Blank" ] [str "docs"] ] ]
            td [ ] [ str prot.Version ]
            td [ ] [ str (string prot.Used) ]
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
                        BorderBottom (sprintf "2px solid %s" ExcelColors.colorfullMode.Accent)
                    if not isActive then
                        Display DisplayOptions.None
                ]
                ColSpan 5
            ] [
                Box.box' [][
                    Columns.columns [][
                        Column.column [][
                            Text.div [][
                                str prot.Description
                                div [][
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline]]] [
                                        b [] [str "Author: "]
                                        str prot.Author
                                    ]
                                    Help.help [Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]]] [
                                        b [][str "Created: "]
                                        str (sprintf "%s %s" (prot.Created.ToShortDateString()) (prot.Created.ToShortTimeString()))
                                    ]
                                ]
                            ]
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
                        Button.OnClick (fun e -> GetProtocolXmlByProtocolRequest prot |> ProtocolInsert |> dispatch)
                        Button.IsFullWidth; Button.Color IsSuccess
                    ] [str "select"]
                ]
            ]
        ]
    ]

let protocolElementContainer (model:Model) dispatch =
    
    let sortTableBySearchQuery (protocol:ProtocolTemplate []) =
        if model.ProtocolInsertState.ProtocolNameSearchQuery <> ""
        then
            let queryBigram = model.ProtocolInsertState.ProtocolNameSearchQuery |> Shared.Suggestion.createBigrams 
            let bigrams =
                protocol
                |> Array.sortByDescending (fun x ->
                    x.Name
                    |> Shared.Suggestion.createBigrams
                    |> Shared.Suggestion.sorensenDice queryBigram
                )
            bigrams
        else
            protocol
    let filterTableByTags (protocol:ProtocolTemplate []) =
        if model.ProtocolInsertState.ProtocolSearchTags |> List.isEmpty |> not then
            protocol |> Array.filter (fun x ->
                let protTagSet = x.Tags |> Set.ofArray
                let filterTags = model.ProtocolInsertState.ProtocolSearchTags |> Set.ofList
                Set.intersect protTagSet filterTags |> Set.isEmpty |> not
            )
        else
            protocol

    let sortedTable =
        model.ProtocolInsertState.ProtocolsAll
        |> filterTableByTags
        |> sortTableBySearchQuery 

    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            //BorderRadius "15px 15px 0 0"
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ] [
        fileSortElements model dispatch
        Table.table [
            //Table.IsBordered
            Table.IsFullWidth
            Table.IsStriped
        ] [
            thead [][
                tr [][
                    
                ]
                tr [][
                    th [][ str "Protocol Name"      ]
                    th [][ str "Documentation"      ]
                    th [][ str "Protocol Version"   ]
                    th [][ str "Uses"               ]
                    th [][]
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
    let isEmpty = model.ProtocolInsertState.ProtocolsAll |> isNull || model.ProtocolInsertState.ProtocolsAll |> Array.isEmpty 
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle dispatch

        if isEmpty then
            Help.help [Help.Color IsDanger][str "No Protocols were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Search the database for a protocol template you want to use."]

        if not isEmpty then
            protocolElementContainer model dispatch
    ]