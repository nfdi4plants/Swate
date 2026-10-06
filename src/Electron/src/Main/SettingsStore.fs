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

/// Writes through a temp file and rename. The result names the failure instead of
/// hiding it, for callers that have to know whether the file was written.
let tryWriteSettingsFileAtomic (fileName: string) (content: string) : Result<unit, string> =
    try
        let filePath = getSettingsFilePath fileName
        let tempPath = filePath + ".tmp"
        writeFileSync tempPath content TextEncoding.Utf8
        renameSync tempPath filePath
        Ok()
    with error ->
        Error $"Could not write settings file '{fileName}': {error.Message}"

let writeSettingsFileAtomic (fileName: string) (content: string) =
    tryWriteSettingsFileAtomic fileName content |> ignore
