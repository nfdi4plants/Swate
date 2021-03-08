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
//                td [Style [Color ""]] [if sugg.IsObsolete then str "obsolete"]
//                td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
//            ])
//        |> List.ofArray
//    else
//        [
//            tr [] [
//                td [] [str "No terms found matching your input."]
//            ]
//        ]


open Fable.Core
open Fable.Core.JsInterop

let simpleSearchComponent (model:Model) (dispatch: Msg -> unit) =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ] [
        Field.div [] [
            AutocompleteSearch.autocompleteTermSearchComponentOfParentOntology
                dispatch
                model.SiteStyleState.ColorMode
                model
                "Start typing to search for terms"
                (Some Size.IsLarge)
                (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofTermSearchState model.TermSearchState)

        ]

        div [][
            Switch.switchInline [
                Switch.Color IsPrimary
                Switch.Id "switch-1"
                Switch.Checked model.TermSearchState.SearchByParentOntology
                Switch.OnChange (fun e ->
                    ToggleSearchByParentOntology |> TermSearch |> dispatch
                    let _ =
                        let inpId = (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofTermSearchState model.TermSearchState).InputId
                        let e = Browser.Dom.document.getElementById inpId
                        e.focus()
                    ()
                    // this one is ugly, what it does is: Do the related search after toggling directed search (by parent ontology) of/on.
                    //((AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofTermSearchState model.TermSearchState).OnInputChangeMsg model.TermSearchState.TermSearchText) |> dispatch
                )
            ] [ str "Use related term directed search." ]

            Help.help [ Help.Props [Style [Display DisplayOptions.Inline; Float FloatOptions.Right]] ] [
                a [OnClick (fun _ -> ToggleModal (AutocompleteSearch.AutocompleteParameters<DbDomain.Term>.ofTermSearchState model.TermSearchState).ModalId |> AdvancedSearch |> dispatch)] [
                    str "Use advanced search"
                ] 
            ]
        ]

        /// For some reason columns seam to be faulty here. Without the workaround of removing negative margin left and right from Columns.columns
        /// It would not be full width. This results in the need to remove padding left/right for Column.column childs.
        Columns.columns [
            Columns.IsMobile;
            Columns.Props [Style [Width "100%"; MarginRight "0px"; MarginLeft "0px"]]
        ][
            Column.column [Column.Props [Style [PaddingLeft "0"; if model.TermSearchState.SelectedTerm.IsNone then PaddingRight "0"]]] [
            // Fill selection confirmation
                Field.div [] [
                    Control.div [] [
                        Button.a [
                            let hasText = model.TermSearchState.TermSearchText.Length > 0
                            if hasText then
                                Button.CustomClass "is-success"
                                //Button.IsActive true
                            else
                                Button.CustomClass "is-danger"
                            Button.Props [
                                Disabled (not hasText)
                            ]
                            Button.IsFullWidth
                            Button.OnClick (fun _ ->
                                if hasText then
                                    FillSelection (model.TermSearchState.TermSearchText, model.TermSearchState.SelectedTerm) |> ExcelInterop |> dispatch
                            )
                        ] [
                            str "Fill selected cells with this term"
                        ]
                    ]
                ]
            ]
            if model.TermSearchState.SelectedTerm.IsSome then
                Column.column [
                    Column.Props [Style [PaddingRight "0"]]
                    Column.Width (Screen.All, Column.IsNarrow)
                ] [
                    Button.a [
                        Button.Props [Title "Copy to Clipboard"]
                        Button.Color IsInfo
                        Button.OnClick (fun e ->
                            /// trigger icon response
                            CustomComponents.ResponsiveFA.triggerResponsiveReturnEle "clipboard_termsearch"
                            //
                            let t = model.TermSearchState.SelectedTerm.Value
                            let txt = [t.Name; t.Accession |> Shared.URLs.termAccessionUrlOfAccessionStr; t.Accession.Split(@":").[0] ] |> String.concat System.Environment.NewLine
                            let textArea = Browser.Dom.document.createElement "textarea"
                            textArea?value <- txt
                            textArea?style?top <- "0"
                            textArea?style?left <- "0"
                            textArea?style?position <- "fixed"

                            Browser.Dom.document.body.appendChild textArea |> ignore

                            textArea.focus()
                            /// Can't belive this actually worked
                            textArea?select()

                            let t = Browser.Dom.document.execCommand("copy")
                            Browser.Dom.document.body.removeChild(textArea) |> ignore
                            ()
                        )
                    ][
                        CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_termsearch" Fa.Regular.Clipboard Fa.Solid.Check
                    ]
                ]
        ]
    ]



let termSearchComponent (model : Model) (dispatch : Msg -> unit) =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Ontology term search"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Search for an ontology term to fill into the selected field(s)"]

        simpleSearchComponent model dispatch

        //if model.TermSearchState.SelectedTerm.IsNone then
        //    str "No Term Selected"
        //else
        //    str (sprintf "%A" model.TermSearchState.SelectedTerm.Value)

        //Button.button [
        //    Button.OnClick (fun e ->
        //        GetParentOntology |> ExcelInterop |> dispatch
        //    )
        //][
        //    str "GetParentOntology"
        //]

        //if model.TermSearchState.ParentOntology.IsNone then
        //    str "No Parent Ontology selected"
        //else
        //    str model.TermSearchState.ParentOntology.Value
    ]