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
open ExcelColors
open Api
open Model
open Messages
open Update
open Shared

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
   

// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let loadCountCmd =
        Cmd.batch [
            Cmd.OfPromise.either
                initializeAddIn
                ()
                (fun x -> (x.host.ToString(),x.platform.ToString()) |> Initialized |> ExcelInterop )
                (fun x -> x |> GenericError |> Dev)
            Cmd.ofMsg (FetchAllOntologies |> Request |> Api)
        ]
    initializeModel() , loadCountCmd

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
                    BackgroundColor model.SiteStyleState.ColorMode.ControlBackground
                    BorderColor     model.SiteStyleState.ColorMode.ControlForeground
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

let createTermSuggestions (model:Model) (dispatch: Msg -> unit) =
    if model.TermSearchState.Simple.TermSuggestions.Length > 0 then
        model.TermSearchState.Simple.TermSuggestions
        |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
        |> Array.map (fun sugg ->
            tr [OnClick (fun _ -> sugg.Name |> TermSuggestionUsed |> Simple |> TermSearch |> dispatch)
                colorControl model.SiteStyleState.ColorMode
                Class "suggestion"
            ] [
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
    else
        [
            tr [] [
                td [] [str "No terms found matching your input."]
            ]
        ]

let createOntologySuggestions (model:Model) (dispatch: Msg -> unit) =
    model.PersistentStorageState.SearchableOntologies
    |> Array.sortByDescending (fun (bigrams,_) ->
        Suggestion.sorensenDice (model.TermSearchState.Advanced.OntologySearchText |> Suggestion.createBigrams) bigrams 
    )
    |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
    |> Array.map (fun (_,sugg) ->
        tr [OnClick (fun _ -> sugg |> OntologySuggestionUsed |> Advanced |> TermSearch |> dispatch)
            colorControl model.SiteStyleState.ColorMode
            Class "suggestion"
        ] [
            td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
                Fa.i [Fa.Solid.InfoCircle] []
            ]
            td [] [
                b [] [str sugg.Name]
            ]
            td [Style [FontWeight "light"]] [small [] [str sugg.CurrentVersion]]
        ])
    |> List.ofArray
        
let createAdvancedTermSearchResultList (model:Model) (dispatch: Msg -> unit) =
    if model.TermSearchState.Advanced.AdvancedSearchTermResults.Length > 0 then
        model.TermSearchState.Advanced.AdvancedSearchTermResults
        |> Array.map (fun sugg ->
            tr [OnClick (fun _ -> sugg.Name |> AdvancedSearchResultUsed |> Advanced |> TermSearch |> dispatch)
                colorControl model.SiteStyleState.ColorMode
                Class "suggestion"
            ] [
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
    else
        [
            tr [] [
                td [] [str "No terms found matching your input."]
            ]
        ]
            
        
let isValidAdancedSearchOptions (opt:AdvancedTermSearchOptions) =
    ((
        opt.DefinitionMustContain.Length
        + opt.EndsWith.Length
        + opt.MustContain.Length
        + opt.DefinitionMustContain.Length
    ) > 0)
    || opt.Ontology.IsSome

module TermSearchComponents =

    let autocompleteSearch (model:Model) (dispatch: Msg -> unit) =
        Field.div [] [

            Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]
            a [OnClick (fun _ -> SwitchSearchMode |> TermSearch |> dispatch)] [str "Use advanced search"]
            br []
            Control.div [] [
                Input.input [   Input.Placeholder ""
                                Input.Size Size.IsLarge
                                Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                                Input.OnChange (fun e ->  e.Value |> SearchTermTextChange |> Simple |> TermSearch |> dispatch)
                                Input.Value model.TermSearchState.Simple.TermSearchText
                            ]
                GenericCustomComponents.autocompleteDropdown
                    model
                    dispatch
                    model.TermSearchState.Simple.ShowSuggestions
                    model.TermSearchState.Simple.HasSuggestionsLoading
                    (createTermSuggestions model dispatch)
            ]
            Help.help [] [str "When applicable, search for an ontology term to fill into the selected field(s)"]
        ]

    let advancedSearch (model:Model) (dispatch: Msg -> unit) =
        [
            Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]
            a [OnClick (fun _ -> SwitchSearchMode |> TermSearch |> dispatch)] [str "Use simple search"]
            br []
            Field.div [] [
                Label.label [] [ str "Ontology"]
                Help.help [] [str "Only search terms in the selected ontology"]
                Field.div [] [
                    Control.div [] [
                        Input.input [   Input.Placeholder ""
                                        Input.Size Size.IsMedium
                                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                                        Input.OnChange (fun e -> e.Value |> SearchOntologyTextChange |> Advanced |> TermSearch |> dispatch)
                                        Input.Value model.TermSearchState.Advanced.OntologySearchText
                                    ]   
                        GenericCustomComponents.autocompleteDropdown
                            model
                            dispatch
                            model.TermSearchState.Advanced.ShowOntologySuggestions
                            model.TermSearchState.Advanced.HasOntologySuggestionsLoading
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
                            Input.Size IsMedium
                            Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun e ->
                                {model.TermSearchState.Advanced.AdvancedSearchOptions
                                    with StartsWith = e.Value
                                }
                                |> AdvancedSearchOptionsChange
                                |> Advanced
                                |> TermSearch
                                |> dispatch)
                            Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.StartsWith
                        ] 
                    ]
                ]
            ]
            Field.div [] [
                Label.label [] [ str "Must contain:"]
                Help.help [] [str "The term name must contain any of these space-separated words (at any position)"]
                Field.div [] [
                    Control.div [] [
                        Input.input [
                            Input.Placeholder ""
                            Input.Size IsMedium
                            Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun e ->
                                {model.TermSearchState.Advanced.AdvancedSearchOptions
                                    with MustContain = e.Value
                                }
                                |> AdvancedSearchOptionsChange
                                |> Advanced
                                |> TermSearch
                                |> dispatch)
                            Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.MustContain
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
                            Input.Size IsMedium
                            Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun e ->
                                {model.TermSearchState.Advanced.AdvancedSearchOptions
                                    with EndsWith = e.Value
                                }
                                |> AdvancedSearchOptionsChange
                                |> Advanced
                                |> TermSearch
                                |> dispatch)
                            Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.EndsWith
                        ] 
                    ]
                ] 
            ]
            Field.div [] [
                Label.label [] [ str "Definition must contain:"]
                Help.help [] [str "The definition of the term must contain any of these space-separated words (at any position)"]
                Field.body [] [
                    Field.div [] [
                        Control.div [] [
                            Input.input [
                                Input.Placeholder ""
                                Input.Size IsMedium
                                Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                                Input.OnChange (fun e ->
                                    {model.TermSearchState.Advanced.AdvancedSearchOptions
                                        with DefinitionMustContain = e.Value
                                    }
                                    |> AdvancedSearchOptionsChange
                                    |> Advanced
                                    |> TermSearch
                                    |> dispatch)
                                Input.Value model.TermSearchState.Advanced.AdvancedSearchOptions.DefinitionMustContain
                            ] 
                        ]
                    ]
                ]
            ]
            Field.div [] [
                Control.div [] [
                    Button.button   [
                        let isValid = isValidAdancedSearchOptions model.TermSearchState.Advanced.AdvancedSearchOptions
                        if isValid then
                            Button.CustomClass "is-success"
                            Button.IsActive true
                        else
                            Button.CustomClass "is-danger"
                            Button.Props [Disabled (not isValid)]
                        Button.IsFullWidth
                        Button.OnClick (fun _ -> model.TermSearchState.Advanced.AdvancedSearchOptions |> GetNewAdvancedTermSearchResults |> Request |> Api |> dispatch)
                    ] [ str "Start advanced search"]
                ]
            ]
            Field.div [Field.Props [] ] [
                Label.label [] [str "Results:"]
                GenericCustomComponents.autocompleteDropdown
                    model
                    dispatch
                    model.TermSearchState.Advanced.ShowAdvancedSearchResults
                    model.TermSearchState.Advanced.HasAdvancedSearchResultsLoading
                    (createAdvancedTermSearchResultList model dispatch)
            ]
        ]

let mainForm (model : Model) (dispatch : Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
    ] [
        // Fill selection components with two search modes
        match model.TermSearchState.SearchMode with
        | TermSearchMode.Simple     -> TermSearchComponents.autocompleteSearch model dispatch
        | TermSearchMode.Advanced   -> yield! TermSearchComponents.advancedSearch model dispatch

        // Fill selection confirmation
        Field.div [] [
            Control.div [] [
                Button.button   [   let hasText = model.TermSearchState.Simple.TermSearchText.Length > 0
                                    if hasText then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ -> model.TermSearchState.Simple.TermSearchText |> FillSelection |> ExcelInterop |> dispatch)

                                ] [
                    str "Fill selected cells with this term"
                    
                ]
            ]
        ]
    ]

let navbar (model : Model) (dispatch : Msg -> unit) =
    Navbar.navbar [Navbar.Props [Props.Role "navigation"; AriaLabel "main navigation" ; ExcelColors.colorElement model.SiteStyleState.ColorMode]] [
        Navbar.Brand.a [] [
            Navbar.Item.a [Navbar.Item.Props [Props.Href "https://csb.bio.uni-kl.de/"]] [
                img [Props.Src "../assets/CSB_Logo.png"]
            ]
            Navbar.burger [ Navbar.Burger.IsActive model.SiteStyleState.BurgerVisible
                            Navbar.Burger.OnClick (fun e -> ToggleBurger |> StyleChange |> dispatch)
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
        Navbar.menu [Navbar.Menu.Props [Id "navbarMenu"; Class (if model.SiteStyleState.BurgerVisible then "navbar-menu is-active" else "navbar-menu") ; ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
            Navbar.Start.div [] [
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "How to use"
                ]
            ]
            Navbar.End.div [] [
                Navbar.Item.div [Navbar.Item.Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
                    Switch.switchInline [
                        Switch.Id "DarkModeSwitch"
                        Switch.IsOutlined
                        Switch.Color IsSuccess
                        Switch.OnChange (fun _ -> ToggleColorMode |> StyleChange |> dispatch)
                    ] [str "DarkMode"]
                ]
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "Contact"
                ]
            ]
        ]
    ]

let renderActivityLog (model:Model) =
    Table.table [
        Table.IsFullWidth
        Table.Props [ExcelColors.colorBackground model.SiteStyleState.ColorMode]
    ] (
        model.DevState.Log
        |> List.map LogItem.toTableRow
    )
    

let view (model : Model) (dispatch : Msg -> unit) =
    div [   Style [MinHeight "100vh"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text;]
        ] [
        navbar model dispatch
        Container.container [Container.IsFluid] [
            br []
            mainForm model dispatch
            br []
            button model.SiteStyleState.ColorMode true "make a test db insert xd" (fun _ -> ((sprintf "Me am test %A" (System.Guid.NewGuid())),"1","Me is testerino",System.DateTime.UtcNow,"MEEEMuser") |> TestOntologyInsert |> Request |> Api|> dispatch)
            button model.SiteStyleState.ColorMode true "idk man=(" (fun _ -> TryExcel |> ExcelInterop |> dispatch)
            button model.SiteStyleState.ColorMode true "create annoation table" (fun _ -> model.SiteStyleState.IsDarkMode |> CreateAnnotationTable |> ExcelInterop |> dispatch)
            button model.SiteStyleState.ColorMode true "Log table metadata" (fun _ -> LogTableMetadata |> Dev |> dispatch)

            Footer.footer [ Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
                Content.content [
                    Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
                    Content.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode] 
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

Program.mkProgram init Update.update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
