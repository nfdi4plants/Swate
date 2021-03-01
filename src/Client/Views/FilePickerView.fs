module FilePickerView

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
open Messages
open Update
open Shared
open Browser.Types

//let createFileList (model:Model) (dispatch: Msg -> unit) =
//    if model.FilePickerState.FileNames.Length > 0 then
//        model.FilePickerState.FileNames
//        |> List.map (fun fileName ->
//            tr [
//                colorControl model.SiteStyleState.ColorMode
//            ] [
//                td [
//                ] [
//                    Delete.delete [
//                        Delete.OnClick (fun _ -> fileName |> RemoveFileFromFileList |> FilePicker |> dispatch)
//                    ][]
//                ]
//                td [] [
//                    b [] [str fileName]
//                ]

//            ])
//    else
//        [
//            tr [] [
//                td [] [str "No Files selected."]
//            ]
//        ]

[<Literal>]
let fileTileHeight = "50px"

[<Literal>]
let fileTileHeightHalfed = "25px"

[<Literal>]
let fileElementContainerId = "File_Element_Container_DragAndDrop"

let getNewOrder dragDown dragUp dragEleOrder droppedOnEleOrder changeEleOrder = 
    if dragDown then
        if changeEleOrder < dragEleOrder
        then
            changeEleOrder
        elif changeEleOrder > droppedOnEleOrder
        then
            changeEleOrder
        elif changeEleOrder = dragEleOrder
        then
            droppedOnEleOrder
        elif changeEleOrder > dragEleOrder && changeEleOrder <= droppedOnEleOrder
        then
            changeEleOrder - 1
        else
            failwith (
                sprintf
                    "Found unknown combination for reordering list elements:
                        dragEleOrder: %i
                        droppedOnEleOrder: %i"
                    dragEleOrder droppedOnEleOrder
                )
    elif dragUp then
        if changeEleOrder < droppedOnEleOrder
        then
            changeEleOrder
        elif changeEleOrder > dragEleOrder
        then
            changeEleOrder
        elif changeEleOrder >= droppedOnEleOrder && changeEleOrder < dragEleOrder
        then
            changeEleOrder + 1
        elif changeEleOrder = dragEleOrder
        then
            droppedOnEleOrder
        else
            failwith (
                sprintf
                    "Found unknown combination for reordering list elements:
                        dragEleOrder: %i
                        droppedOnEleOrder: %i"
                    dragEleOrder droppedOnEleOrder
            )
    else
        failwith "Unknown pattern 0.2"

let createEleId id = sprintf "draggable_filePickerEle_%s" id
let createWrapperId id = sprintf "wrapper_filerPickerEle_%s" id 
let createCloneId id = sprintf "draggable_filePickerEle_%s_Clone" id

let mutable coordinates : {|x:float; y:float|} option = None
let mutable dropped: bool = true
let mutable mustUpdateModel: bool = false

let dragAndDropClone (model:Model) dispatch id =
    let cloneId = createCloneId id
    let eleId = createEleId id
    let clone() = Browser.Dom.document.getElementById(cloneId)
    let child() = Browser.Dom.document.getElementById(eleId)
    div [
        Id cloneId
        Style [
            Cursor "pointer";
            Padding "1rem 1.5rem";
            Position PositionOptions.Absolute
            Opacity "0"
            Visibility "hidden"
            PointerEvents "none"
            ZIndex 2
        ]
        Class "clone"

        OnTransitionEnd (fun eve ->
            if eve.propertyName = "top"
                then
                    let clone = clone()
                    if mustUpdateModel then
                        //printfn "trigger model reorder"
                        printfn "prev list: %A" model.FilePickerState.FileNames
                        // Update model list
                        let newList =
                            [
                                for ind,name in model.FilePickerState.FileNames do
                                    yield (
                                        let wrapperId = createWrapperId name
                                        let wrapper = Browser.Dom.document.getElementById(wrapperId)
                                        let wrapperOrder = wrapper?style?order
                                        //printfn "trigger reorder model %i -> %i,%s" ind (int wrapperOrder) name
                                        wrapper?style?order <- 0
                                        int wrapperOrder,name

                                    )
                            ] |> fun updatedOrderList ->
                                    let sortedList = List.sortBy fst updatedOrderList
                                    printfn "next list: %A" sortedList
                                    UpdateFileNames ( sortedList ) |> FilePicker |> dispatch
                        mustUpdateModel <- false
                        clone?style?opacity <- 0
                        //clone?style?visibility <- "hidden"
                        clone?style?transition <- "all 0s ease 0s"
                        child()?style?display <- "block"
                    else
                        clone?style?opacity <- 0
                        //clone?style?visibility <- "hidden"
                        clone?style?transition <- "all 0s ease 0s"
                        child()?style?display <- "block"

        )
    ][
        Delete.delete [
            Delete.Props [ Style [
                MarginRight "2rem"
            ]]
        ][]
        let fileName = model.FilePickerState.FileNames |> List.find (fun (ind,name) -> id = name) |> snd
        str (sprintf "%s" fileName)
    ]

let findIndByFileName (model:Model) id=
    model.FilePickerState.FileNames |> List.find (fun (ind,name) -> name = id) |> fst

let dragAndDropElement (model:Model) (dispatch: Msg -> unit) id =
    let eleId = createEleId id
    let wrapperId = createWrapperId id
    let cloneId = createCloneId id
    let parent() = Browser.Dom.document.getElementById(wrapperId)
    let child() = Browser.Dom.document.getElementById(eleId)
    let clone() = Browser.Dom.document.getElementById(cloneId)
    // tile
    div [
        Id eleId
        Style [
            Cursor "pointer";
            Padding "1rem 1.5rem";
            Position PositionOptions.Relative
            TextOverflow "Ellipsis"
            OverflowX OverflowOptions.Hidden
            WhiteSpace WhiteSpaceOptions.Nowrap
        ]
        Draggable true
        OnDragStart (fun eve ->
            dropped <- false
            UpdateDNDDropped false |> FilePicker |> dispatch

            eve.stopPropagation()
            let offset = child().getBoundingClientRect()
            let windowScrollY = Browser.Dom.window.scrollY
            parent()?style?height <- "0px"
            // Display none child
            child()?style?display <- "none"
            let clone = clone()
            let x = offset.left
            let y = offset.top + windowScrollY - offset.height
            clone?style?left <- sprintf "%.0fpx" x
            clone?style?top <- sprintf "%.0fpx" y
            coordinates <- Some {|x = x; y = y|}
            clone?style?opacity <- 1
            clone?style?visibility <- "unset"
            // https://www.digitalocean.com/community/tutorials/js-drag-and-drop-vanilla-js
            let set =
                eve
                    .dataTransfer
                    .setData("text/plain", id)
            ()
        )
        OnDragOver(fun e -> e.preventDefault())
        OnDrag (fun eve ->
            let clone = clone()
            let offset = clone.getBoundingClientRect()
            let x = eve.pageX - (offset.width * 0.5)
            let y = eve.pageY - (1.5 * offset.height)
            clone?style?left <- sprintf "%.0fpx" x
            clone?style?top <- sprintf "%.0fpx" y
        )
        OnDragEnter (fun eve ->
            eve.stopPropagation()
            eve.preventDefault()
            //eve.target?style?backgroundColor <- "lightgrey"
            //eve.target?style?borderBottom <- "5px solid darkgrey"
            parent()?style?backgroundColor <- "lightgrey"
            parent()?style?borderBottom <- "5px solid darkgrey"
        )
        OnDragLeave (fun eve ->
            eve.preventDefault()
            //eve.target?style?backgroundColor <- ExcelColors.colorfullMode.BodyBackground
            //eve.target?style?borderBottom <- "0px solid darkgrey"
            parent()?style?backgroundColor <- ExcelColors.colorfullMode.BodyBackground
            parent()?style?borderBottom <- "0px solid darkgrey"
        )
        OnDragEnd (fun eve ->
            // restore wrapper 
            parent()?style?height <- fileTileHeight
            let slideClone =
                if coordinates.IsNone then failwith "Unknown Drag and Drop pattern 0.2"
                if dropped then
                    ()
                else
                    let clone = clone()
                    clone?style?transition  <- "0.5s ease"
                    clone?style?left        <- sprintf "%.0fpx" coordinates.Value.x
                    clone?style?top         <- sprintf "%.0fpx" coordinates.Value.y
                    coordinates <- None
                    dropped <- true
                    UpdateDNDDropped true |> FilePicker |> dispatch
            ()
        )
        OnDrop (fun eve ->
            //eve.stopPropagation()
            eve.preventDefault()
            dropped <- true
            UpdateDNDDropped true |> FilePicker |> dispatch
            //eve.target?style?backgroundColor <- ExcelColors.colorfullMode.BodyBackground
            //eve.target?style?borderBottom <- "0px solid darkgrey"
            parent()?style?backgroundColor <- ExcelColors.colorfullMode.BodyBackground
            parent()?style?borderBottom <- "0px solid darkgrey"

            let prevId      = eve.dataTransfer.getData("text")
            let prevEle     = Browser.Dom.document.getElementById(createEleId prevId)
            let prevWrapper = Browser.Dom.document.getElementById(createWrapperId prevId)
            let prevClone   = Browser.Dom.document.getElementById(createCloneId prevId)
            //printfn "prev id: %i" prevId
            //let dragEleOrder    = prevWrapper?style?order
            let dragEleOrder = findIndByFileName model prevId
            let dragDown     = dragEleOrder < findIndByFileName model id //parent()?style?order
            let dragUp       = dragEleOrder > findIndByFileName model id //parent()?style?order
            //printfn "up: %b, down: %b" dragUp dragDown

            let droppenOnEleOrder     =
                if dragDown then
                    //parent()?style?order
                    findIndByFileName model id 
                elif dragUp then
                    let pOrder = findIndByFileName model id  //parent()?style?order
                    (int pOrder) + 1
                else failwith "Unknown Pattern 0.1"

            let updateOrder =
                for ind,fileName in model.FilePickerState.FileNames do
                    let w = Browser.Dom.document.getElementById(createWrapperId fileName)
                    w?style?order <- ind
                    let changeEleOrder = ind //w?style?order
                    let newOrder =
                        getNewOrder dragDown dragUp dragEleOrder droppenOnEleOrder changeEleOrder
                    //printfn "dragEleOrder %i, droppedOnEleOrder %i, changeEleOrder %i, newOrder: %i" dragEleOrder droppenOnEleOrder changeEleOrder newOrder
                    w?style?order <- newOrder
                    //printfn "trigger reorderList for: %i -> %i,%s" ind newOrder fileName

            mustUpdateModel <- true
            prevWrapper?style?height <- fileTileHeight

            let cloneSlide =
                let offset = prevWrapper.getBoundingClientRect()
                let windowScrollY = Browser.Dom.window.scrollY
                let clone = prevClone

                let x = offset.left
                let y = offset.top + windowScrollY - 50.
                clone?style?transition <- "0.5s ease"
                clone?style?left <- sprintf "%.0fpx" x
                clone?style?top <- sprintf "%.0fpx" y
            ()
        )
    ][
        Delete.delete [
            Delete.OnClick (fun _ ->
                let newList =
                    model.FilePickerState.FileNames
                    |> List.sortBy fst
                    |> List.map snd
                    |> List.filter (fun name -> name <> id)
                    |> List.mapi (fun i name -> i+1,name)
                newList |> UpdateFileNames |> FilePicker |> dispatch
            )
            Delete.Props [ Style [
                if dropped = false then PointerEvents "none"
                MarginRight "2rem"
            ]]
        ][]
        str (sprintf "%s" id)
        Icon.icon [Icon.Props [Style [Float FloatOptions.Right; Color "darkgrey"]]][
            Fa.i [ Fa.Solid.ArrowsAlt][]
        ]
    ]
    

let fileElement (model:Model) dispatch (id:string) =
    let wrapperId   = createWrapperId id
    let order       = model.FilePickerState.FileNames |> List.find (fun (ind,name) -> name = id) |> fst
    // wrapper
    // https://codepen.io/osublake/pen/XJQKVX
    div [
        Id wrapperId
        Style [
            Height fileTileHeight
            Transition "0.5s height ease"
            Order 0
        ]
        OnDragOver(fun e -> e.preventDefault())
    ][
        dragAndDropElement model dispatch id
    ]

let placeOnTopElement model dispatch =
    div [
        OnDragOver(fun e -> e.preventDefault())
        OnDragEnter (fun eve ->
            eve.stopPropagation()
            eve.preventDefault()
            eve.target?style?borderBottom <- "2px solid darkgrey")
        OnDragLeave (fun eve ->
            eve.preventDefault()
            eve.target?style?borderBottom <- "2px solid white" ) //(sprintf "2px solid %s" ExcelColors.colorfullMode.BodyBackground) )
        OnDrop (fun eve ->
            eve.preventDefault()
            eve.target?style?borderBottom <- "2px solid white" //(sprintf "2px solid %s" ExcelColors.colorfullMode.BodyBackground)
            dropped <- true
            UpdateDNDDropped true |> FilePicker |> dispatch
            let prevId      = eve.dataTransfer.getData("text")
            let prevWrapper = Browser.Dom.document.getElementById(createWrapperId prevId)
            let prevClone   = Browser.Dom.document.getElementById(createCloneId prevId)
            //printfn "prev id: %i" prevId

            // always position at position 1
            let droppedOnEleOrder = 1
            let dragEleOrder = findIndByFileName model prevId
            //printfn "up: %b, down: %b" dragUp dragDown

            let updateOrder =
                for ind,fileName in model.FilePickerState.FileNames do
                    let w = Browser.Dom.document.getElementById(createWrapperId fileName)
                    w?style?order <- ind
                    let changeEleOrder = ind //w?style?order
                    let newOrder =
                        getNewOrder false true dragEleOrder droppedOnEleOrder changeEleOrder
                    //printfn "dragEleOrder %i, droppedOnEleOrder %i, changeEleOrder %i, newOrder: %i" dragEleOrder droppenOnEleOrder changeEleOrder newOrder
                    w?style?order <- newOrder
                    //printfn "trigger reorderList for: %i -> %i,%s" ind newOrder fileName

            mustUpdateModel <- true
            prevWrapper?style?height <- fileTileHeight

            let cloneSlide =
                let offset = prevWrapper.getBoundingClientRect()
                let windowScrollY = Browser.Dom.window.scrollY
                let clone = prevClone

                let x = offset.left
                let y = offset.top + windowScrollY - 50.
                clone?style?transition <- "0.5s ease"
                clone?style?left <- sprintf "%.0fpx" x
                clone?style?top <- sprintf "%.0fpx" y
            ()
        )
        Style [
            Height fileTileHeightHalfed
            Order "-1"
            BorderBottom "2px solid white"
        ]
    ][
    ]
    
let fileElementContainer (model:Model) dispatch =
    div [
        Style [Display DisplayOptions.Flex; FlexDirection "column"]
        Id fileElementContainerId
    ][
        yield
            placeOnTopElement model dispatch
        for ind,ele in model.FilePickerState.FileNames do
            yield 
                fileElement model dispatch (ele)
            yield
                dragAndDropClone model dispatch (ele)
    ]

let uploadButton (model:Model) dispatch inputId =
    File.file [
        File.Color IsInfo
        File.IsCentered
        File.Props [Style [Margin "1rem 0"]]
    ] [
        File.label [ Props [Style [Width "100%"]] ] [
            File.input [
                Props [
                    Id inputId
                    Multiple true
                    OnChange (fun ev ->

                        let files : FileList = ev.target?files
                    
                        let fileNames =
                            [ for i=0 to (files.length - 1) do yield files.item i ]
                            |> List.map (fun f -> f.name)

                        fileNames |> LoadNewFiles |> FilePicker |> dispatch

                        let picker = Browser.Dom.document.getElementById(inputId)
                        // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                        picker?value <- null
                    )
                ]
            ]
            File.cta [
                Props [Style [Width "100%"; JustifyContent "center" ]]
            ] [
                File.icon [] [ Fa.i [ Fa.Solid.Upload ] [] ]
                File.label [ Props [
                    OnClick (fun e ->
                        let getUploadElement = Browser.Dom.document.getElementById inputId
                        getUploadElement.click()
                    )
                ] ] [ str "Pick file names" ]
            ]
        ]
    ]


let fileNameElements (model:Model) dispatch =
    div [ ][
        if model.FilePickerState.FileNames <> [] then
            fileElementContainer model dispatch

            Button.a [
                Button.IsFullWidth
                if model.FilePickerState.FileNames |> List.isEmpty then
                    yield! [
                        Button.Disabled true
                        Button.IsActive false
                        Button.Color Color.IsDanger
                    ]
                else
                    Button.Color Color.IsSuccess
                Button.OnClick (fun e ->
                    InsertFileNames (model.FilePickerState.FileNames |> List.map snd) |> ExcelInterop |> dispatch 
                )

            ][
                str "Insert File Names"
            ]
        else
            div [][
                str "All names from your selected files will be displayed here."
            ]
    ]

let sortButton icon msg =
    Button.a [
        Button.IsOutlined
        Button.Color IsPrimary
        Button.OnClick msg
    ][
        Fa.i [ Fa.Size Fa.FaLarge; icon ] [ ] 
    ]

let fileSortElements (model:Model) dispatch =
    div [Style [MarginBottom "1rem"; Display DisplayOptions.Flex]][
        Button.a [
            Button.IsOutlined
            Button.Color IsPrimary
            Button.OnClick (fun e ->
                let txt = model.FilePickerState.FileNames |> List.map snd |> String.concat System.Environment.NewLine
                let textArea = Browser.Dom.document.createElement "textarea"
                textArea?value <- txt
                textArea?style?top <- "0"
                textArea?style?left <- "0"
                textArea?style?position <- "fixed"

                Browser.Dom.document.body.appendChild textArea |> ignore

                textArea.focus()
                /// Can't belive this actually worked
                textArea?select()

                let t = Browser.Dom.document.execCommand("copy")
                Browser.Dom.document.body.removeChild(textArea) |> ignore
                ()
            )
        ][
            Fa.i [Fa.Props [Title "Copy to Clipboard"]; Fa.Regular.Clipboard ] [] 
        ]

        Button.list [
            Button.List.HasAddons
            Button.List.Props [Style [MarginLeft "auto"]]
        ][
            sortButton Fa.Solid.SortAlphaDown (fun e ->
                let sortedList = model.FilePickerState.FileNames |> List.sortBy snd |> List.mapi (fun i x -> i,snd x)
                UpdateFileNames sortedList |> FilePicker |> dispatch
            )
            sortButton Fa.Solid.SortAlphaDownAlt (fun e ->
                let sortedList = model.FilePickerState.FileNames |> List.sortByDescending snd |> List.mapi (fun i x -> i,snd x)
                UpdateFileNames sortedList |> FilePicker |> dispatch
            )
        ]
    ]

let fileContainer (model:Model) dispatch inputId=
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            //BorderRadius "15px 15px 0 0"
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
        fileSortElements model dispatch

        fileNameElements model dispatch
    ]

let filePickerComponent (model:Model) (dispatch:Msg -> unit) =
    let inputId = "filePicker_OnFilePickerMainFunc"
    Content.content [ ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "File Picker"]

        Help.help [][
            b [] [ str "Choose one or multiple files, rearrange them and add their names to the Excel sheet."]
            str " You can use "
            u [][str "drag'n'drop"]
            str " to change the file order or remove files selected by accident."
        ]

        uploadButton model dispatch inputId

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
            str "Select files from your computer and insert their names into Excel."
        ]

        /// COlored container element for all uploaded file names and sort elements
        fileContainer model dispatch inputId
        
    ]