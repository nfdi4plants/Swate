[<AutoOpen>]
module Main.SettingsStore

open System
open Fable.Core
open Fable.Core.JsInterop

[<Literal>]
let appFolderName = "Swate"

[<Literal>]
let appSettingsFolderName = "Settings"

[<Literal>]
let recentArcsSettingsFileName = "recent-arcs.json"

// If appSettingsFolderName is changed in a future release, migrate or delete the old
// folder inside app.getPath("userData") to avoid stale or split settings.
let private fs: obj = importAll "fs"
let private pathModule: obj = importAll "path"
let private electron: obj = importAll "electron"

let getSettingsRootPath () =
    let userDataPath = electron?app?getPath ("userData") |> unbox<string>

    let settingsRootPath =
        pathModule?join (userDataPath, appFolderName, appSettingsFolderName)
        |> unbox<string>

    printfn "Settings root path: %s" settingsRootPath

    fs?mkdirSync (settingsRootPath, createObj [ "recursive" ==> true ]) |> ignore
    settingsRootPath

let getSettingsFilePath (fileName: string) =
    let settingsRootPath = getSettingsRootPath ()
    pathModule?join (settingsRootPath, fileName) |> unbox<string>

let tryReadSettingsFile (fileName: string) =
    try
        let filePath = getSettingsFilePath fileName
        let exists = fs?existsSync (filePath) |> unbox<bool>

        if not exists then
            None
        else
            Some(fs?readFileSync (filePath, "utf8") |> unbox<string>)
    with _ ->
        None

let writeSettingsFileAtomic (fileName: string) (content: string) =
    try
        let filePath = getSettingsFilePath fileName
        let tempPath = filePath + ".tmp"
        fs?writeFileSync (tempPath, content, "utf8") |> ignore
        fs?renameSync (tempPath, filePath) |> ignore
    with _ ->
        ()