module Modals.TermModal

open Shared.TermTypes
open Feliz
open Feliz.Bulma

let l = 200

let private isTooLong (str:string) = str.Length > l

type private UI = {
    DescriptionTooLong: bool
    IsExpanded: bool
    DescriptionShort: string
    DescriptionLong: string
} with
    static member init(description: string) =
        let isTooLong = isTooLong description
        let descriptionShort =
            if isTooLong then description.Substring(0,l).Trim() + ".. " else description
        {
            IsExpanded = false
            DescriptionTooLong = isTooLong
            DescriptionShort = descriptionShort
            DescriptionLong = description
        }


[<ReactComponent>]
let Main (term: Term, dispatch) (rmv: _ -> unit) =

    let state, setState = React.useState(UI.init(term.Description))

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
                                Bulma.size.isSize6
                                prop.text $"{term.Name} ({term.Accession})" 
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
                                Html.p [
                                    Html.span ( if state.IsExpanded then state.DescriptionLong else state.DescriptionShort)
                                    if state.DescriptionTooLong && not state.IsExpanded then
                                        Html.a [
                                            prop.onClick(fun _ -> setState {state with IsExpanded = true})
                                            prop.text "(more)"
                                        ]
                                ]
                                //if term.IsObsolete then
                                Html.div [
                                    prop.style [style.display.flex; style.justifyContent.spaceBetween]
                                    prop.children [
                                        Html.span [
                                            Html.a [
                                                prop.href (Shared.URLs.OntobeeOntologyPrefix + term.FK_Ontology)
                                                prop.text "Source"
                                            ]
                                        ]
                                        if term.IsObsolete then
                                            Html.span [
                                                Bulma.color.hasTextDanger
                                                prop.text "obsolete"
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