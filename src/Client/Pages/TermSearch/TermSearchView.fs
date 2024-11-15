module TermSearch

open Fable.React
open Fable.React.Props
open ExcelColors
open Messages
open Shared
open Elmish
open TermSearch
open Model


let update (termSearchMsg: TermSearch.Msg) (currentState:TermSearch.Model) : TermSearch.Model * Cmd<Messages.Msg> =
    match termSearchMsg with
    // Toggle the search by parent ontology option on/off by clicking on a checkbox
    | TermSearch.UpdateParentTerm oa ->
        {currentState with ParentTerm = oa}, Cmd.none
    | TermSearch.UpdateSelectedTerm oa ->
        {currentState with SelectedTerm = oa}, Cmd.none

open Feliz
open Feliz.DaisyUI
open ARCtrl
open Fable.Core.JsInterop

/// "Fill selected cells with this term" - button //
let private addButton (model: Model, dispatch) =

    // For some reason columns seem to be faulty here. Without the workaround of removing negative margin left and right from Columns.columns
    // It would not be full width. This results in the need to remove padding left/right for Column.column childs.
    Html.div [
        prop.style [style.width(length.perc 100); style.marginRight 0; style.marginLeft 0]
        prop.children [
            Daisy.button.a [
                let hasTerm = model.TermSearchState.SelectedTerm.IsSome
                if hasTerm then
                    button.success
                    //Button.IsActive true
                else
                    button.error
                    prop.disabled true
                prop.onClick (fun _ ->
                    if hasTerm then
                        let oa = model.TermSearchState.SelectedTerm.Value
                        SpreadsheetInterface.InsertOntologyAnnotation oa |> InterfaceMsg |> dispatch
                )
                prop.text "Fill selected cells with this term"
            ]
        ]
    ]
            //if model.TermSearchState.SelectedTerm.IsSome then
            //    Bulma.column [
            //        prop.className "pr-0"
            //        Bulma.column.isNarrow
            //        Daisy.button.a [
            //            prop.title "Copy to Clipboard"
            //            button.info
            //            prop.onClick (fun e ->
            //                // trigger icon response
            //                CustomComponents.ResponsiveFA.triggerResponsiveReturnEle "clipboard_termsearch"
            //                //
            //                let t = model.TermSearchState.SelectedTerm.Value
            //                let txt = [t.Name; t.Accession |> Shared.URLs.termAccessionUrlOfAccessionStr; t.Accession.Split(@":").[0] ] |> String.concat System.Environment.NewLine
            //                let textArea = Browser.Dom.document.createElement "textarea"
            //                textArea?value <- txt
            //                textArea?style?top <- "0"
            //                textArea?style?left <- "0"
            //                textArea?style?position <- "fixed"

            //                Browser.Dom.document.body.appendChild textArea |> ignore

            //                textArea.focus()
            //                // Can't belive this actually worked
            //                textArea?select()

            //                let t = Browser.Dom.document.execCommand("copy")
            //                Browser.Dom.document.body.removeChild(textArea) |> ignore
            //                ()
            //            )
            //            CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_termsearch" "fa-regular fa-clipboard" "fa-solid fa-check"
            //            |> prop.children
            //        ]
            //        |> prop.children
            //    ]

[<ReactComponent>]
let Main (model:Model, dispatch) =
    let setTerm = fun (term: OntologyAnnotation option) -> TermSearch.UpdateSelectedTerm term |> TermSearchMsg |> dispatch
    SidebarComponents.SidebarLayout.Container  [
        SidebarComponents.SidebarLayout.Header "Ontology term search"

        SidebarComponents.SidebarLayout.Description "Search for an ontology term to fill into the selected field(s)"

        SidebarComponents.SidebarLayout.LogicContainer [
            Components.TermSearch.Input(setTerm, fullwidth=true, size=button.lg, ?parent=model.TermSearchState.ParentTerm, advancedSearchDispatch=dispatch)
            addButton(model, dispatch)
        ]
    ]
        //simpleSearchComponent model dispatch

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