namespace Pages

open Model
open Browser.Types
open Elmish
open Messages.FilePicker
open Messages
open Feliz
open Feliz.DaisyUI
open Swate

module FilePicker =

    let update
        (filePickerMsg: FilePicker.Msg)
        (state: FilePicker.Model)
        (model: Model.Model)
        : FilePicker.Model * Cmd<Messages.Msg> =
        match filePickerMsg with
        | LoadNewFiles fileNames ->
            let nextModel = {
                model with
                    Model.FilePickerState.FileNames = fileNames |> List.mapi (fun i x -> i + 1, x)
                    Model.PageState.SidebarPage = Routing.SidebarPage.FilePicker
            }

            let nextCmd = UpdateModel nextModel |> Cmd.ofMsg
            state, nextCmd
        | UpdateFileNames newFileNames ->
            let nextState: FilePicker.Model = { FileNames = newFileNames }
            nextState, Cmd.none

type FilePicker =


    /// "parentContainerResizeClass": uses tailwind container queries. Expects a string like "@md/parentId:flex-row"
    static member private UploadButtons(model: Model, dispatch, parentContainerResizeClass: string) =

        let inputId = "filePicker_OnFilePickerMainFunc"

        Html.div [
            prop.className [ "flex flex-col gap-2"; parentContainerResizeClass ]
            prop.children [
                Html.input [
                    prop.style [ style.display.none ]
                    prop.id inputId
                    prop.multiple true
                    prop.type'.file
                    prop.onChange (fun (ev: File list) ->
                        let files = ev //ev.target?files

                        let fileNames = files |> List.map (fun f -> f.name)

                        fileNames |> LoadNewFiles |> FilePickerMsg |> dispatch

                    //let picker = Browser.Dom.document.getElementById(inputId)
                    //// https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                    //picker?value <- null
                    )
                ]
                match model.PersistentStorageState.Host with
                | Some Swatehost.ARCitect ->
                    Daisy.button.button [
                        button.primary
                        button.block
                        prop.onClick (fun _ -> Start false |> ARCitect.RequestPaths |> ARCitectMsg |> dispatch)
                        prop.text "Pick Files"
                    ]

                    Daisy.button.button [
                        button.primary
                        button.block
                        prop.onClick (fun _ -> Start true |> ARCitect.RequestPaths |> ARCitectMsg |> dispatch)
                        prop.text "Pick Directories"
                    ]
                | _ ->
                    Daisy.button.button [
                        button.primary
                        button.block
                        prop.onClick (fun _ ->
                            let getUploadElement = Browser.Dom.document.getElementById inputId
                            getUploadElement.click ())
                        prop.text "Pick file names"
                    ]
            ]
        ]

    static member private ActionButtons (model: Model) dispatch =
        Html.div [
            prop.className "flex flex-row justify-center gap-2"
            prop.children [

                Daisy.button.button [
                    button.neutral
                    button.outline
                    prop.onClick (fun _ -> Messages.FilePicker.UpdateFileNames [] |> FilePickerMsg |> dispatch)
                    prop.text "Cancel"
                ]

                Daisy.button.button [
                    button.primary
                    prop.onClick (fun _ ->
                        let fileNames = model.FilePickerState.FileNames |> List.map snd
                        SpreadsheetInterface.InsertFileNames fileNames |> InterfaceMsg |> dispatch)
                    prop.text "Insert file names"
                ]
            ]
        ]

    static member private SortButton icon msg =
        Daisy.button.a [
            join.item
            prop.onClick msg
            prop.children [ Html.i [ prop.classes [ "fa-lg"; icon ] ] ]
        ]

    static member private FileSortElements (model: Model) dispatch =
        Html.div [
            Daisy.join [
                prop.children [
                    FilePicker.SortButton "fa-solid fa-arrow-down-a-z" (fun _ ->
                        let sortedList =
                            model.FilePickerState.FileNames
                            |> List.sortBy snd
                            |> List.mapi (fun i x -> i + 1, snd x)

                        UpdateFileNames sortedList |> FilePickerMsg |> dispatch)
                    FilePicker.SortButton "fa-solid fa-arrow-down-z-a" (fun _ ->
                        let sortedList =
                            model.FilePickerState.FileNames
                            |> List.sortByDescending snd
                            |> List.mapi (fun i x -> i + 1, snd x)

                        UpdateFileNames sortedList |> FilePickerMsg |> dispatch)
                ]
            ]
        ]

    static member private DeleteFromTable (id, fileName) (model: Model) dispatch =
        Components.Components.DeleteButton(
            props = [
                prop.onClick (fun _ ->
                    let newList =
                        model.FilePickerState.FileNames
                        |> List.except [ id, fileName ]
                        |> List.mapi (fun i (_, name) -> i + 1, name)

                    newList |> UpdateFileNames |> FilePickerMsg |> dispatch)
                button.xs
                button.error
                button.outline
            ]
        )

    static member private MoveUpButton (id, fileName) (model: Model) dispatch =
        Daisy.button.a [
            button.xs
            join.item
            prop.onClick (fun _ ->
                let sortedList =
                    model.FilePickerState.FileNames
                    |> List.map (fun (iterInd, iterFileName) ->
                        let isNameToMove = (id, fileName) = (iterInd, iterFileName)

                        if isNameToMove then
                            // if the iterated element is the one we want to move, substract 1.5 from it
                            // let sortBy handle all stuff then assign new indices with mapi
                            (float iterInd - 1.5, iterFileName)
                        else
                            (float iterInd, iterFileName))
                    |> List.sortBy fst
                    |> List.mapi (fun i v -> i + 1, snd v)

                UpdateFileNames sortedList |> FilePickerMsg |> dispatch)
            prop.children [ Html.i [ prop.className "fa-solid fa-arrow-up" ] ]
        ]

    static member private MoveDownButton (id, fileName) (model: Model) dispatch =
        Daisy.button.a [
            button.xs
            join.item
            prop.onClick (fun _ ->
                let sortedList =
                    model.FilePickerState.FileNames
                    |> List.map (fun (iterInd, iterFileName) ->
                        let isNameToMove = (id, fileName) = (iterInd, iterFileName)

                        if isNameToMove then
                            // if the iterated element is the one we want to move, add 1.5 from it
                            // let sortBy handle all stuff then assign new indices with mapi
                            (float iterInd + 1.5, iterFileName)
                        else
                            (float iterInd, iterFileName))
                    |> List.sortBy fst
                    |> List.mapi (fun i v -> i + 1, snd v)

                UpdateFileNames sortedList |> FilePickerMsg |> dispatch)
            prop.children [ Html.i [ prop.className "fa-solid fa-arrow-down" ] ]
        ]

    static member private MoveButtonList (id, fileName) (model: Model) dispatch =
        Daisy.join [
            FilePicker.MoveUpButton (id, fileName) model dispatch
            FilePicker.MoveDownButton (id, fileName) model dispatch
        ]


    static member private FileViewTable (model: Model) dispatch =
        Daisy.table [
            table.zebra
            table.xs
            prop.children [
                Html.tbody [
                    for index, fileName in model.FilePickerState.FileNames do
                        Html.tr [
                            Html.td [ Html.b $"{index}" ]
                            Html.td fileName
                            Html.td [ FilePicker.MoveButtonList (index, fileName) model dispatch ]
                            Html.td [
                                prop.style [ style.textAlign.right ]
                                prop.children [ FilePicker.DeleteFromTable (index, fileName) model dispatch ]
                            ]
                        ]
                ]
            ]
        ]


    static member Main(model: Model, dispatch, containerQueryClass: string) =

        React.fragment [

            match model.FilePickerState.FileNames with
            | [] ->

                FilePicker.UploadButtons(model, dispatch, containerQueryClass)
            | _ ->
                FilePicker.FileSortElements model dispatch

                FilePicker.FileViewTable model dispatch
                //fileNameElements model dispatch
                FilePicker.ActionButtons model dispatch
        ]

    static member Sidebar(model: Model, dispatch: Messages.Msg -> unit) =

        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "File Picker"

            SidebarComponents.SidebarLayout.Description
                "Select files from your computer and insert their names into Excel"

            // Colored container element for all uploaded file names and sort elements
            FilePicker.Main(model, dispatch, "@md/sidebar:flex-row")
        ]