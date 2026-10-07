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

let adjustScale (percentagePointDelta: int) : Result<unit, exn> =
    let currentPercentage = getScale () * 100.0 |> Math.Round |> int
    let nextPercentage = currentPercentage + percentagePointDelta

    if nextPercentage <= 0 then
        Ok()
    else
        setScale (float nextPercentage / 100.0)

let private tryGetKeyboardScaleDelta (input: WebContents.BeforeInputEvent.Input) =
    let primaryModifier =
        if Main.Bindings.Node.processPlatform () = "darwin" then
            input.meta
        else
            input.control

    if
        input.``type`` <> "keyDown"
        || input.isComposing
        || input.alt
        || not primaryModifier
    then
        None
    elif input.key = "+" || input.key = "=" || input.code = "NumpadAdd" then
        Some 5
    elif input.key = "-" || input.key = "_" || input.code = "NumpadSubtract" then
        Some -5
    else
        None

let registerWindow (window: BrowserWindow) =
    // Reapply the current global scale after navigation or reload.
    window.webContents.onDidFinishLoad (fun () -> applyToWindow window)

    window.webContents.onBeforeInputEvent (fun event input ->
        match tryGetKeyboardScaleDelta input with
        | None -> ()
        | Some delta ->
            // Suppress Chromium's built-in zoom, which uses a different step size.
            event.preventDefault ()

            match adjustScale delta with
            | Ok() -> ()
            | Error error -> eprintfn "Could not adjust UI scale: %s" error.Message
    )
