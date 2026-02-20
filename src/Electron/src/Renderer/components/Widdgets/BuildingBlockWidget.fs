namespace Renderer.components.Widgets

open Feliz
open Fable.Core
open ARCtrl
open ARCtrl.Json

open Swate.Components
open Swate.Components.Shared


[<RequireQualifiedAccess>]
type DropdownPage =
    | Main
    | More
    | IOTypes of CompositeHeaderDiscriminate

type BuildingBlockState = {
    HeaderCellType: CompositeHeaderDiscriminate
    HeaderArg: U2<OntologyAnnotation, IOType> option
    BodyCellType: CompositeCellDiscriminate
    BodyArg: U2<string, OntologyAnnotation> option
    CommentHeader: string
    DropdownPage: DropdownPage
}

type BuildingBlockStateHandler =

    static member init = {
        HeaderCellType = CompositeHeaderDiscriminate.Parameter
        HeaderArg = None
        BodyCellType = CompositeCellDiscriminate.Term
        BodyArg = None
        CommentHeader = ""
        DropdownPage = DropdownPage.Main
    }

    static member IsSameMajorCompositeHeaderDiscriminate
        (left: CompositeHeaderDiscriminate)
        (right: CompositeHeaderDiscriminate)
        =
        (left.IsTermColumn() = right.IsTermColumn())
        && (left.HasIOType() = right.HasIOType())

    static member TryHeaderOA (state: BuildingBlockState) =
        match state.HeaderCellType, state.HeaderArg with
        | CompositeHeaderDiscriminate.ProtocolType, _ -> CompositeHeader.ProtocolType.ToTerm() |> Some
        | _, Some(U2.Case1 oa) -> Some oa
        | _ -> None

    static member TryHeaderIO (state: BuildingBlockState) =
        match state.HeaderArg with
        | Some(U2.Case2 io) -> Some io
        | _ -> None

    static member TryBodyOA (state: BuildingBlockState) =
        match state.BodyArg with
        | Some(U2.Case2 oa) -> Some oa
        | _ -> None

    static member SetHeaderCellType (next: CompositeHeaderDiscriminate) (state: BuildingBlockState) =
        if BuildingBlockStateHandler.IsSameMajorCompositeHeaderDiscriminate state.HeaderCellType next then
            { state with HeaderCellType = next }
        else
            let nextBodyType =
                if next.IsTermColumn() then
                    CompositeCellDiscriminate.Term
                else
                    CompositeCellDiscriminate.Text

            {
                state with
                    HeaderCellType = next
                    BodyCellType = nextBodyType
                    HeaderArg = None
                    BodyArg = None
            }

    static member SetHeaderWithIO
        (headerType: CompositeHeaderDiscriminate)
        (ioType: IOType)
        (state: BuildingBlockState)
        =
        {
            state with
                HeaderCellType = headerType
                HeaderArg = Some(U2.Case2 ioType)
                BodyArg = None
                BodyCellType = CompositeCellDiscriminate.Text
        }

    static member CreateCompositeHeader (state: BuildingBlockState) =
        let getOA () =
            BuildingBlockStateHandler.TryHeaderOA state
            |> Option.defaultValue (OntologyAnnotation.empty ())

        let getIOType () =
            BuildingBlockStateHandler.TryHeaderIO state
            |> Option.defaultValue (IOType.FreeText "")

        match state.HeaderCellType with
        | CompositeHeaderDiscriminate.Date -> CompositeHeader.Date
        | CompositeHeaderDiscriminate.Input -> CompositeHeader.Input <| getIOType ()
        | CompositeHeaderDiscriminate.Factor -> CompositeHeader.Factor <| getOA ()
        | CompositeHeaderDiscriminate.Output -> CompositeHeader.Output <| getIOType ()
        | CompositeHeaderDiscriminate.Comment -> CompositeHeader.Comment state.CommentHeader
        | CompositeHeaderDiscriminate.Freetext -> CompositeHeader.FreeText ""
        | CompositeHeaderDiscriminate.Parameter -> CompositeHeader.Parameter <| getOA ()
        | CompositeHeaderDiscriminate.Component -> CompositeHeader.Component <| getOA ()
        | CompositeHeaderDiscriminate.Performer -> CompositeHeader.Performer
        | CompositeHeaderDiscriminate.ProtocolUri -> CompositeHeader.ProtocolUri
        | CompositeHeaderDiscriminate.ProtocolREF -> CompositeHeader.ProtocolREF
        | CompositeHeaderDiscriminate.ProtocolType -> CompositeHeader.ProtocolType
        | CompositeHeaderDiscriminate.Characteristic -> CompositeHeader.Characteristic <| getOA ()
        | CompositeHeaderDiscriminate.ProtocolVersion -> CompositeHeader.ProtocolVersion
        | CompositeHeaderDiscriminate.ProtocolDescription -> CompositeHeader.ProtocolDescription

    static member TryCreateCompositeCell (state: BuildingBlockState) =
        match state.HeaderArg, state.BodyCellType, state.BodyArg with
        | Some(U2.Case2 IOType.Data), _, _ -> CompositeCell.emptyData |> Some
        | _, CompositeCellDiscriminate.Term, Some(U2.Case2 oa) -> CompositeCell.createTerm oa |> Some
        | _, CompositeCellDiscriminate.Text, Some(U2.Case1 text) -> CompositeCell.createFreeText text |> Some
        | _, CompositeCellDiscriminate.Unitized, Some(U2.Case2 oa) -> CompositeCell.createUnitized ("", oa) |> Some
        | _ -> None

    static member IsValidColumn (header: CompositeHeader) =
        header.IsFeaturedColumn
        || (header.IsTermColumn && header.ToTerm().NameText.Length > 0)
        || header.IsSingleColumn

type InsertColumnResult = {
        InsertedIndex: int
        Warning: string option
    }

type BuildingBlockDataSource =

    static member NextInsertIndex (selectedColumnIndex: int option) (table: ArcTable) =
        if selectedColumnIndex.IsSome then
            System.Math.Min(selectedColumnIndex.Value + 1, table.ColumnCount)
        else
            table.ColumnCount

    static member TryGetSelectedColumnIndex
        (tableName: string)
        (ctx: Map<string, Contexts.AnnotationTable.AnnotationTableContext>)
        =
        ctx
        |> Map.tryFind tableName
        |> Option.bind (fun x -> x.SelectedCells)
        |> Option.map (fun cells -> cells.xEnd)
        |> Option.map (fun x -> x - 2)

    static member InsertColumn
        (selectedColumnIndex: int option)
        (column: CompositeColumn)
        (table: ArcTable)
        : InsertColumnResult
        =
        let mutable newColumn = column

        let warning =
            if column.Header.isOutput then
                match table.TryGetOutputColumn() with
                | Some existing ->
                    newColumn <- { newColumn with Cells = existing.Cells }
                    Some $"Found existing output column. Changed output column to \"{column.Header.ToString()}\"."
                | None -> None
            elif column.Header.isInput then
                match table.TryGetInputColumn() with
                | Some existing ->
                    newColumn <- { newColumn with Cells = existing.Cells }
                    Some $"Found existing input column. Changed input column to \"{column.Header.ToString()}\"."
                | None -> None
            else
                None

        let index = BuildingBlockDataSource.NextInsertIndex selectedColumnIndex table
        table.AddColumn(newColumn.Header, newColumn.Cells, index, true)

        {
            InsertedIndex = index
            Warning = warning
        }

    static member syncArcVault (arcFile: ArcFiles) : Result<unit, string> =
        try
            match arcFile with
            | ArcFiles.Assay assay ->
                Api.arcVaultApi.updateAssay(assay.ToJsonString())
                |> Result.mapError (fun err -> err.Message)
            | ArcFiles.Study(study, _) ->
                Api.arcVaultApi.updateStudy(study.ToJsonString())
                |> Result.mapError (fun err -> err.Message)
            | ArcFiles.Workflow workflow ->
                Api.arcVaultApi.updateWorkflows(workflow.ToJsonString())
                |> Result.mapError (fun err -> err.Message)
            | _ -> Ok ()
        with exn ->
            Error exn.Message

type BuildingBlockWidget =

    [<ReactComponent>]
    static member FreeTextInputElement (onSubmit: string -> unit) =
        let inputText, setInputText = React.useState ""

        Html.div [
            prop.className "swt:flex swt:flex-row swt:gap-0 swt:p-0 swt:join swt:w-full"
            prop.children [
                Html.input [
                    prop.placeholder "Custom type..."
                    prop.className "swt:input swt:input-sm swt:join-item swt:grow"
                    prop.onClick (fun e -> e.stopPropagation())
                    prop.onChange setInputText
                    prop.onKeyDown (
                        key.enter,
                        fun e ->
                            e.preventDefault ()
                            e.stopPropagation ()
                            onSubmit inputText
                    )
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-accent swt:btn-sm swt:join-item"
                    prop.type'.button
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        onSubmit inputText
                    )
                    prop.children [ Icons.Check() ]
                ]
            ]
        ]

type DropdownElement =

    static member Divider = Html.div [ prop.className "swt:divider swt:mx-2 swt:my-0" ]

    static member ClickableItem (label: string) (onClick: unit -> unit) =
        Html.li [
            Html.a [
                prop.onClick (fun e ->
                    e.preventDefault ()
                    e.stopPropagation ()
                    onClick ()
                )
                prop.onKeyDown (fun k ->
                    if k.code = kbdEventCode.enter then
                        onClick ()
                )
                prop.text label
            ]
        ]

    static member NavItem (label: string) (onClick: unit -> unit) =
        Html.li [
            Html.a [
                prop.className "swt:flex swt:flex-row swt:justify-between"
                prop.onClick (fun e ->
                    e.preventDefault ()
                    e.stopPropagation ()
                    onClick ()
                )
                prop.children [ Html.span label; Icons.ArrowRight() ]
            ]
        ]

    static member InfoFooter (onBack: (unit -> unit) option) =
        Html.li [
            prop.className "swt:flex swt:flex-row swt:justify-between swt:pt-1"
            prop.children [
                match onBack with
                | Some goBack ->
                    Html.a [
                        prop.className "swt:content-center"
                        prop.onClick (fun e ->
                            e.preventDefault ()
                            e.stopPropagation ()
                            goBack ()
                        )
                        prop.children [ Icons.ArrowLeft() ]
                    ]
                | None -> Html.span [ prop.text "" ]
                Html.a [
                    prop.href "#"
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        Browser.Dom.window.``open`` (URLs.AnnotationPrinciplesUrl, "_blank")
                        |> ignore
                    )
                    prop.className "swt:ml-auto swt:link-info"
                    prop.text "info"
                ]
            ]
        ]

type BasicComponent =

    [<ReactComponent>]
    static member HeaderTypeDropdown (state: BuildingBlockState, setState: BuildingBlockState -> unit) =
        let isOpen, setOpen = React.useState false

        let close () = setOpen false

        let setMainAndClose nextState =
            setState { nextState with DropdownPage = DropdownPage.Main }
            close ()

        let openSubPage subPage =
            setState { state with DropdownPage = subPage }

        let selectHeaderType (headerType: CompositeHeaderDiscriminate) =
            state
            |> BuildingBlockStateHandler.SetHeaderCellType headerType
            |> setMainAndClose

        let selectIOType (headerType: CompositeHeaderDiscriminate) (ioType: IOType) =
            state
            |> BuildingBlockStateHandler.SetHeaderWithIO headerType ioType
            |> setMainAndClose

        let dropdownContent =
            match state.DropdownPage with
            | DropdownPage.Main ->
                [
                    DropdownElement.NavItem "Input" (fun () ->
                        openSubPage (DropdownPage.IOTypes CompositeHeaderDiscriminate.Input)
                    )
                    DropdownElement.Divider
                    DropdownElement.ClickableItem "Parameter" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.Parameter
                    )
                    DropdownElement.ClickableItem "Factor" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.Factor
                    )
                    DropdownElement.ClickableItem "Characteristic" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.Characteristic
                    )
                    DropdownElement.ClickableItem "Component" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.Component
                    )
                    DropdownElement.NavItem "More" (fun () -> openSubPage DropdownPage.More)
                    DropdownElement.Divider
                    DropdownElement.NavItem "Output" (fun () ->
                        openSubPage (DropdownPage.IOTypes CompositeHeaderDiscriminate.Output)
                    )
                    DropdownElement.InfoFooter None
                ]
            | DropdownPage.More ->
                [
                    DropdownElement.ClickableItem "Comment" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.Comment
                    )
                    DropdownElement.ClickableItem "Date" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.Date
                    )
                    DropdownElement.ClickableItem "Performer" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.Performer
                    )
                    DropdownElement.ClickableItem "ProtocolDescription" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.ProtocolDescription
                    )
                    DropdownElement.ClickableItem "ProtocolREF" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.ProtocolREF
                    )
                    DropdownElement.ClickableItem "ProtocolType" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.ProtocolType
                    )
                    DropdownElement.ClickableItem "ProtocolUri" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.ProtocolUri
                    )
                    DropdownElement.ClickableItem "ProtocolVersion" (fun () ->
                        selectHeaderType CompositeHeaderDiscriminate.ProtocolVersion
                    )
                    DropdownElement.InfoFooter (Some(fun () -> openSubPage DropdownPage.Main))
                ]
            | DropdownPage.IOTypes headerType ->
                [
                    DropdownElement.ClickableItem "Data" (fun () -> selectIOType headerType IOType.Data)
                    DropdownElement.ClickableItem "Source" (fun () -> selectIOType headerType IOType.Source)
                    DropdownElement.ClickableItem "Sample" (fun () -> selectIOType headerType IOType.Sample)
                    DropdownElement.ClickableItem "Material" (fun () -> selectIOType headerType IOType.Material)
                    Html.li [
                        prop.onClick (fun e ->
                            e.preventDefault ()
                            e.stopPropagation ()
                        )
                        prop.children [
                            BuildingBlockWidget.FreeTextInputElement(fun text -> selectIOType headerType (IOType.FreeText text))
                        ]
                    ]
                    DropdownElement.InfoFooter (Some(fun () -> openSubPage DropdownPage.Main))
                ]

        Components.BaseDropdown.Main(
            isOpen,
            setOpen,
            Html.button [
                prop.onClick (fun _ -> setOpen (not isOpen))
                prop.role "button"
                prop.type'.button
                prop.className "swt:btn swt:btn-primary swt:border swt:!border-base-content swt:join-item swt:flex-nowrap"
                prop.children [
                    Html.span (state.HeaderCellType.ToString())
                    Icons.AngleDown()
                ]
            ],
            dropdownContent
        )

    [<ReactComponent>]
    static member HeaderInputElement (buildingBlockState: BuildingBlockState, setBuildingBlockState: BuildingBlockState -> unit) =
        Html.div [
            prop.style [ style.position.relative ]
            prop.children [
                Html.div [
                    prop.className "swt:join swt:w-full"
                    prop.children [
                        BasicComponent.HeaderTypeDropdown(buildingBlockState, setBuildingBlockState)
                        if buildingBlockState.HeaderCellType = CompositeHeaderDiscriminate.Comment then
                            Html.input [
                                prop.className "swt:input swt:join-item swt:flex-grow"
                                prop.valueOrDefault buildingBlockState.CommentHeader
                                prop.placeholder (CompositeHeaderDiscriminate.Comment.ToString())
                                prop.onChange (fun text ->
                                    setBuildingBlockState {
                                        buildingBlockState with
                                            CommentHeader = text
                                    }
                                )
                            ]
                        elif buildingBlockState.HeaderCellType.HasOA() then
                            let setter (termOpt: Swate.Components.Types.Term option) =
                                let nextHeaderArg =
                                    termOpt
                                    |> Option.map (fun term -> term |> (OntologyAnnotation.from >> U2.Case1))

                                setBuildingBlockState {
                                    buildingBlockState with
                                        HeaderArg = nextHeaderArg
                                }

                            let input = BuildingBlockStateHandler.TryHeaderOA buildingBlockState

                            Swate.Components.TermSearch.TermSearch(
                                (input |> Option.map _.ToTerm()),
                                setter,
                                classNames = Swate.Components.Types.TermSearchStyle(U2.Case1 "swt:border-current swt:join-item swt:w-full")
                            )
                        elif buildingBlockState.HeaderCellType.HasIOType() then
                            Html.input [
                                prop.className "swt:input swt:join-item swt:flex-grow"
                                prop.readOnly true
                                prop.valueOrDefault (
                                    BuildingBlockStateHandler.TryHeaderIO buildingBlockState
                                    |> Option.map _.ToString()
                                    |> Option.defaultValue ""
                                )
                            ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member BodyInputElement (buildingBlockState: BuildingBlockState, setBuildingBlockState: BuildingBlockState -> unit) =
        let setBodyCellType (next: CompositeCellDiscriminate) =
            setBuildingBlockState {
                buildingBlockState with
                    BodyCellType = next
            }

        let toggleClass (isActive: bool) = [
            "swt:btn swt:join-item swt:border swt:!border-base-content"
            if isActive then "swt:btn-primary" else "swt:btn-neutral/50"
        ]

        Html.div [
            prop.style [ style.position.relative ]
            prop.children [
                Html.div [
                    prop.className "swt:join swt:w-full"
                    prop.children [
                        Html.button [
                            let isActive = buildingBlockState.BodyCellType = CompositeCellDiscriminate.Term
                            prop.className (toggleClass isActive)
                            prop.type'.button
                            prop.text "Term"
                            prop.onClick (fun _ -> setBodyCellType CompositeCellDiscriminate.Term)
                        ]
                        Html.button [
                            let isActive = buildingBlockState.BodyCellType = CompositeCellDiscriminate.Unitized
                            prop.className (toggleClass isActive)
                            prop.type'.button
                            prop.text "Unit"
                            prop.onClick (fun _ -> setBodyCellType CompositeCellDiscriminate.Unitized)
                        ]
                        let setter (termOpt: Swate.Components.Types.Term option) =
                            let nextBodyArg =
                                termOpt
                                |> Option.map OntologyAnnotation.from
                                |> Option.map U2.Case2

                            setBuildingBlockState {
                                buildingBlockState with
                                    BodyArg = nextBodyArg
                            }

                        let input = BuildingBlockStateHandler.TryBodyOA buildingBlockState
                        let parent = BuildingBlockStateHandler.TryHeaderOA buildingBlockState

                        Swate.Components.TermSearch.TermSearch(
                            (input |> Option.map _.ToTerm()),
                            setter,
                            classNames = Swate.Components.Types.TermSearchStyle(U2.Case1 "swt:border-current swt:join-item swt:w-full"),
                            ?parentId = (parent |> Option.map _.TermAccessionShort)
                        )
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member StatusElement (status: StatusMessage) =
        let classNames =
            match status.Kind with
            | StatusKind.Info -> [ "swt:alert-info"; "swt:text-info-content" ]
            | StatusKind.Warning -> [ "swt:alert-warning"; "swt:text-warning-content" ]
            | StatusKind.Error -> [ "swt:alert-error"; "swt:text-error-content" ]

        Html.div [
            prop.className ([ "swt:alert swt:py-2 swt:text-sm" ] @ classNames)
            prop.children [ Html.span status.Text ]
        ]

    [<ReactComponent>]
    static member Main (activeTableData: ActiveTableData option, onTableMutated: unit -> unit) =
        let state, setState = React.useState BuildingBlockStateHandler.init
        let status, setStatus = React.useState (None: StatusMessage option)

        let annotationTableCtx =
            React.useContext Contexts.AnnotationTable.AnnotationTableStateCtx

        let selectedColumnIndex =
            activeTableData
            |> Option.bind (fun table ->
                BuildingBlockDataSource.TryGetSelectedColumnIndex table.TableName annotationTableCtx.state
            )

        let createColumn (table: ArcTable) =
            let header = BuildingBlockStateHandler.CreateCompositeHeader state
            let body = BuildingBlockStateHandler.TryCreateCompositeCell state

            let cells =
                if body.IsSome then
                    let rowCount = System.Math.Max(1, table.RowCount)
                    Array.init rowCount (fun _ -> body.Value.Copy()) |> ResizeArray
                else
                    state.HeaderCellType.CreateEmptyDefaultCells table.RowCount

            CompositeColumn.create (header, cells)

        let tryAddColumn () =
            match activeTableData with
            | None ->
                setStatus (
                    Some {
                        Kind = StatusKind.Error
                        Text = "Open a table to add a building block."
                    }
                )
            | Some activeTable ->
                let header = BuildingBlockStateHandler.CreateCompositeHeader state
                let isValid = BuildingBlockStateHandler.IsValidColumn header

                if not isValid then
                    setStatus (
                        Some {
                            Kind = StatusKind.Error
                            Text = "Header is incomplete. Please select a valid building block."
                        }
                    )
                else
                    let column = createColumn activeTable.Table

                    let insertResult =
                        BuildingBlockDataSource.InsertColumn selectedColumnIndex column activeTable.Table

                    let syncResult =
                        BuildingBlockDataSource.syncArcVault activeTable.ArcFile

                    onTableMutated ()

                    let statusMessage =
                        match insertResult.Warning, syncResult with
                        | Some warning, Ok() ->
                            {
                                Kind = StatusKind.Warning
                                Text = warning
                            }
                        | None, Ok() ->
                            {
                                Kind = StatusKind.Info
                                Text = $"Added column \"{header.ToString()}\"."
                            }
                        | Some warning, Error syncError ->
                            {
                                Kind = StatusKind.Warning
                                Text = $"{warning} Vault sync failed: {syncError}"
                            }
                        | None, Error syncError ->
                            {
                                Kind = StatusKind.Error
                                Text = $"Column added locally, but vault sync failed: {syncError}"
                            }

                    setStatus (Some statusMessage)

        let currentHeader = BuildingBlockStateHandler.CreateCompositeHeader state
        let isValid = BuildingBlockStateHandler.IsValidColumn currentHeader
        let isEnabled = activeTableData.IsSome && isValid

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3 swt:p-2"
            prop.children [
                if activeTableData.IsNone then
                    Html.div [
                        prop.className "swt:text-sm swt:opacity-70"
                        prop.text "No active table. Open a table and select a target column."
                    ]
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-3"
                    prop.onSubmit (fun ev ->
                        ev.preventDefault ()
                        if isEnabled then
                            tryAddColumn ()
                    )
                    prop.children [
                        BasicComponent.HeaderInputElement(state, setState)
                        if state.HeaderCellType.IsTermColumn() then
                            BasicComponent.BodyInputElement(state, setState)
                        Html.div [
                            prop.className "swt:flex swt:justify-center"
                            prop.children [
                                Html.button [
                                    prop.type'.button
                                    prop.className [
                                        "swt:btn swt:btn-wide"
                                        if isEnabled then "swt:btn-primary" else "swt:btn-error"
                                    ]
                                    if not isEnabled then
                                        prop.disabled true
                                    prop.onClick (fun _ -> tryAddColumn ())
                                    prop.text "Add Column"
                                ]
                            ]
                        ]
                        Html.input [ prop.type'.submit; prop.style [ style.display.none ] ]
                    ]
                ]
                match status with
                | Some message -> BasicComponent.StatusElement message
                | None -> Html.none
            ]
        ]
