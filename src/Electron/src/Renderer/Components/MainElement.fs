module Renderer.Components.MainElement

open Feliz
open Swate.Components
open Swate.Components.Shared
open ARCtrl
open WidgetRegistry

[<ReactComponent>]
let CreateARCitectNavbar
    (editorState: ArcFileEditorHeaderProps)
    (setArcFileState: ArcFiles -> unit)
    (onSaveClick: Browser.Types.MouseEvent -> unit)
    =

    let activeTableIndex = editorState.activeView.TryTableIndex
    let widgetHostView = editorState.activeView.ToWidgetHostView()

    let templateImportType, setTemplateImportType =
        React.useState TableJoinOptions.Headers

    let widgets =
        createWidgets
            editorState.arcFile
            widgetHostView
            activeTableIndex
            setArcFileState
            templateImportType
            setTemplateImportType

    let hasSelectedTable =
        editorState.arcFile.TryGetActiveTable(activeTableIndex)
        |> Option.isSome

    Widget.WidgetController(
        widgets,
        closeAllWhen = not hasSelectedTable,
        children = [
            Components.BaseNavbar.Main [
                NavbarButtons(widgetTypes, hasSelectedTable)
                QuickAccessButton.QuickAccessButton("Save", Icons.Save(), onSaveClick)
            ]
        ]
    )
