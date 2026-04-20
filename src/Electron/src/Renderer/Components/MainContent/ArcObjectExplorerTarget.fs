module Renderer.Components.MainContent.ArcObjectExplorerTarget

open Feliz
open Swate.Components
open Swate.Components.ARCObjectExplorer

[<ReactComponent>]
let Main () =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx ()

    let viewModel =
        ArcObjectExplorerView.create
            arcObjectCtx.state.Nodes
            fileStateCtx.state.Selection
            KindFilter.ArcObjectExplorerOptions
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
        ARCObjectWidget.SearchAction(
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
            ARCObjectWidget.Navbar(
                ArcObjectExplorerView.selectedTitle viewModel,
                ArcObjectExplorerView.selectedSubtitle viewModel,
                KindFilter.ArcObjectExplorerOptions,
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
                        ?selectedItemId = ArcObjectExplorerView.selectedItemId viewModel,
                        onItemClick =
                            (fun item ->
                                if item.Selectable then
                                    promise { do! handleExplorerSelection item } |> Promise.start)
                    )
            )
        ]
    ]
