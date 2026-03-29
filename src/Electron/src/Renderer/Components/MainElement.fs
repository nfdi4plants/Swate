module Renderer.Components.MainElement

open Feliz
open Swate.Components
open ARCtrl
open WidgetRegistry

[<ReactComponent>]
let CreateARCitectNavbar
    (editorState: ArcFileEditorHeaderProps)
    (setArcFileState: ArcFiles -> unit)
    (onSaveClick: Browser.Types.MouseEvent -> unit)
    =

    let templateImportType, setTemplateImportType =
        React.useState TableJoinOptions.Headers

    let widgets =
        createWidgets
            editorState.arcFile
            editorState.widgetHostView
            editorState.activeTableIndex
            setArcFileState
            templateImportType
            setTemplateImportType

    let hasSelectedTable = editorState.activeTableIndex.IsSome

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
