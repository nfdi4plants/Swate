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


//[<Literal>]
//let fileTileHeight = "50px"

//[<Literal>]
//let fileTileHeightHalfed = "25px"

//[<Literal>]
//let fileElementContainerId = "File_Element_Container_DragAndDrop"

//let getNewOrder dragDown dragUp dragEleOrder droppedOnEleOrder changeEleOrder = 
//    if dragDown then
//        if changeEleOrder < dragEleOrder
//        then
//            changeEleOrder
//        elif changeEleOrder > droppedOnEleOrder
//        then
//            changeEleOrder
//        elif changeEleOrder = dragEleOrder
//        then
//            droppedOnEleOrder
//        elif changeEleOrder > dragEleOrder && changeEleOrder <= droppedOnEleOrder
//        then
//            changeEleOrder - 1
//        else
//            failwith (
//                sprintf
//                    "Found unknown combination for reordering list elements:
//                        dragEleOrder: %i
//                        droppedOnEleOrder: %i"
//                    dragEleOrder droppedOnEleOrder
//                )
//    elif dragUp then
//        if changeEleOrder < droppedOnEleOrder
//        then
//            changeEleOrder
//        elif changeEleOrder > dragEleOrder
//        then
//            changeEleOrder
//        elif changeEleOrder >= droppedOnEleOrder && changeEleOrder < dragEleOrder
//        then
//            changeEleOrder + 1
//        elif changeEleOrder = dragEleOrder
//        then
//            droppedOnEleOrder
//        else
//            failwith (
//                sprintf
//                    "Found unknown combination for reordering list elements:
//                        dragEleOrder: %i
//                        droppedOnEleOrder: %i"
//                    dragEleOrder droppedOnEleOrder
//            )
//    else
//        failwith "Unknown pattern 0.2"

//let createEleId id = sprintf "draggable_filePickerEle_%s" id
//let createWrapperId id = sprintf "wrapper_filerPickerEle_%s" id 
//let createCloneId id = sprintf "draggable_filePickerEle_%s_Clone" id

//let mutable coordinates : {|x:float; y:float|} option = None
//let mutable dropped: bool = true
//let mutable mustUpdateModel: bool = false

//let dragAndDropClone (model:Messages.Model) dispatch id =
//    let cloneId = createCloneId id
//    let eleId = createEleId id
//    let clone() = Browser.Dom.document.getElementById(cloneId)
//    let child() = Browser.Dom.document.getElementById(eleId)
//    div [
//        Id cloneId
//        Style [
//            Cursor "pointer";
//            Padding "1rem 1.5rem";
//            Position PositionOptions.Absolute
//            Opacity "0"
//            Visibility "hidden"
//            PointerEvents "none"
//            ZIndex 2
//        ]
//        Class "clone"

//        OnTransitionEnd (fun eve ->
//            if eve.propertyName = "top"
//                then
//                    let clone = clone()
//                    if mustUpdateModel then
//                        // Update model list
//                        let newList =
//                            [
//                                for ind,name in model.FilePickerState.FileNames do
//                                    yield (
//                                        let wrapperId = createWrapperId name
//                                        let wrapper = Browser.Dom.document.getElementById(wrapperId)
//                                        let wrapperOrder = wrapper?style?order
//                                        //printfn "trigger reorder model %i -> %i,%s" ind (int wrapperOrder) name
//                                        wrapper?style?order <- 0
//                                        int wrapperOrder,name

//                                    )
//                            ] |> fun updatedOrderList ->
//                                    let sortedList = List.sortBy fst updatedOrderList
//                                    UpdateFileNames ( sortedList ) |> FilePickerMsg |> dispatch
//                        mustUpdateModel <- false
//                        clone?style?opacity <- 0
//                        //clone?style?visibility <- "hidden"
//                        clone?style?transition <- "all 0s ease 0s"
//                        child()?style?opacity <- "1"
//                    else
//                        clone?style?opacity <- 0
//                        //clone?style?visibility <- "hidden"
//                        clone?style?transition <- "all 0s ease 0s"
//                        child()?style?opacity <- "1"
//        )
//        OnDragOver(fun e -> e.preventDefault())
//    ][
//        Delete.delete [
//            Delete.Props [ Style [
//                MarginRight "2rem"
//            ]]
//        ][]
//        let fileName = model.FilePickerState.FileNames |> List.find (fun (ind,name) -> id = name) |> snd
//        str (sprintf "%s" fileName)
//    ]

//let findIndByFileName (model:Messages.Model) id =
//    model.FilePickerState.FileNames |> List.find (fun (ind,name) -> name = id) |> fst

//let dragAndDropElement (model:Messages.Model) (dispatch: Messages.Msg -> unit) id =
//    let eleId = createEleId id
//    let wrapperId = createWrapperId id
//    let cloneId = createCloneId id
//    let parent() = Browser.Dom.document.getElementById(wrapperId)
//    let child() = Browser.Dom.document.getElementById(eleId)
//    let clone() = Browser.Dom.document.getElementById(cloneId)
//    // tile
//    div [
//        Id eleId
//        Style [
//            Cursor "pointer";
//            Padding "1rem 1.5rem";
//            Position PositionOptions.Relative
//            TextOverflow "Ellipsis"
//            OverflowX OverflowOptions.Hidden
//            WhiteSpace WhiteSpaceOptions.Nowrap
//        ]
//        Draggable true
//        OnDragStart (fun eve ->
//            dropped <- false
//            eve.stopPropagation()
//            let offset = child().getBoundingClientRect()
//            let windowScrollY = Browser.Dom.window.scrollY
//            parent()?style?height <- "0px"
//            // display stopped working, so we use opacity now.
//            child()?style?opacity <- "0"
//            let clone = clone()
//            let x = offset.left
//            let y = offset.top + windowScrollY - offset.height
//            clone?style?left <- sprintf "%.0fpx" x
//            clone?style?top <- sprintf "%.0fpx" y
//            coordinates <- Some {|x = x; y = y|}
//            clone?style?opacity <- 1
//            clone?style?visibility <- "unset"
//            // https://www.digitalocean.com/community/tutorials/js-drag-and-drop-vanilla-js
//            let set =
//                eve
//                    .dataTransfer
//                    .setData("text/plain", id)
//            ()
//        )
//        OnDragOver(fun e -> e.preventDefault())
//        OnDrag (fun eve ->
//            let clone = clone()
//            let offset = clone.getBoundingClientRect()
//            let x = eve.pageX - (offset.width * 0.5)
//            let y = eve.pageY - (1.5 * offset.height)
//            clone?style?left <- sprintf "%.0fpx" x
//            clone?style?top <- sprintf "%.0fpx" y
//        )
//        OnDragEnter (fun eve ->
//            eve.stopPropagation()
//            eve.preventDefault()
//            //eve.target?style?backgroundColor <- "lightgrey"
//            //eve.target?style?borderBottom <- "5px solid darkgrey"
//            parent()?style?backgroundColor <- model.SiteStyleState.ColorMode.ControlForeground
//            parent()?style?borderBottom <- "5px solid darkgrey"
//        )
//        OnDragLeave (fun eve ->
//            eve.preventDefault()
//            //eve.target?style?backgroundColor <- ExcelColors.colorfullMode.BodyBackground
//            //eve.target?style?borderBottom <- "0px solid darkgrey"
//            parent()?style?backgroundColor <- model.SiteStyleState.ColorMode.BodyBackground
//            parent()?style?borderBottom <- "0px solid darkgrey"
//        )
//        OnDragEnd (fun eve ->
//            // restore wrapper 
//            parent()?style?height <- fileTileHeight
//            let slideClone =
//                if coordinates.IsNone then failwith "Unknown Drag and Drop pattern 0.2"
//                if dropped then
//                    ()
//                else
//                    let clone = clone()
//                    clone?style?transition  <- "0.5s ease"
//                    clone?style?left        <- sprintf "%.0fpx" coordinates.Value.x
//                    clone?style?top         <- sprintf "%.0fpx" coordinates.Value.y
//                    coordinates <- None
//                    dropped <- true
//            ()
//        )
//        OnDrop (fun eve ->
//            //eve.stopPropagation()
//            eve.preventDefault()
//            dropped <- true
//            parent()?style?backgroundColor  <- model.SiteStyleState.ColorMode.BodyBackground
//            parent()?style?borderBottom     <- "0px solid darkgrey"

//            let prevId      = eve.dataTransfer.getData("text")
//            let prevEle     = Browser.Dom.document.getElementById(createEleId prevId)
//            let prevWrapper = Browser.Dom.document.getElementById(createWrapperId prevId)
//            let prevClone   = Browser.Dom.document.getElementById(createCloneId prevId)
//            let dragEleOrder = findIndByFileName model prevId
//            let dragDown     = dragEleOrder < findIndByFileName model id //parent()?style?order
//            let dragUp       = dragEleOrder > findIndByFileName model id //parent()?style?order

//            let droppenOnEleOrder     =
//                if dragDown then
//                    //parent()?style?order
//                    findIndByFileName model id 
//                elif dragUp then
//                    let pOrder = findIndByFileName model id  //parent()?style?order
//                    (int pOrder) + 1
//                else failwith "Unknown Pattern 0.1"

//            let updateOrder =
//                for ind,fileName in model.FilePickerState.FileNames do
//                    let w = Browser.Dom.document.getElementById(createWrapperId fileName)
//                    w?style?order <- ind
//                    let changeEleOrder = ind //w?style?order
//                    let newOrder =
//                        getNewOrder dragDown dragUp dragEleOrder droppenOnEleOrder changeEleOrder
//                    //printfn "dragEleOrder %i, droppedOnEleOrder %i, changeEleOrder %i, newOrder: %i" dragEleOrder droppenOnEleOrder changeEleOrder newOrder
//                    w?style?order <- newOrder
//                    //printfn "trigger reorderList for: %i -> %i,%s" ind newOrder fileName

//            mustUpdateModel <- true
//            prevWrapper?style?height <- fileTileHeight

//            let cloneSlide =
//                let offset = prevWrapper.getBoundingClientRect()
//                let windowScrollY = Browser.Dom.window.scrollY
//                let clone = prevClone

//                let x = offset.left
//                let y = offset.top + windowScrollY - 50.
//                clone?style?transition <- "0.5s ease"
//                clone?style?left <- sprintf "%.0fpx" x
//                clone?style?top <- sprintf "%.0fpx" y
//            ()
//        )
//    ][
//        Delete.delete [
//            Delete.OnClick (fun _ ->
//                let newList =
//                    model.FilePickerState.FileNames
//                    |> List.sortBy fst
//                    |> List.map snd
//                    |> List.filter (fun name -> name <> id)
//                    |> List.mapi (fun i name -> i+1,name)
//                newList |> UpdateFileNames |> FilePickerMsg |> dispatch
//            )
//            Delete.Props [ Style [
//                if dropped = false then PointerEvents "none"
//                MarginRight "2rem"
//            ]]
//        ][]
//        str (sprintf "%s" id)
//        Icon.icon [Icon.Props [Style [Float FloatOptions.Right; Color "darkgrey"]]][
//            Fa.i [ Fa.Solid.ArrowsAlt][]
//        ]
//    ]
    

//let fileElement (model:Messages.Model) dispatch (id:string) =
//    let wrapperId   = createWrapperId id
//    let order       = model.FilePickerState.FileNames |> List.find (fun (ind,name) -> name = id) |> fst
//    // wrapper
//    // https://codepen.io/osublake/pen/XJQKVX
//    div [
//        Id wrapperId
//        Style [
//            Height fileTileHeight
//            Transition "0.5s height ease"
//            Order 0
//        ]
//        OnDragOver(fun e -> e.preventDefault())
//    ][
//        dragAndDropElement model dispatch id
//    ]

//let placeOnTopElement model dispatch =
//    div [
//        OnDragOver(fun e -> e.preventDefault())
//        OnDragEnter (fun eve ->
//            eve.stopPropagation()
//            eve.preventDefault()
//            eve.target?style?borderBottom <- "2px solid darkgrey")
//        OnDragLeave (fun eve ->
//            eve.preventDefault()
//            eve.target?style?borderBottom <- "2px solid white" ) //(sprintf "2px solid %s" ExcelColors.colorfullMode.BodyBackground) )
//        OnDrop (fun eve ->
//            eve.preventDefault()
//            eve.target?style?borderBottom <- "2px solid white" //(sprintf "2px solid %s" ExcelColors.colorfullMode.BodyBackground)
//            dropped <- true
//            let prevId      = eve.dataTransfer.getData("text")
//            let prevWrapper = Browser.Dom.document.getElementById(createWrapperId prevId)
//            let prevClone   = Browser.Dom.document.getElementById(createCloneId prevId)
//            //printfn "prev id: %i" prevId

//            // always position at position 1
//            let droppedOnEleOrder = 1
//            let dragEleOrder = findIndByFileName model prevId
//            //printfn "up: %b, down: %b" dragUp dragDown

//            let updateOrder =
//                for ind,fileName in model.FilePickerState.FileNames do
//                    let w = Browser.Dom.document.getElementById(createWrapperId fileName)
//                    w?style?order <- ind
//                    let changeEleOrder = ind //w?style?order
//                    let newOrder =
//                        getNewOrder false true dragEleOrder droppedOnEleOrder changeEleOrder
//                    //printfn "dragEleOrder %i, droppedOnEleOrder %i, changeEleOrder %i, newOrder: %i" dragEleOrder droppenOnEleOrder changeEleOrder newOrder
//                    w?style?order <- newOrder
//                    //printfn "trigger reorderList for: %i -> %i,%s" ind newOrder fileName

//            mustUpdateModel <- true
//            prevWrapper?style?height <- fileTileHeight

//            let cloneSlide =
//                let offset = prevWrapper.getBoundingClientRect()
//                let windowScrollY = Browser.Dom.window.scrollY
//                let clone = prevClone

//                let x = offset.left
//                let y = offset.top + windowScrollY - 50.
//                clone?style?transition <- "0.5s ease"
//                clone?style?left <- sprintf "%.0fpx" x
//                clone?style?top <- sprintf "%.0fpx" y
//            ()
//        )
//        Style [
//            Height fileTileHeightHalfed
//            Order "-1"
//            BorderBottom "2px solid white"
//        ]
//    ][]
    
//let fileElementContainer (model:Messages.Model) dispatch =
//    div [
//        Style [Display DisplayOptions.Flex; FlexDirection "column"]
//        Id fileElementContainerId
//    ][
//        yield
//            placeOnTopElement model dispatch
//        for ind,ele in model.FilePickerState.FileNames do
//            yield 
//                fileElement model dispatch (ele)
//            yield
//                dragAndDropClone model dispatch (ele)
//    ]

let uploadButton (model:Messages.Model) dispatch inputId =
    Field.div [][
        input [
            Style [Display DisplayOptions.None]
            Id inputId
            Multiple true
            Type "file"
            OnChange (fun ev ->
                let files :FileList = ev.target?files

                let fileNames =
                    [ for i=0 to (files.length - 1) do yield files.item i ]
                    |> List.map (fun f -> f.name)

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
        ][
            str "Pick file names"
        ]
    ]


//let fileNameElements (model:Messages.Model) dispatch =
//    div [ ][
//        if model.FilePickerState.FileNames <> [] then
//            fileElementContainer model dispatch

//            Button.a [
//                Button.IsFullWidth
//                if model.FilePickerState.FileNames |> List.isEmpty then
//                    yield! [
//                        Button.Disabled true
//                        Button.IsActive false
//                        Button.Color Color.IsDanger
//                    ]
//                else
//                    Button.Color Color.IsSuccess
//                Button.OnClick (fun e ->
//                    OfficeInterop.InsertFileNames (model.FilePickerState.FileNames |> List.map snd) |> OfficeInteropMsg |> dispatch 
//                )

//            ][
//                str "Insert File Names"
//            ]
//        else
//            div [][
//                str "All names from your selected files will be displayed here."
//            ]
//    ]

let sortButton icon msg =
    Button.a [
        Button.Color IsInfo
        Button.OnClick msg
    ][
        Fa.i [ Fa.Size Fa.FaLarge; icon ] [ ] 
    ]

let fileSortElements (model:Messages.Model) dispatch =
    Field.div [][
        Button.list [][
            Button.a [
                Button.Props [Title "Copy to Clipboard"]
                Button.Color IsInfo
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
                    /// Can't belive this actually worked
                    textArea?select()

                    let t = Browser.Dom.document.execCommand("copy")
                    Browser.Dom.document.body.removeChild(textArea) |> ignore
                    ()
                )
            ][
                CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_filepicker" Fa.Regular.Clipboard Fa.Solid.Check
            ]

            Button.list [
                Button.List.HasAddons
                Button.List.Props [Style [MarginLeft "auto"]]
            ][
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
        ][]

    let moveUpButton (id,fileName) (model:Model) dispatch =
        Button.a [
            Button.OnClick (fun _ ->
                let sortedList =
                    model.FilePickerState.FileNames
                    |> List.map (fun (iterInd,iterFileName) ->
                        let isNameToMove = (id,fileName) = (iterInd,iterFileName)
                        if isNameToMove then
                            /// if the iterated element is the one we want to move, substract 1.5 from it
                            /// let sortBy handle all stuff then assign new indices with mapi
                            (float iterInd-1.5,iterFileName)
                        else
                            (float iterInd,iterFileName)
                    )
                    |> List.sortBy fst
                    |> List.mapi (fun i v -> i+1, snd v)
                UpdateFileNames sortedList |> FilePickerMsg |> dispatch
            )
            Button.Size IsSmall
        ][
            Fa.i [Fa.Solid.ArrowUp][]
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
                            /// if the iterated element is the one we want to move, add 1.5 from it
                            /// let sortBy handle all stuff then assign new indices with mapi
                            (float iterInd+1.5,iterFileName)
                        else
                            (float iterInd,iterFileName)
                    )
                    |> List.sortBy fst
                    |> List.mapi (fun i v -> i+1, snd v)
                UpdateFileNames sortedList |> FilePickerMsg |> dispatch
            )
        ][
            Fa.i [Fa.Solid.ArrowDown][]
        ]

    let moveButtonList (id,fileName) (model:Model) dispatch =
        Button.list [][
            moveUpButton (id,fileName) model dispatch
            moveDownButton (id,fileName) model dispatch
        ]
        

    let table (model:Messages.Model) dispatch =
        Table.table [
            Table.IsHoverable
            Table.IsStriped
        ][
            tbody [][
                for index,fileName in model.FilePickerState.FileNames do
                    tr [][
                        td [][b [][str $"{index}"]]
                        td [][str fileName]
                        td [][moveButtonList (index,fileName) model dispatch]
                        td [Style [TextAlign TextAlignOptions.Right]][deleteFromTable (index,fileName) model dispatch]
                    ]
            ]
        ]
        

let fileContainer (model:Messages.Model) dispatch inputId=
    mainFunctionContainer [

        Help.help [][
            str "Choose one or multiple files, rearrange them and add their names to the Excel sheet."
            //str " You can use "
            //u [][str "drag'n'drop"]
            //str " to change the file order or remove files selected by accident."
        ]

        uploadButton model dispatch inputId

        if model.FilePickerState.FileNames <> [] then
            fileSortElements model dispatch

            FileNameTable.table model dispatch
            //fileNameElements model dispatch
    ]

let filePickerComponent (model:Messages.Model) (dispatch:Messages.Msg -> unit) =
    let inputId = "filePicker_OnFilePickerMainFunc"
    Content.content [ ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "File Picker"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
            str "Select files from your computer and insert their names into Excel."
        ]

        /// COlored container element for all uploaded file names and sort elements
        fileContainer model dispatch inputId
    ]