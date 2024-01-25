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
    | TermSearch.UpdateParentTerm oa ->
        {currentState with ParentTerm = oa}, Cmd.none
    | TermSearch.UpdateSelectedTerm oa ->
        {currentState with SelectedTerm = oa}, Cmd.none

open Feliz
open Feliz.Bulma
open ARCtrl.ISA
open Fable.Core.JsInterop

/// "Fill selected cells with this term" - button //
let private addButton (model: Messages.Model, dispatch) =

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
                            let hasTerm = model.TermSearchState.SelectedTerm.IsSome
                            if hasTerm then
                                prop.className "is-success"
                                //Button.IsActive true
                            else
                                prop.className "is-danger"
                                prop.disabled true
                            Bulma.button.isFullWidth
                            prop.onClick (fun _ ->
                                if hasTerm then
                                    let oa = model.TermSearchState.SelectedTerm.Value
                                    SpreadsheetInterface.InsertOntologyAnnotation oa |> InterfaceMsg |> dispatch
                            )
                            prop.text "Fill selected cells with this term"
                        ]
                    ]
                ]
                |> prop.children
            ]
            //if model.TermSearchState.SelectedTerm.IsSome then
            //    Bulma.column [
            //        prop.className "pr-0"
            //        Bulma.column.isNarrow
            //        Bulma.button.a [
            //            prop.title "Copy to Clipboard"
            //            Bulma.color.isInfo
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
        ]
    ]

[<ReactComponent>]
let Main (model:Messages.Model, dispatch) =
    let setTerm = fun (term: OntologyAnnotation option) -> TermSearch.UpdateSelectedTerm term |> TermSearchMsg |> dispatch
    div [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [
        pageHeader "Ontology term search"

        Bulma.label "Search for an ontology term to fill into the selected field(s)"

        mainFunctionContainer [
            Bulma.field.div [
                Components.TermSearch.Input(setTerm, fullwidth=true, size=Bulma.input.isLarge, ?parent'=model.TermSearchState.ParentTerm, advancedSearchDispatch=dispatch)
            ]
            addButton(model, dispatch)
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
    ]