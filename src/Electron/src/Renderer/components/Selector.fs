module Renderer.components.Selector

open Fable.Core

open Swate.Components
open Swate.Electron.Shared


///Selector module
let openNewWindow =
    fun _ ->
        promise {
            match! Api.openARCInNewWindow() with
            | Ok _ -> ()
            | Error exn -> failwith $"{exn.Message}"

            return ()
        }
        |> Promise.start

///Selector module
let openCurrentWindow =
    fun _ ->
        promise {
            let! r = Api.openARC()

            match r with
            | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
            | Ok _ -> ()
        }
        |> Promise.start

///Selector module
let openARC (appState: AppState) =
    if appState.IsInit then
        openCurrentWindow
    else
        openNewWindow

///Selector module
let onARCClick (clickedARC: SelectorTypes.ARCPointer) =
    promise {
        match! Api.focusExistingARCWindow clickedARC.path with
        | Ok _ -> ()
        | Error exn -> failwith $"{exn.Message}"

        return ()
    }
    |> Promise.start

///Selector module
let actionbar (appState: AppState) onClick =
    let createARC =
        SelectorTypes.ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", onClick)

    let openARCButtonInfo =
        SelectorTypes.ButtonInfo.create (
            "swt:fluent--folder-open-24-regular swt:size-5",
            "Open an existing ARC",
            fun _ ->
                onClick ()
                openARC appState ()
        )

    let downloadARC =
        SelectorTypes.ButtonInfo.create (
            "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
            "Download an existing ARC",
            onClick
        )

    Actionbar.Main([| createARC; openARCButtonInfo; downloadARC |], 3)

///Selector module
let onOpenSelector (appState: AppState) setRecentARCs () =
    promise {
        let! (newARCs: IPCTypes.FileItemDTO []) = Api.getRecentARCs ()

        match appState with
        | AppState.Init -> ()
        | AppState.ARC path ->
            newARCs
            |> Array.map (fun arc -> SelectorTypes.ARCPointer.create (arc.name, arc.path, arc.path = path))
            |> setRecentARCs
    }
    |> Promise.start