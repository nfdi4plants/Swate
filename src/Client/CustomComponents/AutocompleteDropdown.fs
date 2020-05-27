module CustomComponents.AutocompleteDropdown

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Thoth.Json
open Thoth.Elmish
open ExcelColors
open Api
open Model
open Messages
open Update
open Shared

let autocompleteDropdownComponent (model:Model) (dispatch: Msg -> unit) (isVisible: bool) (isLoading:bool) (suggestions: ReactElement list)  =
    Container.container[] [
        Dropdown.content [Props [
            Style [
                if isVisible then Display DisplayOptions.Block else Display DisplayOptions.None
                //if model.ShowFillSuggestions then Display DisplayOptions.Block else Display DisplayOptions.None
                BackgroundColor model.SiteStyleState.ColorMode.ControlBackground
                BorderColor     model.SiteStyleState.ColorMode.ControlForeground
            ]]
        ] [
            Table.table [Table.IsFullWidth] [
                if isLoading then
                    tbody [] [
                        tr [] [
                            td [Style [TextAlign TextAlignOptions.Center]] [
                                Loading.loadingComponent
                                br []
                            ]
                        ]
                    ]
                else
                    tbody [] suggestions
            ]

            
        ]
    ]