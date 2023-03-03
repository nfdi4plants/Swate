module FilePicker

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
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

let update (filePickerMsg:FilePicker.Msg) (currentState: FilePicker.Model) : FilePicker.Model * Cmd<Messages.Msg> =
    match filePickerMsg with
    | LoadNewFiles fileNames ->
        let nextState = {
            FilePicker.Model.init() with
                FileNames = fileNames |> List.mapi (fun i x -> i+1,x)
        }
        let nextCmd = UpdatePageState (Some Routing.Route.FilePicker) |> Cmd.ofMsg
        nextState, nextCmd
    | UpdateFileNames newFileNames ->
        let nextState = {
            currentState with
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

let uploadButton (model:Messages.Model) dispatch inputId =
    Field.div [] [
        input [
            Style [Display DisplayOptions.None]
            Id inputId
            Multiple true
            Type "file"
            OnChange (fun ev ->
                let files : FileList = ev.target?files

                let fileNames =
                    [ for i=0 to (files.length - 1) do yield files.item i ]
                    |> List.map (fun f ->
                        f.name
                    )

                Browser.Dom.console.log fileNames

                fileNames |> LoadNewFiles |> FilePickerMsg |> dispatch

                //let picker = Browser.Dom.document.getElementById(inputId)
                //// https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                //picker?value <- null
            )
        ]
        Button.button [
            Button.Color IsInfo
            Button.IsFullWidth
            Button.OnClick(fun e ->
                let getUploadElement = Browser.Dom.document.getElementById inputId
                getUploadElement.click()
            )
        ] [
            str "Pick file names"
        ]
    ]

let insertButton (model:Messages.Model) dispatch =
    Field.div [] [
        Button.button [
            Button.Color IsSuccess
            Button.IsFullWidth
            Button.OnClick (fun e ->
                let fileNames = model.FilePickerState.FileNames |> List.map snd
                SpreadsheetInterface.InsertFileNames fileNames |> InterfaceMsg |> dispatch
            )
        ] [
            str "Insert file names"
        ]
    ]

let sortButton icon msg =
    Button.a [
        Button.OnClick msg
    ] [
        Fa.i [ Fa.Size Fa.FaLarge; icon ] [ ] 
    ]

let fileSortElements (model:Messages.Model) dispatch =
    Field.div [] [
        Button.list [] [
            Button.a [
                Button.Props [Title "Copy to Clipboard"]
                Button.OnClick (fun e ->
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
            ] [
                CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_filepicker" Fa.Regular.Clipboard Fa.Solid.Check
            ]

            Button.list [
                Button.List.HasAddons
                Button.List.Props [Style [MarginLeft "auto"]]
            ] [
                sortButton Fa.Solid.SortAlphaDown (fun e ->
                    let sortedList = model.FilePickerState.FileNames |> List.sortBy snd |> List.mapi (fun i x -> i+1,snd x)
                    UpdateFileNames sortedList |> FilePickerMsg |> dispatch
                )
                sortButton Fa.Solid.SortAlphaDownAlt (fun e ->
                    let sortedList = model.FilePickerState.FileNames |> List.sortByDescending snd |> List.mapi (fun i x -> i+1,snd x)
                    UpdateFileNames sortedList |> FilePickerMsg |> dispatch
                )
            ]
        ]
    ]

module FileNameTable =

    let deleteFromTable (id,fileName) (model:Model) dispatch =
        Delete.delete [
            Delete.OnClick (fun _ ->
                let newList =
                    model.FilePickerState.FileNames
                    |> List.except [id,fileName]
                    |> List.mapi (fun i (_,name) -> i+1, name)
                newList |> UpdateFileNames |> FilePickerMsg |> dispatch
            )
            Delete.Props [ Style [
                MarginRight "2rem"
            ]]
        ] []

    let moveUpButton (id,fileName) (model:Model) dispatch =
        Button.a [
            Button.Size IsSmall
            Button.OnClick (fun _ ->
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
        ] [
            Fa.i [Fa.Solid.ArrowUp] []
        ]

    let moveDownButton (id,fileName) (model:Model) dispatch =
        Button.a [
            Button.Size IsSmall
            Button.OnClick (fun _ ->
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
        ] [
            Fa.i [Fa.Solid.ArrowDown] []
        ]

    let moveButtonList (id,fileName) (model:Model) dispatch =
        Button.list [] [
            moveUpButton (id,fileName) model dispatch
            moveDownButton (id,fileName) model dispatch
        ]
        

    let table (model:Messages.Model) dispatch =
        Table.table [
            Table.IsHoverable
            Table.IsStriped
        ] [
            tbody [] [
                for index,fileName in model.FilePickerState.FileNames do
                    tr [] [
                        td [] [b [] [str $"{index}"]]
                        td [] [str fileName]
                        td [] [moveButtonList (index,fileName) model dispatch]
                        td [Style [TextAlign TextAlignOptions.Right]] [deleteFromTable (index,fileName) model dispatch]
                    ]
            ]
        ]
        

let fileContainer (model:Messages.Model) dispatch inputId=
    mainFunctionContainer [

        Help.help [] [
            str "Choose one or multiple files, rearrange them and add their names to the Excel sheet."
            //str " You can use "
            //u [] [str "drag'n'drop"]
            //str " to change the file order or remove files selected by accident."
        ]

        uploadButton model dispatch inputId

        if model.FilePickerState.FileNames <> [] then
            fileSortElements model dispatch

            FileNameTable.table model dispatch
            //fileNameElements model dispatch
            insertButton model dispatch
    ]

let filePickerComponent (model:Messages.Model) (dispatch:Messages.Msg -> unit) =
    let inputId = "filePicker_OnFilePickerMainFunc"
    Content.content [ ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "File Picker"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
            str "Select files from your computer and insert their names into Excel."
        ]

        // Colored container element for all uploaded file names and sort elements
        fileContainer model dispatch inputId
    ]