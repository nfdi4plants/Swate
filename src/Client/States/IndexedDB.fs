module IndexedDB

open Fable.Core
open JsInterop

open System

/// <summary>
/// Create or update an indexed database
/// </summary>
/// <param name="dbName"></param>
/// <param name="version"></param>
let createDatabase (dbName: string) version localStorage =
    promise {
        let indexedDB = emitJsExpr<obj>("globalThis.indexedDB") "globalThis.indexedDB"
        let request: obj = indexedDB?``open``(dbName, version)
        let! db =
            Async.FromContinuations(fun (resolve, reject, _) ->
                request?onsuccess <- fun _ -> resolve(request?result)
                request?onerror <- fun _ -> reject(new Exception(request?error?message))
                request?onupgradeneeded <- fun e ->
                    let resultDb = e?target?result
                    if resultDb?objectStoreNames?contains(localStorage) |> not then
                        let _ = resultDb?createObjectStore(localStorage)
                        resolve(request?result)
                    else
                        resolve(request?result)
            )
            |> Async.StartAsPromise
        return db
    }

/// <summary>
/// Create or update an indexed database
/// </summary>
/// <param name="dbName"></param>
/// <param name="version"></param>
let rec openDatabase (dbName: string) (localStorage: string) =
    promise {
        let indexedDB = emitJsExpr<obj>("globalThis.indexedDB") "globalThis.indexedDB"
        let request: obj = indexedDB?``open``(dbName)
        let! db =
            Async.FromContinuations(fun (resolve, reject, _) ->
                request?onsuccess <- fun e ->
                    let resultDb = e?target?result
                    let version = resultDb?version
                    if resultDb?objectStoreNames?contains(localStorage) |> not then
                        resultDb?close() // Close the current db instance to avoid transaction problems
                        let _ = 
                            createDatabase dbName (version + 1) localStorage 
                            |> Promise.start
                        resolve(None)
                    else
                        resolve(Some request?result)
                request?onerror <- fun _ -> reject(new Exception(request?error?message))
            )
            |> Async.StartAsPromise
        match db with
        | Some db -> return db
        | None -> 
            return! openDatabase dbName localStorage
    }

/// <summary>
/// Closes the given db and associated transactions
/// </summary>
/// <param name="db"></param>
let closeDatabase db =
    db?close()

let addItem db (localStorage: string) (item: obj) (key: string) =
    promise {
        if db?objectStoreNames?contains(localStorage) then
            let transaction = db?transaction(localStorage, "readwrite")
            let store = transaction?objectStore(localStorage)
            let storeRequest = store?add(item, key)
            do! Async.FromContinuations(fun (resolve, reject, _) ->
                    storeRequest?onsuccess <- fun _ -> resolve(storeRequest?result)
                    storeRequest?onerror <- fun _ -> reject(new Exception(storeRequest?error?message))
                )
                |> Async.StartAsPromise
    }

/// <summary>
/// Update an existing item in the IndexedDB object store
/// </summary>
/// <param name="db"></param>
/// <param name="storeName"></param>
/// <param name="data"></param>
let updateItem db (localStorage: string) (item: obj) (key: string) =
    promise {
        if db?objectStoreNames?contains(localStorage) then
            let transaction = db?transaction(localStorage, "readwrite")
            let store = transaction?objectStore(localStorage)
            let storeRequest = store?put(item, key)
            do! Async.FromContinuations(fun (resolve, reject, _) ->
                    storeRequest?onsuccess <- fun _ -> resolve(storeRequest?result)
                    storeRequest?onerror <- fun _ -> reject(new Exception(storeRequest?error?message))
                )
                |> Async.StartAsPromise
    }

/// <summary>
/// Retrive a specific item from the database
/// </summary>
/// <param name="db"></param>
/// <param name="localStorage"></param>
/// <param name="key"></param>
let tryGetItem (db: obj) (localStorage: string) (key: string) =
    promise {
        if db?objectStoreNames?contains(localStorage) then
            let transaction = db?transaction(localStorage, "readonly")
            let store = transaction?objectStore(localStorage)
            let storeRequest = store?get(key)
            let! item =
                Async.FromContinuations(fun (resolve, reject, _) ->
                    storeRequest?onsuccess <- fun _ -> resolve(storeRequest?result)
                    storeRequest?onerror <- fun _ -> reject(new Exception(storeRequest?error?message))
                )
                |> Async.StartAsPromise

            if isNullOrUndefined item then
                return None
            else
                let result = item.ToString()
                if String.IsNullOrEmpty result then
                    return None
                else return Some result
        else
            return None
    }

/// <summary>
/// Retrive all items from the database
/// </summary>
/// <param name="db"></param>
/// <param name="localStorage"></param>
let getAllItems (db: obj) (localStorage: string) =
    promise {
        if db?objectStoreNames?contains(localStorage) then
            let transaction = db?transaction(localStorage, "readonly")
            let store = transaction?objectStore(localStorage)
            let storeRequest = store?getAll()

            let! item =
                Async.FromContinuations(fun (resolve, reject, _) ->
                    storeRequest?onsuccess <- fun _ -> resolve(storeRequest?result)
                    storeRequest?onerror <- fun _ -> reject(new Exception(storeRequest?error?message "Failed to open database"))
                )
                |> Async.StartAsPromise

            let result = item.ToString()

            if String.IsNullOrEmpty result then
                return None
            else return Some result
        else
            return None
    }
