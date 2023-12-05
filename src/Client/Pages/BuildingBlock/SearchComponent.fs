module BuildingBlock.SearchComponent

open Feliz
open Feliz.Bulma
open Shared
open TermTypes
open OfficeInteropTypes
open Fable.Core.JsInterop
open Elmish
open Model.BuildingBlock
open Model.TermSearch
open Model
open Messages

module private DropdownElements =

    let private itemTooltipStyle = [style.fontSize (length.rem 1.1); style.paddingRight (length.px 10); style.textAlign.center; style.color NFDIColors.Yellow.Darker20]
    let private annotationsPrinciplesUrl = Html.a [prop.href Shared.URLs.AnnotationPrinciplesUrl; prop.target "_Blank"; prop.text "info"]

    let selectBuildingBlockType (state: BuildingBlockUIState) bb_type = {state with BuildingBlockType = bb_type; DropdownIsActive = false}

    let createBuildingBlockDropdownItem (state: BuildingBlockUIState) setState (model:Model) (block: BuildingBlockType)  =
        Bulma.dropdownItem.a [
            prop.onClick (fun e ->
                e.stopPropagation()
                selectBuildingBlockType state block |> setState
            )
            prop.onKeyDown(fun k -> if (int k.which) = 13 then setState {state with BuildingBlockType = block})
            prop.children [
                Html.span [
                    prop.style itemTooltipStyle
                    prop.className "has-tooltip-multiline"
                    prop.custom("data-tooltip", block.toShortExplanation)
                    prop.children (Bulma.icon [
                        Html.i [prop.className "fa-solid fa-circle-info"]
                    ])
                ]
                Html.span block.toString
            ]
        ]

    let createBuildingBlockDropdownItem_FeaturedColumns (state: BuildingBlockUIState) setState (model:Model) dispatch (block: BuildingBlockType )  =
        Bulma.dropdownItem.a [
            prop.onClick(fun e ->
                e.stopPropagation()
                if block.isFeaturedColumn then
                    let t = block.getFeaturedColumnTermMinimal.toTerm
                    BuildingBlock.SelectHeaderTerm (Some t) |> BuildingBlockMsg |> dispatch
                selectBuildingBlockType state block |> setState
            )
            prop.onKeyDown(fun k -> if (int k.which) = 13 then setState {state with BuildingBlockType = block})
            prop.children [
                Html.span [
                    prop.style itemTooltipStyle
                    prop.className "has-tooltip-multiline"
                    prop.custom("data-tooltip", block.toShortExplanation)
                    prop.children (Bulma.icon [
                        Html.i [prop.className "fa-solid fa-circle-info"]
                    ])
                ]
                Html.span block.toString
            ]
        ]

    let createSubBuildingBlockDropdownLink (state:BuildingBlockUIState) setState (model:Model) (subpage: Model.BuildingBlock.DropdownPage) =
        Bulma.dropdownItem.a [
            prop.tabIndex 0
            prop.onClick(fun e ->
                e.preventDefault()
                e.stopPropagation()
                setState {state with DropdownPage = subpage}
            )
            prop.style [
                style.paddingRight(length.rem 0.5)
            ]
            prop.children [
                Html.span [
                    prop.style itemTooltipStyle
                    prop.className "has-tooltip-multiline"
                    prop.custom("data-tooltip", subpage.toTooltip)
                    prop.children (Bulma.icon [
                        Html.i [prop.className "fa-solid fa-circle-info"]
                    ])
                ]

                Html.span subpage.toString

                Html.span [
                    prop.style [style.width(length.px 20); style.float'.right; style.lineHeight 1.5; style.fontSize(length.rem 1.1)]
                    prop.children (Bulma.icon (Html.i [prop.className "fa-solid fa-arrow-right"]))
                ]
            ]
        ]

    /// Navigation element back to main page
    let backToMainDropdownButton (state: BuildingBlockUIState) setState (model:Model) =
        Bulma.dropdownItem.div [
            prop.style [style.textAlign.right]
            prop.children [
                Bulma.button.button [
                    prop.style [style.float'.left; style.width(length.px 20); style.height(length.px 20); style.borderRadius(length.px 4); style.custom("border", "unset")]
                    prop.onClick(fun e ->
                        e.preventDefault()
                        e.stopPropagation()
                        setState {state with DropdownPage = BuildingBlock.DropdownPage.Main}
                    )
                    Bulma.button.isInverted
                    Bulma.color.isBlack
                    prop.children [
                        Bulma.icon [Html.i [
                            prop.className "fa-solid fa-arrow-left"
                        ]]
                    ]
                ]
                annotationsPrinciplesUrl
            ]
        ]

    /// Main column types subpage for dropdown
    let dropdownContentMain state setState (model:Model) =
        [
            BuildingBlockType.Source            |> createBuildingBlockDropdownItem state setState model
            Bulma.dropdownDivider []
            BuildingBlockType.Parameter         |> createBuildingBlockDropdownItem state setState model
            BuildingBlockType.Factor            |> createBuildingBlockDropdownItem state setState model
            BuildingBlockType.Characteristic    |> createBuildingBlockDropdownItem state setState model
            BuildingBlockType.Component         |> createBuildingBlockDropdownItem state setState model
            Model.BuildingBlock.DropdownPage.ProtocolTypes |> createSubBuildingBlockDropdownLink state setState model
            Bulma.dropdownDivider []
            Model.BuildingBlock.DropdownPage.Output |>  createSubBuildingBlockDropdownLink state setState model
            Bulma.dropdownItem.div [
                prop.style [style.textAlign.right]
                prop.children annotationsPrinciplesUrl
            ]
        ]

    /// Protocol Type subpage for dropdown
    let dropdownContentProtocolTypeColumns state setState state_search setState_search (model:Model) dispatch =
        [
            // Heading
            Bulma.dropdownItem.div [
                prop.style [style.textAlign.center]
                prop.children [
                    Html.h6 [
                        prop.className "subtitle"
                        prop.style [style.fontWeight.bold]
                        prop.text BuildingBlock.DropdownPage.ProtocolTypes.toString
                    ]
                ]
            ]
            Bulma.dropdownDivider []
            BuildingBlockType.ProtocolType      |> createBuildingBlockDropdownItem_FeaturedColumns state setState model dispatch
            BuildingBlockType.ProtocolREF       |> createBuildingBlockDropdownItem state setState model
            // Navigation element back to main page
            backToMainDropdownButton state setState model
        ]

    /// Output columns subpage for dropdown
    let dropdownContentOutputColumns state setState (model:Model) =
        [
            // Heading
            Bulma.dropdownItem.div [
                prop.style [style.textAlign.center]
                prop.children [
                    Html.h6 [
                        prop.className "subtitle"
                        prop.style [style.fontWeight.bold]
                        prop.text BuildingBlock.DropdownPage.Output.toString
                    ]
                ]
            ]
            Bulma.dropdownDivider []
            BuildingBlockType.Sample            |> createBuildingBlockDropdownItem state setState model
            BuildingBlockType.RawDataFile       |> createBuildingBlockDropdownItem state setState model
            BuildingBlockType.DerivedDataFile   |> createBuildingBlockDropdownItem state setState model
            // Navigation element back to main page
            backToMainDropdownButton state setState model
        ]

let private dropdownChoice_element state setState state_search setState_search (model: Model) dispatch =
    Bulma.control.div [
        Bulma.dropdown [
            if state.DropdownIsActive then Bulma.dropdown.isActive //Dropdown.IsActive model.AddBuildingBlockState.ShowBuildingBlockSelection
            prop.children [
                Bulma.dropdownTrigger [
                    Bulma.button.a [
                        prop.onClick (fun e -> e.stopPropagation(); setState {state with DropdownIsActive = not state.DropdownIsActive})
                        prop.children [
                            Html.span [
                                prop.style [style.marginRight 5]
                                prop.text state.BuildingBlockType.toString
                            ]
                            Bulma.icon [ Html.i [
                                prop.className "fa-solid fa-angle-down"
                            ] ]
                        ]
                    ]
                ]
                Bulma.dropdownMenu [
                    match state.DropdownPage with
                    | Model.BuildingBlock.DropdownPage.Main ->
                        DropdownElements.dropdownContentMain state setState model
                    | Model.BuildingBlock.DropdownPage.ProtocolTypes ->
                        DropdownElements.dropdownContentProtocolTypeColumns state setState state_search setState_search model dispatch
                    | Model.BuildingBlock.DropdownPage.Output ->
                        DropdownElements.dropdownContentOutputColumns state setState model
                    |> fun content -> Bulma.dropdownContent [ prop.children content ] 
                ]
            ]
        ] 
    ]

module private AutocompleteComponents =

    /// inputId is used to set the value to the input field after selecting term.
    let createTermElement_Main (term:Term) selectMsg dispatch =
        let id = term.Accession
        let hiddenId = sprintf "isHidden_%s" id
        let main =
            Html.tr [
                prop.key id
                prop.onClick (fun _ -> selectMsg())
                prop.onKeyDown (fun k -> if k.key = "Enter" then selectMsg())
                prop.tabIndex 0
                prop.className "suggestion"
                prop.children [ 
                    Html.td [ Html.b term.Name ]
                    Html.td [
                        if term.IsObsolete then
                            Bulma.color.hasTextDanger
                            prop.text ("obsolete")
                    ]
                    Html.td [
                        prop.onClick ( fun e -> e.stopPropagation())
                        prop.style [style.fontWeight.lighter]
                        prop.children [SidebarComponents.AdvancedSearch.createLinkOfAccession term.Accession]  
                    ]
                    // Cytoscape graph tree view
                    Html.td [
                        Bulma.buttons [
                            Bulma.buttons.isRight
                            prop.children [
                                Bulma.button.a [
                                    prop.title "Show Term Tree"
                                    Bulma.button.isSmall
                                    Bulma.color.isSuccess
                                    Bulma.button.isInverted
                                    prop.onClick(fun e ->
                                        e.preventDefault()
                                        e.stopPropagation()
                                        Cytoscape.Msg.GetTermTree term.Accession |> CytoscapeMsg |> dispatch
                                    )
                                    prop.children [
                                        Bulma.icon [
                                            Html.i [prop.className "fa-solid fa-tree"]
                                        ]

                                    ]
                                ]
                                Bulma.button.a [
                                    Bulma.button.isSmall
                                    Bulma.color.isBlack
                                    Bulma.button.isInverted
                                    prop.onClick(fun e ->
                                        e.preventDefault()
                                        e.stopPropagation()
                                        let ele = Browser.Dom.document.getElementById(hiddenId)
                                        let isCollapsed =
                                            let vis = string ele?style?display
                                            vis = "none" || vis = ""
                                        if isCollapsed then 
                                            ele?style?display <- "table-row"
                                        else
                                            ele?style?display <- "none"
                                        ()
                                    )
                                    prop.children [
                                        Bulma.icon [
                                            Html.i [prop.className "fa-solid fa-chevron-down"]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        let hidden =
            Html.tr [
                prop.onClick (fun e -> e.stopPropagation())
                prop.id hiddenId
                prop.key hiddenId
                prop.className "suggestion-details"
                prop.children [
                    Html.td [
                        prop.colSpan 4
                        prop.children [
                            Bulma.content [
                                Html.b "Definition: "
                                Html.p (if term.Description = "" then "No definition found" else term.Description)
                            ]
                        ]
                    ]
                ]
            ]
        [|main; hidden|]

    let createAutocompleteSuggestions (termSuggestions: Term []) (selectMsg: Term -> Msg) (state: TermSearchUIState) setState (dispatch: Msg -> unit) =
        let suggestions = 
            if termSuggestions.Length > 0 then
                termSuggestions
                |> Array.distinctBy (fun t -> t.Accession)
                |> Array.collect (fun t ->
                    let msg = fun e ->
                        setState {state with SearchIsActive = false}
                        selectMsg t |> dispatch
                    createTermElement_Main t msg dispatch
                )
                |> List.ofArray
            else
                [ Html.tr [ Html.td "No terms found matching your input." ] ]

        let alternative_advancedSearch =
            Html.tr [
                prop.className "suggestion"
                prop.children [
                    Html.td [
                        prop.colSpan 4
                        prop.children [
                            Html.span "Cant find the Term you are looking for? Try "
                            Html.a [
                                prop.onClick (fun _ -> failwith "Not implemented in createAutocompleteSuggestions!"(*AdvancedSearch.ToggleModal autocompleteParams.ModalId |> AdvancedSearchMsg |> dispatch*))
                                prop.text "Advanced Search"
                            ]
                            Html.span "!"
                        ]
                    ]
                ]
            ]

        let alternative_getInContact =
            Html.tr [
                prop.className "suggestion"
                prop.children [
                    Html.td [
                        prop.colSpan 4
                        prop.children [
                            Html.span "Still can't find what you need? Get in "
                            Html.a [
                                prop.href Shared.URLs.Helpdesk.UrlOntologyTopic;
                                prop.target "_Blank"
                                prop.text "contact"
                            ]
                            Html.span " with us!"
                        ]
                    ]
                ]
            ]

        suggestions @ [alternative_advancedSearch; alternative_getInContact]

    let closeElement state setState =
        Html.div [
            prop.style [
                style.position.fixedRelativeToWindow
                style.left 0
                style.top 0
                style.width(length.percent 100)
                style.height(length.percent 100)
                style.zIndex 19
                style.backgroundColor.transparent
            ]
            prop.onClick(fun _ ->
                setState {state with SearchIsActive = false}
            )
        ]

    let autocompleteDropdownComponent (termSuggestions: Term []) selectMsg (state: TermSearchUIState) setState (model:Model) (dispatch: Msg -> unit) =
        let searchResults = createAutocompleteSuggestions termSuggestions selectMsg state setState dispatch
        Html.div [ 
            prop.style [style.position.relative]
            prop.children [
                closeElement state setState
                Html.div [
                    prop.style [
                        style.zIndex 20
                        style.width(length.percent 100)
                        style.maxHeight 400
                        style.position.absolute
                        style.marginTop(length.rem -0.5)
                        style.overflowY.auto
                        style.custom("borderWidth", "0 0.5px 0.5px 0.5px")
                        style.borderStyle.solid
                    ]
                    prop.children [
                        Bulma.table [
                            Bulma.table.isFullWidth
                            prop.children [
                                if state.SearchIsLoading then
                                    Html.tbody [
                                        prop.style [style.height 75]
                                        prop.children [
                                            Html.tr [
                                                Html.td [
                                                    prop.style [style.textAlign.center]
                                                    prop.children [
                                                        Modals.Loading.loadingComponent
                                                        Html.br []
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                else
                                    Html.tbody searchResults
                            ]
                        ]
                    ]
                ]
            ]
        ]

/// if inputId ends with Main we apply autofocus to the element
let private termSearch_element (inputId: string) (valueOrDefault:string) (onChangeMsg: string -> unit) (onDoubleClickMsg: string -> unit) (state: TermSearchUIState) setState =
    Bulma.input.text [
        prop.id inputId
        prop.key inputId
        prop.autoFocus (inputId.EndsWith "Main")
        prop.placeholder "Start typing to search"
        prop.valueOrDefault valueOrDefault
        prop.onDoubleClick (fun _ -> valueOrDefault |> onDoubleClickMsg)
        prop.onKeyDown(fun (e: Browser.Types.KeyboardEvent) ->
            match e.which with
            | 27. -> // Escape
                setState {state with SearchIsActive = false}
            | _ -> ()
        )
        prop.onChange (fun (e: string) -> onChangeMsg e)
    ]

let private chooseBuildingBlock_element (state_bb: BuildingBlockUIState) setState_bb (state:TermSearchUIState) setState (model: Model) dispatch =
    let inputId = "BuildingBlock_InputMain"
    Bulma.field.div [
        Bulma.field.div [
            Bulma.field.hasAddons
            // Choose building block type dropdown element
            prop.children [
                // Dropdown building block type choice
                dropdownChoice_element state_bb setState_bb state setState model dispatch
                // Term search field
                if state_bb.BuildingBlockType.isTermColumn && state_bb.BuildingBlockType.isFeaturedColumn |> not then
                    let onChangeMsg = fun (v:string) ->
                        BuildingBlock.SelectHeaderTerm None |> BuildingBlockMsg |> dispatch
                        BuildingBlock.UpdateHeaderSearchText v |> BuildingBlockMsg |> dispatch
                        let triggerNewSearch = v.Length > 2
                        if triggerNewSearch then
                            setState {state with SearchIsActive = true; SearchIsLoading = true}
                            let msg = BuildingBlock.Msg.GetHeaderSuggestions (v, {state = state; setState = setState}) |> BuildingBlockMsg
                            let bounce_msg = Bounce (System.TimeSpan.FromSeconds 0.5,nameof(msg), msg)
                            bounce_msg |> dispatch
                    Bulma.control.div [
                        Bulma.control.isExpanded
                        prop.children [
                            termSearch_element inputId model.AddBuildingBlockState.HeaderSearchText onChangeMsg onChangeMsg state setState
                        ]
                    ]
            ]
        ]
        // Ontology Term search preview
        if state.SearchIsActive then
            let selectMsg = fun (term:Term) -> BuildingBlock.SelectHeaderTerm (Some term) |> BuildingBlockMsg
            AutocompleteComponents.autocompleteDropdownComponent model.AddBuildingBlockState.HeaderSearchResults selectMsg state setState model dispatch
    ]

module private BodyTerm =

    let private termOrUnit_switch (state_searchForUnit:bool) setState =
        Bulma.buttons [
            Bulma.buttons.hasAddons
            prop.style [style.flexWrap.nowrap; style.marginBottom 0; style.marginRight (length.rem 1)]
            prop.children [
                Bulma.button.a [
                    let isActive = not state_searchForUnit
                    if isActive then Bulma.color.isSuccess
                    prop.onClick (fun _ -> false |> setState)
                    prop.classes ["pr-2 pl-2 mb-0"; if isActive then "is-selected"]
                    prop.text "Term"
                ]
                Bulma.button.a [
                    let isActive = state_searchForUnit
                    if isActive then Bulma.color.isSuccess
                    prop.onClick (fun _ -> true |> setState)
                    prop.classes ["pr-2 pl-2 mb-0"; if isActive then "is-selected"]
                    prop.text "Unit"
                ]
            ]
        ]

    let private isDirectedSearch_toggle (state: bool) setState =
        Bulma.control.div [
            Bulma.button.a [
                prop.title "Toggle child search"
                if state then Bulma.color.isSuccess
                prop.onClick (fun _ -> not state |> setState)
                prop.children [
                    Bulma.icon [Html.i [prop.className "fa-solid fa-diagram-project"]]
                ]
            ]
        ]

    [<ReactComponent>]
    let private searchElement inputId state setState (model: Model) dispatch =
        let state_isDirectedSearchMode, setState_isDirectedSearchMode = React.useState(true)
        let onChangeMsg = fun (isClicked: bool) (v:string) -> 
            BuildingBlock.Msg.SelectBodyTerm None |> BuildingBlockMsg |> dispatch
            BuildingBlock.UpdateBodySearchText v |> BuildingBlockMsg |> dispatch
            let updateState msg =
                setState {state with SearchIsActive = true; SearchIsLoading = true}
                let bounce_msg = Bounce (System.TimeSpan.FromSeconds 0.5,nameof(msg), msg)
                bounce_msg |> dispatch
            match isClicked, state_isDirectedSearchMode, model.AddBuildingBlockState.HeaderSelectedTerm, model.AddBuildingBlockState.BodySearchText with
            // only execute this on onDoubleClick event. If executed on onChange event it will trigger when deleting term.
            | true, true, Some parent, "" -> // Search all children 
                BuildingBlock.Msg.GetBodyTermsByParent (TermMinimal.ofTerm parent,{state = state; setState = setState}) |> BuildingBlockMsg
                |> updateState
            | _, true, Some parent, any when any.Length > 2 ->
                BuildingBlock.Msg.GetBodySuggestionsByParent (v,TermMinimal.ofTerm parent,{state = state; setState = setState}) |> BuildingBlockMsg
                |> updateState
            | _,_,_, any when any.Length > 2 ->
                BuildingBlock.Msg.GetBodySuggestions (v, {state = state; setState = setState}) |> BuildingBlockMsg
                |> updateState
            | _ -> ()
        Bulma.field.div [
            Bulma.field.hasAddons
            prop.style [style.flexGrow 1 ]
            // Choose building block type dropdown element
            prop.children [
                // Dropdown building block type choice
                isDirectedSearch_toggle state_isDirectedSearchMode setState_isDirectedSearchMode
                // Term search field
                Bulma.control.div [
                    Bulma.control.isExpanded
                    if state_isDirectedSearchMode && model.AddBuildingBlockState.HeaderSelectedTerm.IsNone then
                        prop.title "No parent term selected"
                    elif state_isDirectedSearchMode && model.AddBuildingBlockState.HeaderSelectedTerm.IsSome && model.AddBuildingBlockState.BodySearchText = "" then
                        prop.title "Double click to show all children"
                    prop.style [
                        // display box-shadow if term search is fully activated
                        if state_isDirectedSearchMode && model.AddBuildingBlockState.HeaderSelectedTerm.IsSome then style.boxShadow(2,2,NFDIColors.Mint.Lighter20)
                    ]
                    prop.children [
                        termSearch_element inputId model.AddBuildingBlockState.BodySearchText (onChangeMsg false) (onChangeMsg true) state setState
                    ]
                ]
            ]
        ]

    let Main state setState (state_searchForUnit: bool) setState_searchForUnit model dispatch =
        let inputId = "BuildingBlock_BodyInput"
        Bulma.field.div [
            Bulma.field.div [
                prop.style [ style.display.flex; style.justifyContent.spaceBetween ]
                prop.children [
                    termOrUnit_switch state_searchForUnit setState_searchForUnit
                    searchElement inputId state setState model dispatch
                ]
            ]
            // Ontology Term search preview
            if state.SearchIsActive then
                let selectMsg = fun (term:Term) -> BuildingBlock.SelectBodyTerm (Some term) |> BuildingBlockMsg
                AutocompleteComponents.autocompleteDropdownComponent model.AddBuildingBlockState.BodySearchResults selectMsg state setState model dispatch
        ]

let private add_button (state_bb: BuildingBlockUIState) (state_searchHeader:TermSearchUIState) (state_searchBody:TermSearchUIState) (state_searchForUnit: bool) (model: Model) dispatch =
    Bulma.field.div [
        Bulma.button.button  [
            let colName = BuildingBlockNamePrePrint.create state_bb.BuildingBlockType model.AddBuildingBlockState.HeaderSearchText
            let isValid = colName |> Helper.isValidBuildingBlock
            if isValid then
                Bulma.color.isSuccess
                Bulma.button.isActive
            else
                Bulma.color.isDanger
                prop.disabled true
            Bulma.button.isFullWidth
            prop.onClick (fun _ ->
                let colTerm =
                    if colName.isFeaturedColumn then
                        TermMinimal.create colName.Type.toString colName.Type.getFeaturedColumnAccession |> Some
                    elif model.AddBuildingBlockState.HeaderSelectedTerm.IsSome && not colName.isSingleColumn then
                        TermMinimal.ofTerm model.AddBuildingBlockState.HeaderSelectedTerm.Value |> Some
                    else
                        None
                let newBuildingBlock_HeaderOnly = InsertBuildingBlock.create colName colTerm None Array.empty
                let bodyTerm =
                    if model.AddBuildingBlockState.BodySelectedTerm.IsSome then
                        TermMinimal.ofTerm model.AddBuildingBlockState.BodySelectedTerm.Value
                    else
                        TermMinimal.create model.AddBuildingBlockState.BodySearchText ""
                let newBuildingBlock =
                    match colName.isTermColumn, state_searchForUnit with
                    // term column, unit not toggled and term selected
                    | true, false when bodyTerm.Name <> "" ->
                        {newBuildingBlock_HeaderOnly with Rows = [|bodyTerm|]}
                    // Term column, unit is toggled and term selected.
                    // We add body term selected as unit to all rows
                    | true, true when bodyTerm.Name <> "" ->
                        {newBuildingBlock_HeaderOnly with UnitTerm = Some bodyTerm}
                    | _ -> newBuildingBlock_HeaderOnly
                //SpreadsheetInterface.AddAnnotationBlock newBuildingBlock |> InterfaceMsg |> dispatch
                Browser.Dom.window.alert("AddAnnotationBlock is not implemented")
            )
            prop.text "Add building block"
        ]
    ]

module AdvancedSearch =

    let modal_container (model:Model) dispatch =
        Html.span [
            let selectHeader = fun (term:Term) ->
                Msg.Batch [
                    BuildingBlock.UpdateHeaderSearchText term.Name |> BuildingBlockMsg
                    BuildingBlock.SelectHeaderTerm (Some term) |> BuildingBlockMsg
                ]
            let selectBody = fun (term:Term) ->
                Msg.Batch [
                    BuildingBlock.UpdateBodySearchText term.Name |> BuildingBlockMsg
                    BuildingBlock.SelectBodyTerm (Some term) |> BuildingBlockMsg
                ]
            // added edge case to modal, where relatedInputId = "" is ignored
            SidebarComponents.AdvancedSearch.advancedSearchModal model AdvancedSearch.Model.BuildingBlockHeaderId "" dispatch selectHeader
            SidebarComponents.AdvancedSearch.advancedSearchModal model AdvancedSearch.Model.BuildingBlockBodyId "" dispatch selectBody
        ]

    let links_container (bb_type: BuildingBlockType) dispatch =
        Html.div [
            if not bb_type.isFeaturedColumn then
                Bulma.help [
                    prop.style [style.display.inlineElement]
                    prop.children [
                        Html.a [
                            prop.onClick (fun _ -> AdvancedSearch.ToggleModal AdvancedSearch.Model.BuildingBlockHeaderId |> AdvancedSearchMsg |> dispatch)
                            prop.text "Use advanced search header"
                        ]
                    ]
                ]
            Bulma.help [
                prop.style [style.display.inlineElement; style.float'.right]
                prop.children [
                    Html.a [
                        prop.onClick (fun _ -> AdvancedSearch.ToggleModal AdvancedSearch.Model.BuildingBlockBodyId |> AdvancedSearchMsg |> dispatch)
                        prop.text "Use advanced search body"
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let Main (model: Model) dispatch =
    let state_bb, setState_bb = React.useState(BuildingBlockUIState.init)
    let state_searchHeader, setState_searchHeader = React.useState(TermSearchUIState.init)
    let state_searchBody, setState_searchBody = React.useState(TermSearchUIState.init)
    let state_searchForUnit, setState_searchForUnit = React.useState(false)
    mainFunctionContainer [
        chooseBuildingBlock_element state_bb setState_bb state_searchHeader setState_searchHeader model dispatch
        if state_bb.BuildingBlockType.isTermColumn then
            BodyTerm.Main state_searchBody setState_searchBody state_searchForUnit setState_searchForUnit model dispatch
            AdvancedSearch.modal_container model dispatch
            AdvancedSearch.links_container state_bb.BuildingBlockType dispatch
        add_button state_bb state_searchHeader state_searchBody state_searchForUnit model dispatch
    ]
