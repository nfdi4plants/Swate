module LocalHistory

open Fable.Core
open JsInterop
open System

open Fable.SimpleJson
open IndexedDB

module GeneralHelpers =

    let pako: obj = importAll "pako"

    /// <summary>
    /// Get specific item from session storage
    /// </summary>
    /// <param name="key"></param>
    let tryGetSessionItem(key: string) =
        let v = Browser.WebStorage.sessionStorage.getItem(key)
        if isNull v then None else Some v

    /// <summary>
    /// Get specific item from local storage
    /// </summary>
    /// <param name="key"></param>
    let tryGetLocalItem(key: string) =
        let v = Browser.WebStorage.localStorage.getItem(key)
        if isNull v then None else Some v

    /// <summary>
    /// Compress a JSON string using pako (GZIP) and encode it as a Base64 string
    /// </summary>
    /// <param name="json"></param>
    let compressJsonToBase64String (json: string) =
        let jsonBytes = Text.Encoding.UTF8.GetBytes(json)
        ///Import pako library from JavaScript
        let compressedBytes: byte[] = pako?deflate(jsonBytes)
        Convert.ToBase64String(compressedBytes)

    /// <summary>
    /// Decompress a Base64 string using pako (GZIP)
    /// </summary>
    /// <param name="json"></param>
    let decompressBase64ToJson (json: string) =
        let compressedBytes = Convert.FromBase64String(json)
        let jsonBytes: byte[] = pako?inflate(compressedBytes)
        Text.Encoding.UTF8.GetString(jsonBytes)

module Keys = 

    [<Literal>]
    let swate_local_spreadsheet_key = "swate_local_spreadsheet_key"

    [<Literal>]
    let swate_session_history_key = "swate_session_history_key"

    [<Literal>]
    let swate_session_history_table_prefix = "swate-table-history"

    [<Literal>]
    let swate_session_history_position = "swate_session_history_position"

    let create_swate_session_history_table_key (tableGuid: System.Guid) = swate_session_history_table_prefix + tableGuid.ToString()

module HistoryOrder =
    let ofJson (json: string) = Json.parseAs<System.Guid list>(json)
    let tryFromSession() =
        let tryHistory = GeneralHelpers.tryGetSessionItem Keys.swate_session_history_key
        match tryHistory with
        | Some historyJson -> ofJson historyJson |> Some
        | None -> None
    let toJson(history: System.Guid list) = Json.serialize history

//type OrderList = {
//    guids: System.Guid list
//} with
//    static member init = {
//        guids = List.empty
//    }
//    static member ofJson (json: string) = Json.parseAs<OrderList>(json)
//    static member fromSession() =
//        let tryHistory = tryGetSessionItem Keys.swate_session_history_key
//        match tryHistory with
//        | Some historyJson -> OrderList.ofJson historyJson
//        | None -> failwith "Could not find any history."
//    member this.toJson() = Json.serialize this

module ConversionTypes =  
    open ARCtrl
    open ARCtrl.Json
    open Shared
    open Fable.Core

    [<RequireQualifiedAccess>]
    type JsonArcFiles =
    | Investigation
    | Study
    | Assay
    | Template
    | None

    [<Emit("performance.now")>]
    let performanceNow: unit -> int = jsNative

    type SessionStorage = {
        JsonArcFiles: JsonArcFiles
        JsonString: string
        ActiveView: Spreadsheet.ActiveView
    } with
        static member fromSpreadsheetModel (model: Spreadsheet.Model) =
            let start = performanceNow()
            let jsonArcFile, jsonString =
                match model.ArcFile with
                | Some (ArcFiles.Investigation i) -> JsonArcFiles.Investigation, ArcInvestigation.toJsonString 0 i
                | Some (ArcFiles.Study (s, al)) -> JsonArcFiles.Study, ArcStudy.toJsonString 0 s
                | Some (ArcFiles.Assay a) -> JsonArcFiles.Assay, ArcAssay.toJsonString 0 a
                | Some (ArcFiles.Template t) -> JsonArcFiles.Template, Template.toJsonString 0 t
                | None -> JsonArcFiles.None, ""

            let compressedJsonString = GeneralHelpers.compressJsonToBase64String jsonString
            let ent = performanceNow()
            log(ent - start)
            {
                JsonArcFiles    = jsonArcFile 
                JsonString      = compressedJsonString
                ActiveView      = model.ActiveView
            }

        member this.ToSpreadsheetModel() = 
            let init = Spreadsheet.Model.init()
            try
                let arcFile =
                    match this.JsonArcFiles with
                    | JsonArcFiles.Investigation ->
                        let decompressedString = GeneralHelpers.decompressBase64ToJson this.JsonString
                        ArcInvestigation.fromJsonString decompressedString |> ArcFiles.Investigation |> Some
                    | JsonArcFiles.Study ->
                        let decompressedString = GeneralHelpers.decompressBase64ToJson this.JsonString
                        let s = ArcStudy.fromJsonString decompressedString
                        ArcFiles.Study(s, []) |> Some
                    | JsonArcFiles.Assay ->
                        let decompressedString = GeneralHelpers.decompressBase64ToJson this.JsonString
                        ArcAssay.fromJsonString decompressedString |> ArcFiles.Assay |> Some
                    | JsonArcFiles.Template ->
                        let decompressedString = GeneralHelpers.decompressBase64ToJson this.JsonString
                        Template.fromJsonString decompressedString |> ArcFiles.Template |> Some
                    | JsonArcFiles.None -> None
                {
                    init with
                        ActiveView = this.ActiveView
                        ArcFile = arcFile
                }
            with
                | _ -> init

        static member toSpreadsheetModel (sessionStorage: SessionStorage) =
            sessionStorage.ToSpreadsheetModel()

type Spreadsheet.Model with
    static member fromJsonString (json: string) = 
        let conversionModel = Json.tryParseAs<ConversionTypes.SessionStorage>(json)
        match conversionModel with
        | Ok m      -> m.ToSpreadsheetModel()
        | Error e   -> 
            log ("Error trying to read Spreadsheet.Model from local storage: ", e)
            Spreadsheet.Model.init()
        
    member this.ToJsonString() =
        let conversionModel = ConversionTypes.SessionStorage.fromSpreadsheetModel this
        Json.serialize conversionModel

    static member toJsonString(model: Spreadsheet.Model) =
        model.ToJsonString()

    static member fromSessionStorage (position: int) =
        let history = HistoryOrder.tryFromSession()
        let guid = history |> Option.map (List.tryItem position) |> Option.flatten
        if guid.IsNone then 
            failwith "Not enough items in history list."
        let tryState = GeneralHelpers.tryGetSessionItem (Keys.create_swate_session_history_table_key guid.Value)
        match tryState with
        | Some stateJson -> 
            Spreadsheet.Model.fromJsonString(stateJson)
        | None ->
            failwith "Could not find any history."

    /// <summary>
    /// This function tries to get the data model from local storage saved under "swate_spreadsheet_key"
    /// </summary>
    static member fromLocalStorage() = 
        let snapshotJsonString = GeneralHelpers.tryGetLocalItem(Keys.swate_local_spreadsheet_key)
        match snapshotJsonString with
        | Some j    -> Spreadsheet.Model.fromJsonString j
        | None      -> Spreadsheet.Model.init()

    /// <summary>
    /// Save data in web local storage
    /// </summary>
    member this.SaveToLocalStorage() =
        let snapshotJsonString = this.ToJsonString()
        Browser.WebStorage.localStorage.setItem(Keys.swate_local_spreadsheet_key, snapshotJsonString)

    /// <summary>
    /// Tries to get the data model from indexed db saved under "swate_spreadsheet_key"
    /// </summary>
    static member fromIndexedDB() =
        promise{
            let! indexedDB = openDatabase "StuffDatabase" 2 "items"
            let! snapshotJsonString = getItem indexedDB "items" Keys.swate_local_spreadsheet_key
            match snapshotJsonString with
            | Some j    -> return Spreadsheet.Model.fromJsonString j
            | None      -> return Spreadsheet.Model.init()
        }

    /// <summary>
    /// Save data in web indexedDB
    /// </summary>
    member this.SaveToIndexedDB() =
        promise {
            let snapshotJsonString = this.ToJsonString()
            let! indexedDB = createDatabase "StuffDatabase" 2 "items"
            log(snapshotJsonString)
            do! addItem "StuffDatabase" 2 "items" snapshotJsonString Keys.swate_local_spreadsheet_key
        }
        |> Promise.start

/// <summary>
/// This type is used to store information about local history. Can be used to revert changes.
/// </summary>
type Model = 
    {
        HistoryItemCountLimit: int
        HistoryCurrentPosition: int
        HistoryExistingItemCount: int
        HistoryOrder: System.Guid list
    } 
    static member init() = 
        {
            HistoryItemCountLimit = 31
            HistoryCurrentPosition = 0
            HistoryExistingItemCount = 0
            HistoryOrder = List.empty
        }
    member this.UpdateFromSessionStorage() : Model =
        let position = GeneralHelpers.tryGetSessionItem(Keys.swate_session_history_position) |> Option.map int
        let history = HistoryOrder.tryFromSession()
        match position, history with
        | Some p, Some h -> 
            { this with HistoryCurrentPosition = p; HistoryOrder = h; HistoryExistingItemCount = h.Length }
        | _, _ -> this

    member this.NextPositionIsValid(newPosition: int) =
        let isSmallerZero = newPosition < 0
        let isEqual = newPosition = this.HistoryCurrentPosition
        let isBiggerExisting = newPosition >= this.HistoryExistingItemCount
        let isBiggerLimit = newPosition >= this.HistoryItemCountLimit
        (isSmallerZero || isEqual || isBiggerLimit || isBiggerExisting)
        |> not

    // Whenever `SaveSpreadsheetModelSnapshot` is called, also generate a new GUID. Save the model with the "swate_session_history_table_prefix_GUID" as key in session storage.
    // In addition save with key "swate_session_history_key" a list with the generated GUIDs in which the newest GUID is added first.
    // Save the current history position with key "swate_session_history_position" in session storage.
    // If we go back the history then update the "swate_session_history_position" and load the table with GUID at position "swate_session_history_position" as model.
    // If the user changes something when "swate_session_history_position" <> 0 (up to date, newest), delete all positions up to "swate_session_history_position" and the corresponding tables.
    ///<summary>Save the next table state to session storage for history control. Table state is stored with guid as key and order is stored as guid list.</summary>
    member this.SaveSessionSnapshot (model: Spreadsheet.Model) : Model =
        /// recursively generate new guids, check if existing and if so repeat until new guid.
        let rec generateNewGuid() =
            let g = System.Guid.NewGuid()
            let key = Keys.create_swate_session_history_table_key g
            let try_key = GeneralHelpers.tryGetSessionItem(key)
            // if key exists redo, else use key
            match try_key with
            | Some _ -> generateNewGuid()
            | None -> g, key
        /// newGuid will be used to store the order in a list
        /// newKey is used as key to store the table state
        let newGuid, newKey = generateNewGuid()
        let snapshotJsonString = model.ToJsonString() //Json.serialize state
        // Add new history state to current history order.
        let nextState =
            // if e.g at position 4 and we create new table state from position 4 we want to delete position 0 .. 3 and use 4 as new 0
            let rebranchedList, toRemoveList1 =
                if this.HistoryCurrentPosition <> 0 then
                    this.HistoryOrder
                    |> List.splitAt this.HistoryCurrentPosition
                    |> fun (remove, keep) -> keep, remove
                else
                    this.HistoryOrder, []
            let newlist = newGuid::rebranchedList
            /// Split list into two at index of MaxHistory. Kepp the list with index below MaxHistory and iterate over the other list to remove the stored states.
            let newlist, toRemoveList2 = if newlist.Length > this.HistoryItemCountLimit then List.splitAt this.HistoryItemCountLimit newlist else newlist, []
            let toRemoveList = toRemoveList1@toRemoveList2
            if List.isEmpty toRemoveList |> not then
                toRemoveList |> List.iter (fun guid ->
                    let rmvKey = Keys.create_swate_session_history_table_key(guid)
                    Browser.WebStorage.sessionStorage.removeItem(rmvKey)
                )
            { this with HistoryOrder = newlist; HistoryExistingItemCount = newlist.Length; HistoryCurrentPosition = 0 }
        // apply all storage changes after everything else went through
        // set current table state with key and guid
        Browser.WebStorage.sessionStorage.setItem(newKey, snapshotJsonString)
        // set new table guid history
        Browser.WebStorage.sessionStorage.setItem(Keys.swate_session_history_key, HistoryOrder.toJson(nextState.HistoryOrder))
        // reset new table position to 0
        Browser.WebStorage.sessionStorage.setItem(Keys.swate_session_history_position, "0")
        nextState

    static member ResetHistoryWebStorage() =
        Browser.WebStorage.localStorage.removeItem(Keys.swate_local_spreadsheet_key)
        Browser.WebStorage.sessionStorage.clear()

    member this.ResetAll() =
        Model.ResetHistoryWebStorage()
        Model.init()