module IndexedDB

open Fable.Core
open JsInterop

open System

/// <summary>
/// Create or update an indexed database
/// </summary>
/// <param name="dbName"></param>
/// <param name="version"></param>
let createDatabase (dbName: string) version tableKey = promise {
    let indexedDB = emitJsExpr<obj> ("globalThis.indexedDB") "globalThis.indexedDB"
    let request: obj = indexedDB?``open`` (dbName, version)

    let! db =
        Async.FromContinuations(fun (resolve, reject, _) ->
            request?onsuccess <- fun _ -> resolve (request?result)
            request?onerror <- fun _ -> reject (new Exception(request?error?message))

            request?onupgradeneeded <-
                fun e ->
                    let resultDb = e?target?result

                    if resultDb?objectStoreNames?contains (tableKey) |> not then
                        let _ = resultDb?createObjectStore (tableKey)
                        resolve (request?result)
                    else
                        resolve (request?result))
        |> Async.StartAsPromise

    return db
}

/// <summary>
/// Create or update an indexed database
/// </summary>
/// <param name="dbName"></param>
/// <param name="version"></param>
let rec openDatabase (dbName: string) (tableKey: string) = promise {
    let indexedDB = emitJsExpr<obj> ("globalThis.indexedDB") "globalThis.indexedDB"
    let request: obj = indexedDB?``open`` (dbName)

    let! db =
        Async.FromContinuations(fun (resolve, reject, _) ->
            request?onsuccess <-
                fun e ->
                    let resultDb = e?target?result
                    let version = resultDb?version

                    if resultDb?objectStoreNames?contains (tableKey) |> not then
                        resultDb?close () // Close the current db instance to avoid transaction problems
                        let _ = createDatabase dbName (version + 1) tableKey |> Promise.start
                        resolve (None)
                    else
                        resolve (Some request?result)

            request?onerror <- fun _ -> reject (new Exception(request?error?message)))
        |> Async.StartAsPromise

    match db with
    | Some db -> return db
    | None -> return! openDatabase dbName tableKey
}

/// <summary>
/// Closes the given db and associated transactions
/// </summary>
/// <param name="db"></param>
let closeDatabase db = db?close ()

/// <summary>
/// Initializes the indexedDB and its associated tables
/// </summary>
/// <param name="dbName"></param>
/// <param name="tableKeys"></param>
let initInidexedDB (dbName: string) (tableKeys: string[]) = promise {
    for tableKey in tableKeys do
        let! db = openDatabase dbName tableKey
        closeDatabase db
}

let clearTable db (tableKey: string) = promise {
    if db?objectStoreNames?contains (tableKey) then
        let transaction = db?transaction (tableKey, "readwrite")
        let store = transaction?objectStore (tableKey)
        let storeRequest = store?clear ()

        do!
            Async.FromContinuations(fun (resolve, reject, _) ->
                storeRequest?onsuccess <- fun _ -> resolve (storeRequest?result)
                storeRequest?onerror <- fun _ -> reject (new Exception(storeRequest?error?message)))
            |> Async.StartAsPromise
}

let clearInidexedDB (dbName: string) (tableKeys: string[]) = promise {
    for tableKey in tableKeys do
        let! db = openDatabase dbName tableKey
        do! clearTable db tableKey
        closeDatabase db
}

/// <summary>
/// Add item to indexedDB
/// </summary>
/// <param name="db"></param>
/// <param name="tableKey"></param>
/// <param name="item"></param>
/// <param name="key"></param>
let addItem db (tableKey: string) (item: obj) (key: string) = promise {
    if db?objectStoreNames?contains (tableKey) then
        let transaction = db?transaction (tableKey, "readwrite")
        let store = transaction?objectStore (tableKey)
        let storeRequest = store?add (item, key)

        do!
            Async.FromContinuations(fun (resolve, reject, _) ->
                storeRequest?onsuccess <- fun _ -> resolve (storeRequest?result)
                storeRequest?onerror <- fun _ -> reject (new Exception(storeRequest?error?message)))
            |> Async.StartAsPromise
}

/// <summary>
/// Delete item from indexedDB
/// </summary>
/// <param name="db"></param>
/// <param name="tableKey"></param>
/// <param name="key"></param>
let deleteItem db (tableKey: string) (key: string) = promise {
    if db?objectStoreNames?contains (tableKey) then
        let transaction = db?transaction (tableKey, "readwrite")
        let store = transaction?objectStore (tableKey)
        let storeRequest = store?delete (key)

        do!
            Async.FromContinuations(fun (resolve, reject, _) ->
                storeRequest?onsuccess <- fun _ -> resolve (storeRequest?result)
                storeRequest?onerror <- fun _ -> reject (new Exception(storeRequest?error?message)))
            |> Async.StartAsPromise
}

/// <summary>
/// Update an existing item in the IndexedDB object store
/// </summary>
/// <param name="db"></param>
/// <param name="storeName"></param>
/// <param name="data"></param>
let updateItem db (tableKey: string) (item: obj) (key: string) = promise {
    if db?objectStoreNames?contains (tableKey) then
        let transaction = db?transaction (tableKey, "readwrite")
        let store = transaction?objectStore (tableKey)
        let storeRequest = store?put (item, key)

        do!
            Async.FromContinuations(fun (resolve, reject, _) ->
                storeRequest?onsuccess <- fun _ -> resolve (storeRequest?result)
                storeRequest?onerror <- fun _ -> reject (new Exception(storeRequest?error?message)))
            |> Async.StartAsPromise
}

/// <summary>
/// Retrive a specific item from the database
/// </summary>
/// <param name="db"></param>
/// <param name="localStorage"></param>
/// <param name="key"></param>
let tryGetItem (db: obj) (tableKey: string) (key: string) = promise {
    if db?objectStoreNames?contains (tableKey) then
        let transaction = db?transaction (tableKey, "readonly")
        let store = transaction?objectStore (tableKey)
        let storeRequest = store?get (key)

        let! item =
            Async.FromContinuations(fun (resolve, reject, _) ->
                storeRequest?onsuccess <- fun _ -> resolve (storeRequest?result)
                storeRequest?onerror <- fun _ -> reject (new Exception(storeRequest?error?message)))
            |> Async.StartAsPromise

        if isNullOrUndefined item then
            return None
        else
            let result = item.ToString()

            if String.IsNullOrEmpty result then
                return None
            else
                return Some result
    else
        return None
}

/// <summary>
/// Retrive all items from the database
/// </summary>
/// <param name="db"></param>
/// <param name="localStorage"></param>
let getAllItems (db: obj) (tableKey: string) = promise {
    if db?objectStoreNames?contains (tableKey) then
        let transaction = db?transaction (tableKey, "readonly")
        let store = transaction?objectStore (tableKey)
        let storeRequest = store?getAll ()

        let! item =
            Async.FromContinuations(fun (resolve, reject, _) ->
                storeRequest?onsuccess <- fun _ -> resolve (storeRequest?result)

                storeRequest?onerror <-
                    fun _ -> reject (new Exception(storeRequest?error?message "Failed to open database")))
            |> Async.StartAsPromise

        if isNullOrUndefined item then
            return None
        else
            let result = item.ToString()

            if String.IsNullOrEmpty result then
                return None
            else
                return Some result
    else
        return None
}