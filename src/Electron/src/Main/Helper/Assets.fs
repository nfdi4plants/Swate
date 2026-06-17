module Main.Helper.Assets

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Main
open Main.Bindings
open Node.Api

let processResourcesPath = emitJsExpr () "process.resourcesPath" |> unbox<string>
let processPlatform = emitJsExpr () "process.platform" |> unbox<string>

let getAssetPath (fileName: string) =
    if app.isPackaged then
        path.join (processResourcesPath, "assets", fileName)
    else
        path.join (__dirname, "../../assets", fileName)

let getIcon () =
    // mac uses icon from forge.config so we only need to differ between linux and windows here. Windows needs the .ico file for best results, while Linux can use the PNG.
    let isWin = processPlatform = "win32"

    if isWin then
        getAssetPath "icons/win/icon.ico"
    else
        getAssetPath "icons/png/512x512.png"
