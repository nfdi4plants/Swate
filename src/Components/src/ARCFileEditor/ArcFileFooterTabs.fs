namespace Swate.Components.ArcFileEditor

open Feliz
open Fable.Core
open ARCtrl
open Swate.Components.ArcFileEditor.Types
open Swate.Components.Shared
open Swate.Components
open Swate.Components.JsBindings
open Fable.Core.JsInterop

module private ArcFileFooterTabsHelper =

    [<Literal>]
    let FooterTabIdDataKey = "footertabid"

    [<Literal>]
    let TableDragIdPrefix = "table-"

    [<Literal>]
    let MetadataDropTargetId = "drop-metadata"

    [<Literal>]
    let DataMapDropTargetId = "drop-datamap"

    [<Literal>]
    let PlusDropTargetId = "drop-plus"

    let mkTableDragId (index: int) = $"{TableDragIdPrefix}{index}"

    let tryParseTableDragId (i: string) =
        if i.StartsWith TableDragIdPrefix then
            let indexStr = i.Substring(TableDragIdPrefix.Length)

            match System.Int32.TryParse indexStr with
            | true, index -> Some index
            | _ -> None
        else
            None

    let resolveDropTargetTableIndex (targetId: string) (tableCount: int) =
        match targetId with
        | MetadataDropTargetId
        | DataMapDropTargetId -> Some 0
        | PlusDropTargetId -> Some tableCount
        | _ -> tryParseTableDragId targetId

    let tryGetDndEventId (eventNode: obj) =
        if isNull eventNode then
            None
        else
            let idObj: obj = eventNode?``id``

            if isNull idObj then None else Some(string idObj)

    let dndObjectProps (obj: Swate.Components.JsBindings.IObject) = [
        for key in Swate.Components.JsBindings.Object.keys obj do
            prop.custom (key, obj.get key)
    ]

    type prop with
        static member dataFooterTabId(arctiveView: ActiveView) : IReactProperty =
            match arctiveView with
            | ActiveView.DataMap -> "datamap"
            | ActiveView.Metadata -> "metadata"
            | ActiveView.Table index -> mkTableDragId index
            |> fun v -> unbox ($"data-{FooterTabIdDataKey}", v)

    let tryParseDataFooterTabId (i: string) =
        match i with
        | "metadata" -> Some ActiveView.Metadata
        | "datamap" -> Some ActiveView.DataMap
        | _ ->
            match tryParseTableDragId i with
            | Some index -> Some(ActiveView.Table index)
            | None -> None

    module ContextMenu =

        let onSpawn (elementRef: IRefValue<option<Browser.Types.HTMLElement>>) =
            (fun (e: Browser.Types.MouseEvent) ->
                let target = e.target :?> Browser.Types.HTMLElement

                match target.closest ($"[data-{FooterTabIdDataKey}]"), elementRef.current with
                | Some cell, Some container when container.contains (cell) ->
                    let cell = cell :?> Browser.Types.HTMLElement
                    let tabId: string = cell?dataset?footertabid
                    unbox (tryParseDataFooterTabId tabId)
                | _ -> None
            )

open ArcFileFooterTabsHelper

[<Erase; Mangle(false)>]
type ArcFileFooterTabs =

    static member private BaseTab
        (
            children: ReactElement,
            onClick: Browser.Types.MouseEvent -> unit,
            ?iconClass: string,
            ?isActive: bool,
            ?key,
            ?extraProps: IReactProperty list,
            // This arg is used to get tab information for context menu
            ?activeView: ActiveView
        ) =
        Html.button [
            if extraProps.IsSome then
                yield! extraProps.Value
            if activeView.IsSome then
                prop.dataFooterTabId activeView.Value
            prop.className [
                "swt:tab swt:items-center swt:min-w-fit"
                match isActive with
                | Some true -> "swt:tab-active"
                | _ -> ()
            ]
            prop.onClick onClick
            prop.children [
                if iconClass.IsSome then
                    Html.i [ prop.className [ iconClass.Value; "swt:min-w-4" ] ]
                children
            ]
        ]

    [<ReactComponent>]
    static member private TableTab(index: int, tableName: string, onClick, isActive: bool, ?key: int) =

        let sortable = DndKit.useSortable ({| id = mkTableDragId index |})

        let style = [
            style.custom ("transform", DndKit.CSS.Transform.toString sortable.transform)
            style.custom ("transition", sortable.transition)
            style.cursor.grab
        ]

        let dndProps = [
            prop.id (mkTableDragId index)
            prop.ref sortable.setNodeRef
            prop.style style
        ]

        let dndProps =
            dndProps
            @ dndObjectProps sortable.attributes
            @ dndObjectProps sortable.listeners

        ArcFileFooterTabs.BaseTab(
            Html.span [ prop.text tableName ],
            onClick,
            "swt:iconify swt:fluent--table-24-regular",
            isActive = isActive,
            activeView = ActiveView.Table index,
            extraProps = dndProps,
            ?key = key
        )

    [<ReactComponent>]
    static member private EditTableNameTab
        (currentName: string, onNameChange: string -> unit, closeEdit: unit -> unit, isActive: bool)
        =
        let localInput, setLocalInput = React.useState currentName

        let submit =
            fun (newName: string) ->
                let trimmed = newName.Trim()

                match System.String.IsNullOrWhiteSpace trimmed || trimmed = currentName with
                | true -> ()
                | _ -> onNameChange localInput

        ArcFileFooterTabs.BaseTab(
            Html.input [
                prop.autoFocus true
                prop.className "swt:input swt:input-ghost swt:input-xs swt:w-full swt:max-w-xs"
                prop.value localInput
                prop.onChange (fun (e: string) -> setLocalInput e)
                prop.onKeyDown (fun (e: Browser.Types.KeyboardEvent) ->
                    match e.code with
                    | kbdEventCode.enter ->
                        e.preventDefault ()
                        submit localInput
                    | kbdEventCode.escape ->
                        e.preventDefault ()
                        setLocalInput currentName
                        closeEdit ()
                    | _ -> ()
                )
                prop.onBlur (fun _ -> closeEdit ())
            ],
            ignore,
            isActive = isActive
        )

    [<ReactComponent>]
    static member private MetadataTab(arcFile: ArcFiles, onClick, isActive: bool) =

        let text =
            match arcFile with
            | ArcFiles.Assay _ -> "Assay"
            | ArcFiles.Study _ -> "Study"
            | ArcFiles.Investigation _ -> "Investigation"
            | ArcFiles.Run _ -> "Run"
            | ArcFiles.Workflow _ -> "Workflow"
            | ArcFiles.Template _ -> "Template"
            | ArcFiles.DataMap _ -> "Datamap" // Not sure about this one.

        ArcFileFooterTabs.BaseTab(
            Html.span text,
            onClick,
            "swt:iconify swt:fluent--info-20-filled",
            isActive = isActive,
            activeView = ActiveView.Metadata,
            extraProps = [ prop.id MetadataDropTargetId ],
            key = "MetadataTab"
        )

    [<ReactComponent>]
    static member private DataMapTab(onClick, isActive: bool) =

        ArcFileFooterTabs.BaseTab(
            Html.span "DataMap",
            onClick,
            "swt:iconify swt:fluent--map-16-regular",
            isActive = isActive,
            activeView = ActiveView.DataMap,
            extraProps = [ prop.id DataMapDropTargetId ],
            key = "DataMapTab"
        )

    [<ReactComponent>]
    static member private PlusBtn(onClick) =

        ArcFileFooterTabs.BaseTab(
            Html.none,
            onClick,
            "swt:iconify swt:fluent--add-12-filled",
            extraProps = [ prop.id PlusDropTargetId ]
        )

    [<ReactComponent>]
    static member private ContextMenu
        (
            elementRef: IRefValue<option<Browser.Types.HTMLElement>>,
            setEditorMode: int option -> unit,
            deleteTable: int -> unit
        ) =


        let delete (tableIndex: int) = fun _ -> deleteTable tableIndex

        let rename (tableIndex: int) =
            fun _ -> setEditorMode (Some tableIndex)

        let children =
            fun e ->
                let activeView = unbox<ActiveView option> e

                match activeView with
                | Some(ActiveView.Table index) -> [
                    Swate.Components.ContextMenuItem(
                        Html.span "Rename Table",
                        icon =
                            Html.i [
                                prop.className "swt:iconify swt:fluent--slide-text-title-edit-20-regular swt:size-4"
                            ],
                        onClick = rename index
                    )
                    Swate.Components.ContextMenuItem(
                        Html.span "Delete Table",
                        icon =
                            Html.i [
                                prop.className "swt:iconify swt:fluent--delete-20-filled swt:size-4"
                            ],
                        onClick = delete index
                    )
                  ]
                | _ -> []


        Swate.Components.ContextMenu.ContextMenu(children, ref = elementRef, onSpawn = ContextMenu.onSpawn elementRef)

    [<ReactComponent>]
    static member DragAndDropContainer(tableIds: ResizeArray<string>, handleDragEnd, children: ReactElement) =

        let sensors = DndKit.useSensors [| DndKit.useSensor DndKit.PointerSensor |]

        DndKit.DndContext(
            sensors = sensors,
            onDragEnd = handleDragEnd,
            collisionDetection = DndKit.closestCenter,
            children =
                DndKit.SortableContext(
                    items = tableIds,
                    strategy = DndKit.horizontalListSortingStrategy,
                    children = children
                )
        )

    [<ReactComponent>]
    static member Main
        (arcFile: ArcFiles, activeView: ActiveView, setActiveView: ActiveView -> unit, setArcFile: ArcFiles -> unit)
        =
        let tables = arcFile.Tables()
        let canAddTable = arcFile.CanCreateTables()
        let tabsRef = React.useElementRef ()

        let isEditorModeTableTab, setIsEditorModeTableTab =
            React.useState (None: int option)

        let sensors = DndKit.useSensors [| DndKit.useSensor DndKit.PointerSensor |]

        let addNewTable _ =
            if canAddTable then
                let nextName = Helper.createNewTableName tables
                let nextTable = ArcTable.init nextName

                tables.Add nextTable
                setArcFile (WidgetArcFile.refreshRef arcFile)
                setActiveView (ActiveView.Table(tables.Count - 1))

        let deleteTable (tableIndex: int) =
            arcFile.ArcTables().RemoveTableAt tableIndex

            match activeView with
            | ActiveView.Table i when i = tableIndex -> setActiveView ActiveView.Metadata
            | _ -> ()

            setArcFile (WidgetArcFile.refreshRef arcFile)

        let updateTableOrder (oldIndex: int, newIndex: int) =
            arcFile.ArcTables().MoveTable(oldIndex, newIndex)
            let lastIndex = tables.Count - 1
            let nextActiveIndex = max 0 (min newIndex lastIndex)
            setActiveView (ActiveView.Table nextActiveIndex)
            setArcFile (WidgetArcFile.refreshRef arcFile)

        let handleDragEnd (event: DndKit.IDndKitEvent) =

            if isEditorModeTableTab.IsSome then
                ()
            else
                match tryGetDndEventId (box event.active), tryGetDndEventId (box event.over) with
                | Some activeId, Some overId when activeId <> overId ->
                    match tryParseTableDragId activeId, resolveDropTargetTableIndex overId tables.Count with
                    | Some oldIndex, Some newIndex when oldIndex <> newIndex -> updateTableOrder (oldIndex, newIndex)
                    | _ -> ()
                | _ -> ()

        let tableIds =
            tables |> Seq.mapi (fun index _ -> mkTableDragId index) |> ResizeArray

        React.Fragment [
            ArcFileFooterTabs.ContextMenu(tabsRef, setIsEditorModeTableTab, deleteTable)
            Html.div [
                prop.className "swt:bg-base-300"
                prop.children [

                    Html.div [
                        prop.ref tabsRef
                        prop.className
                            "swt:*:[--tab-border-color:var(--color-base-content)] swt:tabs swt:tabs-lift swt:w-full \
                                swt:overflow-x-auto swt:overflow-y-hidden swt:flex swt:flex-row swt:items-center \
                                swt:justify-start swt:pt-1 swt:*:border-b-0! swt:*:gap-1 swt:flex-nowrap swt:*:flex-nowrap"
                        prop.children [

                            Html.div [ // This element is a spacer to create some whitespace between the tabs and the left edge of the container.
                                prop.className "swt:tab swt:max-w-min swt:px-2!"
                                prop.style [ style.custom ("order", -99) ]
                            ]

                            ArcFileFooterTabs.MetadataTab(
                                arcFile,
                                (fun _ -> setActiveView ActiveView.Metadata),
                                isActive = (activeView = ActiveView.Metadata)
                            )

                            if arcFile.CanRenderDataMapView() then
                                ArcFileFooterTabs.DataMapTab(
                                    (fun _ -> setActiveView ActiveView.DataMap),
                                    isActive = (activeView = ActiveView.DataMap)
                                )

                            ArcFileFooterTabs.DragAndDropContainer(
                                tableIds,
                                handleDragEnd,
                                children =
                                    React.Fragment [

                                        for index = 0 to tables.Count - 1 do
                                            let table = tables.[index]

                                            let isEditorMode = isEditorModeTableTab = Some index
                                            let isActive = activeView = ActiveView.Table index

                                            match isEditorMode with
                                            | true ->
                                                let renameTable newName =
                                                    match
                                                        System.String.IsNullOrWhiteSpace newName || newName = table.Name
                                                    with
                                                    | true -> ()
                                                    | false ->
                                                        arcFile.ArcTables().RenameTableAt(index, newName)
                                                        setArcFile (WidgetArcFile.refreshRef arcFile)
                                                        setIsEditorModeTableTab None

                                                let closeEdit () = setIsEditorModeTableTab None

                                                ArcFileFooterTabs.EditTableNameTab(
                                                    table.Name,
                                                    renameTable,
                                                    closeEdit,
                                                    isActive = isActive
                                                )
                                            | false ->
                                                ArcFileFooterTabs.TableTab(
                                                    index,
                                                    table.Name,
                                                    (fun _ -> setActiveView (ActiveView.Table index)),
                                                    isActive = isActive,
                                                    key = index
                                                )

                                    ]
                            )


                            if canAddTable then
                                ArcFileFooterTabs.PlusBtn addNewTable

                            Html.div [ // This element is a spacer to create some whitespace between the tabs and the right edge of the container.
                                prop.className "swt:tab swt:max-w-min swt:px-2!"
                                prop.style [ style.custom ("order", System.Int32.MaxValue) ]
                            ]
                        ]
                    ]
                ]
            ]
        ]