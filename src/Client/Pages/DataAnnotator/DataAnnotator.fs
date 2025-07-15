namespace Pages

open Fable.Core
open DataAnnotator
open Model
open Messages
open Feliz
open Feliz.DaisyUI
open Swate.Components

module private DataAnnotatorHelper =

    module DataAnnotatorButtons =

        let ResetButton model (rmvFile: Browser.Types.Event -> unit) =
            //Daisy.button.button [
            Html.button [
                if model.DataAnnotatorModel.DataFile.IsNone then
                    prop.className "swt:btn swt:btn-disabled"
                else
                    prop.className "swt:btn swt:btn-error"
                prop.text "Reset"
                prop.onClick rmvFile
            ]

        let dropdownElement (text: string) (value: string) setSeperator close =
            Html.li [
                Html.a [
                    prop.onClick (fun _ ->
                        setSeperator value
                        close()
                    )
                    prop.children [
                        Html.span [
                            prop.text text ]
                    ]
                ]
            ]

        [<ReactComponent>]
        let UpdateSeparatorButton dispatch =
            let updateSeparator =
                fun s -> DataAnnotator.UpdateSeperator s |> DataAnnotatorMsg |> dispatch
            let input_, setInput = React.useState ("")
            let isOpen, setOpen = React.useState false
            let close = fun _ -> setOpen false

            Html.div [
                prop.className "swt:join"
                prop.children [
                    Components.BaseDropdown.Main(
                        isOpen,
                        setOpen,
                        Html.button [
                            prop.onClick (fun _ -> setOpen (not isOpen))
                            prop.role "button"
                            prop.className "swt:btn swt:btn-primary swt:border swt:!border-base-content swt:join-item swt:flex-nowrap"
                            prop.children [
                                Html.i [ prop.className "fa-solid fa-angle-down" ]
                            ]
                        ],
                        [
                            dropdownElement "Tab (\\t)" "\\t" setInput close
                            dropdownElement "," "," setInput close
                            dropdownElement ";" ";" setInput close
                            dropdownElement "|" "|" setInput close
                        ],
                        style = Style.init ("swt:join-item swt:dropdown", Map [ "content", Style.init ("swt:!min-w-64") ])
                    )
                    Html.input [
                        prop.className "swt:input swt:join-item"
                        prop.placeholder ".. update separator"
                        prop.value input_
                        prop.onChange (fun s -> setInput s)
                        prop.onKeyDown (key.enter, fun _ -> updateSeparator input_)
                    ]
                    Html.button [
                        prop.className "swt:btn swt:join-item"
                        prop.text "Update"
                        prop.onClick (fun _ -> updateSeparator input_)
                    ]
                ]
            ]

        let UpdateIsHeaderCheckbox (model: Model) dispatch =
            let hasHeader = model.DataAnnotatorModel.ParsedFile.Value.HeaderRow.IsSome

            let txtEle =
                Html.p [
                    if not hasHeader then
                        prop.className "swt:line-through"
                    prop.text "Has Header"
                ]

            //Daisy.button.button [
            Html.button [
                if hasHeader then
                    prop.className "swt:btn swt:btn-primary"
                else
                    prop.className "swt:btn"
                prop.text "Update"
                prop.onClick (fun _ -> DataAnnotator.ToggleHeader |> DataAnnotatorMsg |> dispatch)
                prop.children txtEle
            ]

        let UpdateTargetColumn (current: DataAnnotator.TargetColumn) setTarget =
            let mkOption (target) =
                Html.option [ prop.value (string target); prop.text (string target) ]

            let infoText =
                match current with
                | TargetColumn.Autodetect -> "Creates missing Input or Output column, if both exist submit will fail!"
                | TargetColumn.Input -> "Create Input column, will overwrite!"
                | TargetColumn.Output -> "Create Output column, will overwrite!"

            //Daisy.tooltip [
            Html.div [
                //tooltip.bottom
                prop.className "swt:tooltip swt:tooltip-bottom"
                //tooltip.text infoText
                prop.custom ("data-tip", infoText)
                prop.children [
                    //Daisy.indicator [
                    Html.div [
                        prop.className "swt:indicator"
                        prop.children [
                            Html.i [
                                prop.className "swt:indicator-item fa-solid fa-info-circle fa-lg swt:text-accent"
                            ]
                            //Daisy.select [
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

        let RequestPathButton (fileName: string option, requestPath, isLoading: bool) =
            let fileName = defaultArg fileName "Choose File"

            Html.label [
                prop.onClick requestPath
                prop.className "swt:join swt:flex"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-primary swt:join-item"
                        prop.text "Choose File"
                    ]
                    Html.input [
                        prop.title fileName
                        prop.className "swt:input swt:input-disabled swt:join-item swt:grow swt:w-full"
                        prop.value fileName
                        prop.readOnly true
                    ]
                    Html.span [
                        prop.className "swt:btn swt:btn-primary swt:join-item swt:btn-disabled"
                        prop.children [
                            if isLoading then
                                //Daisy.loading []
                                Html.div [ prop.className "swt:loading" ]

                        ]
                    ]
                ]
            ]

        let UploadButton
            (ref: IRefValue<#Browser.Types.HTMLElement option>)
            (model: Model)
            (uploadFile: Browser.Types.File -> unit)
            =
            //Daisy.file [
            Html.input [
                prop.type' "file"
                prop.className "swt:file-input swt:file-input-primary swt:col-span-2"
                prop.ref ref
                prop.onChange uploadFile
            ]

        let OpenModalButton model mkOpen =
            //Daisy.button.button [
            Html.button [
                if model.DataAnnotatorModel.DataFile.IsNone then
                    prop.className "swt:btn swt:btn-primary swt:grow swt:disabled"
                else
                    prop.className "swt:btn swt:btn-primary swt:grow"
                prop.text "Open Annotator"
                prop.onClick mkOpen
            ]

    open DataAnnotatorButtons


    let ModalMangementContainer (children: ReactElement list) =
        Html.div [ prop.className "swt:flex swt:flex-col swt:gap-4"; prop.children children ]

    let DataFileConfigComponent model rmvFile target setTarget dispatch =
        Html.div [
            prop.className "swt:flex swt:flex-row swt:gap-4"
            prop.children [
                match model.SpreadsheetModel.ActiveView with
                | Spreadsheet.ActivePattern.IsTable ->
                    UpdateSeparatorButton dispatch
                    UpdateIsHeaderCheckbox model dispatch
                    UpdateTargetColumn target setTarget
                //ResetButton model rmvFile
                | Spreadsheet.ActivePattern.IsDataMap ->
                    UpdateSeparatorButton dispatch
                    UpdateIsHeaderCheckbox model dispatch
                //ResetButton model rmvFile
                | _ -> Html.none
            ]
        ]

    let FileMetadataComponent (file: DataFile) =
        Html.p [
            Html.strong file.DataFileName
            Html.text " - "
            Html.strong file.DataFileType
        ]

    let IsAddedIcon =
        Html.div [
            prop.className "swt:absolute swt:top-0 swt:right-0 swt:has-text-success swt:m-0"
            prop.children [ Html.i [ prop.className "fa-solid fa-square-plus fa-lg" ] ]
        ]

    let CellButton
        (rowIndex: int, columnIndex: int, content: string, dtrgt: DataTarget option, state: Set<DataTarget>, setState)
        =

        let isDirectlyActive =
            dtrgt
            |> Option.map (fun dtrgt -> state.Contains dtrgt)
            |> Option.defaultValue false

        let isActive =
            dtrgt
            |> Option.map (function
                | DataTarget.Column _
                | DataTarget.Row _ -> isDirectlyActive
                | DataTarget.Cell(ci, ri) -> state.Contains(DataTarget.Column ci) || state.Contains(DataTarget.Row ri))
            |> Option.defaultValue false

        TableCell.BaseCell(
            rowIndex,
            columnIndex,
            (match dtrgt with
             | Some dtrgt ->
                 Html.div [
                     prop.className "swt:w-full swt:h-full swt:flex swt:items-center swt:px-2 swt:py-1"
                     prop.onClick (fun _ ->
                         if isDirectlyActive then
                             state.Remove dtrgt
                         else
                             state.Add dtrgt
                         |> setState)
                     prop.children [
                         if isDirectlyActive then
                             IsAddedIcon
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

    let FileViewComponent (file: DataAnnotator.ParsedDataFile, state, setState) =
        let headerRow =
            file.HeaderRow
            |> Option.map (fun headerRow ->
                let data = [
                    (0, 0, "", None, state, setState)
                    for ci in 0 .. headerRow.Length - 1 do
                        (0, ci, file.HeaderRow.Value.[ci], (DataTarget.Column ci |> Some), state, setState)
                ]

                data)

        let bodyRows = [|
            for ri in 0 .. file.BodyRows.Length - 1 do
                let row = file.BodyRows.[ri]

                [
                    (ri, 0, (string ri), (DataTarget.Row ri |> Some), state, setState)
                    for ci in 0 .. row.Length - 1 do
                        (ri, ci, row.[ci], (DataTarget.Cell(ci, ri) |> Some), state, setState)
                ]
        |]

        let colCount = bodyRows |> Array.map (fun row -> row.Length) |> Array.max

        let tableRef = React.useRef<TableHandle> null

        let render =
            React.memo (
                (fun tcc ->
                    if tcc.Index.y = 0 && file.HeaderRow.IsSome then
                        // Header Row
                        let content = headerRow.Value.[tcc.Index.x]
                        CellButton content
                    else
                        // Body Row
                        let input = bodyRows.[tcc.Index.y].[tcc.Index.x]
                        CellButton input),
                withKey = (fun (ts: TableCellController) -> $"{ts.Index.x}-{ts.Index.y}")
            )

        Html.div [
            prop.className "swt:overflow-hidden swt:flex"
            prop.children [
                Swate.Components.Table.Table(file.BodyRows.Length, colCount, render, (fun _ -> Html.div []), tableRef)
            // Components.LazyLoadTable.Main(
            //     "DataAnnotatorFileView",
            //     bodyRows,
            //     CellButton,
            //     ?headerRow = headerRow,
            //     rowHeight = rowHeight
            // )
            ]
        ]

open DataAnnotatorHelper
open System
open Components

type DataAnnotator =


    [<ReactComponent>]
    static member private Modal(model: Model, dispatch, rmvFile, rmv) =
        let init: unit -> Set<DataTarget> = fun () -> Set.empty
        let state, setState = React.useState (init)

        let (targetCol: TargetColumn), setTargetCol =
            React.useState (TargetColumn.Autodetect)

        let modalActivity =
            Html.div [
                prop.children [
                    DataFileConfigComponent model rmvFile targetCol setTargetCol dispatch
                    FileMetadataComponent model.DataAnnotatorModel.DataFile.Value
                ]
            ]

        let content =
            FileViewComponent(model.DataAnnotatorModel.ParsedFile.Value, state, setState)

        let footer =
            Html.div [
                prop.className "swt:w-full swt:flex swt:justify-between swt:items-center swt:gap-2"
                prop.children [
                    Html.div [ prop.children [ DataAnnotatorButtons.ResetButton model rmvFile ] ]
                    Html.div [
                        prop.className "swt:ml-auto swt:flex swt:gap-2"
                        prop.style [ style.marginLeft length.auto ]
                        prop.children [
                            //Daisy.button.button [ prop.onClick rmv; button.outline; prop.text "Cancel" ]
                            Html.button [
                                prop.className "swt:btn swt:btn-outline"
                                prop.text "Cancel"
                                prop.onClick rmv
                            ]
                            //Daisy.button.button [
                            Html.button [
                                prop.className "swt:btn swt:btn-primary"
                                prop.text "Submit"
                                prop.onClick rmv
                                prop.onClick (fun e ->
                                    match model.DataAnnotatorModel.DataFile with
                                    | Some dtf ->
                                        let selectors = [|
                                            for x in state do
                                                x.ToFragmentSelectorString(
                                                    model.DataAnnotatorModel.ParsedFile.Value.HeaderRow.IsSome
                                                )
                                        |]

                                        let name = dtf.DataFileName
                                        let dt = dtf.DataFileType

                                        SpreadsheetInterface.AddDataAnnotation {|
                                            fileName = name
                                            fileType = dt
                                            fragmentSelectors = selectors
                                            targetColumn = targetCol
                                        |}
                                        |> InterfaceMsg
                                        |> dispatch
                                    | None -> logw "No file selected"

                                    rmv e)
                            ]
                        ]
                    ]
                ]
            ]

        Swate.Components.BaseModal.BaseModal(
            rmv,
            header = Html.p "Data Annotator",
            modalClassInfo = "swt:max-w-none",
            modalActions = modalActivity,
            content = content,
            contentClassInfo = "swt:grid swt:grid-cols-1 swt:grid-rows swt:h-[600px] swt:overflow-hidden",
            footer = footer
        )

    [<ReactComponent>]
    static member Main(model: Model, dispatch: Msg -> unit) =
        let showModal, setShowModal = React.useState (false)
        let ref = React.useInputRef ()

        let uploadFileOnChange =
            fun (e: Browser.Types.File) ->
                promise {
                    let! content = e.text ()
                    let dtf = DataFile.create (e.name, e.``type``, content, e.size)
                    dtf |> Some |> UpdateDataFile |> DataAnnotatorMsg |> dispatch
                }
                |> Async.AwaitPromise
                |> Async.StartImmediate

        let rmvFile =
            fun _ ->
                UpdateDataFile None |> DataAnnotatorMsg |> dispatch

                if ref.current.IsSome then
                    ref.current.Value.value <- null

        let requestFileFromARCitect =
            fun (e: Browser.Types.MouseEvent) ->
                e.preventDefault ()

                if model.PersistentStorageState.IsARCitect then
                    Elmish.ApiCall.Start() |> ARCitect.RequestFile |> ARCitectMsg |> dispatch

        let activateModal = fun _ -> setShowModal true

        React.fragment [
            ModalMangementContainer [
                match model.PersistentStorageState.IsARCitect with
                | true ->
                    DataAnnotatorHelper.DataAnnotatorButtons.RequestPathButton(
                        model.DataAnnotatorModel.DataFile |> Option.map _.DataFileName,
                        requestFileFromARCitect,
                        model.DataAnnotatorModel.Loading
                    )
                | false -> DataAnnotatorHelper.DataAnnotatorButtons.UploadButton ref model uploadFileOnChange
                Html.div [
                    prop.className "swt:flex swt:flex-row swt:gap-4"
                    prop.children [
                        DataAnnotatorHelper.DataAnnotatorButtons.ResetButton model rmvFile
                        DataAnnotatorHelper.DataAnnotatorButtons.OpenModalButton model activateModal
                    ]
                ]
            ]
            match model.DataAnnotatorModel, showModal with
            | {
                  DataFile = Some _
                  ParsedFile = Some _
              },
              true ->
                ReactDOM.createPortal (
                    DataAnnotator.Modal(model, dispatch, rmvFile, fun _ -> setShowModal false),
                    Browser.Dom.document.body
                ) // Create a portal to render the modal in the body
            | _, _ -> Html.none
        ]

    static member Sidebar(model, dispatch) =
        SidebarComponents.SidebarLayout.Container [

            SidebarComponents.SidebarLayout.Header "Data Annotator"

            SidebarComponents.SidebarLayout.Description "Specify exact data points for annotation."

            SidebarComponents.SidebarLayout.LogicContainer [ DataAnnotator.Main(model, dispatch) ]

        ]