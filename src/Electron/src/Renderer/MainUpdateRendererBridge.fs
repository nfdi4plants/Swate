module Renderer.MainUpdateRendererBridge

open System.Collections.Generic
open Fable.Electron.Remoting.Renderer
open Swate.Components
open Swate.Components.Authentication.Types
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

let mutable private isInitialized = false
let mutable private nextSubscriptionId = 0

let private pathChangeSubscribers = Dictionary<int, string option -> unit>()
let private recentArcsSubscribers = Dictionary<int, SelectorTypes.ARCPointer[] -> unit>()
let private authAccountsSubscribers = Dictionary<int, AuthStateDto -> unit>()
let private fileTreeSubscribers = Dictionary<int, Dictionary<string, FileEntry> -> unit>()
let private gitProgressSubscribers = Dictionary<int, GitProgressDto -> unit>()

let private notify (subscribers: Dictionary<int, 'T -> unit>) (payload: 'T) =
    subscribers.Values
    |> Seq.toArray
    |> Array.iter (fun handler -> handler payload)

let private ensureInitialized () =
    if not isInitialized then
        isInitialized <- true

        // The preload bridge uses additive ipcRenderer.on listeners, so build this handler once
        // and fan out to local subscribers instead of registering duplicate no-op handlers.
        let ipcHandler: IMainUpdateRendererApi = {
            pathChange = notify pathChangeSubscribers
            recentARCsUpdate = notify recentArcsSubscribers
            authAccountsUpdate = notify authAccountsSubscribers
            fileTreeUpdate = notify fileTreeSubscribers
            gitProgressUpdate = notify gitProgressSubscribers
        }

        Remoting.init |> Remoting.buildHandler ipcHandler

let private subscribe (subscribers: Dictionary<int, 'T -> unit>) (handler: 'T -> unit) =
    ensureInitialized ()
    nextSubscriptionId <- nextSubscriptionId + 1
    let subscriptionId = nextSubscriptionId
    subscribers[subscriptionId] <- handler

    fun () -> subscribers.Remove subscriptionId |> ignore

let subscribePathChange = subscribe pathChangeSubscribers
let subscribeRecentArcsUpdate = subscribe recentArcsSubscribers
let subscribeAuthAccountsUpdate = subscribe authAccountsSubscribers
let subscribeFileTreeUpdate = subscribe fileTreeSubscribers
let subscribeGitProgressUpdate = subscribe gitProgressSubscribers
