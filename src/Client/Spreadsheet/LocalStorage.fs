module Spreadsheet.LocalStorage

open Fable.SimpleJson

[<Literal>]
let swate_spreadsheet_key = "swate_spreadsheet_key"

///<summary>This function sends the Spreadsheet.Model to local browser storage</summary>
let tablesToLocalStorage (state: Spreadsheet.Model) : Spreadsheet.Model =
    Browser.WebStorage.localStorage.removeItem(swate_spreadsheet_key)
    let json = Json.serialize state
    Browser.WebStorage.localStorage.setItem(swate_spreadsheet_key, json)
    state

///<summary>This function is very sensitive to changes to the Spreadsheet.Model. Be careful to change it.</summary>
let tableOfLocalStorage () : Spreadsheet.Model =
    let json = Browser.WebStorage.localStorage.getItem(swate_spreadsheet_key)
    let state_result = Json.tryParseAs<Spreadsheet.Model>(json)
    match state_result with
    | Ok state -> state
    | Error e -> failwith $"Unable to load stored tables from Browser. {e}"

type Spreadsheet.Model with
    static member tryInitFromLocalStorage() =
        let ls = Browser.WebStorage.localStorage
        let count = ls.length
        let keys = [for i in 0. .. float count-1. do yield ls.key i]
        if keys |> Seq.contains swate_spreadsheet_key then
            let state = tableOfLocalStorage()
            state
        else
            Spreadsheet.Model.init()

