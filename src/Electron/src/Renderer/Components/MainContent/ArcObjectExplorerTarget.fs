module Renderer.Components.MainContent.ArcObjectExplorerTarget

open Feliz
open Swate.Components
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.Model

[<ReactComponent>]
let Main () =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx ()

    let viewModel =
        Swate.Components.ARCObjectExplorer.Model.create
            arcObjectCtx.state.Nodes
            fileStateCtx.state.Selection
            KindFilter.arcObjectExplorerOptions
            arcObjectCtx.state.SelectedKindIndices

    let services =
        Renderer.Components.ARCHelper.createArcExplorerServices
            pageStateCtx.setState
            arcObjectCtx.setArcFileState
            arcObjectCtx.setPreviewState
            arcObjectCtx.setStatusMessage

    let handleExplorerSelection =
        ARCExplorer.createOpenPreviewHandler
            fileStateCtx.setSelection
            services

    let searchAction =
        ARCObjectWidget.SearchActionForExplorerItems(
            viewModel.SearchItems,
            (fun item -> promise { do! handleExplorerSelection item } |> Promise.start)
        )

    Html.div [
        prop.id "arc-object-target"
        prop.className
            "swt:size-full swt:min-w-0 swt:min-h-0 swt:flex swt:flex-col swt:gap-3 swt:overflow-hidden swt:p-4"
        prop.children [
            ARCObjectWidget.Navbar(
                Swate.Components.ARCObjectExplorer.Model.selectedTitle viewModel,
                Swate.Components.ARCObjectExplorer.Model.selectedSubtitle viewModel,
                KindFilter.arcObjectExplorerOptions,
                arcObjectCtx.state.SelectedKindIndices,
                arcObjectCtx.setSelectedKindIndices,
                rightActions = searchAction
            )
            match arcObjectCtx.state.StatusMessage with
            | Some statusMessage ->
                Html.div [
                    prop.role.alert
                    prop.className "swt:alert swt:alert-warning swt:text-sm"
                    prop.text statusMessage
                ]
            | None -> Html.none
            ARCObjectPanel.Main(
                "ARC Object Explorer",
                content =
                    ARCObjectWidget.ExplorerContent(
                        viewModel.ExplorerItems,
                        ?selectedItemId = Swate.Components.ARCObjectExplorer.Model.selectedItemId viewModel,
                        onItemClick =
                            (fun item ->
                                if item.Selectable then
                                    promise { do! handleExplorerSelection item } |> Promise.start)
                    )
            )
        ]
    ]

