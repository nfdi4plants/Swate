module Main.UiSettings

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main

[<Literal>]
let private settingsFileName = "ui-scale.json"

let private isValidScale (scale: float) =
    not (Double.IsNaN scale || Double.IsInfinity scale) && scale > 0.0

let private loadScale () =
    try
        match SettingsStore.tryReadSettingsFile settingsFileName with
        | Some content ->
            let value = JS.JSON.parse content

            if jsTypeof value = "number" && isValidScale (unbox<float> value) then
                unbox<float> value
            else
                1.0
        | None -> 1.0
    with _ ->
        1.0

// Load only after Electron is ready and its settings directory is available.
let mutable private currentScale: float option = None

let getScale () =
    match currentScale with
    | Some scale -> scale
    | None ->
        let scale = loadScale ()
        currentScale <- Some scale
        scale

let applyToWindow (window: BrowserWindow) =
    if not (window.isDestroyed ()) && not (window.webContents.isDestroyed ()) then
        window.webContents.setZoomFactor (getScale ())

let setScale (scale: float) : Result<unit, exn> =
    if not (isValidScale scale) then
        Error(exn "UI scale must be a finite number greater than zero (1.0 = 100%).")
    else
        try
            match SettingsStore.tryWriteSettingsFileAtomic settingsFileName (JS.JSON.stringify scale) with
            | Error message -> Error(exn message)
            | Ok() ->
                currentScale <- Some scale
                BrowserWindow.getAllWindows () |> Array.iter applyToWindow
                Ok()
        with error ->
            Error error
