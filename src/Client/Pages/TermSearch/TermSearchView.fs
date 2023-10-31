module TermSearch

open Fable.React
open Fable.React.Props
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
open Feliz
open Feliz.Bulma

let simpleSearchComponent model dispatch =
    mainFunctionContainer [

        // main container //

        Bulma.field.div [
            AutocompleteSearch.autocompleteTermSearchComponentOfParentOntology
                dispatch
                model.SiteStyleState.ColorMode
                model
                "Start typing to search for terms"
                (Some Bulma.button.isLarge)
                (AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState)
        ]

        // relationship directed search switch //

        div [] [
            Switch.checkbox [
                Bulma.color.isPrimary
                prop.id "switch-1"
                prop.isChecked model.TermSearchState.SearchByParentOntology
                prop.onChange (fun (e:bool) ->
                    TermSearch.ToggleSearchByParentOntology |> TermSearchMsg |> dispatch
                    let _ =
                        let inpId = (AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState).InputId
                        let e = Browser.Dom.document.getElementById inpId
                        e.focus()
                    ()
                    // this one is ugly, what it does is: Do the related search after toggling directed search (by parent ontology) of/on.
                    //((AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState).OnInputChangeMsg model.TermSearchState.TermSearchText) |> dispatch
                )
                prop.text "Use related term directed search." ]

            Bulma.help [
                prop.style [style.display.inlineElement; style.float'.right] 
                a [OnClick (fun _ -> AdvancedSearch.ToggleModal (AutocompleteSearch.AutocompleteParameters<Term>.ofTermSearchState model.TermSearchState).ModalId |> AdvancedSearchMsg |> dispatch)] [
                    str "Use advanced search"
                ] |> prop.children
            ]
        ]

        // "Fill selected cells with this term" - button //

        // For some reason columns seem to be faulty here. Without the workaround of removing negative margin left and right from Columns.columns
        // It would not be full width. This results in the need to remove padding left/right for Column.column childs.
        Bulma.columns [
            Bulma.columns.isMobile;
            prop.style [style.width(length.perc 100); style.marginRight 0; style.marginLeft 0]
            prop.children [
                Bulma.column [
                    prop.style [style.paddingLeft 0; if model.TermSearchState.SelectedTerm.IsNone then style.paddingRight 0]
                    // Fill selection confirmation
                    Bulma.field.div [
                        Bulma.control.div [
                            Bulma.button.a [
                                let hasText = model.TermSearchState.TermSearchText.Length > 0
                                if hasText then
                                   prop.className "is-success"
                                    //Button.IsActive true
                                else
                                    prop.className "is-danger"
                                    prop.disabled true
                                Bulma.button.isFullWidth
                                prop.onClick (fun _ ->
                                    if hasText then
                                        let term = if model.TermSearchState.SelectedTerm.IsSome then TermMinimal.ofTerm model.TermSearchState.SelectedTerm.Value else TermMinimal.create model.TermSearchState.TermSearchText ""
                                        SpreadsheetInterface.InsertOntologyTerm term |> InterfaceMsg |> dispatch
                                )
                                prop.text "Fill selected cells with this term"
                            ]
                        ]
                    ]
                    |> prop.children
                ]
                if model.TermSearchState.SelectedTerm.IsSome then
                    Bulma.column [
                        prop.className "pr-0"
                        Bulma.column.isNarrow
                        Bulma.button.a [
                            prop.title "Copy to Clipboard"
                            Bulma.color.isInfo
                            prop.onClick (fun e ->
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
                            CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_termsearch" "fa-regular fa-clipboard" "fa-solid fa-check"
                            |> prop.children
                        ]
                        |> prop.children
                    ]
            ]
        ]
    ]

let termSearchComponent (model:Messages.Model) dispatch =
    div [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        Bulma.label "Ontology term search"

        Bulma.label "Search for an ontology term to fill into the selected field(s)"

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