module Renderer.components.BuildingBlockWidget

open Feliz
open Fable.Core
open ARCtrl
open ARCtrl.Json

open Swate.Components
open Swate.Components.Shared

type ActiveTableData = {
    ArcFile: ArcFiles
    Table: ArcTable
    TableName: string
    TableIndex: int
}

type ActiveDataMapData = {
    ArcFile: ArcFiles
    DataMap: DataMap
}

[<RequireQualifiedAccess>]
type private DropdownPage =
    | Main
    | More
    | IOTypes of CompositeHeaderDiscriminate

[<RequireQualifiedAccess>]
type private StatusKind =
    | Info
    | Warning
    | Error

type private StatusMessage = {
    Kind: StatusKind
    Text: string
}

type private BuildingBlockState = {
    HeaderCellType: CompositeHeaderDiscriminate
    HeaderArg: U2<OntologyAnnotation, IOType> option
    BodyCellType: CompositeCellDiscriminate
    BodyArg: U2<string, OntologyAnnotation> option
    CommentHeader: string
    DropdownPage: DropdownPage
}

module private BuildingBlockState =

    let init = {
        HeaderCellType = CompositeHeaderDiscriminate.Parameter
        HeaderArg = None
        BodyCellType = CompositeCellDiscriminate.Term
        BodyArg = None
        CommentHeader = ""
        DropdownPage = DropdownPage.Main
    }

    let private isSameMajorCompositeHeaderDiscriminate
        (left: CompositeHeaderDiscriminate)
        (right: CompositeHeaderDiscriminate)
        =
        (left.IsTermColumn() = right.IsTermColumn())
        && (left.HasIOType() = right.HasIOType())

    let tryHeaderOA (state: BuildingBlockState) =
        match state.HeaderCellType, state.HeaderArg with
        | CompositeHeaderDiscriminate.ProtocolType, _ -> CompositeHeader.ProtocolType.ToTerm() |> Some
        | _, Some(U2.Case1 oa) -> Some oa
        | _ -> None

    let tryHeaderIO (state: BuildingBlockState) =
        match state.HeaderArg with
        | Some(U2.Case2 io) -> Some io
        | _ -> None

    let tryBodyOA (state: BuildingBlockState) =
        match state.BodyArg with
        | Some(U2.Case2 oa) -> Some oa
        | _ -> None

    let setHeaderCellType (next: CompositeHeaderDiscriminate) (state: BuildingBlockState) =
        if isSameMajorCompositeHeaderDiscriminate state.HeaderCellType next then
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

    let setHeaderWithIO
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

    let createCompositeHeader (state: BuildingBlockState) =
        let getOA () =
            tryHeaderOA state
            |> Option.defaultValue (OntologyAnnotation.empty ())

        let getIOType () =
            tryHeaderIO state
            |> Option.defaultValue (IOType.FreeText "")

        match state.HeaderCellType with
        | CompositeHeaderDiscriminate.Component -> CompositeHeader.Component <| getOA ()
        | CompositeHeaderDiscriminate.Characteristic -> CompositeHeader.Characteristic <| getOA ()
        | CompositeHeaderDiscriminate.Factor -> CompositeHeader.Factor <| getOA ()
        | CompositeHeaderDiscriminate.Parameter -> CompositeHeader.Parameter <| getOA ()
        | CompositeHeaderDiscriminate.ProtocolType -> CompositeHeader.ProtocolType
        | CompositeHeaderDiscriminate.ProtocolDescription -> CompositeHeader.ProtocolDescription
        | CompositeHeaderDiscriminate.ProtocolUri -> CompositeHeader.ProtocolUri
        | CompositeHeaderDiscriminate.ProtocolVersion -> CompositeHeader.ProtocolVersion
        | CompositeHeaderDiscriminate.ProtocolREF -> CompositeHeader.ProtocolREF
        | CompositeHeaderDiscriminate.Performer -> CompositeHeader.Performer
        | CompositeHeaderDiscriminate.Date -> CompositeHeader.Date
        | CompositeHeaderDiscriminate.Input -> CompositeHeader.Input <| getIOType ()
        | CompositeHeaderDiscriminate.Output -> CompositeHeader.Output <| getIOType ()
        | CompositeHeaderDiscriminate.Comment -> CompositeHeader.Comment state.CommentHeader
        | CompositeHeaderDiscriminate.Freetext -> CompositeHeader.FreeText ""

    let tryCreateCompositeCell (state: BuildingBlockState) =
        match state.HeaderArg, state.BodyCellType, state.BodyArg with
        | Some(U2.Case2 IOType.Data), _, _ -> CompositeCell.emptyData |> Some
        | _, CompositeCellDiscriminate.Term, Some(U2.Case2 oa) -> CompositeCell.createTerm oa |> Some
        | _, CompositeCellDiscriminate.Unitized, Some(U2.Case2 oa) -> CompositeCell.createUnitized ("", oa) |> Some
        | _, CompositeCellDiscriminate.Text, Some(U2.Case1 text) -> CompositeCell.createFreeText text |> Some
        | _ -> None

    let isValidColumn (header: CompositeHeader) =
        header.IsFeaturedColumn
        || (header.IsTermColumn && header.ToTerm().NameText.Length > 0)
        || header.IsSingleColumn

module BuildingBlockDataSource =

    type InsertColumnResult = {
        InsertedIndex: int
        Warning: string option
    }

    let private nextInsertIndex (selectedColumnIndex: int option) (table: ArcTable) =
        if selectedColumnIndex.IsSome then
            System.Math.Min(selectedColumnIndex.Value + 1, table.ColumnCount)
        else
            table.ColumnCount

    let tryGetSelectedColumnIndex
        (tableName: string)
        (ctx: Map<string, Contexts.AnnotationTable.AnnotationTableContext>)
        =
        ctx
        |> Map.tryFind tableName
        |> Option.bind (fun x -> x.SelectedCells)
        |> Option.map (fun cells -> cells.xEnd)
        |> Option.map (fun x -> x - 2)

    let insertColumn
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

        let index = nextInsertIndex selectedColumnIndex table
        table.AddColumn(newColumn.Header, newColumn.Cells, index, true)

        {
            InsertedIndex = index
            Warning = warning
        }

    let syncArcVault (arcFile: ArcFiles) : Result<unit, string> =
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

[<ReactComponent>]
let private FreeTextInputElement (onSubmit: string -> unit) =
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

module private DropdownElements =

    let divider = Html.div [ prop.className "swt:divider swt:mx-2 swt:my-0" ]

    let clickableItem (label: string) (onClick: unit -> unit) =
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

    let navItem (label: string) (onClick: unit -> unit) =
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

    let infoFooter (onBack: (unit -> unit) option) =
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

[<ReactComponent>]
let private HeaderTypeDropdown (state: BuildingBlockState, setState: BuildingBlockState -> unit) =
    let isOpen, setOpen = React.useState false

    let close () = setOpen false

    let setMainAndClose nextState =
        setState { nextState with DropdownPage = DropdownPage.Main }
        close ()

    let openSubPage subPage =
        setState { state with DropdownPage = subPage }

    let selectHeaderType (headerType: CompositeHeaderDiscriminate) =
        state
        |> BuildingBlockState.setHeaderCellType headerType
        |> setMainAndClose

    let selectIOType (headerType: CompositeHeaderDiscriminate) (ioType: IOType) =
        state
        |> BuildingBlockState.setHeaderWithIO headerType ioType
        |> setMainAndClose

    let dropdownContent =
        match state.DropdownPage with
        | DropdownPage.Main ->
            [
                DropdownElements.navItem "Input" (fun () ->
                    openSubPage (DropdownPage.IOTypes CompositeHeaderDiscriminate.Input)
                )
                DropdownElements.divider
                DropdownElements.clickableItem "Parameter" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.Parameter
                )
                DropdownElements.clickableItem "Factor" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.Factor
                )
                DropdownElements.clickableItem "Characteristic" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.Characteristic
                )
                DropdownElements.clickableItem "Component" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.Component
                )
                DropdownElements.navItem "More" (fun () -> openSubPage DropdownPage.More)
                DropdownElements.divider
                DropdownElements.navItem "Output" (fun () ->
                    openSubPage (DropdownPage.IOTypes CompositeHeaderDiscriminate.Output)
                )
                DropdownElements.infoFooter None
            ]
        | DropdownPage.More ->
            [
                DropdownElements.clickableItem "Comment" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.Comment
                )
                DropdownElements.clickableItem "Date" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.Date
                )
                DropdownElements.clickableItem "Performer" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.Performer
                )
                DropdownElements.clickableItem "ProtocolDescription" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.ProtocolDescription
                )
                DropdownElements.clickableItem "ProtocolREF" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.ProtocolREF
                )
                DropdownElements.clickableItem "ProtocolType" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.ProtocolType
                )
                DropdownElements.clickableItem "ProtocolUri" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.ProtocolUri
                )
                DropdownElements.clickableItem "ProtocolVersion" (fun () ->
                    selectHeaderType CompositeHeaderDiscriminate.ProtocolVersion
                )
                DropdownElements.infoFooter (Some(fun () -> openSubPage DropdownPage.Main))
            ]
        | DropdownPage.IOTypes headerType ->
            [
                DropdownElements.clickableItem "Source" (fun () -> selectIOType headerType IOType.Source)
                DropdownElements.clickableItem "Sample" (fun () -> selectIOType headerType IOType.Sample)
                DropdownElements.clickableItem "Material" (fun () -> selectIOType headerType IOType.Material)
                DropdownElements.clickableItem "Data" (fun () -> selectIOType headerType IOType.Data)
                Html.li [
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                    )
                    prop.children [
                        FreeTextInputElement(fun text -> selectIOType headerType (IOType.FreeText text))
                    ]
                ]
                DropdownElements.infoFooter (Some(fun () -> openSubPage DropdownPage.Main))
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
let private HeaderInputElement (state: BuildingBlockState, setState: BuildingBlockState -> unit) =
    Html.div [
        prop.style [ style.position.relative ]
        prop.children [
            Html.div [
                prop.className "swt:join swt:w-full"
                prop.children [
                    HeaderTypeDropdown(state, setState)
                    if state.HeaderCellType = CompositeHeaderDiscriminate.Comment then
                        Html.input [
                            prop.className "swt:input swt:join-item swt:flex-grow"
                            prop.valueOrDefault state.CommentHeader
                            prop.placeholder (CompositeHeaderDiscriminate.Comment.ToString())
                            prop.onChange (fun text ->
                                setState {
                                    state with
                                        CommentHeader = text
                                }
                            )
                        ]
                    elif state.HeaderCellType.HasOA() then
                        let setter (termOpt: Swate.Components.Types.Term option) =
                            let nextHeaderArg =
                                termOpt
                                |> Option.map (fun term -> term |> (OntologyAnnotation.from >> U2.Case1))

                            setState {
                                state with
                                    HeaderArg = nextHeaderArg
                            }

                        let input = BuildingBlockState.tryHeaderOA state

                        Swate.Components.TermSearch.TermSearch(
                            (input |> Option.map _.ToTerm()),
                            setter,
                            classNames = Swate.Components.Types.TermSearchStyle(U2.Case1 "swt:border-current swt:join-item swt:w-full")
                        )
                    elif state.HeaderCellType.HasIOType() then
                        Html.input [
                            prop.className "swt:input swt:join-item swt:flex-grow"
                            prop.readOnly true
                            prop.valueOrDefault (
                                BuildingBlockState.tryHeaderIO state
                                |> Option.map _.ToString()
                                |> Option.defaultValue ""
                            )
                        ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let private BodyInputElement (state: BuildingBlockState, setState: BuildingBlockState -> unit) =
    let setBodyCellType (next: CompositeCellDiscriminate) =
        setState {
            state with
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
                        let isActive = state.BodyCellType = CompositeCellDiscriminate.Term
                        prop.className (toggleClass isActive)
                        prop.type'.button
                        prop.text "Term"
                        prop.onClick (fun _ -> setBodyCellType CompositeCellDiscriminate.Term)
                    ]
                    Html.button [
                        let isActive = state.BodyCellType = CompositeCellDiscriminate.Unitized
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

                        setState {
                            state with
                                BodyArg = nextBodyArg
                        }

                    let input = BuildingBlockState.tryBodyOA state
                    let parent = BuildingBlockState.tryHeaderOA state

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
let private StatusElement (status: StatusMessage) =
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
let Main (activeTableData: ActiveTableData option, onTableMutated: unit -> unit) =
    let state, setState = React.useState BuildingBlockState.init
    let status, setStatus = React.useState (None: StatusMessage option)

    let annotationTableCtx =
        React.useContext Contexts.AnnotationTable.AnnotationTableStateCtx

    let selectedColumnIndex =
        activeTableData
        |> Option.bind (fun table ->
            BuildingBlockDataSource.tryGetSelectedColumnIndex table.TableName annotationTableCtx.state
        )

    let createColumn (table: ArcTable) =
        let header = BuildingBlockState.createCompositeHeader state
        let body = BuildingBlockState.tryCreateCompositeCell state

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
            let header = BuildingBlockState.createCompositeHeader state
            let isValid = BuildingBlockState.isValidColumn header

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
                    BuildingBlockDataSource.insertColumn selectedColumnIndex column activeTable.Table

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

    let currentHeader = BuildingBlockState.createCompositeHeader state
    let isValid = BuildingBlockState.isValidColumn currentHeader
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
                    HeaderInputElement(state, setState)
                    if state.HeaderCellType.IsTermColumn() then
                        BodyInputElement(state, setState)
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
            | Some message -> StatusElement message
            | None -> Html.none
        ]
    ]
