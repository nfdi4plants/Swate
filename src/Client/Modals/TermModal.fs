module Modals.TermModal

open Shared.TermTypes
open Feliz
open Feliz.Bulma
open ARCtrl
open Shared

type private State =
    | Loading
    | Found of Term
    | NotFound

[<ReactComponent>]
let Main (oa: OntologyAnnotation, dispatch) (rmv: _ -> unit) =

    let state, setState = React.useState(State.Loading)

    React.useEffectOnce (fun _ ->
        async {
            let! term = Api.ontology.getTermById(oa.TermAccessionShort)
            match term with
            | Some t -> Found t |> setState
            | None -> NotFound |> setState
        }
        |> Async.StartImmediate
    )

    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [
                prop.onClick rmv
                prop.style [style.backgroundColor.transparent]
            ]
            Bulma.modalCard [
                prop.className "shadow"
                prop.children [
                    Bulma.modalCardHead [
                        Bulma.modalCardTitle [
                            Bulma.title.h4 oa.NameText
                            Bulma.subtitle.h6 oa.TermAccessionShort
                        ]
                        Bulma.delete [
                            Bulma.delete.isSmall
                            prop.onClick rmv
                        ]
                    ]
                    Bulma.modalCardBody [
                        prop.className "content"
                        prop.children [
                            Html.div [
                                match state with
                                | Loading -> Html.p "loading .."
                                | NotFound ->
                                    Html.p [
                                        prop.dangerouslySetInnerHTML $"Unable to find term with id <b>{oa.TermAccessionShort}</b> in database."
                                    ]
                                | Found term ->
                                    Html.h5 "Description"
                                    Html.p term.Description
                                    Html.h5 "Source Ontology"
                                    Html.p term.FK_Ontology
                                    if term.IsObsolete then
                                        Html.p [
                                            color.hasTextDanger
                                            prop.text "Obsolete"
                                        ]
                                    Html.a [
                                        prop.className "space-x-2 float-right"
                                        prop.href (OntologyAnnotation.fromTerm term |> _.TermAccessionOntobeeUrl)
                                        prop.target.blank
                                        prop.children [
                                            Html.span "Ref"
                                            Html.i [
                                                prop.className "fas fa-external-link-alt"
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]