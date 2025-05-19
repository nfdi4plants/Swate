namespace Modals

open Swate.Components.Shared.Database
open Feliz
open Feliz.DaisyUI
open ARCtrl
open Swate.Components
open Swate.Components.Shared


module private TermModalUtil =
    type State =
        | Loading
        | Found of Database.Term
        | NotFound

open TermModalUtil

type TermModal =

    [<ReactComponent>]
    static member Main(oa: OntologyAnnotation, dispatch) =

        let state, setState = React.useState (State.Loading)
        let rmv = Util.RMV_MODAL dispatch

        React.useEffectOnce (fun _ ->
            async {
                let! term = Api.ontology.getTermById (oa.TermAccessionShort)

                match term with
                | Some t -> Found t |> setState
                | None -> NotFound |> setState
            }
            |> Async.StartImmediate)

        let mkInfoPart (txt: string) (desc: string) =
            Html.div [
                Html.h3 [ prop.className "text-lg font-semibold"; prop.text txt ]
                if desc <> "" then
                    Html.p [ prop.text desc ]
            ]

        let headerElement =
            Html.div [
                Html.span [ prop.className "font-bold"; prop.text oa.NameText ]
                Html.div [ prop.className "text-xs"; prop.text oa.TermAccessionShort ]
            ]

        let modalActivity =
            Html.div [
                prop.className "space-y-4"
                prop.children [
                    match state with
                    | Loading -> Html.p "loading .."
                    | NotFound ->
                        Html.p [
                            prop.dangerouslySetInnerHTML
                                $"Unable to find term with id <b>{oa.TermAccessionShort}</b> in database."
                        ]
                    | Found term ->
                        mkInfoPart "Description" term.Description
                        mkInfoPart "Source Ontology" term.FK_Ontology

                        if term.IsObsolete then
                            Html.p [ prop.className "text-error"; prop.text "Obsolete" ]

                        Html.a [
                            prop.className "space-x-2 float-right link-primary"
                            prop.href (OntologyAnnotation.fromDBTerm term |> _.TermAccessionOntobeeUrl)
                            prop.target.blank
                            prop.children [ Html.span "Ref"; Html.i [ prop.className "fas fa-external-link-alt" ] ]
                        ]
                ]
            ]

        Swate.Components.BaseModal.BaseModal(rmv, header = Html.p headerElement, modalActivity = modalActivity)