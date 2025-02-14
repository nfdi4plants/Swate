module BuildingBlock.SearchComponent

open Feliz
open Feliz.DaisyUI
open Fable.Core.JsInterop
open Model.BuildingBlock
open Model
open Messages
open ARCtrl
open Swate.Components.Shared

let private termOrUnitizedSwitch (model:Model) dispatch =
    let state = model.AddBuildingBlockState
    React.fragment [
        Daisy.button.a [
            join.item
            button.outline
            let isActive = state.BodyCellType = CompositeCellDiscriminate.Term
            if isActive then button.primary
            prop.onClick (fun _ -> BuildingBlock.UpdateBodyCellType CompositeCellDiscriminate.Term |> BuildingBlockMsg |> dispatch)
            prop.text "Term"
        ]
        Daisy.button.a [
            join.item
            button.outline
            let isActive = state.BodyCellType = CompositeCellDiscriminate.Unitized
            if isActive then button.primary
            prop.onClick (fun _ -> BuildingBlock.UpdateBodyCellType CompositeCellDiscriminate.Unitized |> BuildingBlockMsg |> dispatch)
            prop.text "Unit"
        ]
    ]

open Fable.Core

[<ReactComponent>]
let private SearchBuildingBlockBodyElement (model: Model, dispatch) =
    let element = React.useElementRef()
    Html.div [
        prop.ref element
        prop.style [ style.position.relative ]
        prop.children [
            Daisy.join [
                prop.className "w-full"
                prop.children [
                    termOrUnitizedSwitch model dispatch
                    // helper for setting the body cell type
                    let setter (termOpt: Swate.Components.Term option) =
                        let oa = termOpt |> Option.map OntologyAnnotation.fromTerm
                        let case = oa |> Option.map (fun oa -> !^oa)
                        BuildingBlock.UpdateBodyArg case |> BuildingBlockMsg |> dispatch
                    let parent = model.AddBuildingBlockState.TryHeaderOA()
                    let input = model.AddBuildingBlockState.TryBodyOA()
                    Swate.Components.TermSearch.TermSearch(
                            setter,
                            (input |> Option.map _.ToTerm()),
                            ?parentId=(parent |> Option.map _.TermAccessionShort),
                            portalTermSelectArea=element,
                            fullwidth=true,
                            classNames = Swate.Components.TermSearchStyle(!^"border-current join-item"),
                            advancedSearch = !^true,
                            showDetails = true,
                            disableDefaultSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            disableDefaultAllChildrenSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            disableDefaultParentSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            termSearchQueries = model.PersistentStorageState.TIBQueries.TermSearch,
                            parentSearchQueries = model.PersistentStorageState.TIBQueries.ParentSearch,
                            allChildrenSearchQueries = model.PersistentStorageState.TIBQueries.AllChildrenSearch
                        )
                ]
            ]
        ]
    ]

[<ReactComponent>]
let private SearchBuildingBlockHeaderElement (ui: BuildingBlockUIState, setUi, model: Model, dispatch) =
    let state = model.AddBuildingBlockState
    let element = React.useElementRef()
    Html.div [
        prop.ref element // The ref must be place here, otherwise the portalled term select area will trigger daisy join syntax
        prop.style [style.position.relative]
        prop.children [
            Daisy.join [
                prop.className "w-full"
                prop.children [
                    // Choose building block type dropdown element
                    // Dropdown building block type choice
                    Dropdown.Main(ui, setUi, model, dispatch)
                    // Term search field
                    if state.HeaderCellType = CompositeHeaderDiscriminate.Comment then
                        Daisy.input [
                            prop.className "join-item flex-grow"
                            prop.readOnly false
                            prop.valueOrDefault (model.AddBuildingBlockState.CommentHeader)
                            prop.placeholder (CompositeHeaderDiscriminate.Comment.ToString())
                            prop.onChange (fun (ev:string) ->
                                BuildingBlock.UpdateCommentHeader ev |> BuildingBlockMsg |> dispatch)
                        ]
                    elif state.HeaderCellType.HasOA() then
                        let setter (oaOpt: Swate.Components.Term option) =
                            let case = oaOpt |> Option.map (fun oa -> OntologyAnnotation.fromTerm >> U2.Case1 <| oa)
                            BuildingBlock.UpdateHeaderArg case |> BuildingBlockMsg |> dispatch
                            //selectHeader ui setUi h |> dispatch
                        let input = model.AddBuildingBlockState.TryHeaderOA()
                        Swate.Components.TermSearch.TermSearch(
                            setter,
                            (input |> Option.map _.ToTerm() ),
                            portalTermSelectArea=element,
                            fullwidth=true,
                            classNames = Swate.Components.TermSearchStyle(!^"border-current join-item"),
                            advancedSearch = !^true,
                            showDetails = true,
                            disableDefaultSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            disableDefaultAllChildrenSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            disableDefaultParentSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            termSearchQueries = model.PersistentStorageState.TIBQueries.TermSearch,
                            parentSearchQueries = model.PersistentStorageState.TIBQueries.ParentSearch,
                            allChildrenSearchQueries = model.PersistentStorageState.TIBQueries.AllChildrenSearch
                        )
                    elif state.HeaderCellType.HasIOType() then
                        Daisy.input [
                            prop.readOnly true
                            prop.valueOrDefault (
                                state.TryHeaderIO()
                                |> Option.get
                                |> _.ToString()
                            )
                        ]
                ]
            ]
        ]

    ]

let private scrollIntoViewRetry (id: string) =
    let rec loop (iteration: int) =
        let headerelement = Browser.Dom.document.getElementById(id)
        if isNull headerelement then
            if iteration < 5 then
                Fable.Core.JS.setTimeout (fun _ -> loop (iteration+1)) 100 |> ignore
            else
                ()
        else
            let rect = headerelement.getBoundingClientRect()
            if rect.left >= 0 && ((rect.right <= Browser.Dom.window.innerWidth) || (rect.right <= Browser.Dom.document.documentElement.clientWidth)) then
                ()
            else
                let config = createEmpty<Browser.Types.ScrollIntoViewOptions>
                config.behavior <- Browser.Types.ScrollBehavior.Smooth
                config.block <- Browser.Types.ScrollAlignment.End
                config.``inline`` <- Browser.Types.ScrollAlignment.End
                //log headerelement
                headerelement.scrollIntoView(config)
    loop 0

let private AddBuildingBlockButton (model: Model) dispatch =
    let state = model.AddBuildingBlockState
    Html.div [
        prop.className "flex justify-center"
        prop.children [
            Daisy.button.button  [
                let header = Helper.createCompositeHeaderFromState state
                let body = Helper.tryCreateCompositeCellFromState state
                let isValid = Helper.isValidColumn header
                button.wide
                if isValid then
                    button.success
                else
                    button.error
                    prop.disabled true
                prop.onClick (fun _ ->
                    let bodyCells =
                        if body.IsSome then // create as many body cells as there are rows in the active table
                            let rowCount = System.Math.Max(1, model.SpreadsheetModel.ActiveTable.RowCount)
                            Array.init rowCount (fun _ -> body.Value.Copy())
                        else
                            Array.empty
                    let column = CompositeColumn.create(header, bodyCells)
                    let index = Spreadsheet.Controller.BuildingBlocks.SidebarControllerAux.getNextColumnIndex model.SpreadsheetModel
                    SpreadsheetInterface.AddAnnotationBlock column |> InterfaceMsg |> dispatch
                    let id = $"Header_{index}_Main"
                    scrollIntoViewRetry id
                )
                prop.text "Add Column"
            ]
        ]
    ]

[<ReactComponent>]
let Main (model: Model) dispatch =
    let state_bb, setState_bb = React.useState(BuildingBlockUIState.init)
    //let state_searchHeader, setState_searchHeader = React.useState(TermSearchUIState.init)
    //let state_searchBody, setState_searchBody = React.useState(TermSearchUIState.init)
    Html.div [
        prop.className "flex flex-col gap-4"
        prop.children [
            SearchBuildingBlockHeaderElement (state_bb, setState_bb, model, dispatch)
            if model.AddBuildingBlockState.HeaderCellType.IsTermColumn() then
                SearchBuildingBlockBodyElement (model, dispatch)
            AddBuildingBlockButton model dispatch
        ]
    ]
