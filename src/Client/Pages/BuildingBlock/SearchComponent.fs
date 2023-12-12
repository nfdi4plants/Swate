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
open ARCtrl.ISA
open BuildingBlock.Helper

open Microsoft.FSharp.Reflection

[<ReactComponent>]
let FreeTextInputElement(onSubmit: string -> unit) =
    let input, setInput = React.useState ""
    Html.span [
        Html.input [
            prop.onClick (fun e -> e.stopPropagation())
            prop.onChange (fun (v:string) -> setInput v)
        ]
        Html.button [
            prop.onClick (fun e -> e.stopPropagation(); onSubmit input)
            prop.text "✅"
        ]
    ]

module private DropdownElements =

    let private itemTooltipStyle = [style.fontSize (length.rem 1.1); style.paddingRight (length.px 10); style.textAlign.center; style.color NFDIColors.Yellow.Darker20]
    let private annotationsPrinciplesUrl = Html.a [prop.href Shared.URLs.AnnotationPrinciplesUrl; prop.target "_Blank"; prop.text "info"]

    let createSubBuildingBlockDropdownLink (state:BuildingBlockUIState) setState (subpage: Model.BuildingBlock.DropdownPage) =
        Bulma.dropdownItem.a [
            prop.tabIndex 0
            prop.onClick(fun e ->
                e.preventDefault()
                e.stopPropagation()
                setState {state with DropdownPage = subpage}
            )
            prop.style [
                style.paddingRight(length.rem 1)
                style.display.inlineFlex
                style.justifyContent.spaceBetween
            ]
            prop.children [
                //Html.span [
                //    prop.style itemTooltipStyle
                //    prop.className "has-tooltip-multiline"
                //    prop.custom("data-tooltip", subpage.toTooltip)
                //    prop.children (Bulma.icon [
                //        Html.i [prop.className "fa-solid fa-circle-info"]
                //    ])
                //]

                Html.span subpage.toString

                Html.span [
                    prop.style [style.width(length.px 20); style.alignSelf.flexEnd; style.lineHeight 1.5; style.fontSize(length.rem 1.1)]
                    prop.children (Bulma.icon (Html.i [prop.className "fa-solid fa-arrow-right"]))
                ]
            ]
        ]

    /// Navigation element back to main page
    let backToMainDropdownButton (state: BuildingBlockUIState) setState =
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

    let createBuildingBlockDropdownItem dispatch uiState setUiState (header: CompositeHeader) =
        Bulma.dropdownItem.a [
            match header with
            | CompositeHeader.FreeText _ 
            | CompositeHeader.Input (IOType.FreeText _) 
            | CompositeHeader.Output (IOType.FreeText _) -> 
                ()
            | _ ->
                prop.onClick (fun e ->
                    e.stopPropagation()
                    selectHeader uiState setUiState header |> dispatch
                )
                prop.onKeyDown(fun k -> if (int k.which) = 13 then selectHeader uiState setUiState header |> dispatch)
            prop.children [
                //Html.span [
                //    prop.style itemTooltipStyle
                //    prop.className "has-tooltip-multiline"
                //    prop.custom("data-tooltip", header.GetUITooltip())
                //    prop.children (Bulma.icon [
                //        Html.i [prop.className "fa-solid fa-circle-info"]
                //    ])
                //]
                match header with
                | CompositeHeader.Output io | CompositeHeader.Input io -> 
                    match io with
                    | IOType.FreeText str ->
                        let ch = if header.isOutput then CompositeHeader.Output else CompositeHeader.Input
                        let onSubmit = fun (v: string) -> 
                            let header = IOType.FreeText v |> ch
                            selectHeader uiState setUiState header |> dispatch
                        FreeTextInputElement onSubmit
                    | otherIO -> 
                        Html.span (string otherIO)
                | CompositeHeader.Component _ | CompositeHeader.Parameter _ | CompositeHeader.Factor _ | CompositeHeader.Characteristic _ ->
                        Html.span header.AsButtonName
                | _ -> Html.span (string header)
            ]
        ]

    /// Main column types subpage for dropdown
    let dropdownContentMain state setState (model:Model) dispatch =
        [
            DropdownPage.IOTypes (CompositeHeader.Input, CompositeHeader.InputEmpty.AsButtonName) |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownDivider []
            CompositeHeader.ParameterEmpty      |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.FactorEmpty         |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.CharacteristicEmpty |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.ComponentEmpty      |> createBuildingBlockDropdownItem dispatch state setState
            Model.BuildingBlock.DropdownPage.More  |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownDivider []
            DropdownPage.IOTypes (CompositeHeader.Output, CompositeHeader.OutputEmpty.AsButtonName) |> createSubBuildingBlockDropdownLink state setState
            Bulma.dropdownItem.div [
                prop.style [style.textAlign.right]
                prop.children annotationsPrinciplesUrl
            ]
        ]

    /// Protocol Type subpage for dropdown
    let dropdownContentProtocolTypeColumns state setState state_search setState_search (model:Model) dispatch =
        [
            // Heading
            //Bulma.dropdownItem.div [
            //    prop.style [style.textAlign.center]
            //    prop.children [
            //        Html.h6 [
            //            prop.className "subtitle"
            //            prop.style [style.fontWeight.bold]
            //            prop.text BuildingBlock.DropdownPage.More.toString
            //        ]
            //    ]
            //]
            //Bulma.dropdownDivider []
            CompositeHeader.Date                |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.Performer           |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.ProtocolDescription |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.ProtocolREF         |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.ProtocolType        |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.ProtocolUri         |> createBuildingBlockDropdownItem dispatch state setState
            CompositeHeader.ProtocolVersion     |> createBuildingBlockDropdownItem dispatch state setState
            // Navigation element back to main page
            backToMainDropdownButton state setState
        ]

    /// Output columns subpage for dropdown
    let dropdownContentIOTypeColumns (createHeaderFunc: IOType -> CompositeHeader) state setState (model:Model) dispatch =
        [
            // Heading
            //Bulma.dropdownItem.div [
            //    prop.style [style.textAlign.center]
            //    prop.children [
            //        Html.h6 [
            //            prop.className "subtitle"
            //            prop.style [style.fontWeight.bold]
            //            prop.text name
            //        ]
            //    ]
            //]
            //Bulma.dropdownDivider []
            createHeaderFunc IOType.Source            |> createBuildingBlockDropdownItem dispatch state setState
            createHeaderFunc IOType.Sample            |> createBuildingBlockDropdownItem dispatch state setState
            createHeaderFunc IOType.Material          |> createBuildingBlockDropdownItem dispatch state setState
            createHeaderFunc IOType.RawDataFile       |> createBuildingBlockDropdownItem dispatch state setState
            createHeaderFunc IOType.DerivedDataFile   |> createBuildingBlockDropdownItem dispatch state setState
            createHeaderFunc IOType.ImageFile         |> createBuildingBlockDropdownItem dispatch state setState
            createHeaderFunc (IOType.FreeText "")     |> createBuildingBlockDropdownItem dispatch state setState
            // Navigation element back to main page
            backToMainDropdownButton state setState
        ]

let private dropdownChoice_element state setState state_search setState_search (model: Model) dispatch =
    let state_bb = model.AddBuildingBlockState
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
                                prop.text (state_bb.Header.AsButtonName)
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
                        DropdownElements.dropdownContentMain state setState model dispatch
                    | Model.BuildingBlock.DropdownPage.More ->
                        DropdownElements.dropdownContentProtocolTypeColumns state setState state_search setState_search model dispatch
                    | Model.BuildingBlock.DropdownPage.IOTypes (createHeader,_) ->
                        DropdownElements.dropdownContentIOTypeColumns createHeader state setState model dispatch
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

    let createAutocompleteSuggestions (termSuggestions: Term []) (selectMsg: Term -> unit) (state: TermSearchUIState) setState (dispatch: Msg -> unit) =
        let suggestions = 
            if termSuggestions.Length > 0 then
                termSuggestions
                |> Array.distinctBy (fun t -> t.Accession)
                |> Array.collect (fun t ->
                    let msg = fun e ->
                        setState {state with SearchIsActive = false}
                        selectMsg t
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
let private termSearch_element (inputId: string) (onChangeMsg: string -> unit) (onDoubleClickMsg: string -> unit) (isVerified:bool) (state: TermSearchUIState) setState (valueOrDefault: string) =
    Bulma.control.p [
        Bulma.control.hasIconsRight
        prop.children [
            Bulma.input.text [
                if isVerified then Bulma.color.isSuccess
                prop.id inputId
                prop.key inputId
                prop.autoFocus (inputId.EndsWith "Main")
                prop.placeholder "Start typing to search"
                prop.valueOrDefault valueOrDefault
                prop.onDoubleClick (fun (e: Browser.Types.MouseEvent) -> 
                    let v: string = e.target?value
                    v |> onDoubleClickMsg
                )
                prop.onKeyDown(fun (e: Browser.Types.KeyboardEvent) ->
                    match e.which with
                    | 27. -> // Escape
                        setState {state with SearchIsActive = false}
                    | _ -> ()
                )
                prop.onChange (fun (e: string) -> onChangeMsg e)
            ]
            if isVerified then
                Bulma.icon [
                    Bulma.icon.isRight
                    Bulma.icon.isSmall
                    prop.className "fas fa-check"
                ]
        ]
    ]

let private chooseBuildingBlock_element (ui: BuildingBlockUIState) setUi (ui_search:TermSearchUIState) setUi_search (model: Model) dispatch =
    let state = model.AddBuildingBlockState
    let inputId = "BuildingBlock_InputMain"
    Bulma.field.div [
        Bulma.field.div [
            Bulma.field.hasAddons
            // Choose building block type dropdown element
            prop.children [
                // Dropdown building block type choice
                dropdownChoice_element ui setUi ui_search setUi_search model dispatch
                // Term search field
                if state.Header.IsTermColumn && not state.Header.IsFeaturedColumn then
                    let onChangeMsg = fun (v:string) ->
                        let triggerNewSearch = v.Length > 2
                        let h = state.Header.UpdateWithOA (OntologyAnnotation.fromString v)
                        selectHeader ui setUi h |> dispatch 
                        if triggerNewSearch then
                            setUi_search { SearchIsActive = true; SearchIsLoading = true }
                            let msg = BuildingBlock.Msg.GetHeaderSuggestions (v, {state = ui_search; setState = setUi_search}) |> BuildingBlockMsg
                            let bounce_msg = Bounce (System.TimeSpan.FromSeconds 0.5,nameof(msg), msg)
                            bounce_msg |> dispatch
                    Bulma.control.div [
                        Bulma.control.isExpanded
                        prop.children [
                            let valueOrDefault = model.AddBuildingBlockState.Header.ToTerm().NameText
                            termSearch_element inputId onChangeMsg onChangeMsg (hasVerifiedTermHeader state.Header) ui_search setUi_search valueOrDefault
                        ]
                    ]
                elif state.Header.IsIOType then
                    Bulma.control.div [
                        Bulma.control.isExpanded
                        prop.children [
                            Bulma.control.p [
                                Bulma.input.text [
                                    Bulma.color.hasBackgroundGreyLighter
                                    prop.readOnly true
                                    prop.valueOrDefault (
                                        state.Header.TryIOType()
                                        |> Option.get
                                        |> _.ToString()
                                    )
                                ]
                            ]
                        ]
                    ]
            ]
        ]
        // Ontology Term search preview
        if ui_search.SearchIsActive then
            let selectMsg = fun (term:Term) -> 
                let oa = OntologyAnnotation.fromTerm term
                let h = model.AddBuildingBlockState.Header.UpdateWithOA oa
                selectHeader ui setUi h |> dispatch
            AutocompleteComponents.autocompleteDropdownComponent model.AddBuildingBlockState.HeaderSearchResults selectMsg ui_search setUi_search model dispatch
    ]

module private BodyTerm =

    let private termOrUnit_switch (uiState: BuildingBlockUIState) setUiState =

        Bulma.buttons [
            Bulma.buttons.hasAddons
            prop.style [style.flexWrap.nowrap; style.marginBottom 0; style.marginRight (length.rem 1)]
            prop.children [
                Bulma.button.a [
                    let isActive = uiState.BodyCellType = BodyCellType.Term
                    if isActive then Bulma.color.isSuccess
                    prop.onClick (fun _ -> {uiState with BodyCellType = BodyCellType.Term } |> setUiState)
                    prop.classes ["pr-2 pl-2 mb-0"; if isActive then "is-selected"]
                    prop.text "Term"
                ]
                Bulma.button.a [
                    let isActive = uiState.BodyCellType = BodyCellType.Unitized
                    if isActive then Bulma.color.isSuccess
                    prop.onClick (fun _ -> {uiState with BodyCellType = BodyCellType.Unitized } |> setUiState)
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
            //BuildingBlock.Msg.SelectBodyCell None |> BuildingBlockMsg |> dispatch
            //BuildingBlock.UpdateBodySearchText v |> BuildingBlockMsg |> dispatch
            let updateState msg =
                setState {SearchIsActive = true; SearchIsLoading = true}
                let bounce_msg = Bounce (System.TimeSpan.FromSeconds 0.5,nameof(msg), msg)
                bounce_msg |> dispatch
            match isClicked, state_isDirectedSearchMode, model.AddBuildingBlockState.Header.IsTermColumn, model.AddBuildingBlockState.BodySearchText with
            // only execute this on onDoubleClick event. If executed on onChange event it will trigger when deleting term.
            | true, true, true, "" -> // Search all children 
                let parent = model.AddBuildingBlockState.Header.ToTerm().ToTermMinimal()
                BuildingBlock.Msg.GetBodyTermsByParent (parent, {state = state; setState = setState}) |> BuildingBlockMsg
                |> updateState
            | _, true, true, any when any.Length > 2 ->
                let parent = model.AddBuildingBlockState.Header.ToTerm().ToTermMinimal()
                BuildingBlock.Msg.GetBodySuggestionsByParent (v,parent,{state = state; setState = setState}) |> BuildingBlockMsg
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
                    if state_isDirectedSearchMode && not model.AddBuildingBlockState.Header.IsTermColumn then
                        prop.title "No parent term selected"
                    elif state_isDirectedSearchMode && hasVerifiedTermHeader model.AddBuildingBlockState.Header && model.AddBuildingBlockState.BodySearchText = "" then
                        prop.title "Double click to show all children"
                    prop.style [
                        // display box-shadow if term search is fully activated
                        if state_isDirectedSearchMode && hasVerifiedTermHeader model.AddBuildingBlockState.Header then style.boxShadow(2,2,NFDIColors.Mint.Lighter20)
                    ]
                    prop.children [
                        let valueOrDefault = model.AddBuildingBlockState.BodyCell.ToTerm().NameText
                        termSearch_element inputId (onChangeMsg false) (onChangeMsg true) (hasVerifiedCell model.AddBuildingBlockState.BodyCell) state setState valueOrDefault
                    ]
                ]
            ]
        ]

    let Main uiState setState searchState setSearchState model dispatch =
        let inputId = "BuildingBlock_BodyInput"
        Bulma.field.div [
            Bulma.field.div [
                prop.style [ style.display.flex; style.justifyContent.spaceBetween ]
                prop.children [
                    termOrUnit_switch uiState setState
                    searchElement inputId searchState setSearchState model dispatch
                ]
            ]
            // Ontology Term search preview
            if searchState.SearchIsActive then
                let selectMsg = fun (term:Term) -> 
                    let oa = OntologyAnnotation.fromTerm term
                    let cell = createCellFromUiStateAndOA uiState oa
                    BuildingBlock.SelectBodyCell cell |> BuildingBlockMsg |> dispatch
                AutocompleteComponents.autocompleteDropdownComponent model.AddBuildingBlockState.BodySearchResults selectMsg searchState setSearchState model dispatch
        ]

let private add_button (ui: BuildingBlockUIState) (model: Model) dispatch =
    let state = model.AddBuildingBlockState
    Bulma.field.div [
        Bulma.button.button  [
            let header = state.Header
            let body = state.BodyCell
            let isValid = Helper.isValidColumn header
            if isValid then
                Bulma.color.isSuccess
                Bulma.button.isActive
            else
                Bulma.color.isDanger
                prop.disabled true
            Bulma.button.isFullWidth
            prop.onClick (fun _ ->
                let updateBodyCell =
                    match ui.BodyCellType with
                    | BodyCellType.Text -> body.ToFreeTextCell()
                    | BodyCellType.Term -> body.ToTermCell()
                    | BodyCellType.Unitized -> body.ToUnitizedCell()
                let column = CompositeColumn.create(header, [|updateBodyCell|])
                SpreadsheetInterface.AddAnnotationBlock column |> InterfaceMsg |> dispatch
                BuildingBlock.Msg.SelectBodyCell (updateBodyCell.GetEmptyCell()) |> BuildingBlockMsg |> dispatch
            )
            prop.text "Add Column"
        ]
    ]

module private AdvancedSearch =

    let modal_container (uiState: BuildingBlockUIState) setUiState (model:Model) dispatch =
        Html.span [
            let selectHeader = fun (term:Term) ->
                let h = model.AddBuildingBlockState.Header.UpdateWithOA(OntologyAnnotation.fromTerm term)
                Msg.Batch [
                    BuildingBlock.UpdateHeaderSearchText term.Name |> BuildingBlockMsg
                    selectHeader uiState setUiState h
                ]
            let selectBody = fun (term:Term) ->
                let oa = OntologyAnnotation.fromTerm term
                let cell = createCellFromUiStateAndOA uiState oa
                Msg.Batch [
                    BuildingBlock.UpdateBodySearchText term.Name |> BuildingBlockMsg
                    BuildingBlock.SelectBodyCell cell |> BuildingBlockMsg
                ]
            // added edge case to modal, where relatedInputId = "" is ignored
            SidebarComponents.AdvancedSearch.advancedSearchModal model AdvancedSearch.Model.BuildingBlockHeaderId "" dispatch selectHeader
            SidebarComponents.AdvancedSearch.advancedSearchModal model AdvancedSearch.Model.BuildingBlockBodyId "" dispatch selectBody
        ]

    let links_container (bb_type: CompositeHeader) dispatch =
        Html.div [
            if not bb_type.IsFeaturedColumn then
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
    mainFunctionContainer [
        chooseBuildingBlock_element state_bb setState_bb state_searchHeader setState_searchHeader model dispatch
        if model.AddBuildingBlockState.Header.IsTermColumn then
            BodyTerm.Main state_bb setState_bb state_searchBody setState_searchBody model dispatch
            AdvancedSearch.modal_container state_bb setState_bb model dispatch
            AdvancedSearch.links_container model.AddBuildingBlockState.Header dispatch
        add_button state_bb model dispatch
    ]
