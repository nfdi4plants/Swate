module TermSearchView

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared
open CustomComponents


//let createTermSuggestions (model:Model) (dispatch: Msg -> unit) =
//    if model.TermSearchState.TermSuggestions.Length > 0 then
//        model.TermSearchState.TermSuggestions
//        |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
//        |> Array.map (fun sugg ->
//            tr [OnClick (fun _ -> sugg.Name |> TermSuggestionUsed |> Simple |> TermSearch |> dispatch)
//                colorControl model.SiteStyleState.ColorMode
//                Class "suggestion"
//            ] [
//                td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
//                    Fa.i [Fa.Solid.InfoCircle] []
//                ]
//                td [] [
//                    b [] [str sugg.Name]
//                ]
//                td [Style [Color "red"]] [if sugg.IsObsolete then str "obsolete"]
//                td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
//            ])
//        |> List.ofArray
//    else
//        [
//            tr [] [
//                td [] [str "No terms found matching your input."]
//            ]
//        ]

let simpleSearchComponent (model:Model) (dispatch: Msg -> unit) =
    Field.div [] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]
        br []

        AutocompleteSearch.autocompleteTermSearchComponent
            dispatch
            model.SiteStyleState.ColorMode
            model
            "Start typing to search for terms"
            (Some Size.IsLarge)
            (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofTermSearchState model.TermSearchState)

        //Control.div [] [
            
            //Input.input [   Input.Placeholder "Start typing to start search"
            //                Input.Size Size.IsLarge
            //                Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
            //                Input.OnChange (fun e ->  e.Value |> SearchTermTextChange |> Simple |> TermSearch |> dispatch)
            //                Input.Value model.TermSearchState.Simple.TermSearchText
            //            ]
            //AutocompleteDropdown.autocompleteDropdownComponent
            //    model
            //    dispatch
            //    model.TermSearchState.Simple.ShowSuggestions
            //    model.TermSearchState.Simple.HasSuggestionsLoading
            //    (createTermSuggestions model dispatch)
        //]
        Help.help [] [str "When applicable, search for an ontology term to fill into the selected field(s)"]
    ]



let termSearchComponent (model : Model) (dispatch : Msg -> unit) =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.keyCode) = 13 then k.preventDefault())
    ] [
        simpleSearchComponent model dispatch

        // Fill selection confirmation
        Field.div [] [
            Control.div [] [
                Button.button   [   let hasText = model.TermSearchState.TermSearchText.Length > 0
                                    if hasText then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    Button.OnClick (fun _ -> model.TermSearchState.TermSearchText |> FillSelection |> ExcelInterop |> dispatch)

                                ] [
                    str "Fill selected cells with this term"
                    
                ]
            ]
        ]
    ]