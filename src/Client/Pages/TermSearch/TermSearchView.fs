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

[<ReactComponent>]
let Main (model:Messages.Model, dispatch) =
    let state, setState = React.useState(OntologyAnnotation.empty)
    let setTerm = fun (term: OntologyAnnotation option) -> TermSearch.UpdateSelectedTerm term |> TermSearchMsg |> dispatch
    div [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        pageHeader "Ontology term search"

        Bulma.label "Search for an ontology term to fill into the selected field(s)"

        Components.TermSearch.Input(setTerm, fullwidth=true, size=Bulma.input.isLarge, ?parent'=model.TermSearchState.ParentTerm)

        Html.div state.NameText

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