module BuildingBlock.SearchComponent

open Feliz
open Feliz.DaisyUI
open Fable.Core.JsInterop
open Model.BuildingBlock
open Model
open Messages
open ARCtrl
open Swate.Components.Shared
open Fable.Core

module SearchComponentHelper =

    let addBuildingBlock (selectedColumnIndex: int option) (model: Model) dispatch =

        let state = model.AddBuildingBlockState
        let header = Helper.createCompositeHeaderFromState state
        let body = Helper.tryCreateCompositeCellFromState state

        let bodyCells =
            if body.IsSome then // create as many body cells as there are rows in the active table
                let rowCount = System.Math.Max(1, model.SpreadsheetModel.ActiveTable.RowCount)
                Array.init rowCount (fun _ -> body.Value.Copy())
                |> ResizeArray
            else
                ResizeArray()

        let column = CompositeColumn.create (header, bodyCells)

        let index =
            Spreadsheet.Controller.BuildingBlocks.SidebarControllerAux.getNextColumnIndex
                selectedColumnIndex
                model.SpreadsheetModel

        SpreadsheetInterface.AddAnnotationBlock(Some index, column)
        |> InterfaceMsg
        |> dispatch

type SearchComponent =

    static member private termOrUnitizedSwitch(model: Model, dispatch) =
        let state = model.AddBuildingBlockState

        let mkClasses (isActive: bool) = [
            "swt:btn swt:join-item swt:border swt:!border-base-content"
            if isActive then "swt:btn-primary" else "swt:btn-neutral/50"
        ]

        React.fragment [
            Html.button [
                let isActive = state.BodyCellType = CompositeCellDiscriminate.Term

                prop.className (mkClasses isActive)
                prop.type'.button
                prop.text "Term"

                prop.onClick (fun _ ->
                    BuildingBlock.UpdateBodyCellType CompositeCellDiscriminate.Term
                    |> BuildingBlockMsg
                    |> dispatch
                )
            ]
            Html.button [
                let isActive = state.BodyCellType = CompositeCellDiscriminate.Unitized

                prop.className (mkClasses isActive)
                prop.type'.button
                prop.text "Unit"

                prop.onClick (fun _ ->
                    BuildingBlock.UpdateBodyCellType CompositeCellDiscriminate.Unitized
                    |> BuildingBlockMsg
                    |> dispatch
                )
            ]
        ]

    [<ReactComponent>]
    static member private SearchBuildingBlockBodyElement(model: Model, dispatch) =
        let element = React.useElementRef ()

        Html.div [
            prop.ref element
            prop.style [ style.position.relative ]
            prop.children [
                Html.div [
                    prop.className "swt:join swt:w-full"
                    prop.children [
                        SearchComponent.termOrUnitizedSwitch (model, dispatch)
                        // helper for setting the body cell type
                        let setter (termOpt: Swate.Components.Types.Term option) =
                            let oa = termOpt |> Option.map OntologyAnnotation.fromTerm
                            let case = oa |> Option.map (fun oa -> !^oa)
                            BuildingBlock.UpdateBodyArg case |> BuildingBlockMsg |> dispatch

                        let parent = model.AddBuildingBlockState.TryHeaderOA()
                        let input = model.AddBuildingBlockState.TryBodyOA()

                        Swate.Components.TermSearch.TermSearch(
                            (input |> Option.map _.ToTerm()),
                            setter,
                            classNames =
                                Swate.Components.Types.TermSearchStyle(!^"swt:border-current swt:join-item swt:w-full"),
                            ?parentId = (parent |> Option.map _.TermAccessionShort)
                        )
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private SearchBuildingBlockHeaderElement(ui: BuildingBlockUIState, setUi, model: Model, dispatch) =
        let state = model.AddBuildingBlockState
        let element = React.useElementRef ()

        Html.div [
            prop.ref element // The ref must be place here, otherwise the portalled term select area will trigger daisy join syntax
            prop.style [ style.position.relative ]
            prop.children [
                Html.div [
                    prop.className "swt:join swt:w-full"
                    prop.children [
                        // Choose building block type dropdown element
                        Dropdown.Main(ui, setUi, model, dispatch)
                        // Term search field
                        if state.HeaderCellType = CompositeHeaderDiscriminate.Comment then
                            Html.input [
                                prop.className "swt:input swt:join-item swt:flex-grow"
                                prop.readOnly false
                                prop.valueOrDefault (model.AddBuildingBlockState.CommentHeader)
                                prop.placeholder (CompositeHeaderDiscriminate.Comment.ToString())
                                prop.onChange (fun (ev: string) ->
                                    BuildingBlock.UpdateCommentHeader ev |> BuildingBlockMsg |> dispatch
                                )
                            ]
                        elif state.HeaderCellType.HasOA() then
                            let setter (oaOpt: Swate.Components.Types.Term option) =
                                let case =
                                    oaOpt |> Option.map (fun oa -> OntologyAnnotation.fromTerm >> U2.Case1 <| oa)

                                BuildingBlock.UpdateHeaderArg case |> BuildingBlockMsg |> dispatch
                            //selectHeader ui setUi h |> dispatch
                            let input = model.AddBuildingBlockState.TryHeaderOA()

                            Swate.Components.TermSearch.TermSearch(
                                (input |> Option.map _.ToTerm()),
                                setter,
                                classNames =
                                    Swate.Components.Types.TermSearchStyle(
                                        !^"swt:border-current swt:join-item swt:w-full"
                                    )
                            )

                        elif state.HeaderCellType.HasIOType() then
                            Html.input [
                                prop.className "swt:input"
                                prop.readOnly true
                                prop.valueOrDefault (state.TryHeaderIO() |> Option.get |> _.ToString())
                            ]
                    ]
                ]
            ]

        ]

    [<ReactComponent>]
    static member private AddBuildingBlockButton(model: Model, onClick: unit -> unit) =
        let state = model.AddBuildingBlockState

        Html.div [
            prop.className "swt:flex swt:justify-center"
            prop.children [
                Html.button [
                    let header = Helper.createCompositeHeaderFromState state
                    let body = Helper.tryCreateCompositeCellFromState state
                    let isValid = Helper.isValidColumn header
                    prop.type'.button

                    prop.className [
                        "swt:btn swt:btn-wide"
                        if isValid then "swt:btn-primary" else "swt:btn-error"
                    ]

                    if not isValid then
                        prop.disabled true

                    prop.onClick (fun _ -> onClick ())

                    prop.text "Add Column"
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch) : ReactElement =
        let state_bb, setState_bb = React.useState (BuildingBlockUIState.init)

        let ctx =
            React.useContext (Swate.Components.Contexts.AnnotationTable.AnnotationTableStateCtx)

        let xIndex =
            ctx.data
            |> Map.tryFind model.SpreadsheetModel.ActiveTable.Name
            |> Option.bind (fun x -> x.SelectedCells)
            |> Option.map (fun cells -> cells.xEnd)
            |> Option.map (fun x -> x - 2)

        let callback =
            fun () -> SearchComponentHelper.addBuildingBlock xIndex model dispatch

        Html.div [
            Html.form [
                prop.className "swt:flex swt:flex-col swt:gap-4 swt:p-2"
                prop.onSubmit (fun ev -> ev.preventDefault ()
                // callback ()
                )
                prop.children [
                    SearchComponent.SearchBuildingBlockHeaderElement(state_bb, setState_bb, model, dispatch)
                    if model.AddBuildingBlockState.HeaderCellType.IsTermColumn() then
                        SearchComponent.SearchBuildingBlockBodyElement(model, dispatch)
                    SearchComponent.AddBuildingBlockButton(model, callback)
                    Html.input [ prop.type'.submit; prop.style [ style.display.none ] ]
                ]
            ]
        ]