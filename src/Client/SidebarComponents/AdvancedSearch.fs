module SidebarComponents.AdvancedSearch

open Fable.React
open Fable.React.Props
open Elmish

open Shared
open TermTypes
open ExcelColors
open Model
open Messages
open CustomComponents

open Feliz
open Feliz.Bulma

open AdvancedSearchTypes
open AdvancedSearch

let update (advancedTermSearchMsg: AdvancedSearch.Msg) (currentState:AdvancedSearch.Model) : AdvancedSearch.Model * Cmd<Messages.Msg> =
    match advancedTermSearchMsg with
    | UpdateAdvancedTermSearchSubpage subpage ->
        let nextState = {
            currentState with
                AdvancedTermSearchSubpage = subpage
        }
        nextState, Cmd.none

    | ToggleModal modalId ->
        let nextState = {
            currentState with
                ModalId = modalId
                HasModalVisible = (not currentState.HasModalVisible)
        }

        nextState,Cmd.none

    | ToggleOntologyDropdown ->
        let nextState = {
            currentState with
                HasOntologyDropdownVisible = (not currentState.HasOntologyDropdownVisible)
        }

        nextState,Cmd.none

    | UpdateAdvancedTermSearchOptions opts ->

        let nextState = {
            currentState with
                AdvancedSearchOptions = opts
                HasOntologyDropdownVisible = false
        }

        nextState,Cmd.none

    | StartAdvancedSearch ->

        let nextState = {
            currentState with
                AdvancedTermSearchSubpage       = AdvancedSearchSubpages.ResultsSubpage
                HasAdvancedSearchResultsLoading = true
        }

        let nextCmd =
            currentState.AdvancedSearchOptions
            |> GetNewAdvancedTermSearchResults
            |> Request
            |> Api
            |> Cmd.ofMsg

        nextState,nextCmd

    | ResetAdvancedSearchState ->
        let nextState = AdvancedSearch.Model.init()

        nextState,Cmd.none

    | NewAdvancedSearchResults results ->
        let nextState = {
            currentState with
                AdvancedSearchTermResults       = results
                AdvancedTermSearchSubpage       = AdvancedSearchSubpages.ResultsSubpage
                HasAdvancedSearchResultsLoading = false
        }

        nextState,Cmd.none

open Messages

let createLinkOfAccession (accession:string) =
    a [
        let link = accession |> URLs.termAccessionUrlOfAccessionStr
        Href link
        Target "_Blank"
    ] [
        str accession
    ]

let private isValidAdancedSearchOptions (opt:AdvancedSearchOptions) =
    ((
        opt.TermName.Length
        + opt.TermDefinition.Length
    ) > 0)

let private ontologyDropdownItem (model:Model) (dispatch:Msg -> unit) (ontOpt: Ontology option)  =
    let str =
        if ontOpt.IsSome then
            ontOpt.Value.Name |> str
        else
            "All Ontologies" |> str
    option [
        TabIndex 0
        Value str
    ] [
        str
    ]

open Fable.Core.JsInterop

module private ResultsTable =
    open Feliz

    open Shared.TermTypes

    type TableModel = {
        Data            : Term []
        ActiveDropdowns : string list
        ElementsPerPage : int
        PageIndex       : int
        Dispatch        : Msg -> unit
        RelatedInputId  : string 
        ResultHandler   : Term -> Msg
    }

    let createPaginationLinkFromIndex (updatePageIndex: int ->unit) (pageIndex:int) (currentPageinationIndex: int)=
        let isActve = pageIndex = currentPageinationIndex
        Bulma.paginationLink.a [
            if isActve then Bulma.paginationLink.isCurrent
            prop.style [
                if isActve then style.color "white";
                style.backgroundColor NFDIColors.Mint.Base
                style.borderColor NFDIColors.Mint.Base
            ]
            prop.onClick(fun _ -> pageIndex |> updatePageIndex)
            prop.text (string (pageIndex+1))
        ]

    let pageinateDynamic (updatePageIndex:int->unit) (currentPageinationIndex: int) (pageCount:int)  = 
        (*[0 .. pageCount-1].*)
        [(max 1 (currentPageinationIndex-2)) .. (min (currentPageinationIndex+2) (pageCount-1)) ]
        |> List.map (
            fun index -> createPaginationLinkFromIndex updatePageIndex index currentPageinationIndex
        ) 

    let private createAdvancedTermSearchResultRows (state:TableModel) (setState:TableModel -> unit) =
        if state.Data |> Array.isEmpty |> not then
            state.Data
            |> Array.collect (fun sugg ->
                let id = sprintf "isHidden_advanced_%s" sugg.Accession 
                [|
                    Html.tr [
                        prop.onClick (fun e ->
                            // dont close modal on click
                            e.stopPropagation()
                            e.preventDefault()
                            if state.RelatedInputId <> "" then
                                let relInput = Browser.Dom.document.getElementById(state.RelatedInputId)
                                // propagate wanted term name to related input on main page
                                relInput?value <- sugg.Name
                            // select wanted term
                            sugg |> state.ResultHandler |> state.Dispatch
                            // reset advanced term search state
                            ResetAdvancedSearchState |> AdvancedSearchMsg |> state.Dispatch
                        )
                        prop.tabIndex 0
                        prop.className "suggestion"
                        prop.children [
                            td [] [
                                b [] [ str sugg.Name ]
                            ]
                            if sugg.IsObsolete then
                                td [Style [Color "red"]] [str "obsolete"]
                            else
                                td [] []
                            td [
                                OnClick (
                                    fun e ->
                                        e.stopPropagation()
                                )
                                Style [FontWeight "light"]
                            ] [
                                small [] [
                                    createLinkOfAccession sugg.Accession
                            ] ]
                            td [] [
                                Bulma.buttons [
                                    Bulma.buttons.isRight
                                    prop.children [
                                        //Button.a [
                                        //    Button.Props [Title "Show Term Tree"]
                                        //    Button.Size IsSmall
                                        //    Button.Color IsSuccess
                                        //    Button.IsInverted
                                        //    Button.OnClick(fun e ->
                                        //        e.preventDefault()
                                        //        e.stopPropagation()
                                        //        Cytoscape.Msg.GetTermTree sugg.Accession |> CytoscapeMsg |> dispatch
                                        //    )
                                        //] [
                                        //    Icon.icon [] [
                                        //        Fa.i [Fa.Solid.Tree] []
                                        //    ]
                                        //]
                                        Bulma.button.a [
                                            Bulma.button.isSmall
                                            Bulma.color.isBlack
                                            Bulma.button.isInverted
                                            prop.onClick(fun e ->
                                                e.preventDefault()
                                                e.stopPropagation()
                                                if List.contains id state.ActiveDropdowns then
                                                    let nextState = { state with ActiveDropdowns = List.except [id] state.ActiveDropdowns}
                                                    setState nextState
                                                else
                                                    let nextState = { state with ActiveDropdowns = id::state.ActiveDropdowns}
                                                    setState nextState
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
                    Html.tr [
                        prop.onClick(fun e -> e.stopPropagation())
                        prop.id id
                        prop.className "suggestion-details"
                        prop.style [if List.contains id state.ActiveDropdowns then style.visibility.visible else style.visibility.collapse]
                        prop.children [
                            td [ColSpan 4] [
                                Bulma.content [
                                    b [] [ str "Definition: " ]
                                    str sugg.Description
                                ]
                            ]
                        ]
                    ]
                |]
            )
        else
            [|
                Html.tr [
                    td [] [str "No terms found matching your input."]
                ]
            |]


    open Feliz


    [<ReactComponent>]
    let paginatedTableComponent (model:Model) (data: TableModel) =

        let handlerState, setState = React.useState data
        let updatePageIndex (model:TableModel) (ind:int) =
            let nextState = {model with PageIndex = ind}
            setState nextState

        if data.Data.Length > 0 then 

            let tableRows = createAdvancedTermSearchResultRows handlerState setState
            let currentPageinationIndex = handlerState.PageIndex
            let chunked = tableRows |> Array.chunkBySize data.ElementsPerPage
            let len = chunked.Length 
    
            Bulma.container [
                Bulma.table [
                    Bulma.table.isFullWidth
                    prop.children [
                        thead [] []
                        tbody [] (
                            chunked.[currentPageinationIndex] |> List.ofArray
                        )
                    ]
                ]
                Bulma.pagination [
                    Bulma.pagination.isCentered
                    prop.children [
                        Bulma.paginationPrevious.button [
                            prop.style [style.cursor.pointer]
                            prop.onClick (fun _ ->
                                max (currentPageinationIndex - 1) 0 |> updatePageIndex handlerState 
                            )
                            prop.disabled <| (currentPageinationIndex = 0)
                            prop.text "Prev"
                        ]
                        Bulma.paginationList [
                            yield createPaginationLinkFromIndex (updatePageIndex handlerState) 0 currentPageinationIndex
                            if len > 5 && currentPageinationIndex > 3 then yield Bulma.paginationEllipsis []
                            yield! pageinateDynamic (updatePageIndex handlerState) currentPageinationIndex (len - 1)
                            if len > 5 && currentPageinationIndex < len-4 then yield Bulma.paginationEllipsis []
                            if len > 1 then yield createPaginationLinkFromIndex (updatePageIndex handlerState) (len-1) currentPageinationIndex
                        ]
                        Bulma.paginationNext.button [
                            prop.style [style.cursor.pointer]
                            prop.onClick (fun _ ->
                                let next = min (currentPageinationIndex + 1) (len - 1)
                                next |> updatePageIndex handlerState
                            )
                            prop.disabled <| (currentPageinationIndex = len - 1)
                            prop.text "Next"
                        ]
                    ]
                ]
            ]
        else
            Html.div []

let private keepObsoleteCheckradioElement (model:Model) dispatch (keepObsolete:bool) modalId =
    let checkradioName = "keepObsolete_checkradio"
    let id = sprintf "%s_%A_%A"checkradioName keepObsolete modalId
    Bulma.field.div [
        Bulma.Checkradio.checkbox [
            prop.name checkradioName
            prop.id id
            prop.isChecked (model.AdvancedSearchState.AdvancedSearchOptions.KeepObsolete = keepObsolete)
            prop.onChange (fun (e:bool) ->
                {model.AdvancedSearchState.AdvancedSearchOptions
                    with KeepObsolete = keepObsolete
                }
                |> UpdateAdvancedTermSearchOptions
                |> AdvancedSearchMsg
                |> dispatch
            )
        ]
        Html.label [
            prop.htmlFor id
            prop.text (if keepObsolete then "yes" else "no")
        ]
    ]

let private inputFormPage modalId (model:Model) (dispatch: Msg -> unit) =
    Html.div [
        Bulma.field.div [
            Bulma.label  "Term name keywords:"
            Bulma.field.div [
                Bulma.control.div [
                    Bulma.input.text [
                        prop.placeholder "... search term name"
                        Bulma.input.isSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        prop.onChange (fun (e:string) ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with TermName = e
                            }
                            |> UpdateAdvancedTermSearchOptions
                            |> AdvancedSearchMsg
                            |> dispatch)
                        prop.valueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.TermName
                        prop.onKeyDown (fun e ->
                            match e.which with
                            | 13. ->
                                e.preventDefault()
                                e.stopPropagation();
                                let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                                if isValid then
                                    StartAdvancedSearch |> AdvancedSearchMsg |> dispatch
                            | _ -> ()
                        )
                    ]
                ] 
            ]
        ]
        Bulma.field.div [
            Bulma.label "Term definition keywords:"
            Bulma.field.div [
                Bulma.control.div [
                    Bulma.input.text [
                        prop.placeholder "... search term definition"
                        Bulma.input.isSmall
                        //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                        prop.onChange (fun (e: string) ->
                            {model.AdvancedSearchState.AdvancedSearchOptions
                                with TermDefinition = e
                            }
                            |> UpdateAdvancedTermSearchOptions
                            |> AdvancedSearchMsg
                            |> dispatch)
                        prop.onKeyDown (fun e ->
                            match e.which with
                            | 13. ->
                                e.preventDefault()
                                e.stopPropagation();
                                let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                                if isValid then
                                    StartAdvancedSearch |> AdvancedSearchMsg |> dispatch
                            | _ -> ()
                        )
                        prop.valueOrDefault model.AdvancedSearchState.AdvancedSearchOptions.TermDefinition
                    ]
                ] 
            ]
        ] 
        Bulma.field.div [
            Bulma.label "Ontology"
            Bulma.control.div [
                Bulma.select [
                    Html.select [
                        prop.placeholder "All Ontologies";
                        if model.AdvancedSearchState.AdvancedSearchOptions.OntologyName.IsSome then prop.value model.AdvancedSearchState.AdvancedSearchOptions.OntologyName.Value
                        prop.onChange (fun (e:string) ->
                            let nextSearchOptions = {
                                model.AdvancedSearchState.AdvancedSearchOptions
                                    with OntologyName = if e = "All Ontologies" then None else Some e
                            }
                            nextSearchOptions |> UpdateAdvancedTermSearchOptions |> AdvancedSearchMsg |> dispatch
                        )
                        prop.children [
                            ontologyDropdownItem model dispatch None
                            yield! (
                                model.PersistentStorageState.SearchableOntologies
                                |> Array.map snd
                                |> Array.toList
                                |> List.sortBy (fun o -> o.Name)
                                |> List.map (fun ont -> ontologyDropdownItem model dispatch (Some ont))
                            )
                        ]
                    ]
                ]
            ]
        ]
        Bulma.field.div [
            Bulma.label "Keep obsolete terms"
            Html.div [
                keepObsoleteCheckradioElement model dispatch true modalId
                keepObsoleteCheckradioElement model dispatch false modalId
            ]
        ]
    ]

let private resultsPage relatedInputId resultHandler (model:Model) (dispatch: Msg -> unit) =
    Bulma.field.div [
        Bulma.label "Results:"
        if model.AdvancedSearchState.AdvancedTermSearchSubpage = AdvancedSearchSubpages.ResultsSubpage then
            if model.AdvancedSearchState.HasAdvancedSearchResultsLoading then
                Html.div [
                    prop.style [style.width(length.perc 100); style.display.flex; style.justifyContent.center]
                    prop.children Modals.Loading.loadingComponent
                ]
            else
                let init: ResultsTable.TableModel = {
                    Data            = model.AdvancedSearchState.AdvancedSearchTermResults
                    ActiveDropdowns = []
                    ElementsPerPage = 10
                    PageIndex       = 0
                    Dispatch        = dispatch
                    ResultHandler   = resultHandler
                    RelatedInputId  = relatedInputId
                }
                ResultsTable.paginatedTableComponent
                    model
                    init
    ]

let advancedSearchModal (model:Model) (modalId: string) (relatedInputId:string) (dispatch: Msg -> unit) (resultHandler: Term -> Msg) =
    Bulma.modal [
        if (model.AdvancedSearchState.HasModalVisible
            && model.AdvancedSearchState.ModalId = modalId) then Bulma.modal.isActive
        prop.id modalId
        prop.children [
            // Close modal on click on background
            Bulma.modalBackground [ prop.onClick (fun e -> ResetAdvancedSearchState |> AdvancedSearchMsg |> dispatch)]
            Bulma.modalCard [
                prop.style [style.width(length.perc 90); style.maxWidth(length.px 600); style.height(length.perc 80); style.maxHeight(length.px 600)]
                prop.children [
                    Bulma.modalCardHead [
                        prop.children [
                            Bulma.modalCardTitle "Advanced Search"
                            Bulma.delete [prop.onClick(fun _ -> ResetAdvancedSearchState |> AdvancedSearchMsg |> dispatch)]
                        ]
                    ]
                    Bulma.modalCardBody [
                        prop.children [
                            Bulma.field.div [ Bulma.help [
                                prop.style [style.textAlign.justify]
                                prop.text "Swate advanced search uses the Apache Lucene query parser syntax. Feel free to read the related Swate documentation [wip] for guidance on how to use it."
                            ]]
                            match model.AdvancedSearchState.AdvancedTermSearchSubpage with
                            | AdvancedSearchSubpages.InputFormSubpage ->
                                // we need to propagate the modal id here, so we can use meaningful and UNIQUE ids to the checkradio id's
                                inputFormPage modalId model dispatch
                            | AdvancedSearchSubpages.ResultsSubpage ->
                                resultsPage relatedInputId resultHandler model dispatch
                        ]
                    ]
                    Bulma.modalCardFoot [
                        Html.form [
                            prop.onSubmit (fun e -> e.preventDefault())
                            prop.onKeyDown(key.enter, fun k -> k.preventDefault())
                            prop.style [style.width(length.perc 100)]
                            prop.children [
                                if model.AdvancedSearchState.AdvancedTermSearchSubpage <> AdvancedSearchSubpages.InputFormSubpage then
                                    Bulma.levelItem [
                                        Bulma.button.button [   
                                            Bulma.color.isDanger
                                            Bulma.button.isFullWidth
                                            prop.onClick (fun e -> e.stopPropagation(); e.preventDefault(); UpdateAdvancedTermSearchSubpage InputFormSubpage |> AdvancedSearchMsg |> dispatch)
                                            prop.text "Back"
                                        ]
                                    ]
                                // Show "Start advanced search" button ONLY on first subpage
                                if model.AdvancedSearchState.AdvancedTermSearchSubpage = AdvancedSearchSubpages.InputFormSubpage then
                                    Bulma.levelItem [
                                        Bulma.button.button [
                                            let isValid = isValidAdancedSearchOptions model.AdvancedSearchState.AdvancedSearchOptions
                                            if isValid then
                                                Bulma.color.isSuccess
                                                Bulma.button.isActive
                                            else
                                                Bulma.color.isDanger
                                                prop.disabled true
                                            Bulma.button.isFullWidth
                                            prop.onClick (fun e ->
                                                e.preventDefault()
                                                e.stopPropagation();
                                                StartAdvancedSearch |> AdvancedSearchMsg |> dispatch
                                            )
                                            prop.text "Start advanced search"
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]