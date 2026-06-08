module ElectronCore.TestHelpers

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.Bindings.Path
open ARCtrl

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"

let expectLoadedArc (result: Result<ARC, string[]>) =
    match result with
    | Ok arc -> arc
    | Error errors -> failwith (errors |> String.concat "\n")

let createTempDirectoryAsync (tempPrefix: string) : JS.Promise<string> =
    let prefix =
        join [|
            osDynamic?tmpdir () |> unbox<string>
            tempPrefix
        |]

    fsPromisesDynamic?mkdtemp (prefix) |> unbox<JS.Promise<string>>

let removeDirectoryAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let pathExistsAsync (path: string) : JS.Promise<bool> = promise {
    try
        let! _ = fsPromisesDynamic?access (path) |> unbox<JS.Promise<obj>>
        return true
    with _ ->
        return false
}

let loadArcAsync (arcPath: string) : JS.Promise<ARC> = promise {
    let! loaded = ARC.tryLoadAsync arcPath
    return expectLoadedArc loaded
}

let withTempArcWith
    (tempPrefix: string)
    (arcName: string)
    (seedArc: ARC -> unit)
    (testBody: string -> JS.Promise<unit>)
    : JS.Promise<unit> =
    promise {
        let! rootPath = createTempDirectoryAsync tempPrefix
        let arcPath = join [| rootPath; "arc" |]

        try
            let arc = ARC(arcName)
            seedArc arc
            do! arc.WriteAsync arcPath
            do! testBody arcPath
            do! removeDirectoryAsync rootPath
        with error ->
            do! removeDirectoryAsync rootPath
            return raise error
    }

let testWindow () =
    let noopSend: obj = emitJsExpr () "((..._args) => {})"

    createObj [
        "id" ==> 0
        "title" ==> ""
        "webContents" ==> createObj [ "send" ==> noopSend ]
    ]
    |> unbox<BrowserWindow>
