module Renderer.Components.MainContent.ArcFilePreviewTarget

open ARCtrl
open Feliz
open Renderer.Components.MainElement
open Renderer.Components.MainContent.Types
open Renderer.Components.MainContent.Helper
open Renderer.Components.MainContent.CreateARCPreview
open Renderer.Types

[<ReactComponent>]
let ArcFilePreviewTarget (arcFile: ArcFiles) =

    let activeView, setActiveView = React.useState PreviewActiveView.Metadata
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()

    let setArcFile =
        fun (arcFile: ArcFiles) ->
            let page = PageState.ArcFilePage arcFile

            pageStateCtx.setState (Some page)

    let onSaveArcFile =
        fun _ ->
            promise {
                match! MainContentHelper.saveArcFile arcFile with
                | Ok() -> ()
                | Error exn -> pageStateCtx.setState (PageState.ErrorPage exn.Message |> Some)
            }
            |> Promise.start

    let activeTableIndex =
        match activeView with
        | PreviewActiveView.Table tableIndex -> Some tableIndex
        | _ -> None

    Html.div [
        prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
        prop.children [
            Html.div [
                prop.className "swt:flex-none"
                prop.children [
                    CreateARCitectNavbar arcFile activeView activeTableIndex setArcFile onSaveArcFile
                ]
            ]
            Html.div [
                prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                prop.children [
                    CreateARCPreview arcFile setArcFile activeView setActiveView
                ]
            ]
        ]
    ]