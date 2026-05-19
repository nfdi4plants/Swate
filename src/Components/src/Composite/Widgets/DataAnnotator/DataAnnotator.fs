namespace Swate.Components.Composite.Widgets.DataAnnotator

open System
open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Primitive.BaseModal
open Swate.Components.Composite.Table
open Swate.Components.Composite.Table.Types
open Swate.Components.Primitive.Dropdown
open Swate.Components.Composite.Widgets.Context
open Swate.Components.Composite.Widgets.DataAnnotator.Types
open Swate.Components.Composite.Widgets.DataAnnotator.Helper

module DataAnnotatorWidgetModel =

    type Model = {
        DataFile: DataFile option
        ParsedFile: ParsedDataFile option
        Loading: bool
    } with

        static member init() = {
            DataFile = None
            ParsedFile = None
            Loading = false
        }

    type Msg =
        | UpdateDataFile of DataFile option
        | ToggleHeader
        | UpdateSeperator of string
        | UpdateLoading of bool

    let update (state: Model) (msg: Msg) =
        match msg with
        | UpdateDataFile dataFile ->
            let parsedFile =
                dataFile
                |> Option.map (fun file ->
                    let s = file.ExpectedSeparator
                    ParsedDataFile.fromFileBySeparator s file
                )

            let nextState: Model = {
                state with
                    DataFile = dataFile
                    ParsedFile = parsedFile
                    Loading = false
            }

            nextState
        | ToggleHeader ->
            let nextState = {
                state with
                    ParsedFile = state.ParsedFile |> Option.map (fun file -> file.ToggleHeader())
            }

            nextState
        | UpdateSeperator newSep ->
            let parsedFile =
                state.DataFile
                |> Option.map (fun file -> ParsedDataFile.fromFileBySeparator newSep file)

            let nextState = { state with ParsedFile = parsedFile }
            nextState
        | UpdateLoading isLoading -> { state with Loading = isLoading }

open DataAnnotatorWidgetModel

[<Erase; Mangle(false)>]
type DataAnnotatorWidget =

    [<ReactComponent>]
    static member private FileMetadataComponent(file: DataFile) =
        Html.p [
            Html.strong file.DataFileName
            Html.text " - "
            Html.strong file.DataFileType
        ]

    [<ReactComponent>]
    static member private InfoText() =
        Html.div [
            prop.role.alert
            prop.className "swt:alert"
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--info-24-regular swt:text-info swt:size-6"
                ]
                Html.span [
                    Html.p "Load a CSV or TSV file to preview its contents as a selectable table."
                    Html.p [
                        prop.className "swt:text-xs swt:text-base-content/50"
                        prop.text
                            "Click column headers, row numbers, or individual cells to select annotation targets. Selections are highlighted and included when you submit."
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private UploadButton
        (ref: IRefValue<#Browser.Types.HTMLElement option>, uploadFile: Browser.Types.File -> unit)
        =

        Html.input [
            prop.type'.file
            prop.className "swt:file-input swt:file-input-primary swt:w-full"
            prop.ref ref
            prop.onChange uploadFile
        ]

    [<ReactComponent>]
    static member private OpenAnnotatorTableModal(model: Model, openModal) =
        let isDisabled = model.DataFile.IsNone || model.ParsedFile.IsNone || model.Loading

        Html.button [
            prop.className "swt:btn swt:btn-primary swt:w-full"
            prop.disabled isDisabled
            match model with
            | { Loading = true } ->
                prop.children [
                    Html.span [ prop.className "swt:loading swt:loading-spinner" ]
                ]
            | { ParsedFile = None; DataFile = None } -> prop.text "Preview and Select Targets"
            | {
                  ParsedFile = Some _
                  DataFile = Some _
              } ->
                prop.text "Preview and Select Targets"
                prop.onClick (fun _ -> openModal ())
            | _ -> prop.text "..."
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private TableCellButton
        (
            rowIndex: int,
            columnIndex: int,
            content: string,
            dtrgt: DataTarget option,
            isDirectlyActive: bool,
            isActive: bool,
            toggleTarget: DataTarget -> unit
        ) =
        TableCell.BaseCell(
            rowIndex,
            columnIndex,
            (match dtrgt with
             | Some dtrgt ->
                 Html.div [
                     prop.className "swt:w-full swt:h-full swt:flex swt:items-center swt:px-2 swt:py-1 swt:truncate"
                     prop.onClick (fun _ -> toggleTarget dtrgt)
                     prop.children [
                         if isDirectlyActive then
                             Html.div [
                                 prop.className "swt:absolute swt:top-0 swt:right-0 swt:has-text-success swt:m-0"
                                 prop.children [ Primitive.Icons.SquarePlus() ]
                             ]
                         Html.text content
                     ]
                 ]
             | None -> Html.p "-"),
            className =
                String.concat " " [
                    "swt:w-full swt:h-full"
                    if isDirectlyActive || isActive then
                        "swt:bg-primary swt:text-primary-content"
                ]
        )

    [<ReactComponent>]
    static member private Table
        (file: ParsedDataFile, state: Set<DataTarget>, setState: (Set<DataTarget> -> Set<DataTarget>) -> unit)
        =
        let toggleTarget =
            React.useCallback (
                (fun (target: DataTarget) ->
                    setState (fun (currentState: Set<DataTarget>) ->
                        if currentState.Contains target then
                            currentState.Remove target
                        else
                            currentState.Add target
                    )
                ),
                [| box setState |]
            )

        let selectedRows, selectedColumns, selectedCells: Set<int> * Set<int> * Set<int * int> =
            React.useMemo (
                (fun () ->
                    state
                    |> Seq.fold
                        (fun (rows, cols, cells) target ->
                            match target with
                            | DataTarget.Row rowIndex -> (rows.Add rowIndex, cols, cells)
                            | DataTarget.Column columnIndex -> (rows, cols.Add columnIndex, cells)
                            | DataTarget.Cell(columnIndex, rowIndex) ->
                                (rows, cols, cells.Add((columnIndex, rowIndex)))
                        )
                        (Set.empty<int>, Set.empty<int>, Set.empty<int * int>)
                ),
                [| box state |]
            )

        let hasHeader = file.HeaderRow.IsSome

        let bodyMaxColumnCount =
            React.useMemo (
                (fun () -> file.BodyRows |> Array.fold (fun count row -> max count row.Length) 0),
                [| box file |]
            )

        let headerColumnCount =
            file.HeaderRow |> Option.map _.Length |> Option.defaultValue 0

        let bodyRowCount = file.BodyRows.Length
        let rowCount = max 1 (bodyRowCount + if hasHeader then 1 else 0)
        let columnCount = max 1 (max headerColumnCount bodyMaxColumnCount + 1)

        let tableRef = React.useRef<TableHandle> (null)

        let getBodyRowIndex (virtualRowIndex: int) =
            if hasHeader then virtualRowIndex - 1 else virtualRowIndex

        let mkCell (rowIndex: int) (columnIndex: int) (content: string) (target: DataTarget option) =
            let isDirectlyActive =
                match target with
                | Some(DataTarget.Row targetRowIndex) -> selectedRows.Contains targetRowIndex
                | Some(DataTarget.Column targetColumnIndex) -> selectedColumns.Contains targetColumnIndex
                | Some(DataTarget.Cell(targetColumnIndex, targetRowIndex)) ->
                    selectedCells.Contains((targetColumnIndex, targetRowIndex))
                | None -> false

            let isInheritedActive =
                match target with
                | Some(DataTarget.Cell(targetColumnIndex, targetRowIndex)) ->
                    selectedColumns.Contains targetColumnIndex
                    || selectedRows.Contains targetRowIndex
                | _ -> false

            DataAnnotatorWidget.TableCellButton(
                rowIndex,
                columnIndex,
                content,
                target,
                isDirectlyActive,
                isDirectlyActive || isInheritedActive,
                toggleTarget
            )

        Html.div [
            prop.className "swt:overflow-hidden swt:grid swt:grid-cols-1 swt:grid-rows swt:h-[80%]"
            prop.children [
                Table.Table(
                    rowCount,
                    columnCount,
                    (fun index ->
                        if index.x = 0 then
                            if hasHeader && index.y = 0 then
                                mkCell index.y index.x "" None
                            else
                                let bodyRowIndex = getBodyRowIndex index.y

                                if bodyRowIndex < 0 || bodyRowIndex >= bodyRowCount then
                                    mkCell index.y index.x "" None
                                else
                                    mkCell index.y index.x (string bodyRowIndex) (Some(DataTarget.Row bodyRowIndex))
                        elif hasHeader && index.y = 0 then
                            let dataColumnIndex = index.x - 1

                            let headerValue =
                                file.HeaderRow |> Option.bind (fun row -> row |> Array.tryItem dataColumnIndex)

                            match headerValue with
                            | Some value -> mkCell index.y index.x value (Some(DataTarget.Column dataColumnIndex))
                            | None -> mkCell index.y index.x "" None
                        else
                            let bodyRowIndex = getBodyRowIndex index.y
                            let dataColumnIndex = index.x - 1

                            if bodyRowIndex < 0 || bodyRowIndex >= bodyRowCount then
                                mkCell index.y index.x "" None
                            else
                                let cellValue =
                                    file.BodyRows
                                    |> Array.tryItem bodyRowIndex
                                    |> Option.bind (fun row -> row |> Array.tryItem dataColumnIndex)

                                match cellValue with
                                | Some value ->
                                    mkCell index.y index.x value (Some(DataTarget.Cell(dataColumnIndex, bodyRowIndex)))
                                | None -> mkCell index.y index.x "" None

                    ),
                    (fun _ -> Html.div []),
                    tableRef
                )
            ]
        ]

    [<ReactComponent>]
    static member private FileControllerElements
        (pickFile: Browser.Types.File -> JS.Promise<unit>, dataFile: DataFile option, reset: unit -> unit)
        =

        let inputRef = React.useInputRef ()

        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:gap-2"
            prop.children [
                Html.input [
                    prop.ref inputRef
                    prop.type'.file
                    prop.className "swt:file-input swt:w-full"
                    prop.onChange (fun (file: Browser.Types.File) -> promise { do! pickFile file } |> Promise.start)
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-outline swt:btn-sm"
                    prop.disabled dataFile.IsNone
                    prop.text "Reset"
                    prop.onClick (fun _ ->
                        if inputRef.current.IsSome then
                            inputRef.current.Value.value <- null

                        reset ()
                    )
                ]
            ]
        ]


    [<ReactComponent>]
    static member private UpdateSeparatorDropdownElement(text: string, setSeperator) =
        Html.li [
            Html.a [
                prop.onClick setSeperator
                prop.children [ Html.span [ prop.text text ] ]
            ]
        ]

    [<ReactComponent>]
    static member private UpdateSeparatorButton dispatch =
        let updateSeparator = fun s -> UpdateSeperator s |> dispatch

        let input_, setInput = React.useState ("")
        let isOpen, setOpen = React.useState false
        let close = fun _ -> setOpen false
        let hasError = String.IsNullOrEmpty input_

        let setInputDropdown (s: string) =
            fun _ ->
                setInput s
                setOpen false

        Html.div [
            prop.className "swt:join"
            prop.children [
                Dropdown.Main(
                    isOpen,
                    setOpen,
                    Html.button [
                        prop.onClick (fun _ -> setOpen (not isOpen))
                        prop.role.button
                        prop.className
                            "swt:btn swt:btn-primary swt:border swt:border-base-content! swt:join-item swt:flex-nowrap"
                        prop.children [ Primitive.Icons.AngleDown() ]
                    ],
                    React.Fragment [
                        DataAnnotatorWidget.UpdateSeparatorDropdownElement("Tab (\\t)", setInputDropdown "\\t")
                        DataAnnotatorWidget.UpdateSeparatorDropdownElement(",", setInputDropdown ",")
                        DataAnnotatorWidget.UpdateSeparatorDropdownElement(";", setInputDropdown ";")
                        DataAnnotatorWidget.UpdateSeparatorDropdownElement("|", setInputDropdown "|")
                    ]
                )
                Html.input [
                    prop.className "swt:input swt:join-item"
                    prop.placeholder ".. update separator"
                    prop.value input_
                    prop.onChange (fun s -> setInput s)
                    prop.onKeyDown (
                        key.enter,
                        fun _ ->
                            if not hasError then
                                updateSeparator input_
                    )
                ]
                Html.button [
                    prop.className "swt:btn swt:join-item"
                    prop.text "Update"
                    prop.disabled hasError
                    prop.onClick (fun _ -> updateSeparator input_)
                ]
            ]
        ]

    [<ReactComponent>]
    static member private UpdateIsHeaderCheckbox(model: Model, dispatch) =
        let hasHeader = model.ParsedFile.IsSome && model.ParsedFile.Value.HeaderRow.IsSome

        Html.button [
            if hasHeader then
                prop.className "swt:btn swt:btn-primary"
            else
                prop.className "swt:btn"
            prop.onClick (fun _ -> ToggleHeader |> dispatch)
            prop.children [
                Html.p [
                    if not hasHeader then
                        prop.className "swt:line-through"
                    prop.text "Has Header"
                ]
            ]
        ]

    [<ReactComponent>]
    static member private UpdateTargetColumn(current: TargetColumn, setTarget) =
        let mkOption (target) =
            Html.option [ prop.value (string target); prop.text (string target) ]

        let infoText =
            match current with
            | TargetColumn.Autodetect -> "Creates missing Input or Output column, if both exist submit will fail!"
            | TargetColumn.Input -> "Create Input column, will overwrite!"
            | TargetColumn.Output -> "Create Output column, will overwrite!"

        Html.div [
            prop.className "swt:tooltip swt:tooltip-bottom"
            prop.custom ("data-tip", infoText)
            prop.children [
                Html.div [
                    prop.className "swt:indicator"
                    prop.children [
                        Primitive.Icons.InfoCircle([| "swt:indicator-item swt:text-accent" |])
                        Html.select [
                            prop.className "swt:select swt:join-item swt:min-w-fit"
                            prop.title infoText
                            prop.defaultValue (string current)
                            prop.onChange (fun (e: string) -> TargetColumn.fromString e |> setTarget)
                            prop.children [
                                mkOption TargetColumn.Autodetect
                                mkOption TargetColumn.Input
                                mkOption TargetColumn.Output
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private DataFileConfigComponent(model, target, setTarget, dispatch) =
        Html.div [
            prop.className "swt:flex swt:flex-row swt:gap-4"
            prop.children [
                DataAnnotatorWidget.UpdateSeparatorButton dispatch
                DataAnnotatorWidget.UpdateIsHeaderCheckbox(model, dispatch)
                DataAnnotatorWidget.UpdateTargetColumn(target, setTarget)
            ]
        ]

    [<ReactComponent>]
    static member private Modal(model: Model, dispatch, isOpen, setIsOpen, submit) =
        let state, setState: Set<DataTarget> * (((Set<DataTarget> -> Set<DataTarget>) -> unit)) =
            React.useStateWithUpdater (Set.empty<DataTarget>)

        let errorMessage, setErrorMessage = React.useState (None: string option)

        let (targetCol: TargetColumn), setTargetCol =
            React.useState (TargetColumn.Autodetect)

        let modalActivity =
            Html.div [
                prop.children [
                    DataAnnotatorWidget.DataFileConfigComponent(model, targetCol, setTargetCol, dispatch)
                    DataAnnotatorWidget.FileMetadataComponent model.DataFile.Value
                ]
            ]

        let content = DataAnnotatorWidget.Table(model.ParsedFile.Value, state, setState)

        let footer =
            Html.div [
                prop.className "swt:w-full swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    Html.div [
                        prop.className "swt:w-full swt:flex swt:justify-between swt:items-center swt:gap-2"
                        prop.children [
                            Html.div [
                                prop.className "swt:ml-auto swt:flex swt:gap-2"
                                prop.style [ style.marginLeft length.auto ]
                                prop.children [
                                    Html.button [
                                        prop.className "swt:btn swt:btn-outline"
                                        prop.text "Cancel"
                                        prop.onClick (fun _ -> setIsOpen false)
                                    ]
                                    Html.button [
                                        prop.className "swt:btn swt:btn-primary"
                                        prop.text "Submit"
                                        prop.disabled state.IsEmpty
                                        prop.onClick (fun e ->
                                            // match DataAnnotator.tryValidateSubmit (model, state, targetCol) with
                                            // | Some message -> setErrorMessage (Some message)
                                            // | None ->
                                            match model.DataFile with
                                            | Some dtf ->
                                                let selectors = [|
                                                    for x in state do
                                                        x.ToFragmentSelectorString(
                                                            model.ParsedFile.Value.HeaderRow.IsSome
                                                        )
                                                |]

                                                let name = dtf.DataFileName
                                                let dt = dtf.DataFileType

                                                let input: AnnotationInput = {
                                                    Selectors = selectors
                                                    FileName = name
                                                    FileType = dt
                                                    TargetColumn = targetCol
                                                }

                                                submit input
                                            | None -> setErrorMessage (Some "No file selected.")
                                        )
                                    ]
                                ]
                            ]
                        ]
                    ]
                    if errorMessage.IsSome then
                        Html.div [
                            prop.className "swt:alert swt:alert-error swt:text-sm"
                            prop.children [ Html.text errorMessage.Value ]
                        ]
                ]
            ]

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.p "Data Annotator",
            content,
            className = "swt:max-w-none",
            modalActions = modalActivity,
            footer = footer
        )

    [<ReactComponent(true)>]
    static member Main(setAnnotationInput: AnnotationInput -> unit, ?onError: string -> unit) =

        let onError =
            defaultArg onError (fun message -> console.error ("DataAnnotatorWidget error: " + message))

        let model, dispatch =
            React.useReducer (DataAnnotatorWidgetModel.update, DataAnnotatorWidgetModel.Model.init ())

        let showModal, setShowModal = React.useState false

        let UploadButtonInputRef = React.useInputRef ()

        let reset () =
            UpdateDataFile None |> dispatch

            if UploadButtonInputRef.current.IsSome then
                UploadButtonInputRef.current.Value.value <- null

        let pickFile (file: Browser.Types.File) =
            promise {

                UpdateLoading true |> dispatch

                try
                    try
                        let! content = file.text ()

                        let name = file.name

                        let loadedDataFile =
                            DataFile.create (name, fileTypeFromName name, content, float file.size)

                        UpdateDataFile(Some loadedDataFile) |> dispatch
                    with ex ->
                        onError $"Failed to read file: {ex.Message}"
                // match parseDataFileBySeparator loadedDataFile.ExpectedSeparator loadedDataFile with
                // | Ok parsed -> setLoadedFile loadedDataFile (Some parsed)
                // | Error message ->
                //     setLoadedFile loadedDataFile None
                //     onError message
                finally
                    console.log "Finished processing file."
                    UpdateLoading false |> dispatch
            }
            |> Promise.start

        let submit =
            fun (input: AnnotationInput) ->
                match model with
                | {
                      DataFile = Some loadedDataFile
                      ParsedFile = Some currentParsedFile
                  } ->
                    try
                        setAnnotationInput input
                        setShowModal false
                    with exceptionValue ->
                        onError exceptionValue.Message
                | _ -> onError "Load a file first."

        // let previewSection =
        //     match parsedFile with
        //     | None ->
        //         Html.div [
        //             prop.className
        //                 "swt:rounded-box swt:border swt:border-base-300 swt:flex swt:items-center swt:justify-center swt:text-sm swt:opacity-70 swt:min-h-24 swt:px-3 swt:py-4"
        //             prop.text "Load a data file to preview selectable targets."
        //         ]
        //     | Some currentParsedFile ->
        //         let headerCount =
        //             currentParsedFile.HeaderRow |> Option.map _.Length |> Option.defaultValue 0

        //         let bodyCount =
        //             currentParsedFile.BodyRows
        //             |> Array.fold (fun count row -> max count row.Length) 0

        //         let columnCount = max headerCount bodyCount

        //         Html.div [
        //             prop.className "swt:overflow-auto swt:rounded-box swt:border swt:border-base-300 swt:max-h-[55vh]"
        //             prop.children [
        //                 if currentParsedFile.BodyRows.Length = 0 || columnCount = 0 then
        //                     Html.div [
        //                         prop.className
        //                             "swt:flex swt:items-center swt:justify-center swt:h-full swt:text-sm swt:opacity-70"
        //                         prop.text "Parsed file contains no data rows."
        //                     ]
        //                 else
        //                     DataAnnotatorWidget.Table(currentParsedFile, selectedTargets, columnCount, toggleTarget)
        //             ]
        //         ]

        React.Fragment [

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    DataAnnotatorWidget.InfoText()
                    DataAnnotatorWidget.UploadButton(UploadButtonInputRef, pickFile)
                    DataAnnotatorWidget.OpenAnnotatorTableModal(model, (fun () -> setShowModal true))
                ]
            ]

            match model, showModal with
            | {
                  DataFile = Some _
                  ParsedFile = Some _
              },
              true -> DataAnnotatorWidget.Modal(model, dispatch, showModal, setShowModal, submit)
            | _, _ -> Html.none
        ]