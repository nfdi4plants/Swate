module Modals.TermModal

open Shared.TermTypes
open Feliz
open Feliz.Bulma
open ARCtrl

let private l = 200

let private isTooLong (str:string) = str.Length > l

type private State =
    | Loading
    | Found of Term
    | NotFound

//type private UI = {
//    DescriptionTooLong: bool
//    IsExpanded: bool
//    DescriptionShort: string
//    DescriptionLong: string
//} with
//    static member init(description: string) =
//        let isTooLong = isTooLong description
//        let descriptionShort =
//            if isTooLong then description.Substring(0,l).Trim() + ".. " else description
//        {
//            IsExpanded = false
//            DescriptionTooLong = isTooLong
//            DescriptionShort = descriptionShort
//            DescriptionLong = description
//        }


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
                prop.style [style.maxWidth 300; style.border (1,borderStyle.solid,NFDIColors.black); style.borderRadius 0]
                prop.children [
                    Bulma.modalCardHead [
                        prop.className "p-2"
                        prop.children [
                            Bulma.modalCardTitle [
                                prop.textf "%s - %s" oa.NameText oa.TermAccessionShort 
                            ]
                            Bulma.delete [
                                Bulma.delete.isSmall
                                prop.onClick rmv
                            ]
                        ]
                    ]
                    Bulma.modalCardBody [
                        prop.className "p-2 has-text-justified"
                        prop.children [
                            Bulma.content [
                                match state with
                                | Loading -> Html.p "loading .."
                                | NotFound ->
                                    Html.p [
                                        prop.dangerouslySetInnerHTML $"Unable to find term with id <b>{oa.TermAccessionShort}</b> in database."
                                    ]
                                | Found term ->
                                    Html.h6 "Description"
                                    Html.p term.Description
                                    Html.h6 "Source Ontology"
                                    Html.p term.FK_Ontology
                                    if term.IsObsolete then
                                        Html.p [
                                            color.hasTextDanger
                                            prop.text "Obsolete"
                                        ] 
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]