module Spreadsheet.LocalStorage

open Fable.SimpleJson


/// stores maximum number of states to save, inclusive of active state
let MaxHistory = 31
let CurrentHistoryPosition_init = 0
let AvailableHistoryItems_init = 0

// ----!----
// I know these mutables are not best practise but i really did not want to add those to elmish.
// ----!----
/// stores current position
let mutable CurrentHistoryPosition = CurrentHistoryPosition_init
/// store how many states are saved currently
let mutable AvailableHistoryItems = AvailableHistoryItems_init


module Keys = 

    [<Literal>]
    let swate_session_history_key = "swate_session_history_key"

    [<Literal>]
    let swate_session_history_table_prefix = "swate-table-history"

    [<Literal>]
    let swate_session_history_position = "swate_session_history_position"

    let create_swate_session_history_table_key (tableGuid: System.Guid) = swate_session_history_table_prefix + tableGuid.ToString()

module GeneralHelpers =

    let tryGetSessionItem(key: string) =
        let v = Browser.WebStorage.sessionStorage.getItem(key)
        if isNull v then None else Some v

    let tryGetLocalItem(key: string) =
        let v = Browser.WebStorage.localStorage.getItem(key)
        if isNull v then None else Some v

open GeneralHelpers

type OrderList = {
    guids: System.Guid list
} with
    static member init = {
        guids = List.empty
    }
    static member ofJson (json: string) = Json.parseAs<OrderList>(json)
    static member fromSession() =
        let tryHistory = tryGetSessionItem Keys.swate_session_history_key
        match tryHistory with
        | Some historyJson -> OrderList.ofJson historyJson
        | None -> failwith "Could not find any history."
    member this.toJson() = Json.serialize this

type Spreadsheet.Model with
    static member ofJson (json: string) = Json.parseAs<Spreadsheet.Model>(json)
    static member fromSession(position: int) =
        let history = OrderList.fromSession()
        let guid = history.guids |> List.tryItem position
        if guid.IsNone then failwith "Not enough items in history list."
        let tryState = tryGetSessionItem (Keys.create_swate_session_history_table_key guid.Value)
        match tryState with
        | Some stateJson -> 
            Spreadsheet.Model.ofJson(stateJson)
        | None ->
            failwith "Could not find any history."

///<summary>Update mutable variables based on session storage. Must be done on main after checking if host <> Excel.</summary>
let onInit() =
    let position = tryGetSessionItem(Keys.swate_session_history_position)
    if position.IsSome then
        CurrentHistoryPosition <- int position.Value
    // wrap in try..with just to not throw errors if new session.
    try
        let nHistoryItems = OrderList.fromSession()
        AvailableHistoryItems <- nHistoryItems.guids.Length
    with
        | _ -> ()
    

///<summary>Get current history, find table state for specified position update table state.</summary>
let updateHistoryPosition (newPosition: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let isSmallerZero = newPosition < 0
    let isEqual = newPosition = CurrentHistoryPosition
    let isBiggerMax = newPosition >= MaxHistory
    match newPosition with
    | _ when isSmallerZero || isEqual || isBiggerMax ->
        state
    | _ ->
        /// Run this first so an error breaks the function before any mutables are changed
        let state = Spreadsheet.Model.fromSession(newPosition)
        Browser.WebStorage.sessionStorage.setItem(Keys.swate_session_history_position, string newPosition)
        CurrentHistoryPosition <- newPosition
        state


// Whenever tablesToSessionStorage is called, also generate a new GUID. Save the model with the "swate_session_history_table_prefix_GUID" as key in session storage.
// In addition save with key "swate_session_history_key" a list with the generated GUIDs in which the newest GUID is added first.
// Save the current history position with key "swate_session_history_position" in session storage.
// If we go back the history then update the "swate_session_history_position" and load the table with GUID at position "swate_session_history_position" as model.
// If the user changes something when "swate_session_history_position" <> 0 (up to date, newest), delete all positions up to "swate_session_history_position" and the corresponding tables.
///<summary>Save the next table state to session storage for history control. Table state is stored with guid as key and order is stored as guid list.</summary>
let tablesToSessionStorage (state: Spreadsheet.Model) : unit =
    /// recursively generate new guids, check if existing and if so repeat until new guid.
    let rec generateNewGuid() =
        let g = System.Guid.NewGuid()
        let key = Keys.create_swate_session_history_table_key g
        let try_key = tryGetSessionItem(key)
        // if key exists redo, else use key
        match try_key with
        | Some _ -> generateNewGuid()
        | None -> g, key
    /// newGuid will be used to store the order in a list
    /// newKey is used as key to store the table state
    let newGuid, newKey = generateNewGuid()
    let json = Json.serialize state
    // Adjust order list
    let history =
        let tryHistory = tryGetSessionItem(Keys.swate_session_history_key)
        match tryHistory with
        | Some history -> OrderList.ofJson(history)
        | None -> OrderList.init
    // Add new history state to current history order.
    let newHistory =
        // if e.g at position 4 and we create new table state from position 4 we want to delete position 0 .. 3 and use 4 as new 0
        let rebranchedList, toRemoveList1 =
            if CurrentHistoryPosition <> 0 then
                printfn "[HISTORY] Rebranch to %i" CurrentHistoryPosition
                history.guids
                |> List.splitAt CurrentHistoryPosition
                |> fun (remove, keep) -> keep, remove
            else
                history.guids, []
        let newlist = newGuid::rebranchedList
        /// Split list into two at index of MaxHistory. Kepp the list with index below MaxHistory and iterate over the other list to remove the stored states.
        let newlist, toRemoveList2 = if newlist.Length > MaxHistory then List.splitAt MaxHistory newlist else newlist, []
        let toRemoveList = toRemoveList1@toRemoveList2
        if List.isEmpty toRemoveList |> not then
            toRemoveList |> List.iter (fun guid ->
                let rmvKey = Keys.create_swate_session_history_table_key(guid)
                Browser.WebStorage.sessionStorage.removeItem(rmvKey)
            )
        { history with guids = newlist }
    // apply all mutable changes/non-type-safe changes after everything else went through
    // set current table state with key and guid
    Browser.WebStorage.sessionStorage.setItem(newKey, json)
    // set new table history
    Browser.WebStorage.sessionStorage.setItem(Keys.swate_session_history_key, newHistory.toJson())
    // reset new table position to 0
    Browser.WebStorage.sessionStorage.setItem(Keys.swate_session_history_position, "0")
    AvailableHistoryItems <- newHistory.guids.Length
    CurrentHistoryPosition <- 0
    printfn "[HISTORY] length: %i" newHistory.guids.Length
    ()

// Local Storage

[<Literal>]
let swate_spreadsheet_key = "swate_spreadsheet_key"

///<summary>This function sends the Spreadsheet.Model to local browser storage</summary>
let tablesToLocalStorage (state: Spreadsheet.Model) : unit =
    //Browser.WebStorage.localStorage.removeItem(swate_spreadsheet_key)
    let json = Json.serialize state
    Browser.WebStorage.localStorage.setItem(swate_spreadsheet_key, json)
    ()

///<summary>This function is very sensitive to changes to the Spreadsheet.Model. Be careful to change it.</summary>
let tableOfLocalStorage () : Spreadsheet.Model =
    let json = tryGetLocalItem(swate_spreadsheet_key)
    match json with
    | Some json -> Spreadsheet.Model.ofJson json
    | None      -> failwith $"No tables cached!"

let resetAll() =
    CurrentHistoryPosition <- CurrentHistoryPosition_init
    AvailableHistoryItems <- AvailableHistoryItems_init
    Browser.WebStorage.localStorage.clear()
    Browser.WebStorage.sessionStorage.clear()

type Spreadsheet.Model with
    ///</summary>This function tries to get the data model from local storage saved under "swate_spreadsheet_key"</summary>
    static member tryInitFromLocalStorage() =
        let try_swate_spreadsheet_key = tryGetLocalItem(swate_spreadsheet_key)
        match try_swate_spreadsheet_key with
        | Some json ->
            try 
                let state = Spreadsheet.Model.ofJson json
                tablesToSessionStorage state
                state
            with
                | _ -> Spreadsheet.Model.init()
        | None ->
            Spreadsheet.Model.init()
            

