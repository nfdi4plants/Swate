module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Json

module OfficeInterop =
    open Fable.Core
    open Fable.Core.JsInterop
    open OfficeJS
    open Excel
    open System.Collections.Generic
    
    [<Global>]
    let Office : Office.IExports = jsNative

    [<Global>]
    //[<CompiledName("Office.Excel")>]
    let Excel : Excel.IExports = jsNative

    [<Global>]
    let RangeLoadOptions : Interfaces.RangeLoadOptions = jsNative

    [<Emit("console.log($0)")>]
    let consoleLog (message: string): unit = jsNative
    
    let exampleExcelFunction () =
        Excel.run(fun context ->
            let ranges = context.workbook.getSelectedRanges()
            ranges.format.fill.color <- "red"
            let x = ranges.load(U2.Case1 "address")
            context.sync().``then``(
                fun _ -> x.address
            )
        )

    let createEmptyMatrixForTables (colCount:int) (rowCount:int) value =
        [|
            for i in 0 .. rowCount-1 do
                yield   [|
                            for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
                        |] :> IList<U3<bool,string,float>>
        |] :> IList<IList<U3<bool,string,float>>>

    let createAnnotationTable (isDark:bool) =
        Excel.run(fun context ->
            let tableRange = context.workbook.getSelectedRange()
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            //delete table with the same name if present because there can only be one chosen one <3
            sheet.tables.getItemOrNullObject("annotationTable").delete()

            let annotationTable = sheet.tables.add(U2.Case1 tableRange,true)
            annotationTable.name <- "annotationTable"

            tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount";"address"]))) |> ignore
            annotationTable.load(U2.Case1 "style") |> ignore

            //sync with proxy objects after loading values from excel   
            context.sync()
                .``then``( fun _ ->

                    let style =
                        if isDark then
                            "TableStyleMedium15"
                        else
                            "TableStyleMedium7"

                    annotationTable.style <- style

                    if tableRange.columnCount < 2. then // only one column there, so add data col to end.

                        let dataCol = createEmptyMatrixForTables 1 (int tableRange.rowCount) ""

                        (annotationTable.columns.getItemAt 0.).name <- "Sample Name"
                        annotationTable.columns.add(-1.,U4.Case1 dataCol, "Data File Name") |> ignore

                        sheet.getUsedRange().format.autofitColumns()
                        sheet.getUsedRange().format.autofitRows()

                        sprintf "Annotation Table created in [%s] with dimensions %.0f + 1 mandatory c x (%.0f + 1h)r" tableRange.address tableRange.columnCount (tableRange.rowCount - 1.) 
                    else

                        (annotationTable.columns.getItemAt 0.).name <- "Sample Name"
                        (annotationTable.columns.getItemAt (tableRange.columnCount - 1.)).name <- "Data File Name"

                        sheet.getUsedRange().format.autofitColumns()
                        sheet.getUsedRange().format.autofitRows()

                        sprintf "Annotation Table created in [%s] with dimensions %.0fc x (%.0f + 1h)r. Adapted style to %s" tableRange.address tableRange.columnCount (tableRange.rowCount - 1.) style
                        
                    )
                //.catch (fun e -> e |> unbox<System.Exception> |> fun x -> x.Message)
        )



    let addAnnotationColumn (colName:string) =
        Excel.run(fun context ->
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem("annotationTable")

            let tableRange = annotationTable.getRange()
            tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"]))) |> ignore

            context.sync().``then``( fun _ ->
                let colCount = tableRange.columnCount
                let rowCount = tableRange.rowCount |> int
                let testCol = createEmptyMatrixForTables 1 rowCount ""

                let _ =
                    annotationTable.columns.add(
                        colCount - 1., //last column should always be the predefined results column
                        values = U4.Case1 testCol, name=colName
                    )
                sprintf "%s column was added." colName
            )
        )

    let fillValue (v:string) =
        Excel.run(fun context ->
            let range = context.workbook.getSelectedRange()
            let _ = range.load(U2.Case2 (ResizeArray(["address";"values"])))

            //sync with proxy objects after loading values from excel
            context.sync().``then``( fun _ ->
                let newVals = ResizeArray([
                    for arr in range.values do
                        let tmp = arr |> Seq.map (fun _ -> Some (v |> box))
                        ResizeArray(tmp)
                ])
                range.values <- newVals
                sprintf "%s filled with %s" range.address v
            )
        )

    let getTableMetaData () =
        Excel.run (fun context ->
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem("annotationTable")
            annotationTable.columns.load(propertyNames = U2.Case1 "count") |> ignore
            annotationTable.rows.load(propertyNames = U2.Case1 "count")    |> ignore
            let rowRange = annotationTable.getRange()
            rowRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore
            let headerRange = annotationTable.getHeaderRowRange()
            headerRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore

            context.sync().``then``(fun _ ->
                let colCount,rowCount = annotationTable.columns.count, annotationTable.rows.count
                let rowRangeAddr, rowRangeColCount, rowRangeRowCount = rowRange.address,rowRange.columnCount,rowRange.rowCount
                let headerRangeAddr, headerRangeColCount, headerRangeRowCount = headerRange.address,headerRange.columnCount,headerRange.rowCount

                sprintf "Table Metadata: [Table] : %ic x %ir ; [TotalRange] : %s : %ic x %ir ; [HeaderRowRange] : %s : %ic x %ir "
                    (colCount            |> int)
                    (rowCount            |> int)
                    (rowRangeAddr.Replace("Sheet",""))
                    (rowRangeColCount    |> int)
                    (rowRangeRowCount    |> int)
                    (headerRangeAddr.Replace("Sheet",""))
                    (headerRangeColCount |> int)
                    (headerRangeRowCount |> int)
            )
        )

    let syncContext (passthroughMessage : string) =
        Excel.run (fun context -> context.sync(passthroughMessage))

module ExcelColors =
    //https://developer.microsoft.com/en-us/fluentui#/styles/web/colors/products
    module Excel =

        let Shade20    = "#004b1c"
        let Shade10    = "#0e5c2f"
        let Primary    = "#217346"
        let Tint10     = "#3f8159"
        let Tint20     = "#4e9668"
        let Tint30     = "#6eb38a"
        let Tint40     = "#9fcdb3"
        let Tint50     = "#e9f5ee"

    module Colorfull =

        let gray180 = "#252423"
        let gray140 = "#484644"
        let gray130 = "#605e5c"
        let gray80  = "#b3b0ad"
        let gray60  = "#c8c6c4"
        let gray50  = "#d2d0ce"
        let gray40  = "#e1dfdd"
        let gray30  = "#edebe9"
        let gray20  = "#f3f2f1"
        let white   = "#ffffff"


    module Black =

        let Primary = "#000000"
        let gray190 = "#201f1e"
        let gray180 = "#252423"
        let gray170 = "#292827"
        let gray160 = "#323130"
        let gray150 = "#3b3a39"
        let gray140 = "#484644"
        let gray130 = "#605e5c"
        let gray100 = "#979593"
        let gray90  = "#a19f9d"
        let gray70  = "#bebbb8"
        let gray40  = "#e1dfdd"
        let white   = "#ffffff"

    type ColorMode = {
        Name                    : string
        BodyBackground          : string
        BodyForeground          : string
        ControlBackground       : string
        ControlForeground       : string
        ElementBackground       : string
        ElementForeground       : string
        Text                    : string
        Accent                  : string
        Fade                    : string

    }

    let darkMode = {
        Name                    = "Dark"
        BodyBackground          = Black.gray180
        BodyForeground          = Black.gray160
        ControlBackground       = Black.gray140
        ControlForeground       = Black.gray100
        ElementBackground       = Black.Primary
        ElementForeground       = Black.gray140
        Text                    = Black.white
        Accent                  = Black.white
        Fade                    = Black.gray70
    }

    let colorfullMode = {
        Name                    = "Colorfull"
        BodyBackground          = Colorfull.gray20
        BodyForeground          = Colorfull.gray20
        ControlBackground       = Colorfull.white
        ControlForeground       = Colorfull.gray40
        ElementBackground       = Excel.Tint10
        ElementForeground       = Colorfull.white
        Text                    = Colorfull.gray180
        Accent                  = Excel.Primary
        Fade                    = Excel.Tint30
    }

    let colorElement (mode:ColorMode) =
        Style [
            BackgroundColor mode.ElementBackground
            BorderColor     mode.ElementForeground
            Color           mode.Text
        ]

    let colorControl (mode:ColorMode) =
        Style [
            BackgroundColor mode.ControlBackground
            BorderColor     mode.ControlForeground
            Color           mode.Text
        ]

    let colorBackground (mode:ColorMode) =
        Style [
            BackgroundColor mode.BodyBackground
            BorderColor     mode.BodyForeground
            Color           mode.Text
        ]

open Shared

type LogItem =
    | Debug of (System.DateTime*string)
    | Info  of (System.DateTime*string)
    | Error of (System.DateTime*string)

    static member toTableRow = function
        | Debug (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "green"; FontWeight "bold"]] [str "Debug"]
                td [] [str m]
            ]
        | Info  (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "lightblue"; FontWeight "bold"]] [str "Info"]
                td [] [str m]
            ]
        | Error (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "red"; FontWeight "bold"]] [str "ERROR"]
                td [] [str m]
            ]

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = {
    //Error handling
    LastFullError       : System.Exception option
    Log                 : LogItem list

    //Site Meta Options (Styling etc)
    DisplayMessage      : string
    BurgerVisible       : bool
    IsDarkMode          : bool
    ColorMode           : ExcelColors.ColorMode

    //Data for the App
    FillSelectionText   : string
    AddColumnText       : string
    }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    //=======================================================
    //Office Api specific calls needed to keep models in sync
    | Initialized           of (string*string)
    | SyncContext           of string
    | InSync                of string

    //=======================================================
    //Styling and general website behaviour
    | ToggleBurger
    | ToggleColorMode

    //=======================================================
    //EDebugging
    | LogTableMetadata

    //=======================================================
    //Error handling
    | GenericError          of exn

    //=======================================================
    //UserInput
    | FillSectionTextChange of string
    | AddColumnTextChange   of string

    //=======================================================
    //App specific messages
    | ExcelTestResponse     of string
    | TryExcel
    | FillSelection         of string
    | AddColumn             of string
    | CreateAnnotationTable
    | FormatAnnotationTable


module Server =

    open Shared
    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api : ICounterApi =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<ICounterApi>

let initializeAddIn () =
    OfficeInterop.Office.onReady()
    
let initialModel = {
    LastFullError       = None
    Log                 = []
    DisplayMessage      = "Initializing AddIn ..."
    BurgerVisible       = false
    IsDarkMode          = false
    ColorMode           = (ExcelColors.colorfullMode)
    FillSelectionText   = ""
    AddColumnText       = ""
    }
// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let loadCountCmd =
        Cmd.OfPromise.either
            initializeAddIn
            ()
            (fun x -> Initialized (x.host.ToString(),x.platform.ToString()))
            GenericError
    initialModel, loadCountCmd

let init2 () : Model * Cmd<Msg> =
    initialModel, Cmd.none

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with

    //=======================================================
    //Office Api specific calls needed to keep models in sync
    | Initialized (h,p) ->
        let welcomeMsg = sprintf "Ready to go in %s running on %s" h p
        let nextModel = {
            currentModel with
                DisplayMessage = welcomeMsg
                Log = LogItem.Info(System.DateTime.Now,welcomeMsg)::currentModel.Log
            }
        nextModel, Cmd.none
        
    | SyncContext passthroughMessage ->
        currentModel,
        Cmd.OfPromise.either
            OfficeInterop.syncContext
            passthroughMessage
            (fun _ -> InSync passthroughMessage)
            GenericError

    | InSync passthroughMessage ->
        let nextModel = {
            currentModel with
                DisplayMessage = passthroughMessage
                Log = LogItem.Debug(System.DateTime.Now,passthroughMessage)::currentModel.Log
            }
        nextModel, Cmd.none

    //=======================================================
    //Styling and general website behaviour
    | ToggleBurger          ->
        {currentModel with BurgerVisible = not currentModel.BurgerVisible},Cmd.none

    | ToggleColorMode       -> 
        let opposite = not currentModel.IsDarkMode
        let nextModel = {
            currentModel with
                IsDarkMode = opposite;
                ColorMode = if opposite then ExcelColors.darkMode else ExcelColors.colorfullMode
        }
        nextModel,Cmd.none

    //=======================================================
    //Debugging
    | LogTableMetadata ->
        currentModel,
        Cmd.OfPromise.either
            OfficeInterop.getTableMetaData 
            ()
            SyncContext
            GenericError

    //=======================================================
    //Error handling
    | GenericError e        ->
        OfficeInterop.consoleLog (sprintf "GenericError occured: %s" e.Message)
        let nextModel = {
            currentModel with
                LastFullError = Some e
                DisplayMessage = (sprintf "GenericError occured: %s" e.Message)
                Log = LogItem.Error(System.DateTime.Now,e.Message)::currentModel.Log
            }
        nextModel, Cmd.none

    //=======================================================
    //UserInput
    | FillSectionTextChange newText ->
        let nextModel = {
            currentModel with
                FillSelectionText = newText
            }
        nextModel, Cmd.none
    | AddColumnTextChange newText ->
        let nextModel = {
            currentModel with
                AddColumnText = newText
            }
        nextModel, Cmd.none

    //=======================================================
    //App specific messages

    | ExcelTestResponse res ->
        {currentModel with DisplayMessage = res},Cmd.none

    | TryExcel ->
        currentModel,
        Cmd.OfPromise.either
            OfficeInterop.exampleExcelFunction 
            ()
            SyncContext
            GenericError

    | FillSelection fillValue ->
        currentModel,
        Cmd.OfPromise.either
            OfficeInterop.fillValue  
            fillValue
            SyncContext
            GenericError

    | AddColumn columnValue ->

        currentModel,
        Cmd.OfPromise.either
            OfficeInterop.addAnnotationColumn  
            columnValue
            SyncContext
            GenericError

    | CreateAnnotationTable ->

        currentModel,
        Cmd.OfPromise.either
            OfficeInterop.createAnnotationTable  
            currentModel.IsDarkMode
            SyncContext
            GenericError



    //| _ -> currentModel, Cmd.none




let button (colorMode: ExcelColors.ColorMode) (isActive:bool) txt onClick =

    Button.button [
        Button.IsFullWidth
        Button.IsActive isActive
        Button.Props [Style [BackgroundColor colorMode.BodyForeground; Color colorMode.Text]]
        Button.OnClick onClick
        ][
        str txt
    ]

let mainForm (model : Model) (dispatch : Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
    ] [
        Field.div [] [
            Label.label [   Label.Size Size.IsLarge
                            Label.Props [Style [Color model.ColorMode.Accent]]
            ][
                str "Fill Selection"
            ]
            Control.div [] [
                Input.input [   Input.Placeholder ""
                                Input.Size Size.IsLarge
                                Input.Props [ExcelColors.colorControl model.ColorMode]
                                Input.OnChange (fun e -> FillSectionTextChange e.Value |> dispatch)
                            ]   
            ]
            Help.help [] [str "When applicable, search for an ontology item to fill into the selected field(s)"]
        ]
        Field.div [] [
            Control.div [] [
                Button.button   [   let hasText = model.FillSelectionText.Length > 0
                                    if hasText then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ -> FillSelection model.FillSelectionText |> dispatch)

                                ] [
                    str "Fill"
                    
                ]
            ]
        ]
        Field.div [] [
            Label.label [   Label.Size Size.IsLarge
                            Label.Props [Style [Color model.ColorMode.Accent]]
            ][
                str "Add Column"
            ]
            Control.div [] [
                Input.input [   Input.Placeholder ""
                                Input.Size Size.IsLarge
                                Input.Props [ExcelColors.colorControl model.ColorMode]
                                Input.OnChange (fun e -> AddColumnTextChange e.Value |> dispatch)
                            ]
            ]
            Help.help [] [str "Search annotation columns to add to the annotation table"]
        ]
        Field.div [] [
            Control.div [] [
                Button.button   [   let hasText = model.AddColumnText.Length > 0
                                    if hasText then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ -> AddColumn model.AddColumnText |> dispatch)

                                ] [
                    str "Add"
                    
                ]
            ]
        ]
    ]

let navbar (model : Model) (dispatch : Msg -> unit) =
    Navbar.navbar [Navbar.Props [Props.Role "navigation"; AriaLabel "main navigation" ; ExcelColors.colorElement model.ColorMode]] [
        Navbar.Brand.a [] [
            Navbar.Item.a [Navbar.Item.Props [Props.Href "https://csb.bio.uni-kl.de/"]] [
                img [Props.Src "../assets/CSB_Logo.png"]
            ]
            Navbar.burger [ if model.BurgerVisible then CustomClass "is-active"
                            Props [
                                    Role "button"
                                    AriaLabel "menu"
                                    Props.AriaExpanded false
                                    OnClick (fun e -> ToggleBurger |> dispatch)

                            ]
            ] [
                span [AriaHidden true] []
                span [AriaHidden true] []
                span [AriaHidden true] []
            ]
        ]
        Navbar.menu [Navbar.Menu.Props [Id "navbarMenu"; Class (if model.BurgerVisible then "navbar-menu is-active" else "navbar-menu") ; ExcelColors.colorControl model.ColorMode]] [
            Navbar.Start.div [] [
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.ColorMode.Text]]] [
                    str "How to use"
                ]
            ]
            Navbar.End.div [] [
                Navbar.Item.div [Navbar.Item.Props [ Style [if model.IsDarkMode then Color model.ColorMode.Text else Color model.ColorMode.Fade]]] [
                    Checkbox.checkbox [] [
                        Checkbox.input [Props [OnClick (fun _ -> ToggleColorMode |> dispatch); Checked model.IsDarkMode]]
                        str "Dark Mode"
                    ]
                ]
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.ColorMode.Text]]] [
                    str "Contact"
                ]
            ]
        ]
    ]

let renderActivityLog (model:Model) =
    Table.table [
        Table.IsFullWidth
        Table.Props [ExcelColors.colorBackground model.ColorMode]
    ] (
        model.Log
        |> List.map LogItem.toTableRow
    )
    

let view (model : Model) (dispatch : Msg -> unit) =
    div [   Style [MinHeight "100vh"; BackgroundColor model.ColorMode.BodyBackground; Color model.ColorMode.Text;]
        ] [
        navbar model dispatch
        Container.container [Container.IsFluid] [
            br []
            mainForm model dispatch
            br []
            button model.ColorMode true "idk man=(" (fun _ -> TryExcel |> dispatch)
            button model.ColorMode true "create annoation table" (fun _ -> CreateAnnotationTable |> dispatch)
            button model.ColorMode true "Log table metadata" (fun _ -> LogTableMetadata |> dispatch)

            Footer.footer [ Props [ExcelColors.colorControl model.ColorMode]] [
                Content.content [
                    Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
                    Content.Props [ExcelColors.colorControl model.ColorMode] 
                ][
                    renderActivityLog model
                ]
            ] 
        ]
    ]
#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
