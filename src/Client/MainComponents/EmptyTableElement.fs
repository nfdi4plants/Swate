namespace MainComponents

open Feliz
open Feliz.Bulma
open ARCtrl

type EmptyTableElement =
    static member Main() =
        Html.div [
            prop.className "is-flex is-justify-content-center is-align-items-center"
            prop.style [style.height (length.perc 100)]
            prop.children [
                Bulma.box [
                    Bulma.content [
                        prop.children [
                            Html.h1 [
                                prop.className "title"
                                prop.text "No data to display"
                            ]
                            Html.h2 [
                                prop.className "subtitle"
                                prop.text "Please upload a file or create a new table"
                            ]
                        ]
                    ]
                ]
            ]
        ]