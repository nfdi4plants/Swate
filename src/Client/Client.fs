module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Thoth.Json
open Thoth.Elmish
open Model
open ExcelColors
open Shared

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    //=======================================================
    //Debouncing
    | DebouncerSelfMsg                          of Debouncer.SelfMessage<Msg>
    | DoNothing

    //=======================================================
    //Office Api specific calls needed to keep models in sync
    | Initialized                               of (string*string)
    | SyncContext                               of string
    | InSync                                    of string

    //=======================================================
    //Styling and general website behaviour
    | ToggleBurger
    | ToggleColorMode

    //=======================================================
    //Debugging
    | LogTableMetadata
    | GenericLog                                of (string*string)
    //=======================================================
    //Error handling
    | GenericError                              of exn
    //=======================================================
    //UserInput
    //---------------------
    //FillSelection related
    | FillSelectionSearchTermChange             of string
    | FillSelectionSearchOntologyChange         of string
    | FillTermSuggestionUsed                    of string
    | FillOntologySuggestionUsed                of string
    | SwitchFillSearchMode
    | FillSelectionAdvancedSearchOptionsChange  of FillSelectionAdvancedSearchOptions
    //---------------------
    //insert column related
    | AddColumnTextChange                       of string

    //=======================================================
    //App specific messages
    | ExcelTestResponse                         of string
    | TryExcel
    | FillSelection                             of string
    | AddColumn                                 of string
    | CreateAnnotationTable

    //=======================================================
    //Server communication
    | TestOntologyInsert                        of (string*string*string*System.DateTime*string)
    | GetNewTermSuggestions                     of string
    | TermSuggestionResponse                    of DbDomain.Term []
    | FetchAllOntologies
    | FetchAllOntologiesResponse                of DbDomain.Ontology []


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
    //One time sync with server
    SearchableOntologies                = [||]
    HasOntologiesLoaded                 = false

    //Debouncing
    Debouncer                           = Debouncer.create()

    //Error handling
    LastFullError                       = None
    Log                                 = []

    //Site Meta Options (Styling etc)
    DisplayMessage                      = "Initializing AddIn ..."
    BurgerVisible                       = false
    IsDarkMode                          = false
    ColorMode                           = (ExcelColors.colorfullMode)

    //Fill Selection term search
    FillSearchMode                      = Autocomplete

    //simple term search
    FillSelectionTermSearchText         = ""
    FillSelectionOntologySearchText     = ""
    TermSuggestions                     = [||]
    ShowFillSuggestions                 = false
    ShowFillSuggestionUsed              = false
    HadFirstSuggestion                  = false
    HasSuggestionsLoading               = false

    //Advanced term search
    FillSelectionAdvancedSearchOptions  = {
        Ontology                = None
        StartsWith              = ""
        MustContain             = ""
        EndsWith                = ""
        DefinitionMustContain   = ""
        KeepObsolete            = false
    }

    //Column insert
    AddColumnText           = ""
    }

// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let loadCountCmd =
        Cmd.batch [
            Cmd.OfPromise.either
                initializeAddIn
                ()
                (fun x -> Initialized (x.host.ToString(),x.platform.ToString()))
                GenericError
            Cmd.ofMsg FetchAllOntologies
        ]
    initialModel, loadCountCmd

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    //=======================================================
    //One-time Messages to be processed when initializing 
    | FetchAllOntologies ->
        let fetchOntologiesCmd =
            Cmd.OfAsync.either
                Server.api.getAllOntologies
                ()
                FetchAllOntologiesResponse
                GenericError
        currentModel,fetchOntologiesCmd

    | FetchAllOntologiesResponse ontologies ->
        let nextModel = {
            currentModel with
                SearchableOntologies    = ontologies |> Array.map (fun ont -> ont.Name |> Suggestion.createBigrams, ont)
                HasOntologiesLoaded     = true
        }
        nextModel, Cmd.none

    //=======================================================
    //Debouncing
    | DebouncerSelfMsg debouncerMsg ->
        let (debouncerModel, debouncerCmd) = Debouncer.update debouncerMsg currentModel.Debouncer
        { currentModel with Debouncer = debouncerModel }, debouncerCmd

    | DoNothing ->
        currentModel,Cmd.none

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
    //---------------------
    //FillSelection related
    | FillSelectionSearchOntologyChange newOntology ->

        let nextModel = {
            currentModel with
                FillSelectionOntologySearchText = newOntology
                ShowFillSuggestions         = newOntology.Length > 0
                ShowFillSuggestionUsed      = false
                HasSuggestionsLoading       = false
        }
        nextModel,Cmd.none

    | FillSelectionSearchTermChange newTerm ->

        let triggerNewSearch =
            newTerm.Length > 2
           
        let (debouncerModel, debouncerCmd) =
            currentModel.Debouncer
            |> Debouncer.bounce
                (System.TimeSpan.FromSeconds 0.35)
                "FillSectionTextChange"
                (
                    if triggerNewSearch then
                        GetNewTermSuggestions newTerm
                    else
                        DoNothing
                )

        let nextModel = {
            currentModel with
                Debouncer                   = debouncerModel
                FillSelectionTermSearchText = newTerm
                ShowFillSuggestions         = triggerNewSearch
                ShowFillSuggestionUsed      = false
                HasSuggestionsLoading       = true
            }

        nextModel, Cmd.batch [ Cmd.map DebouncerSelfMsg debouncerCmd ]

    | FillTermSuggestionUsed suggestion ->
        let nextModel = {
            currentModel with
                FillSelectionTermSearchText = suggestion
                ShowFillSuggestions         = false
                ShowFillSuggestionUsed      = true
            }
        nextModel, Cmd.none

    | FillOntologySuggestionUsed suggestion ->
        let nextModel = {
            currentModel with
                FillSelectionOntologySearchText = suggestion
                ShowFillSuggestions             = false
                ShowFillSuggestionUsed          = true
            }
        nextModel, Cmd.none

    | SwitchFillSearchMode ->

        let nextSearchMode =
            match currentModel.FillSearchMode with
            | Autocomplete  -> Advanced
            | Advanced      -> Autocomplete

        let nextModel = {
            currentModel with
                FillSelectionOntologySearchText = ""
                FillSelectionTermSearchText = ""
                FillSearchMode = nextSearchMode
                ShowFillSuggestions = false
        }

        nextModel,Cmd.none

    | FillSelectionAdvancedSearchOptionsChange options ->
        let nextModel = {
            currentModel with
                FillSelectionAdvancedSearchOptions = options
        }
        nextModel,Cmd.none

    //---------------------
    //insert column related
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

    | GetNewTermSuggestions queryString ->
        let nextModel = {
            currentModel with
                HadFirstSuggestion      = true
                HasSuggestionsLoading   = true
        }
        nextModel,
        Cmd.OfAsync.either
            Server.api.getTermSuggestions
            (5,queryString)
            TermSuggestionResponse
            GenericError

    | TermSuggestionResponse termSuggestions ->
        let nextModel = {
            currentModel with
                TermSuggestions         = termSuggestions
                HasSuggestionsLoading   = false
            }
        nextModel,Cmd.none
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

module GenericCustomComponents =

    let loading =
        Fa.i [
            Fa.Solid.Spinner
            Fa.Pulse
            Fa.Size Fa.Fa4x
        ] []

    let autocompleteDropdown (model:Model) (dispatch: Msg -> unit) (isVisible: bool) (isLoading:bool) (suggestions: ReactElement list)  =
        Container.container[] [
            Dropdown.content [Props [
                Style [
                    if isVisible then Display DisplayOptions.Block else Display DisplayOptions.None
                    //if model.ShowFillSuggestions then Display DisplayOptions.Block else Display DisplayOptions.None
                    BackgroundColor model.ColorMode.ControlBackground
                    BorderColor     model.ColorMode.ControlForeground
                ]]
            ] [
                Table.table [Table.IsFullWidth] (
                    if isLoading then
                        [
                            tr [] [
                                td [Style [TextAlign TextAlignOptions.Center]] [
                                    loading
                                    br []
                                ]
                            ]
                        ]
                    else
                        suggestions
                )

                
            ]
        ]

let createTermSuggestions model dispatch =
    model.TermSuggestions
    |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
    |> Array.map (fun sugg ->
        tr [OnClick (fun _ -> (sugg.Name |> FillTermSuggestionUsed) |> dispatch); colorControl model.ColorMode; Class "suggestion"] [
            td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
                Fa.i [Fa.Solid.InfoCircle] []
            ]
            td [] [
                b [] [str sugg.Name]
            ]
            td [Style [Color "red"]] [if sugg.IsObsolete then str "obsolete"]
            td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
        ])
    |> List.ofArray

let createOntologySuggestions (model:Model) (dispatch: Msg -> unit) =
    model.SearchableOntologies
    |> Array.sortByDescending (fun (bigrams,_) ->
        Suggestion.sorensenDice (model.FillSelectionOntologySearchText |> Suggestion.createBigrams) bigrams 
    )
    |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
    |> Array.map (fun (_,sugg) ->
        tr [OnClick (fun _ -> (sugg.Name |> FillOntologySuggestionUsed) |> dispatch); colorControl model.ColorMode; Class "suggestion"] [
            td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
                Fa.i [Fa.Solid.InfoCircle] []
            ]
            td [] [
                b [] [str sugg.Name]
            ]
            td [Style [FontWeight "light"]] [small [] [str sugg.CurrentVersion]]
        ])
    |> List.ofArray
        
//let createAdvancedTermSearchResultList (model:Model) (dispatch: Msg -> unit) =
//    model.AdvancedSearchTermResults
//    |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
//    |> Array.map (fun sugg ->
//        tr [OnClick (fun _ -> (sugg.Name |> FillTermSuggestionUsed) |> dispatch); colorControl model.ColorMode; Class "suggestion"] [
//            td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
//                Fa.i [Fa.Solid.InfoCircle] []
//            ]
//            td [] [
//                b [] [str sugg.Name]
//            ]
//            td [Style [Color "red"]] [if sugg.IsObsolete then str "obsolete"]
//            td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
//        ])
//    |> List.ofArray
            
        //]

module FillSelectionComponents =

    let autocompleteSearch (model:Model) (dispatch: Msg -> unit) =
        Field.div [] [
            Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.ColorMode.Accent]]][ str "Fill Selection"]
            a [OnClick (fun _ -> SwitchFillSearchMode |> dispatch)] [str "Use advanced search"]
            Control.div [] [
                Input.input [   Input.Placeholder ""
                                Input.Size Size.IsLarge
                                Input.Props [ExcelColors.colorControl model.ColorMode]
                                Input.OnChange (fun e -> FillSelectionSearchTermChange e.Value |> dispatch)
                                Input.Value model.FillSelectionTermSearchText
                            ]   
                GenericCustomComponents.autocompleteDropdown
                    model
                    dispatch
                    model.ShowFillSuggestions
                    model.HasSuggestionsLoading
                    (createTermSuggestions model dispatch)
            ]
            Help.help [] [str "When applicable, search for an ontology term to fill into the selected field(s)"]
        ]

    let advancedSearch (model:Model) (dispatch: Msg -> unit) =
        [
            Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.ColorMode.Accent]]][ str "Fill Selection"]
            a [OnClick (fun _ -> SwitchFillSearchMode |> dispatch)] [str "Use simple search"]
            br []
            Field.div [] [
                Label.label [] [ str "Ontology"]
                Help.help [] [str "Only search terms in the selected ontology"]
                Field.div [] [
                    Control.div [] [
                        Input.input [   Input.Placeholder ""
                                        Input.Size Size.IsMedium
                                        Input.Props [ExcelColors.colorControl model.ColorMode]
                                        Input.OnChange (fun e -> FillSelectionSearchOntologyChange e.Value |> dispatch)
                                        Input.Value model.FillSelectionOntologySearchText
                                    ]   
                        GenericCustomComponents.autocompleteDropdown
                            model
                            dispatch
                            model.ShowFillSuggestions
                            model.HasSuggestionsLoading
                            (createOntologySuggestions model dispatch)
                    ]
                ]
            ]
            Field.div [] [
                Label.label [] [ str "Starts with:"]
                Help.help [] [str "The term name must start with this string"]
                Field.div [] [
                    Control.div [] [
                        Input.input [
                            Input.Placeholder ""
                            Input.Size IsSmall
                            Input.Props [ExcelColors.colorControl model.ColorMode]
                            Input.OnChange (fun e -> FillSelectionAdvancedSearchOptionsChange {model.FillSelectionAdvancedSearchOptions with StartsWith = e.Value} |> dispatch)
                            Input.Value model.FillSelectionAdvancedSearchOptions.StartsWith
                        ] 
                    ]
                ]
            ]
            Field.div [] [
                Label.label [] [ str "Must contain:"]
                Help.help [] [str "The term name must contain this string (at any position)"]
                Field.div [] [
                    Control.div [] [
                        Input.input [
                            Input.Placeholder ""
                            Input.Size IsSmall
                            Input.Props [ExcelColors.colorControl model.ColorMode]
                            Input.OnChange (fun e -> FillSelectionAdvancedSearchOptionsChange {model.FillSelectionAdvancedSearchOptions with MustContain = e.Value} |> dispatch)
                            Input.Value model.FillSelectionAdvancedSearchOptions.MustContain
                        ] 
                    ]
                ]
            ]
            Field.div [] [
                Label.label [] [ str "Ends with:"]
                Help.help [] [str "The term must end with this string"]
                Field.div [] [
                    Control.div [] [
                        Input.input [
                            Input.Placeholder ""
                            Input.Size IsSmall
                            Input.Props [ExcelColors.colorControl model.ColorMode]
                            Input.OnChange (fun e -> FillSelectionAdvancedSearchOptionsChange {model.FillSelectionAdvancedSearchOptions with EndsWith = e.Value} |> dispatch)
                            Input.Value model.FillSelectionAdvancedSearchOptions.EndsWith
                        ] 
                    ]
                ] 
            ]
            Field.div [] [
                Label.label [] [ str "Definition must contain:"]
                Help.help [] [str "The definition of the term must contain this string (at any position)"]
                Field.body [] [
                    Field.div [] [
                        Control.div [] [
                            Input.input [
                                Input.Placeholder ""
                                Input.Size IsSmall
                                Input.Props [ExcelColors.colorControl model.ColorMode]
                                Input.OnChange (fun e -> FillSelectionAdvancedSearchOptionsChange {model.FillSelectionAdvancedSearchOptions with DefinitionMustContain = e.Value} |> dispatch)
                                Input.Value model.FillSelectionAdvancedSearchOptions.DefinitionMustContain
                            ] 
                        ]
                    ]
                ]
            ]
            Field.div [] [
                Control.div [] [
                    Button.button   [
                        let hasText = model.FillSelectionTermSearchText.Length > 0
                        if hasText then
                            Button.CustomClass "is-success"
                            Button.IsActive true
                        else
                            Button.CustomClass "is-danger"
                            Button.Props [Disabled true]
                        Button.IsFullWidth
                        Button.OnClick (fun _ -> FillSelection model.FillSelectionTermSearchText |> dispatch)
                    ] [ str "Start advanced search"]
                ]
            ]
            Field.div [Field.Props [] ] [
                Label.label [] [str "Results:"]
                GenericCustomComponents.autocompleteDropdown
                    model
                    dispatch
                    true
                    false
                    [str "meeem"]
            ]
        ]

let mainForm (model : Model) (dispatch : Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
    ] [
        // Fill selection components with two search modes
        match model.FillSearchMode with
        | Autocomplete  -> FillSelectionComponents.autocompleteSearch model dispatch
        | Advanced      -> yield! FillSelectionComponents.advancedSearch model dispatch

        // Fill selection confirmation
        Field.div [] [
            Control.div [] [
                Button.button   [   let hasText = model.FillSelectionTermSearchText.Length > 0
                                    if hasText then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ -> FillSelection model.FillSelectionTermSearchText |> dispatch)

                                ] [
                    str "Fill"
                    
                ]
            ]
        ]
        //Field.div [] [
        //    Label.label [   Label.Size Size.IsLarge
        //                    Label.Props [Style [Color model.ColorMode.Accent]]
        //    ][
        //        str "Add Column"
        //    ]
        //    Control.div [] [
        //        Input.input [   Input.Placeholder ""
        //                        Input.Size Size.IsLarge
        //                        Input.Props [ExcelColors.colorControl model.ColorMode]
        //                        Input.OnChange (fun e -> AddColumnTextChange e.Value |> dispatch)
        //                    ]
        //    ]
        //    Help.help [] [str "Search annotation columns to add to the annotation table"]
        //]
        //Field.div [] [
        //    Control.div [] [
        //        Button.button   [   let hasText = model.AddColumnText.Length > 0
        //                            if hasText then
        //                                Button.CustomClass "is-success"
        //                                Button.IsActive true
        //                            else
        //                                Button.CustomClass "is-danger"
        //                                Button.Props [Disabled true]
        //                            Button.IsFullWidth
        //                            Button.OnClick (fun _ -> AddColumn model.AddColumnText |> dispatch)

        //                        ] [
        //            str "Add"
                    
        //        ]
        //    ]
        //]
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
                    Switch.switchInline [
                        Switch.Id "DarkModeSwitch"
                        Switch.IsOutlined
                        Switch.Color IsSuccess
                        Switch.OnChange (fun _ -> ToggleColorMode |> dispatch)
                    ] [str "DarkMode"]
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
