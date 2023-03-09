module TermSearch

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared
open TermTypes
open CustomComponents
open Elmish

open TermSearch

let update (termSearchMsg: TermSearch.Msg) (currentState:TermSearch.Model) : TermSearch.Model * Cmd<Messages.Msg> =
    match termSearchMsg with
    // Toggle the search by parent ontology option on/off by clicking on a checkbox
    | TermSearch.ToggleSearchByParentOntology ->
        let nextState = {
            currentState with
                SearchByParentOntology = currentState.SearchByParentOntology |> not
        }

        nextState, Cmd.none

    | SearchTermTextChange (newTerm, parentTerm) ->

        let triggerNewSearch = newTerm.Trim() <> ""
       
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 1.),
            "GetNewTermSuggestions",
            (
                if triggerNewSearch then
                    match parentTerm, currentState.SearchByParentOntology with
                    | Some termMin, true ->
                        (newTerm,termMin) |> (GetNewTermSuggestionsByParentTerm >> Request >> Api)
                    | None,_ | _, false ->
                        newTerm  |> (GetNewTermSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextState = {
            currentState with
                TermSearchText = newTerm
                SelectedTerm = None
                ShowSuggestions = triggerNewSearch
                HasSuggestionsLoading = true
        }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | TermSuggestionUsed suggestion ->

        let nextState = {
            Model.init() with
                SearchByParentOntology = currentState.SearchByParentOntology
                SelectedTerm = Some suggestion
                TermSearchText = suggestion.Name
        }
        nextState, Cmd.none

    | NewSuggestions suggestions ->

        let nextState = {
            currentState with
                TermSuggestions         = suggestions
                ShowSuggestions         = true
                HasSuggestionsLoading   = false
        }

        nextState,Cmd.none

    | StoreParentOntologyFromOfficeInterop parentTerm ->
        let nextState = {
            currentState with
                ParentOntology = parentTerm
        }
        nextState, Cmd.none

    // Server

    | GetAllTermsByParentTermRequest ontInfo ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.getAllTermsByParentTerm
                ontInfo
                (GetAllTermsByParentTermResponse >> TermSearchMsg)
                (curry GenericError Cmd.none >> DevMsg)

        let nextState = {
            currentState with
                HasSuggestionsLoading = true
        }

        nextState, cmd

    | GetAllTermsByParentTermResponse terms ->
        let nextState = {
            currentState with
                TermSuggestions = terms
                HasSuggestionsLoading = false
                ShowSuggestions = true
            
        }
        nextState, Cmd.none


open Fable.Core
open Fable.Core.JsInterop
open SidebarComponents

let simpleSearchComponent model dispatch =
    mainFunctionContainer [

        // main container //

        Field.div [] [
            AutocompleteSearch.autocompleteTermSearchComponentOfParentOntology
                dispatch
                model.SiteStyleState.ColorMode
                model
                "Start typing to search for terms"
                (Some Size.IsLarge)
                (AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState)
        ]

        // relationship directed search switch //

        div [] [
            Switch.switchInline [
                Switch.Color IsPrimary
                Switch.Id "switch-1"
                Switch.Checked model.TermSearchState.SearchByParentOntology
                Switch.OnChange (fun e ->
                    TermSearch.ToggleSearchByParentOntology |> TermSearchMsg |> dispatch
                    let _ =
                        let inpId = (AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState).InputId
                        let e = Browser.Dom.document.getElementById inpId
                        e.focus()
                    ()
                    // this one is ugly, what it does is: Do the related search after toggling directed search (by parent ontology) of/on.
                    //((AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState).OnInputChangeMsg model.TermSearchState.TermSearchText) |> dispatch
                )
            ] [ str "Use related term directed search." ]

            Help.help [ Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]] ] [
                a [OnClick (fun _ -> AdvancedSearch.ToggleModal (AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState).ModalId |> AdvancedSearchMsg |> dispatch)] [
                    str "Use advanced search"
                ] 
            ]
        ]

        // "Fill selected cells with this term" - button //

        // For some reason columns seem to be faulty here. Without the workaround of removing negative margin left and right from Columns.columns
        // It would not be full width. This results in the need to remove padding left/right for Column.column childs.
        Columns.columns [
            Columns.IsMobile;
            Columns.Props [Style [Width "100%"; MarginRight "0px"; MarginLeft "0px"]]
        ] [
            Column.column [Column.Props [Style [PaddingLeft "0"; if model.TermSearchState.SelectedTerm.IsNone then PaddingRight "0"]]] [
            // Fill selection confirmation
                Field.div [] [
                    Control.div [] [
                        Button.a [
                            let hasText = model.TermSearchState.TermSearchText.Length > 0
                            if hasText then
                                Button.CustomClass "is-success"
                                //Button.IsActive true
                            else
                                Button.CustomClass "is-danger"
                            Button.Props [
                                Disabled (not hasText)
                            ]
                            Button.IsFullWidth
                            Button.OnClick (fun _ ->
                                if hasText then
                                    let term = if model.TermSearchState.SelectedTerm.IsSome then TermMinimal.ofTerm model.TermSearchState.SelectedTerm.Value else TermMinimal.create model.TermSearchState.TermSearchText ""
                                    SpreadsheetInterface.InsertOntologyTerm term |> InterfaceMsg |> dispatch
                            )
                        ] [
                            str "Fill selected cells with this term"
                        ]
                    ]
                ]
            ]
            if model.TermSearchState.SelectedTerm.IsSome then
                Column.column [
                    Column.Props [Style [PaddingRight "0"]]
                    Column.Width (Screen.All, Column.IsNarrow)
                ] [
                    Button.a [
                        Button.Props [Title "Copy to Clipboard"]
                        Button.Color IsInfo
                        Button.OnClick (fun e ->
                            // trigger icon response
                            CustomComponents.ResponsiveFA.triggerResponsiveReturnEle "clipboard_termsearch"
                            //
                            let t = model.TermSearchState.SelectedTerm.Value
                            let txt = [t.Name; t.Accession |> Shared.URLs.termAccessionUrlOfAccessionStr; t.Accession.Split(@":").[0] ] |> String.concat System.Environment.NewLine
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
                        CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_termsearch" Fa.Regular.Clipboard Fa.Solid.Check
                    ]
                ]
        ]
    ]

let termSearchComponent (model:Messages.Model) dispatch =
    div [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Ontology term search"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Search for an ontology term to fill into the selected field(s)"]

        simpleSearchComponent model dispatch

        //if model.TermSearchState.SelectedTerm.IsNone then
        //    str "No Term Selected"
        //else
        //    str (sprintf "%A" model.TermSearchState.SelectedTerm.Value)

        //Button.button [
        //    Button.OnClick (fun e ->
        //        GetParentOntology |> ExcelInterop |> dispatch
        //    )
        //] [
        //    str "GetParentOntology"
        //]

        //if model.TermSearchState.ParentOntology.IsNone then
        //    str "No Parent Ontology selected"
        //else
        //    str model.TermSearchState.ParentOntology.Value
    ]