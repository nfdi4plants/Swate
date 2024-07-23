namespace Pages

open Fable.Core
open DataAnnotator
open Model
open Messages
open Feliz
open Feliz.Bulma

module private DataAnnotatorHelper =

    module DataAnnotatorButtons =

        let ResetButton model (rmvFile: Browser.Types.Event -> unit) =
            Bulma.button.button [
                prop.onClick rmvFile
                if model.DataAnnotatorModel.DataFile.IsNone then
                    Bulma.button.isStatic
                else
                    Bulma.color.isDanger
                prop.text "Reset"
            ]

        [<ReactComponent>]
        let UpdateSeparatorButton dispatch =
            let updateSeparator = fun s -> DataAnnotator.UpdateSeperator s |> DataAnnotatorMsg |> dispatch
            let input, setInput = React.useState("")
            Bulma.field.div [
                field.hasAddons
                prop.className "mb-0"
                prop.children [
                    Bulma.control.div [
                        Bulma.input.text [
                            prop.defaultValue input
                            prop.onChange(fun s -> setInput s)
                            prop.onKeyDown(key.enter, fun e ->
                                updateSeparator input
                            )
                        ]
                    ]
                    Bulma.control.div [
                        Bulma.button.button [
                            prop.text "Update"
                            prop.onClick (fun _ -> updateSeparator input)
                        ]
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
            Bulma.button.button [
                if hasHeader then color.isSuccess
                prop.onClick (fun _ -> DataAnnotator.ToggleHeader |> DataAnnotatorMsg |> dispatch)
                prop.children txtEle
            ]

        let UploadButton (ref: IRefValue<#Browser.Types.HTMLElement option>) (model: Model) (uploadFile: Browser.Types.File -> unit) =
            let fileName = model.DataAnnotatorModel.DataFile |> Option.map (fun x -> x.DataFileName) |> Option.defaultValue "No file selected"
            Bulma.file [
                color.isSuccess
                file.hasName
                prop.children [
                    Bulma.fileLabel.label [
                        Bulma.fileInput [
                            prop.ref ref
                            prop.onChange uploadFile
                        ]
                        Bulma.fileCta [
                            Bulma.fileIcon [Html.i [prop.className "fa-solid fa-upload"]]
                            Bulma.fileLabel.span "Choose a file..."
                        ]
                        Bulma.fileName fileName
                    ]
                ]
            ]

        let OpenModalButton model mkOpen =  
            Bulma.button.button [
                if model.DataAnnotatorModel.DataFile.IsNone then
                    Bulma.button.isStatic
                prop.text "Open Annotator"
                prop.onClick mkOpen
            ]

    open DataAnnotatorButtons


    let ModalMangementComponent ref (model: Model) (openModal: Browser.Types.Event -> unit) rmvFile uploadFile =
        Bulma.field.div [
            field.isGroupedMultiline
            field.isGrouped
            prop.children [
                Bulma.control.div [
                    UploadButton ref model uploadFile
                ]
                Bulma.control.div [
                    ResetButton model rmvFile
                ]
                Bulma.control.div [
                    OpenModalButton model openModal
                ]
            ]
        ]

    let DataFileConfigComponent model rmvFile dispatch =
        Bulma.block [
            Bulma.buttons [
                prop.children [
                    UpdateSeparatorButton dispatch
                    UpdateIsHeaderCheckbox model dispatch
                    ResetButton model rmvFile
                ]
            ]
        ]

    let FileMetadataComponent (file: DataFile) =
        Bulma.block [
            Html.textf "Length %i - %s" file.DataContent.Length file.DataFileName
        ]

    let IsAddedIcon =
        Bulma.icon [
            prop.className "absolute top-0 right-0 has-text-success"
            prop.children [
                    Html.i [prop.className "fa-solid fa-square-plus fa-lg"]
            ]
        ]

    let CellButton (isHeader:bool, content: string, dtrgt: DataTarget, state: Set<DataTarget>, setState) =
        let isDirectlyActive = state.Contains dtrgt
        let isActive =
            match dtrgt with
            | DataTarget.Column _ | DataTarget.Row _ -> isDirectlyActive
            | DataTarget.Cell (ci, ri) -> state.Contains (DataTarget.Column ci) || state.Contains (DataTarget.Row ri)
        let mkCell: IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
        mkCell [
            prop.className "p-0"
            prop.key "dtrgt"
            prop.children [
                Bulma.button.button [
                    prop.className ["w-full rounded-none border-0 relative"; if not isHeader then "font-light"]
                    if isDirectlyActive || isActive then color.isPrimary
                    prop.onClick(fun _ ->
                        if isDirectlyActive then state.Remove dtrgt else state.Add dtrgt
                        |> setState
                    )
                    prop.children [
                        if isDirectlyActive then
                            IsAddedIcon
                        Html.text content
                    ]
                ]
            ]
        ]
        |> List.singleton

open DataAnnotatorHelper
open System

type DataAnnotator =

    [<ReactComponent>]
    static member private FileViewComponent (file: DataAnnotator.ParsedDataFile) =
        let init: unit -> Set<DataTarget> = fun () -> Set.empty
        let state, setState = React.useState(init)
        let headerRow =
            file.HeaderRow
            |> Option.map (fun headerRow ->
                let data =
                    [
                        for ci in 0 .. headerRow.Length-1 do
                            (
                                true,
                                file.HeaderRow.Value.[ci],
                                (DataTarget.Column ci),
                                state,
                                setState
                            )
                    ]
                {|data = data; createCell = CellButton|}
            )
        let bodyRows = [|
            for ri in 0 .. file.BodyRows.Length-1 do
                let row = file.BodyRows.[ri]
                [
                    for ci in 0 .. row.Length-1 do
                        (
                            false,
                            row.[ci],
                            (DataTarget.Cell (ci, ri)),
                            state,
                            setState
                        )
                ]
        |]
        let rowHeight = 57.
        Bulma.block [
            prop.className "overflow-hidden"
            prop.children [
                Components.LazyLoadTable.Main(
                    "DataAnnotatorFileView",
                    bodyRows,
                    CellButton,
                    ?headerRow = headerRow,
                    rowHeight=rowHeight
                )
            ]
        ]

    static member private Modal(model: Model, dispatch, rmvFile, rmv) =
        Bulma.modal [
            Bulma.modal.isActive
            prop.children [
                Bulma.modalBackground [ prop.onClick rmv ]
                Bulma.modalCard [
                    prop.style [style.maxHeight(length.percent 80); style.width (length.perc 80); style.overflowY.hidden]
                    prop.children [
                        Bulma.modalCardHead [
                            Bulma.modalCardTitle "Data Annotator"
                            Bulma.delete [ prop.onClick rmv ]
                        ]
                        Bulma.modalCardBody [
                            prop.className "p-5 overflow-hidden flex flex-col"
                            prop.children [
                                DataFileConfigComponent model rmvFile dispatch
                                FileMetadataComponent model.DataAnnotatorModel.DataFile.Value
                                DataAnnotator.FileViewComponent model.DataAnnotatorModel.ParsedFile.Value
                            ]
                        ]    
                        Bulma.modalCardFoot [
                            Bulma.button.button [
                                color.isInfo
                                prop.style [style.marginLeft length.auto]
                                prop.text "Submit"
                                prop.onClick(fun e ->
                                    log "SUBMIT"
                                    rmv e
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch: Msg -> unit) =
        let showModal, setShowModal = React.useState(false)
        let ref = React.useInputRef()
        let uploadFile = fun (e: Browser.Types.File) ->
            promise {
                let! content = e.text()
                let dtf = DataFile.create(e.name, e.``type``, content, e.size)
                setShowModal true
                dtf |> Some |> UpdateDataFile |> DataAnnotatorMsg |> dispatch
            }
            |> Async.AwaitPromise
            |> Async.StartImmediate
        let rmvFile = fun _ ->
            UpdateDataFile None |> DataAnnotatorMsg |> dispatch
            if ref.current.IsSome then
                ref.current.Value.value <- null
        Html.div [
            ModalMangementComponent ref model (fun _ -> setShowModal true) rmvFile uploadFile
            match model.DataAnnotatorModel, showModal with
            | { DataFile = Some _; ParsedFile = Some _ }, true -> DataAnnotator.Modal(model, dispatch, rmvFile, fun _ -> setShowModal false)
            | _, _ -> Html.none
        ]

