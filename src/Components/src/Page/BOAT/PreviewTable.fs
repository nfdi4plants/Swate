namespace Components

open Feliz
open ARCtrl
open Types

open Components.FunctionsContextmenu

module PreviewTable =

    let table (annoState: Annotation list, setState: Annotation list -> unit, highlight, setHighlight) =

        let cellClass = "swt:border swt:border-black swt:px-3 swt:py-2 swt:min-w-32"

        Html.div [
            Html.table [
                prop.className "swt:border-b swt:border-black"
                prop.children [
                    if annoState = [] then
                        Html.none
                    else
                        Html.thead [
                            Html.tr [
                                Html.th [ prop.text "No."; prop.className cellClass ]
                                Html.th [ prop.text "Key"; prop.className cellClass ]
                                Html.th [ prop.text "KeyType"; prop.className cellClass ]
                                Html.th [ prop.text "Term"; prop.className cellClass ]
                                Html.th [ prop.text "Value (Unit)"; prop.className cellClass ]
                                Html.th [ prop.text ""; prop.className cellClass ]
                            ]
                        ]

                        Html.tbody [
                            for a in 0 .. annoState.Length - 1 do
                                Html.tr [
                                    prop.children [
                                        Html.td [ prop.text (a + 1); prop.className cellClass ]
                                        Html.td [
                                            prop.text (annoState[a].Search.Key.NameText)
                                            prop.className cellClass
                                        ]
                                        Html.td [
                                            prop.text (annoState[a].Search.KeyType.ToString())
                                            prop.className cellClass
                                        ]
                                        match annoState[a].Search.Body with
                                        | CompositeCell.Term oa ->
                                            Html.td [ prop.text oa.NameText; prop.className cellClass ]
                                            Html.td [ prop.text ""; prop.className cellClass ]
                                        | CompositeCell.Unitized(v, oa) ->
                                            Html.td [ prop.text oa.NameText; prop.className cellClass ]
                                            Html.td [ prop.text v; prop.className cellClass ]
                                        | _ -> ()
                                        Html.td [
                                            prop.className cellClass
                                            prop.children [
                                                Html.div [
                                                    prop.className "swt:flex swt:justify-center swt:items-center"
                                                    prop.children [
                                                        Html.button [
                                                            prop.className "swt:cursor-pointer"
                                                            prop.onClick (fun _ ->
                                                                let newAnnoList: Annotation list =
                                                                    annoState
                                                                    |> List.filter (fun x -> x = annoState[a] |> not)

                                                                setState newAnnoList

                                                                let newHighlight = {
                                                                    Keys =
                                                                        highlight.Keys
                                                                        |> Map.remove annoState[a].Height
                                                                    Terms =
                                                                        highlight.Terms
                                                                        |> Map.remove annoState[a].Height
                                                                    Values =
                                                                        highlight.Values
                                                                        |> Map.remove annoState[a].Height
                                                                }

                                                                setHighlight newHighlight
                                                            )
                                                            prop.children [
                                                                Html.i [
                                                                    prop.className
                                                                        "swt:iconify swt:fluent--delete-24-regular swt:size-4"
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
            ]
        ]
