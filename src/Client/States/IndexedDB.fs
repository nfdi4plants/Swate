module IndexedDB

open Fable.Core
open JsInterop

open System

/// <summary>
/// Create or update an indexed database
/// </summary>
/// <param name="dbName"></param>
/// <param name="version"></param>
let createDatabase (dbName: string) version =
    promise {
        let indexedDB = emitJsExpr<obj>("globalThis.indexedDB") "globalThis.indexedDB"
        let request: obj = indexedDB?``open``(dbName, version)
        let! db =
            Async.FromContinuations(fun (resolve, reject, _) ->
                request?onsuccess <- fun _ -> resolve(request?result)
                request?onerror <- fun _ -> reject(new Exception(request?error?message "Failed to open database"))
                request?onupgradeneeded <- fun e ->
                    let resultDb = e?target?result
                    if resultDb?objectStoreNames?contains("items") |> not then
                        resultDb?createObjectStore("items")
                    resolve(request?result)
            )
            |> Async.StartAsPromise
        return db
    }

/// <summary>
/// Add an item to the indexed database
/// </summary>
/// <param name="dbName"></param>
/// <param name="version"></param>
/// <param name="localStorage"></param>
/// <param name="item"></param>
/// <param name="key"></param>
let addItem (dbName: string) version (localStorage: string) (item: obj) (key: string) =
    promise {
        let addData db localStorage item key =
            let transaction = db?transaction(localStorage, "readwrite")
            let store = transaction?objectStore(localStorage)
            store?add(item, key) |> Async.AwaitPromise |> Async.StartImmediate
            transaction?``done`` |> Async.AwaitPromise |> Async.StartImmediate

        let indexedDB = emitJsExpr<obj>("globalThis.indexedDB") "globalThis.indexedDB"
        let request: obj = indexedDB?``open``(dbName, version)
        do! Async.FromContinuations(fun (resolve, reject, _) ->
                request?onsuccess <- fun _ ->
                    let db = request?result
                    addData db localStorage item key
                    resolve(request?result)
                request?onerror <- fun _ -> reject(new Exception(request?error?message "Failed to open database"))
                request?onupgradeneeded <- fun e ->
                    let transaction = e?target?transaction
                    transaction?oncomplete <- fun _ ->
                        let db = request?result
                        addData db localStorage item
            )
            |> Async.StartAsPromise
        return ()
    }

/// <summary>
/// Retrive a specific item from the database
/// </summary>
/// <param name="db"></param>
/// <param name="localStorage"></param>
/// <param name="key"></param>
let getItem (db: obj) (localStorage: string) (key: string) =
    promise {
        let tx = db?transaction(localStorage, "readonly")
        let store = tx?objectStore(localStorage)
        let storeRequest = store?get(key)

        let! items =
            Async.FromContinuations(fun (resolve, reject, _) ->
                storeRequest?onsuccess <- fun _ -> resolve(storeRequest?result)
                storeRequest?onerror <- fun _ -> reject(new Exception(storeRequest?error?message "Failed to open database"))
            )
            |> Async.StartAsPromise
        return items
    }

/// <summary>
/// Retrive all items from the database
/// </summary>
/// <param name="db"></param>
/// <param name="localStorage"></param>
let getAllItems (db: obj) (localStorage: string) =
    async {
        let tx = db?transaction(localStorage, "readonly")
        let store = tx?objectStore(localStorage)
        let! items = store?getAll() |> Async.AwaitPromise
        return items
    }
