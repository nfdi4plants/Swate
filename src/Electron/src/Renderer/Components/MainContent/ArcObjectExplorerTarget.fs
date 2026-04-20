module Renderer.Components.MainContent.ArcObjectExplorerTarget

open Feliz
open Swate.Components

[<ReactComponent>]
let Main () =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx ()

    let viewModel =
        ArcObjectExplorerView.create
            arcObjectCtx.state.Nodes
            fileStateCtx.state.Selection
            arcObjectCtx.state.SelectedKindIndices

    let services =
        Renderer.Components.ARCHelper.createArcExplorerServices
            pageStateCtx.setState
            arcObjectCtx.setArcFileState
            arcObjectCtx.setPreviewState
            arcObjectCtx.setStatusMessage

    let handleExplorerSelection =
        Swate.Components.ARCExplorer.createOpenPreviewHandler
            fileStateCtx.setSelection
            services

    let searchAction =
        Swate.Components.ARCObjectWidget.SearchAction(
            viewModel.SearchItems,
            (fun (name, _, _) -> name),
            (fun (_, _, item) -> promise { do! handleExplorerSelection item } |> Promise.start),
            itemSubtitle = (fun (_, subtitle, _) -> subtitle)
        )

    Html.div [
        prop.id "arc-object-target"
        prop.className
            "swt:size-full swt:min-w-0 swt:min-h-0 swt:flex swt:flex-col swt:gap-3 swt:overflow-hidden swt:p-4"
        prop.children [
            Swate.Components.ARCObjectWidget.Navbar(
                ArcObjectExplorerView.selectedTitle viewModel,
                ArcObjectExplorerView.selectedSubtitle viewModel,
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
            Swate.Components.ARCObjectPanel.Main(
                "ARC Object Explorer",
                content =
                    Swate.Components.ARCObjectWidget.ExplorerContent(
                        viewModel.ExplorerItems,
                        ?selectedItemId = ArcObjectExplorerView.selectedItemId viewModel,
                        onItemClick =
                            (fun item ->
                                if item.Selectable then
                                    promise { do! handleExplorerSelection item } |> Promise.start)
                    )
            )
        ]
    ]
