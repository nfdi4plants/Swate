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
            Daisy.button.button [
                prop.onClick rmvFile
                if model.DataAnnotatorModel.DataFile.IsNone then
                    button.disabled
                else
                    button.error
                prop.text "Reset"
            ]

        [<ReactComponent>]
        let UpdateSeparatorButton dispatch =
            let updateSeparator =
                fun s -> DataAnnotator.UpdateSeperator s |> DataAnnotatorMsg |> dispatch

            let input_, setInput = React.useState ("")

            Daisy.join [
                prop.children [
                    Daisy.input [
                        input.bordered
                        join.item
                        prop.placeholder ".. update separator"
                        prop.defaultValue input_
                        prop.onChange (fun s -> setInput s)
                        prop.onKeyDown (key.enter, fun e -> updateSeparator input_)
                    ]
                    Daisy.button.button [
                        join.item
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
                        prop.className "line-through"
                    prop.text "Has Header"
                ]

            Daisy.button.button [
                if hasHeader then
                    button.primary
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

            Daisy.tooltip [
                tooltip.bottom
                tooltip.text infoText
                prop.children [
                    Daisy.indicator [
                        prop.children [
                            Html.i [ prop.className "indicator-item fa-solid fa-info-circle fa-lg text-accent" ]
                            Daisy.select [
                                select.bordered
                                join.item
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
                prop.className "join flex"
                prop.children [
                    Html.button [ prop.className "btn btn-primary join-item"; prop.text "Choose File" ]
                    Html.input [
                        prop.title fileName
                        prop.className "input input-bordered input-disabled join-item grow w-full"
                        prop.value fileName
                        prop.readOnly true
                    ]
                    Html.span [
                        prop.className "btn btn-primary join-item btn-disabled"
                        prop.children [
                            if isLoading then
                                Daisy.loading []
                        ]
                    ]
                ]
            ]

        let UploadButton
            (ref: IRefValue<#Browser.Types.HTMLElement option>)
            (model: Model)
            (uploadFile: Browser.Types.File -> unit)
            =
            Daisy.file [
                prop.className "col-span-2"
                file.primary
                file.bordered
                prop.ref ref
                prop.onChange uploadFile
            ]

        let OpenModalButton model mkOpen =
            Daisy.button.button [
                button.primary
                prop.className "grow"
                if model.DataAnnotatorModel.DataFile.IsNone then
                    button.disabled
                prop.text "Open Annotator"
                prop.onClick mkOpen
            ]

    open DataAnnotatorButtons


    let ModalMangementContainer (children: ReactElement list) =
        Html.div [ prop.className "flex flex-col gap-4"; prop.children children ]

    let DataFileConfigComponent model rmvFile target setTarget dispatch =
        Html.div [
            prop.className "flex flex-row gap-4"
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
            prop.className "absolute top-0 right-0 has-text-success m-0"
            prop.children [ Html.i [ prop.className "fa-solid fa-square-plus fa-lg" ] ]
        ]

    let CellButton (isHeader: bool, content: string, dtrgt: DataTarget option, state: Set<DataTarget>, setState) =
        let mkCell: IReactProperty list -> ReactElement =
            if isHeader then Html.th else Html.td

        match dtrgt with
        | Some dtrgt ->
            let isDirectlyActive = state.Contains dtrgt

            let isActive =
                match dtrgt with
                | DataTarget.Column _
                | DataTarget.Row _ -> isDirectlyActive
                | DataTarget.Cell(ci, ri) -> state.Contains(DataTarget.Column ci) || state.Contains(DataTarget.Row ri)

            mkCell [
                prop.className "p-0"
                prop.key $"DataAnnotator_{dtrgt.ToReactKey()}"
                prop.children [
                    Daisy.button.button [
                        prop.className [
                            "w-full rounded-none border-0 relative"
                            if not isHeader then
                                "font-light"
                        ]
                        if isDirectlyActive || isActive then
                            button.primary
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
                ]
            ]
        | None -> mkCell []
        |> List.singleton

    let FileViewComponent (file: DataAnnotator.ParsedDataFile, state, setState) =
        let headerRow =
            file.HeaderRow
            |> Option.map (fun headerRow ->
                let data = [
                    (true, "", None, state, setState)
                    for ci in 0 .. headerRow.Length - 1 do
                        (true, file.HeaderRow.Value.[ci], (DataTarget.Column ci |> Some), state, setState)
                ]

                {|
                    data = data
                    createCell = CellButton
                |})

        let bodyRows = [|
            for ri in 0 .. file.BodyRows.Length - 1 do
                let row = file.BodyRows.[ri]

                [
                    (true, (string ri), (DataTarget.Row ri |> Some), state, setState)
                    for ci in 0 .. row.Length - 1 do
                        (false, row.[ci], (DataTarget.Cell(ci, ri) |> Some), state, setState)
                ]
        |]

        let rowHeight = 57.

        Html.div [
            prop.className "overflow-hidden flex"
            prop.children [
                Components.LazyLoadTable.Main(
                    "DataAnnotatorFileView",
                    bodyRows,
                    CellButton,
                    ?headerRow = headerRow,
                    rowHeight = rowHeight
                )
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
                prop.className "w-full flex justify-between items-center gap-2"
                prop.children [
                    Html.div [ prop.children [ DataAnnotatorButtons.ResetButton model rmvFile ] ]
                    Html.div [
                        prop.className "ml-auto flex gap-2"
                        prop.style [ style.marginLeft length.auto ]
                        prop.children [
                            Daisy.button.button [ prop.onClick rmv; button.outline; prop.text "Cancel" ]
                            Daisy.button.button [
                                button.primary
                                prop.text "Submit"
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
            modalClassInfo = "max-w-none",
            modalActions = modalActivity,
            content = content,
            contentClassInfo = "grid grid-cols-1 grid-rows h-[600px] overflow-hidden",
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
                    prop.className "flex flex-row gap-4"
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
              true -> DataAnnotator.Modal(model, dispatch, rmvFile, fun _ -> setShowModal false)
            | _, _ -> Html.none
        ]

    static member Sidebar(model, dispatch) =
        SidebarComponents.SidebarLayout.Container [

            SidebarComponents.SidebarLayout.Header "Data Annotator"

            SidebarComponents.SidebarLayout.Description "Specify exact data points for annotation."

            SidebarComponents.SidebarLayout.LogicContainer [ DataAnnotator.Main(model, dispatch) ]

        ]