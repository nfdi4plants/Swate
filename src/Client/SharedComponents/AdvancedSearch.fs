module Components.AdvancedSearch

open Fable.React
open Fable.React.Props
open Elmish
open Swate
open Swate.Components
open Shared
open Database
open Model
open Messages

open Feliz
open Feliz.DaisyUI

open AdvancedSearchTypes
open AdvancedSearch

open Messages


let private StartAdvancedSearch (state: AdvancedSearch.Model) setState dispatch =
    let setter (terms: Term []) =
        setState
            { state with
                AdvancedSearchTermResults = terms
                Subpage = AdvancedSearch.AdvancedSearchSubpages.ResultsSubpage
            }
    AdvancedSearch.Msg.GetSearchResults {|config=state.AdvancedSearchOptions; responseSetter = setter|}  |> AdvancedSearchMsg |> dispatch

let private createLinkOfAccession (accession:string) =
    let link = ARCtrl.OntologyAnnotation(tan=accession).TermAccessionOntobeeUrl
    Html.a [
        prop.href link
        prop.target.blank
        prop.text accession
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

    type TableModel = {
        Data            : Term []
        ActiveDropdowns : string list
        ElementsPerPage : int
        PageIndex       : int
        ResultHandler   : Term -> unit
    }

    let createPaginationLinkFromIndex (updatePageIndex: int ->unit) (pageIndex:int) (currentPageinationIndex: int)=
        let isActve = pageIndex = currentPageinationIndex
        Daisy.button.a [
            join.item
            if isActve then
                button.active
                button.primary
            prop.onClick(fun _ -> pageIndex |> updatePageIndex)
            prop.text (string (pageIndex+1))
        ]

    let pageinateDynamic (updatePageIndex:int->unit) (currentPageinationIndex: int) (pageCount:int)  =
        (*[0 .. pageCount-1].*)
        React.fragment [
            for index in (max 1 (currentPageinationIndex-1)) .. (min (currentPageinationIndex+1) (pageCount-1)) do
                createPaginationLinkFromIndex updatePageIndex index currentPageinationIndex
        ]

    let private createAdvancedTermSearchResultRows (state:TableModel) (setState:TableModel -> unit) (resetAdvancedSearchState: unit -> unit) =
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
                            // select wanted term
                            state.ResultHandler sugg
                            // reset advanced term search state
                            resetAdvancedSearchState()
                        )
                        prop.tabIndex 0
                        prop.className "suggestion"
                        prop.children [
                            Html.td [Html.b sugg.Name ]
                            if sugg.IsObsolete then
                                Html.td [prop.style [style.color "red"]; prop.text "obsolete"]
                            else
                                Html.td []
                            Html.td [
                                prop.onClick (
                                    fun e ->
                                        e.stopPropagation()
                                )
                                prop.style [style.fontWeight.lighter]
                                prop.children [
                                    Html.small [
                                        createLinkOfAccession sugg.Accession
                                    ]
                                ]
                            ]
                            Html.td [
                                Html.div [
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
                                        Daisy.button.a [
                                            button.sm
                                            button.neutral
                                            button.outline
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
                                                Html.i [prop.className "fa-solid fa-chevron-down"]
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
                            Html.td [
                                prop.colSpan 4
                                prop.children [
                                    Html.div [
                                        prop.className "prose"
                                        prop.children [
                                            Html.b "Definition: "
                                            Html.text sugg.Description
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                |]
            )
        else
            [|
                Html.tr [
                    Html.td "No terms found matching your input."
                ]
            |]


    open Feliz


    [<ReactComponent>]
    let paginatedTableComponent (state:AdvancedSearch.Model) setState (data: TableModel) =

        let resetAdvancedSearchState = fun _ -> AdvancedSearch.Model.init() |> setState
        let handlerState, setState = React.useState data
        let updatePageIndex (model:TableModel) (ind:int) =
            let nextState = {model with PageIndex = ind}
            setState nextState

        if data.Data.Length > 0 then

            let tableRows = createAdvancedTermSearchResultRows handlerState setState resetAdvancedSearchState
            let currentPageinationIndex = handlerState.PageIndex
            let chunked = tableRows |> Array.chunkBySize data.ElementsPerPage
            let len = chunked.Length
            let disabledEllipsisButton = Daisy.button.button [button.disabled; prop.className "join-item"; prop.text "..."]
            Html.div [
                prop.className "space-y-2 flex flex-col h-full"
                prop.children [
                    Daisy.table [
                        prop.className "grow"
                        table.xs
                        prop.children [
                            Html.thead []
                            Html.tbody (
                                chunked.[currentPageinationIndex] |> List.ofArray
                            )
                        ]
                    ]
                    Daisy.join [
                        prop.children [
                            Daisy.button.button [
                                join.item
                                prop.className "cursor-pointer join-item"
                                prop.onClick (fun _ ->
                                    max (currentPageinationIndex - 1) 0 |> updatePageIndex handlerState
                                )
                                prop.disabled <| (currentPageinationIndex = 0)
                                prop.text "«"
                            ]
                            createPaginationLinkFromIndex (updatePageIndex handlerState) 0 currentPageinationIndex
                            // if len > 5 && currentPageinationIndex > 3 then disabledEllipsisButton
                            // pageinateDynamic (updatePageIndex handlerState) currentPageinationIndex (len - 1)
                            if currentPageinationIndex = 0 || currentPageinationIndex = len - 1 then
                                disabledEllipsisButton
                            else
                                createPaginationLinkFromIndex (updatePageIndex handlerState) currentPageinationIndex currentPageinationIndex
                            // if len > 5 && currentPageinationIndex < len-4 then disabledEllipsisButton
                            if len > 1 then createPaginationLinkFromIndex (updatePageIndex handlerState) (len-1) currentPageinationIndex
                            Daisy.button.button [
                                join.item
                                prop.style [style.cursor.pointer]
                                prop.onClick (fun _ ->
                                    let next = min (currentPageinationIndex + 1) (len - 1)
                                    next |> updatePageIndex handlerState
                                )
                                prop.disabled <| (currentPageinationIndex = len - 1)
                                prop.text "»"
                            ]
                        ]
                    ]
                ]
            ]
        else
            Html.div []

let private keepObsoleteCheckradioElement (state:AdvancedSearch.Model) setState =
    let currentKeepObsolete = state.AdvancedSearchOptions.KeepObsolete
    Daisy.formControl [
        Daisy.label [
            Daisy.checkbox [
                prop.isChecked (state.AdvancedSearchOptions.KeepObsolete)
                prop.onChange (fun (e:bool) ->
                    {state with AdvancedSearch.Model.AdvancedSearchOptions.KeepObsolete = e}
                    |> setState
                )
            ]
            Daisy.labelText [
                prop.className "is-unselectable"
                prop.text (if currentKeepObsolete then " yes" else " no")
            ]
        ]
    ]

let private inputFormPage (state:AdvancedSearch.Model) (setState:AdvancedSearch.Model -> unit) dispatch =
    Html.div [
        Daisy.formControl [
            Daisy.label [ Daisy.labelText "Term name keywords:" ]
            Daisy.input [
                prop.placeholder "... search term name"
                input.sm
                prop.autoFocus true
                input.bordered
                prop.onChange (fun (e:string) ->
                    {state with AdvancedSearch.Model.AdvancedSearchOptions.TermName = e }|> setState
                )
                prop.valueOrDefault state.AdvancedSearchOptions.TermName
                prop.onKeyDown (fun e ->
                    match e.which with
                    | 13. ->
                        e.preventDefault()
                        e.stopPropagation();
                        let isValid = isValidAdancedSearchOptions state.AdvancedSearchOptions
                        if isValid then
                            setState
                                { state with
                                    Subpage                         = AdvancedSearchSubpages.ResultsSubpage
                                    HasAdvancedSearchResultsLoading = true
                                }
                            StartAdvancedSearch state setState dispatch
                    | _ -> ()
                )
            ]
        ]
        Daisy.formControl [
            Daisy.label [ Daisy.labelText "Term definition keywords:" ]
            Daisy.input [
                prop.placeholder "... search term definition"
                input.sm
                input.bordered
                //Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                prop.onChange (fun (e: string) -> {state with AdvancedSearch.Model.AdvancedSearchOptions.TermDefinition = e} |> setState)
                prop.onKeyDown (fun e ->
                    match e.which with
                    | 13. ->
                        e.preventDefault()
                        e.stopPropagation();
                        let isValid = isValidAdancedSearchOptions state.AdvancedSearchOptions
                        if isValid then
                            StartAdvancedSearch state setState dispatch
                    | _ -> ()
                )
                prop.valueOrDefault state.AdvancedSearchOptions.TermDefinition
            ]
        ]
        Daisy.formControl [
            Daisy.label [ Daisy.labelText "Keep obsolete terms" ]
            keepObsoleteCheckradioElement state setState
        ]
    ]

let private resultsPage (resultHandler: Term -> unit) (state: AdvancedSearch.Model) setState =
    Html.div [
        prop.className "h-full flex"
        prop.children [
            if state.Subpage = AdvancedSearchSubpages.ResultsSubpage then
                if state.HasAdvancedSearchResultsLoading then
                    Html.div [
                        prop.style [style.width(length.perc 100); style.display.flex; style.justifyContent.center]
                        prop.children Modals.Loading.Component
                    ]
                else
                    let init: ResultsTable.TableModel = {
                        Data            = state.AdvancedSearchTermResults
                        ActiveDropdowns = []
                        ElementsPerPage = 10
                        PageIndex       = 0
                        ResultHandler   = resultHandler
                    }
                    ResultsTable.paginatedTableComponent
                        state
                        setState
                        init
        ]
    ]

[<ReactComponent>]
let Main (isActive: bool, setIsActive: bool -> unit, resultHandler: Term -> unit, dispatch) =
    let state, setState = React.useState(AdvancedSearch.Model.init)
    React.useEffect(
        (fun _ -> AdvancedSearch.Model.init() |> setState),
        [|box isActive|]
    )
    Daisy.modal.div [
        //if (model.AdvancedSearchState.HasModalVisible
        //    && model.AdvancedSearchState.ModalId = modalId) then
        if isActive then modal.active
        //prop.id modalId
        prop.children [
            // Close modal on click on background
            Daisy.modalBackdrop [ prop.onClick (fun e -> setIsActive false)]
            Daisy.modalBox.div [
                prop.style [style.width(length.perc 90); style.maxWidth(length.px 600); style.height(length.perc 80); style.maxHeight(length.px 600)]
                prop.children [
                    Daisy.card [
                        card.compact
                        prop.children [
                            Daisy.cardBody [
                                Daisy.cardTitle [
                                    prop.className "flex flex-row justify-between"
                                    prop.children [
                                        Html.span "Advanced Search"
                                        Components.DeleteButton(props=[prop.onClick(fun _ -> setIsActive false)])
                                    ]
                                ]
                                Html.div [
                                    prop.className "prose text-sm"
                                    prop.text "Swate advanced search uses the Apache Lucene query parser syntax. Feel free to read the related Swate documentation [wip] for guidance on how to use it."
                                ]
                                match state.Subpage with
                                | AdvancedSearchSubpages.InputFormSubpage ->
                                    // we need to propagate the modal id here, so we can use meaningful and UNIQUE ids to the checkradio id's
                                    inputFormPage state setState dispatch
                                | AdvancedSearchSubpages.ResultsSubpage ->
                                    resultsPage resultHandler state setState
                                Daisy.cardActions [
                                    if state.Subpage <> AdvancedSearchSubpages.InputFormSubpage then
                                        Daisy.button.button [
                                            button.error
                                            prop.onClick (fun e -> e.stopPropagation(); e.preventDefault(); setState {state with Subpage = InputFormSubpage })
                                            prop.text "Back"
                                        ]
                                    // Show "Start advanced search" button ONLY on first subpage
                                    if state.Subpage = AdvancedSearchSubpages.InputFormSubpage then
                                        Daisy.button.button [
                                            let isValid = isValidAdancedSearchOptions state.AdvancedSearchOptions
                                            if isValid then
                                                button.success
                                            else
                                                button.error
                                                prop.disabled true
                                            prop.onClick (fun e ->
                                                e.preventDefault()
                                                e.stopPropagation();
                                                StartAdvancedSearch state setState dispatch
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