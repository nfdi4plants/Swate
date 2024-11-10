module Modals.TermModal

open Shared.Database
open Feliz
open Feliz.DaisyUI
open ARCtrl
open Shared
open Components

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

    Daisy.modal.div [
        modal.active
        prop.children [
            Daisy.modalBackdrop [
                prop.onClick rmv
                prop.style [style.backgroundColor.transparent]
            ]
            Daisy.modalBox.div [
                prop.className "shadow"
                prop.children [
                    Daisy.cardActions [
                        prop.className "justify-end"
                        prop.children [
                            Components.Components.DeleteButton(props=[prop.onClick rmv])
                        ]
                    ]
                    Daisy.card [
                        Daisy.cardBody [
                            Daisy.cardTitle [
                                Html.h3 [ prop.className "font-bold"; prop.text oa.NameText]
                                Html.div [ prop.className "text-xs"; prop.text oa.TermAccessionShort]
                            ]
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
                                            prop.className "text-error"
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