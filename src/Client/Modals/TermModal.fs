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

    let mkInfoPart (txt: string) (desc: string) =
        Html.div [
            Html.h3 [
                prop.className "text-lg font-semibold"
                prop.text txt
            ]
            if desc <> "" then
                Html.p [
                    prop.text desc
                ]
        ]

    Daisy.modal.div [
        modal.active
        prop.children [
            Daisy.modalBackdrop [
                prop.onClick rmv
                prop.style [style.backgroundColor.transparent]
            ]
            Daisy.modalBox.div [
                prop.children [
                    Daisy.card [
                        Daisy.cardBody [
                            Daisy.cardTitle [
                                prop.className "flex flex-row justify-between"
                                prop.children [
                                    Html.div [
                                        Html.h3 [ prop.className "font-bold"; prop.text oa.NameText]
                                        Html.div [ prop.className "text-xs"; prop.text oa.TermAccessionShort]
                                    ]
                                    Components.Components.DeleteButton(props=[prop.onClick rmv])
                                ]
                            ]
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    match state with
                                    | Loading -> Html.p "loading .."
                                    | NotFound ->
                                        Html.p [
                                            prop.dangerouslySetInnerHTML $"Unable to find term with id <b>{oa.TermAccessionShort}</b> in database."
                                        ]
                                    | Found term ->
                                        mkInfoPart
                                            "Description"
                                            term.Description
                                        mkInfoPart
                                            "Source Ontology"
                                            term.FK_Ontology
                                        if term.IsObsolete then
                                            Html.p [
                                                prop.className "text-error"
                                                prop.text "Obsolete"
                                            ]
                                        Html.a [
                                            prop.className "space-x-2 float-right link-primary"
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
    ]