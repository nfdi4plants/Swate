module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Json
open Model
open ExcelColors
open Shared

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
    //Debugging
    | LogTableMetadata
    | GenericLog            of (string*string)
    //=======================================================
    //Error handling
    | GenericError          of exn
    //=======================================================
    //UserInput
    | FillSectionTextChange of string
    | FillSuggestionUsed    of string
    | AddColumnTextChange   of string

    //=======================================================
    //App specific messages
    | ExcelTestResponse     of string
    | TryExcel
    | FillSelection         of string
    | AddColumn             of string
    | CreateAnnotationTable
    | FormatAnnotationTable

    //=======================================================
    //Server communication
    | TestOntologyInsert    of (string*string*string*System.DateTime*string)

module Server =

    open Shared
    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api : IAnnotatorAPI =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<IAnnotatorAPI>

let initializeAddIn () =
    OfficeInterop.Office.onReady()
    
let initialModel = {
    LastFullError           = None
    Log                     = []
    DisplayMessage          = "Initializing AddIn ..."
    BurgerVisible           = false
    IsDarkMode              = false
    ColorMode               = (ExcelColors.colorfullMode)
    FillSelectionText       = ""
    FillSuggestions         = [|
        "Some";"Random";"Text";"IDK";"IsThiSCasEsEnsItIve?";"ISTHISCASESENSITIVE?";
        "isthiscasesensitive?";"Lena";"Kevin";"Schneider";"Hallo";"Halli";"allo";"sup";
        "AWD";"EFEWTGWE";"AWDfFGRGH";"erte_EWh";"wAWWWWW";"EGSWRH";"EHRJJJJJJJJJJJJJ";"AWgGRSHSR"
    |]
    ShowFillSuggestions     = false
    ShowFillSuggestionUsed  = false
    AddColumnText           = ""
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

    | GenericLog (level,passthroughMessage) ->
        let nextModel = {
            currentModel with
                DisplayMessage = passthroughMessage
                Log = (LogItem.ofStringNow level passthroughMessage)::currentModel.Log
            }
        nextModel, Cmd.none

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
                ShowFillSuggestions = newText.Length > 2
                ShowFillSuggestionUsed = false
            }
        nextModel, Cmd.none

    | FillSuggestionUsed suggestion ->
        let nextModel = {
            currentModel with
                FillSelectionText = suggestion
                ShowFillSuggestions = false
                ShowFillSuggestionUsed = true
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

    //=======================================================
    //Server communication
    | TestOntologyInsert (a,b,c,d,e) ->
        currentModel,
        Cmd.OfAsync.either
            Server.api.testOntologyInsert
            (a,b,c,d,e)
            (fun x -> GenericLog ("Debug",sprintf "Successfully created %A" x))
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

let inline sorensenDice (x : Set<'T>) (y : Set<'T>) =
    match  (x.Count, y.Count) with
    | (0,0) -> 1.
    | (xCount,yCount) -> (2. * (Set.intersect x y |> Set.count |> float)) / ((xCount + yCount) |> float)


let createBigrams (s:string) =
    s
        .ToUpperInvariant()
        .ToCharArray()
    |> Array.windowed 2
    |> Array.map (fun inner -> sprintf "%c%c" inner.[0] inner.[1])
    |> set

let getBestSuggestions (model:Model)  =
    let searchSet = model.FillSelectionText |> createBigrams
    model.FillSuggestions
    |> Array.sortByDescending (fun sugg ->
        sorensenDice (createBigrams sugg) searchSet
    )
    |> Array.take 5

let createSuggestions model dispatch =
    getBestSuggestions model
    |> Array.map (fun sugg ->
        Dropdown.Item.div [Dropdown.Item.Props [ OnClick (fun _ -> (sugg |> FillSuggestionUsed) |> dispatch); colorControl model.ColorMode]] [str sugg      ]
    )

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
                                if model.ShowFillSuggestionUsed then Input.Value model.FillSelectionText
                            ]   

                Container.container[] [
                    Dropdown.content [Props [
                        Style [
                            if model.ShowFillSuggestions then Display DisplayOptions.Block else Display DisplayOptions.None
                            BackgroundColor model.ColorMode.ControlBackground
                            BorderColor     model.ColorMode.ControlForeground
                        ]]
                    ] [
                        yield! createSuggestions model dispatch
                    ]
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
            Navbar.burger [ Navbar.Burger.IsActive model.BurgerVisible
                            Navbar.Burger.OnClick (fun e -> ToggleBurger |> dispatch)
                            Navbar.Burger.Props[
                                    Role "button"
                                    AriaLabel "menu"
                                    Props.AriaExpanded false
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
            button model.ColorMode true "make a test db insert xd" (fun _ -> TestOntologyInsert ((sprintf "Me am test %A" (System.Guid.NewGuid())),"1","Me is testerino",System.DateTime.UtcNow,"MEEEMuser") |> dispatch)
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
