module Model

open Fable.React
open Fable.React.Props
open Fulma
open Shared
open Thoth.Elmish

type LogItem =
    | Debug of (System.DateTime*string)
    | Info  of (System.DateTime*string)
    | Error of (System.DateTime*string)

    static member toTableRow = function
        | Debug (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "green"; FontWeight "bold"]] [str "Debug"]
                td [] [str m]
            ]
        | Info  (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "lightblue"; FontWeight "bold"]] [str "Info"]
                td [] [str m]
            ]
        | Error (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "red"; FontWeight "bold"]] [str "ERROR"]
                td [] [str m]
            ]

    static member ofStringNow (level:string) (message: string) =
        match level with
        | "Debug" -> Debug(System.DateTime.UtcNow,message)
        | "Info"  -> Info (System.DateTime.UtcNow,message)
        | "Error" -> Error(System.DateTime.UtcNow,message)

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = {
    //Debouncing
    Debouncer               : Debouncer.State

    //Error handling
    LastFullError           : System.Exception option
    Log                     : LogItem list

    //Site Meta Options (Styling etc)
    DisplayMessage          : string
    BurgerVisible           : bool
    IsDarkMode              : bool
    ColorMode               : ExcelColors.ColorMode
    ShowFillSuggestions     : bool
    ShowFillSuggestionUsed  : bool
    HadFirstSuggestion      : bool
    HasSuggestionsLoading   : bool

    //Data for the App
    FillSelectionText       : string
    TermSuggestions         : DbDomain.Term []
    AddColumnText           : string
    }