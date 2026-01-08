
#r "nuget: Fable.Core"

open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop

[<StringEnum(CaseRules.LowerFirst)>]
type Events =
    | Add
    | Change
    | Outer
    | Unlink
    | AddDir
    | UnlinkDir
    | Error
    | Ready
    | Raw

[<Global; AllowNullLiteral>]
type WatchOptions
    [<ParamObjectAttribute; Emit("$0")>]
    (
        ?persistent: bool,
        ?ignored: U2<string, ResizeArray<string>>,
        ?ignoreInitial: bool,
        ?followSimlinks: bool,
        ?cwd: string
    ) =
    member val persistent: bool option = persistent with get, set
    member val ignored: U2<string, ResizeArray<string>> option = ignored with get, set
    member val ignoreInitial: bool option = ignoreInitial with get, set
    member val followSimlinks: bool option = followSimlinks with get, set
    member val cwd: string option = cwd with get, set

type IWatched =
    [<EmitIndexerAttribute>]
    abstract member item: path: string -> string[] with get

type IWatcher =
    abstract member close: unit -> Promise<unit>
    abstract member add: paths: string -> unit
    abstract member add: paths: string[] -> unit
    abstract member unwatch: paths: string -> Promise<unit>
    abstract member unwatch: paths: string[] -> Promise<unit>
    abstract member on: eventName: Events * callback: (string -> unit) -> IWatcher
    abstract member getWatched: unit -> IWatched


[<Erase>]
type Chokidar =

    [<Import("watch", "chokidar")>]
    static member watch (paths: string, options: WatchOptions) : IWatcher = jsNative

    [<Import("watch", "chokidar")>]
    static member watch (paths: string[], options: WatchOptions) : IWatcher = jsNative

    [<Import("watch", "chokidar")>]
    [<ParamObjectAttribute(1)>]
    static member watch
        (paths: string, ?persistent: bool, ?ignored: string, ?ignoreInitial: bool, ?followSymlinks: bool, ?cwd: string)
        : IWatcher =
        jsNative

// let watcher = Chokidar.watch("C:/Users/User/source/repos/Fable.Electron/src/main", persistent = true, ignoreInitial = true, followSymlinks = true)
let watcher =
    Chokidar.watch ("./", WatchOptions(persistent = true, ignoreInitial = true, followSimlinks = true))

watcher
    .on(Events.Add, fun path -> console.log ($"File added {path}"))
    .on(Events.Change, fun path -> console.log ($"File changed {path}"))
    .on(Events.Unlink, fun path -> console.log ($"File unlinked {path}"))
    .on (
        Events.Ready,
        fun path ->
            let getWatched = watcher.getWatched ()
            console.log ($"getWatched: {getWatched}")
            console.log (getWatched)
    )
