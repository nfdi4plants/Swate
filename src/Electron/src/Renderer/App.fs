module Renderer.App

open Feliz

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes

open Renderer.components
open Swate.Components.Landing
open Renderer.state.AppShellState

[<ReactComponent>]
let Main () =

    let appShellState = useAppShellState ()

    ///Main content module
    let children =
        React.useMemo (
            (fun _ ->
                MainWindowContent.content {
                    AppState = appShellState.appState
                    SetArcFileState = appShellState.setArcFileState
                    ActiveView = appShellState.activeView
                    SetActiveView = appShellState.setActiveView
                    ArcFileState = appShellState.arcFileState
                    PreviewData = appShellState.previewData
                    SetPreviewData = appShellState.setPreviewData
                    SetSelectedTreeItemPath = appShellState.setSelectedTreeItemPath
                }
            ),
            [|
                box appShellState.appState
                box appShellState.previewData
                box appShellState.activeView
                box appShellState.arcFileState
                box appShellState.landingDraftActive
            |]
        )
        
    let selector =
        Selector.Main(
            appShellState.recentARCs,
            Selector.actionbar appShellState.appState,
            onOpenSelector = Selector.onOpenSelector appShellState.appState appShellState.setRecentARCs,
            onClick = Selector.onARCClick
        )

    let navbar = Navbar.Main(selector)

    React.Fragment [|
        CloseWindowController.CloseWindowController.Subscription(
            (fun () ->
                promise {
                    match appShellState.arcFileState with
                    | None -> return Ok()
                    | Some arcFile ->
                        let! saveResult = Navbar.saveArcFileWithPreview arcFile

                        match saveResult with
                        | Ok updatedPreview ->
                            appShellState.setPreviewData (Some updatedPreview)
                            return Ok()
                        | Microsoft.FSharp.Core.Error errorMsg ->
                            let message = $"Save failed: {errorMsg}"
                            appShellState.setPreviewData (Some (Error message))
                            return Microsoft.FSharp.Core.Error message
                }),
            onConfirmClose = (fun () -> console.log "User chose to close without saving."),
            onCancelClose = (fun () -> console.log "User cancelled the close action.")
        )
        context.AppStateCtx.AppStateCtx.Provider(
            {
                state = appShellState.appState
                setState = appShellState.setAppState
            },
            Layout.Main(
                children = children,
                navbar = navbar,
                ?leftSidebar =
                    (let sidebarContent =
                        match appShellState.fileExplorer with
                        | Some fe -> fe
                        | None -> Html.span [ prop.className "swt:opacity-50"; prop.text "No files" ]
                 Some(
                     Html.div [
                         prop.className "swt:p-4"
                         prop.children [|
                            match appShellState.appState with
                            | AppState.ARC _ ->
                                Html.button [
                                    prop.className "swt:btn swt:btn-sm swt:btn-outline swt:mb-2 swt:w-full"
                                    prop.text "Landing Page"
                                    prop.onClick (fun _ ->
                                        if not appShellState.landingDraftActive then
                                            Landing.ResetLandingDraft(appShellState.setPreviewData, appShellState.setSelectedTreeItemPath)
                                    )
                                ]
                            | _ -> Html.none
                            Html.h2 [
                                prop.text "ARC-Tree"
                            ]
                            sidebarContent
                        |]
                     ]
                 )),
                leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
            )
        )
    |]
