module Renderer.components.Widgets.AddBuildingBlockWidget

open Feliz
open ARCtrl
open Fable.Core
open Model.BuildingBlock
open BuildingBlock.Helper
open Swate.Components

[<RequireQualifiedAccess>]
module private ArcFile =

    let refreshRef (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Investigation investigation -> ArcFiles.Investigation investigation
        | ArcFiles.Study(study, assays) -> ArcFiles.Study(study, assays)
        | ArcFiles.Assay assay -> ArcFiles.Assay assay
        | ArcFiles.Run run -> ArcFiles.Run run
        | ArcFiles.Workflow workflow -> ArcFiles.Workflow workflow
        | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap)
        | ArcFiles.Template template -> ArcFiles.Template template

let private headerOptions: (string * CompositeHeaderDiscriminate)[] = [|
    "Input", CompositeHeaderDiscriminate.Input
    "Parameter", CompositeHeaderDiscriminate.Parameter
    "Factor", CompositeHeaderDiscriminate.Factor
    "Characteristic", CompositeHeaderDiscriminate.Characteristic
    "Component", CompositeHeaderDiscriminate.Component
    "Output", CompositeHeaderDiscriminate.Output
    "Comment", CompositeHeaderDiscriminate.Comment
    "Date", CompositeHeaderDiscriminate.Date
    "Performer", CompositeHeaderDiscriminate.Performer
    "ProtocolDescription", CompositeHeaderDiscriminate.ProtocolDescription
    "ProtocolREF", CompositeHeaderDiscriminate.ProtocolREF
    "ProtocolType", CompositeHeaderDiscriminate.ProtocolType
    "ProtocolUri", CompositeHeaderDiscriminate.ProtocolUri
    "ProtocolVersion", CompositeHeaderDiscriminate.ProtocolVersion
|]

let private ioTypeOptions: (string * IOType)[] = [|
    "Source", IOType.Source
    "Sample", IOType.Sample
    "Material", IOType.Material
    "Data", IOType.Data
    "Free Text", IOType.FreeText ""
|]

let private disabledState (message: string) =
    Html.div [
        prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-80 swt:p-2"
        prop.children [
            Html.h3 [
                prop.className "swt:text-sm swt:font-semibold"
                prop.text "Add Building Block"
            ]
            Html.p [
                prop.className "swt:text-xs swt:opacity-70"
                prop.text message
            ]
        ]
    ]

[<ReactComponent>]
let TermSearchComponent state setState setBodyTerm =
    if state.HeaderCellType.IsTermColumn() then
        Html.div [
            prop.children [
                Html.div [
                    prop.className "swt:join swt:w-full"
                    prop.style [ style.position.relative ]
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:join-item swt:border swt:!border-base-content"
                                if state.BodyCellType = CompositeCellDiscriminate.Term then
                                    "swt:btn-primary"
                                else
                                    "swt:btn-neutral/50"
                            ]
                            prop.text "Term"
                            prop.onClick (fun _ ->
                                setState {
                                    state with
                                        BodyCellType = CompositeCellDiscriminate.Term
                                }
                            )
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:join-item swt:border swt:!border-base-content"
                                if state.BodyCellType = CompositeCellDiscriminate.Unitized then
                                    "swt:btn-primary"
                                else
                                    "swt:btn-neutral/50"
                            ]
                            prop.text "Unit"
                            prop.onClick (fun _ ->
                                setState {
                                    state with
                                        BodyCellType = CompositeCellDiscriminate.Unitized
                                }
                            )
                        ]
                        TermSearch.TermSearch(
                            (state.TryBodyOA() |> Option.map (fun oa -> oa.ToTerm())),
                            setBodyTerm,
                            classNames =
                                Types.TermSearchStyle(U2.Case1 "swt:border-current swt:w-full"),
                            ?parentId =
                                (state.TryHeaderOA() |> Option.map (fun oa -> oa.TermAccessionShort))
                        )
                    ]
                ]
            ]
        ]
    else
        Html.div []

[<ReactComponent>]
let Main
    (
        arcFileState: ArcFiles option,
        activeTableIndex: int option,
        setArcFileState: ArcFiles option -> unit
    ) =

    let state, setState = React.useState (Model.BuildingBlock.Model.init ())

    match arcFileState, activeTableIndex with
    | None, _ -> disabledState "Open an ARC first."
    | Some _, None -> disabledState "Switch to a table tab to add a building block."
    | Some arcFile, Some tableIndex ->
        let tables = arcFile.Tables()

        if tableIndex < 0 || tableIndex >= tables.Count then
            disabledState "The active table is not available."
        else
            let spreadsheetModel =
                Spreadsheet.Model.init (arcFile, Spreadsheet.ActiveView.Table tableIndex)

            let ctx =
                React.useContext (Contexts.AnnotationTable.AnnotationTableStateCtx)

            let selectedColumnIndex =
                ctx.state
                |> Map.tryFind spreadsheetModel.ActiveTable.Name
                |> Option.bind (fun tableCtx -> tableCtx.SelectedCells)
                |> Option.map (fun cells -> cells.xEnd)
                |> Option.map (fun x -> x - 2)

            let setHeaderCellType (next: CompositeHeaderDiscriminate) =
                let nextState =
                    if isSameMajorCompositeHeaderDiscriminate state.HeaderCellType next then
                        { state with HeaderCellType = next }
                    else
                        let nextBodyCellType =
                            if next.IsTermColumn() then
                                CompositeCellDiscriminate.Term
                            else
                                CompositeCellDiscriminate.Text

                        {
                            state with
                                HeaderCellType = next
                                BodyCellType = nextBodyCellType
                                HeaderArg = None
                                BodyArg = None
                        }

                setState nextState

            let setHeaderIOType (headerType: CompositeHeaderDiscriminate) (ioType: IOType) =
                setState {
                    state with
                        HeaderCellType = headerType
                        HeaderArg = Some(U2.Case2 ioType)
                        BodyArg = None
                        BodyCellType = CompositeCellDiscriminate.Text
                }

            let setHeaderTerm (termOpt: Types.Term option) =
                let nextHeaderArg =
                    termOpt
                    |> Option.map (fun term -> term |> (OntologyAnnotation.from >> U2.Case1))

                setState { state with HeaderArg = nextHeaderArg }

            let setBodyTerm (termOpt: Types.Term option) =
                let nextBodyArg =
                    termOpt
                    |> Option.map (fun term -> term |> (OntologyAnnotation.from >> U2.Case2))

                setState { state with BodyArg = nextBodyArg }

            let addBuildingBlock () =
                let header = createCompositeHeaderFromState state
                let body = tryCreateCompositeCellFromState state

                let bodyCells =
                    if body.IsSome then
                        let rowCount = System.Math.Max(1, spreadsheetModel.ActiveTable.RowCount)
                        Array.init rowCount (fun _ -> body.Value.Copy()) |> ResizeArray
                    else
                        state.HeaderCellType.CreateEmptyDefaultCells spreadsheetModel.ActiveTable.RowCount

                let column = CompositeColumn.create (header, bodyCells)

                // Keep insertion behaviour aligned with current Client flow.
                let insertionReference =
                    Spreadsheet.Controller.BuildingBlocks.SidebarControllerAux.getNextColumnIndex
                        selectedColumnIndex
                        spreadsheetModel

                let _, nextSpreadsheetModel =
                    Spreadsheet.Controller.BuildingBlocks.addBuildingBlock
                        (Some insertionReference)
                        column
                        spreadsheetModel

                match nextSpreadsheetModel.ArcFile with
                | Some nextArcFile ->
                    nextArcFile
                    |> ArcFile.refreshRef
                    |> Some
                    |> setArcFileState
                | None -> ()

            let header = createCompositeHeaderFromState state
            let isValid = isValidColumn header

            Html.div [
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-4 swt:p-2 swt:min-w-80"
                    prop.onSubmit (fun ev ->
                        ev.preventDefault ()

                        if isValid then
                            addBuildingBlock ()
                    )
                    prop.children [
                        Renderer.components.Widgets.SearchComponents.SearchBuildingBockHeaderElement
                            state
                            setState
                            headerOptions
                            ioTypeOptions
                            setHeaderIOType
                            setHeaderCellType
                            setHeaderTerm

                        TermSearchComponent state setState setBodyTerm

                        Html.div [
                            prop.className "swt:flex swt:justify-center"
                            prop.children [
                                Html.button [
                                    prop.type'.submit
                                    prop.className [
                                        "swt:btn swt:btn-wide"
                                        if isValid then
                                            "swt:btn-primary"
                                        else
                                            "swt:btn-error"
                                    ]
                                    prop.disabled (not isValid)
                                    prop.text "Add Column"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
