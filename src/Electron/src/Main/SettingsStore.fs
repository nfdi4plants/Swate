[<AutoOpen>]
module Main.SettingsStore

open System
open Fable.Core
open Main.Bindings.Filesystem
open Main.Bindings.Path


[<Literal>]
let appSettingsFolderName = "Settings"

[<Literal>]
let recentArcsSettingsFileName = "recent-arcs.json"

// If appSettingsFolderName is changed in a future release, migrate or delete the old
// folder inside app.getPath("userData") to avoid stale or split settings.

open Fable.Electron.Main

let getSettingsRootPath () =
    let userDataPath = app.getPath Enums.App.GetPath.Name.UserData
    /// Something like: C:\Users\Kevin\AppData\Roaming\Swate\Settings
    let settingsRootPath = join [| userDataPath; appSettingsFolderName |]

    mkdirSync settingsRootPath (MkdirOptions(recursive = true))
    settingsRootPath

let getSettingsFilePath (fileName: string) =
    let settingsRootPath = getSettingsRootPath ()
    join [| settingsRootPath; fileName |]

let tryReadSettingsFile (fileName: string) =
    try
        let filePath = getSettingsFilePath fileName
        let exists = existsSync filePath

        if not exists then
            None
        else
            Some(readFileSync filePath TextEncoding.Utf8)
    with _ ->
        None

let writeSettingsFileAtomic (fileName: string) (content: string) =
    try
        let filePath = getSettingsFilePath fileName
        let tempPath = filePath + ".tmp"
        writeFileSync tempPath content TextEncoding.Utf8
        renameSync tempPath filePath
    with _ ->
        ()