module FilePicker

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Thoth.Json
open Thoth.Elmish
open ExcelColors
open Api
open Model
open Shared
open Browser.Types
open Elmish
open Messages.FilePicker
open Messages
open Feliz
open Feliz.Bulma

let update (filePickerMsg:FilePicker.Msg) (currentState: FilePicker.Model) : FilePicker.Model * Cmd<Messages.Msg> =
    match filePickerMsg with
    | LoadNewFiles fileNames ->
        let nextState : FilePicker.Model = {
            FileNames = fileNames |> List.mapi (fun i x -> i+1,x)
        }
        let nextCmd = UpdatePageState (Some Routing.Route.FilePicker) |> Cmd.ofMsg
        nextState, nextCmd
    | UpdateFileNames newFileNames ->
        let nextState : FilePicker.Model = {
            FileNames = newFileNames
        }
        nextState, Cmd.none

/// This logic only works as soon as we can access electron. Will not work in Browser.
module PathRerooting =

    open Fable.Core
    open Fable.Core.JsInterop

    let private normalizePath (path:string) =
        path.Replace('\\','/')

    let listOfSupportedDirectories = ["studies"; "assays"; "workflows"; "runs"] 

    let private matchesSupportedDirectory (str:string) =
        listOfSupportedDirectories |> List.contains str

    /// <summary>Normalizes path and searches for 'listOfSupportedDirectories' (["studies"; "assays"; "workflows"; "runs"]) in path. reroots path to parent of supported directory if found
    /// else returns only file name.</summary>
    let rerootPath (path:string) =
        let sep = '/'
        let path = normalizePath path // shadow path variable to normalized
        let splitPath = path.Split(sep)
        let tryFindLevel = Array.tryFindIndexBack (fun x -> matchesSupportedDirectory x) splitPath
        match tryFindLevel with
        // if we cannot find any of `listOfSupportedDirectories` we just return the file name
        | None ->
            splitPath |> Array.last
        | Some levelIndex ->
            // If we find one of `listOfSupportedDirectories` we want to reroot relative to the folder containing the investigation file.
            // It is located one level higher than any of `listOfSupportedDirectories`
            let rootPath =
                Array.take levelIndex splitPath // one level higher so `levelIndex` instead of `(levelIndex + 1)`
                |> String.concat (string sep)
            let relativePath =
                path.Replace(rootPath + string sep, "")
            relativePath

let uploadButton (model:Messages.Model) dispatch (inputId: string) =
    Bulma.field.div [
        Html.input [
            prop.style [style.display.none]
            prop.id inputId
            prop.multiple true
            prop.type' "file"
            prop.onChange (fun (ev: File list) ->
                let files = ev //ev.target?files

                let fileNames =
                    files |> List.map (fun f -> f.name)

                Browser.Dom.console.log fileNames

                fileNames |> LoadNewFiles |> FilePickerMsg |> dispatch

                //let picker = Browser.Dom.document.getElementById(inputId)
                //// https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                //picker?value <- null
            )
        ]
        Bulma.button.button [
            Bulma.color.isInfo
            Bulma.button.isFullWidth
            prop.onClick(fun e ->
                let getUploadElement = Browser.Dom.document.getElementById inputId
                getUploadElement.click()
            )
            prop.text "Pick file names"
        ]
    ]

let insertButton (model:Messages.Model) dispatch =
    Bulma.field.div [
        Bulma.button.button [
            Bulma.color.isSuccess
            Bulma.button.isFullWidth
            prop.onClick (fun _ ->
                let fileNames = model.FilePickerState.FileNames |> List.map snd
                SpreadsheetInterface.InsertFileNames fileNames |> InterfaceMsg |> dispatch
            )
            prop.text "Insert file names"
        ]
    ]

let sortButton icon msg =
    Bulma.button.a [
        prop.onClick msg
        prop.children [
            Bulma.icon [Html.i [prop.classes ["fa-lg"; icon]]]
        ]
    ]

let fileSortElements (model:Messages.Model) dispatch =
    Bulma.field.div [
        Bulma.buttons [
            Bulma.button.a [
                prop.title "Copy to Clipboard"
                prop.onClick(fun e ->
                    CustomComponents.ResponsiveFA.triggerResponsiveReturnEle "clipboard_filepicker" 
                    let txt = model.FilePickerState.FileNames |> List.map snd |> String.concat System.Environment.NewLine
                    let textArea = Browser.Dom.document.createElement "textarea"
                    textArea?value <- txt
                    textArea?style?top <- "0"
                    textArea?style?left <- "0"
                    textArea?style?position <- "fixed"

                    Browser.Dom.document.body.appendChild textArea |> ignore

                    textArea.focus()
                    // Can't belive this actually worked
                    textArea?select()

                    let t = Browser.Dom.document.execCommand("copy")
                    Browser.Dom.document.body.removeChild(textArea) |> ignore
                    ()
                )
                prop.children [
                    CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_filepicker" "fa-regular fa-clipboard" "fa-solid fa-check"
                ]
            ]

            Bulma.buttons [
                Bulma.buttons.hasAddons
                prop.style [style.custom("marginLeft", "auto")]
                prop.children [
                    sortButton "fa-solid fa-arrow-down-a-z" (fun e ->
                        let sortedList = model.FilePickerState.FileNames |> List.sortBy snd |> List.mapi (fun i x -> i+1,snd x)
                        UpdateFileNames sortedList |> FilePickerMsg |> dispatch
                    )
                    sortButton "fa-solid fa-arrow-down-z-a" (fun e ->
                        let sortedList = model.FilePickerState.FileNames |> List.sortByDescending snd |> List.mapi (fun i x -> i+1,snd x)
                        UpdateFileNames sortedList |> FilePickerMsg |> dispatch
                    )
                ]
            ]
        ]
    ]

module FileNameTable =

    let deleteFromTable (id,fileName) (model:Model) dispatch =
        Bulma.delete [
            prop.onClick (fun _ ->
                let newList =
                    model.FilePickerState.FileNames
                    |> List.except [id,fileName]
                    |> List.mapi (fun i (_,name) -> i+1, name)
                newList |> UpdateFileNames |> FilePickerMsg |> dispatch
            )
            prop.style [
                style.marginRight(length.rem 2)
            ]
        ]

    let moveUpButton (id,fileName) (model:Model) dispatch =
        Bulma.button.a [
            Bulma.button.isSmall
            prop.onClick (fun _ ->
                let sortedList =
                    model.FilePickerState.FileNames
                    |> List.map (fun (iterInd,iterFileName) ->
                        let isNameToMove = (id,fileName) = (iterInd,iterFileName)
                        if isNameToMove then
                            // if the iterated element is the one we want to move, substract 1.5 from it
                            // let sortBy handle all stuff then assign new indices with mapi
                            (float iterInd-1.5,iterFileName)
                        else
                            (float iterInd,iterFileName)
                    )
                    |> List.sortBy fst
                    |> List.mapi (fun i v -> i+1, snd v)
                UpdateFileNames sortedList |> FilePickerMsg |> dispatch
            )
            prop.children [
                Bulma.icon [Html.i [prop.className "fa-solid fa-arrow-up"]]
            ]
        ]

    let moveDownButton (id,fileName) (model:Model) dispatch =
        Bulma.button.a [
            Bulma.button.isSmall
            prop.onClick (fun _ ->
                let sortedList =
                    model.FilePickerState.FileNames
                    |> List.map (fun (iterInd,iterFileName) ->
                        let isNameToMove = (id,fileName) = (iterInd,iterFileName)
                        if isNameToMove then
                            // if the iterated element is the one we want to move, add 1.5 from it
                            // let sortBy handle all stuff then assign new indices with mapi
                            (float iterInd+1.5,iterFileName)
                        else
                            (float iterInd,iterFileName)
                    )
                    |> List.sortBy fst
                    |> List.mapi (fun i v -> i+1, snd v)
                UpdateFileNames sortedList |> FilePickerMsg |> dispatch
            )
            prop.children [
                Bulma.icon [Html.i [prop.className "fa-solid fa-arrow-down"]]
            ]
        ]

    let moveButtonList (id,fileName) (model:Model) dispatch =
        Bulma.buttons [
            moveUpButton (id,fileName) model dispatch
            moveDownButton (id,fileName) model dispatch
        ]
        

    let table (model:Messages.Model) dispatch =
        Bulma.table [
            Bulma.table.isHoverable
            Bulma.table.isStriped
            Bulma.table.isHoverable
            prop.children [
                tbody [] [
                    for index,fileName in model.FilePickerState.FileNames do
                        Html.tr [
                            td [] [b [] [str $"{index}"]]
                            td [] [str fileName]
                            td [] [moveButtonList (index,fileName) model dispatch]
                            td [Style [TextAlign TextAlignOptions.Right]] [deleteFromTable (index,fileName) model dispatch]
                        ]
                ]
            ]
        ]
        

let fileContainer (model:Messages.Model) dispatch inputId=
    mainFunctionContainer [

        Bulma.help  "Choose one or multiple files, rearrange them and add their names to the Excel sheet."

        uploadButton model dispatch inputId

        if model.FilePickerState.FileNames <> [] then
            fileSortElements model dispatch

            FileNameTable.table model dispatch
            //fileNameElements model dispatch
            insertButton model dispatch
    ]

let filePickerComponent (model:Messages.Model) (dispatch:Messages.Msg -> unit) =
    let inputId = "filePicker_OnFilePickerMainFunc"
    Bulma.content [
        pageHeader "File Picker"

        Bulma.label "Select files from your computer and insert their names into Excel"

        // Colored container element for all uploaded file names and sort elements
        fileContainer model dispatch inputId
    ]