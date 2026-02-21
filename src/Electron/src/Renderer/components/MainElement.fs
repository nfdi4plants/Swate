module Renderer.components.MainElement

open Feliz
open Swate.Components
open Browser.Dom
open ARCtrl
open Renderer.MetadataForms

[<RequireQualifiedAccess>]
type PreviewActiveView =
    | Metadata
    | Table of int
    | DataMap

type ArcFileState = {
    ArcFile: ArcFiles
    ActiveView: PreviewActiveView
}

[<ReactComponent>]
let CreateTablePreview (table: ARCtrl.ArcTable) =
    let tableState, setTableState = React.useState (table)

    React.useEffect (
        (fun () ->
            console.log ("TablePreview received table object")
            setTableState (table)
        ),
        [| box table |]
    )

    AnnotationTableContextProvider.AnnotationTableContextProvider(
        Html.div [
            //It works but not as clean as we want it
            prop.className "swt:w-screen swt:pb-4"
            prop.children [
                AnnotationTable.AnnotationTable(tableState, setTableState)
            ]
        ]
    )

let CreateARCitectWidgetNavbarList (activeView: PreviewActiveView) (addWidget: MainComponents.Widget -> unit) =
    let addBuildingBlock =
        QuickAccessButton.QuickAccessButton(
            "Add Building Block",
            Icons.BuildingBlock(),
            (fun _ -> addWidget MainComponents.Widget._BuildingBlock)
        )

    let addTemplate =
        QuickAccessButton.QuickAccessButton(
            "Add Template",
            Icons.Templates(),
            (fun _ -> addWidget MainComponents.Widget._Template)
        )

    let filePicker =
        QuickAccessButton.QuickAccessButton(
            "File Picker",
            Icons.FilePicker(),
            (fun _ -> addWidget MainComponents.Widget._FilePicker)
        )

    let dataAnnotator =
        QuickAccessButton.QuickAccessButton(
            "Data Annotator",
            Icons.DataAnnotator(),
            (fun _ -> addWidget MainComponents.Widget._DataAnnotator),
            classes = "swt:w-min"
        )

    React.Fragment [
        match activeView with
        | PreviewActiveView.Table _ ->
            addBuildingBlock
            addTemplate
            filePicker
            dataAnnotator
        | PreviewActiveView.DataMap -> dataAnnotator
        | PreviewActiveView.Metadata -> Html.none
    ]

let CreateARCitectNavbarList (arcFile: ArcFiles option) onClick =

    let openReset, setOpenReset = React.useState false

    React.Fragment [
        //Modals.ResetTable.Main(isOpen = openReset, setIsOpen = setOpenReset, dispatch = dispatch)
        QuickAccessButton.QuickAccessButton(
            "Save",
            Icons.Save(),
            onClick,
            //(fun _ ->
            //    ARCitect.Save model.SpreadsheetModel.ArcFile.Value |> ARCitectMsg |> dispatch
            //),
            isDisabled = arcFile.IsNone
        )

        //NavbarBurger.Main(model, dispatch, host = Swatehost.ARCitect)
    ]

[<ReactComponent>]
let CreateARCitectNavbar (activeView: PreviewActiveView) addWidget arcFile onClick =

    //let state, setState = React.useState (SidebarComponents.Navbar.NavbarState.init)
    //let inline toggleMetdadataModal _ =
    //    {
    //        state with
    //            ExcelMetadataModalActive = not state.ExcelMetadataModalActive
    //    }
    //    |> setState

    Components.BaseNavbar.Main [
        CreateARCitectWidgetNavbarList activeView addWidget
        CreateARCitectNavbarList arcFile onClick
        //Html.div [
        //    prop.className "swt:ml-auto"
        //    prop.children [ CreateARCitectNavbarList arcFile onClick ]
        //]
    ]

[<Literal>]
let private NewTablePrefix = "NewTable"

let private canCreateTables (arcFile: ArcFiles) =
    match arcFile with
    | ArcFiles.Assay _
    | ArcFiles.Study _
    | ArcFiles.Run _ -> true
    | _ -> false

let private createNewTableName (tables: ResizeArray<ArcTable>) =
    let existingNames = tables |> Seq.map (fun t -> t.Name)

    let rec loop ind =
        let name = NewTablePrefix + string ind

        if Seq.contains name existingNames then
            loop (ind + 1)
        else
            name

    loop 0

let private refreshArcFileRef (arcFile: ArcFiles) =
    match arcFile with
    | ArcFiles.Investigation investigation -> ArcFiles.Investigation investigation
    | ArcFiles.Study(study, assays) -> ArcFiles.Study(study, assays)
    | ArcFiles.Assay assay -> ArcFiles.Assay assay
    | ArcFiles.Run run -> ArcFiles.Run run
    | ArcFiles.Workflow workflow -> ArcFiles.Workflow workflow
    | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap)
    | ArcFiles.Template template -> ArcFiles.Template template

[<ReactComponent>]
let CreateAddRowsFooter (arcFile: ArcFiles) (activeView: PreviewActiveView) (setArcFile: ArcFiles -> unit) =
    let tables = arcFile.Tables()
    let minRowsToAdd = 1
    let rowsToAdd, setRowsToAdd = React.useState minRowsToAdd
    let rowInputRef = React.useInputRef ()

    let clampRowsToAdd rows = max minRowsToAdd rows

    let canAddRows =
        match activeView with
        | PreviewActiveView.Table tableIndex -> tableIndex >= 0 && tableIndex < tables.Count
        | PreviewActiveView.DataMap ->
            match arcFile with
            | ArcFiles.Assay assay -> assay.DataMap.IsSome
            | ArcFiles.Study(study, _) -> study.DataMap.IsSome
            | ArcFiles.Run run -> run.DataMap.IsSome
            | ArcFiles.DataMap _ -> true
            | _ -> false
        | PreviewActiveView.Metadata -> false

    let addRows () =
        let rowCount = clampRowsToAdd rowsToAdd

        if canAddRows then
            match activeView, arcFile with
            | PreviewActiveView.Table tableIndex, _ when tableIndex >= 0 && tableIndex < tables.Count ->
                tables.[tableIndex].AddRowsEmpty rowCount
                setArcFile (refreshArcFileRef arcFile)
            | PreviewActiveView.DataMap, ArcFiles.Assay assay when assay.DataMap.IsSome ->
                assay.DataMap.Value.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                setArcFile (refreshArcFileRef arcFile)
            | PreviewActiveView.DataMap, ArcFiles.Study(study, _) when study.DataMap.IsSome ->
                study.DataMap.Value.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                setArcFile (refreshArcFileRef arcFile)
            | PreviewActiveView.DataMap, ArcFiles.Run run when run.DataMap.IsSome ->
                run.DataMap.Value.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                setArcFile (refreshArcFileRef arcFile)
            | PreviewActiveView.DataMap, ArcFiles.DataMap(_, dataMap) ->
                dataMap.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                setArcFile (refreshArcFileRef arcFile)
            | _ -> ()

    if canAddRows then
        Html.div [
            prop.className "swt:w-full swt:flex swt:justify-center swt:items-center swt:shrink-0 swt:p-2 swt:bg-base-200 swt:border-t swt:border-base-300"
            prop.title "Add Rows"
            prop.children [
                Html.div [
                    prop.className "swt:join"
                    prop.children [
                        Html.input [
                            prop.className "swt:input swt:join-item swt:border-current"
                            prop.type'.number
                            prop.ref rowInputRef
                            prop.min minRowsToAdd
                            prop.defaultValue minRowsToAdd
                            prop.onChange (fun (value: int) -> setRowsToAdd (clampRowsToAdd value))
                            prop.onKeyDown (key.enter, fun _ -> addRows ())
                            prop.style [ style.width 100 ]
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-outline swt:join-item"
                            prop.onClick (fun _ ->
                                rowInputRef.current.Value.value <- unbox minRowsToAdd
                                setRowsToAdd minRowsToAdd
                                addRows ()
                            )
                            prop.children [ Icons.Plus() ]
                        ]
                    ]
                ]
            ]
        ]
    else
        Html.none

[<ReactComponent>]
let CreateARCitectFooter
    (arcFile: ArcFiles)
    (activeView: PreviewActiveView)
    (setActiveView: PreviewActiveView -> unit)
    (setArcFile: ArcFiles -> unit)
    =
    let tables = arcFile.Tables()
    let canAddTable = canCreateTables arcFile

    let addNewTable () =
        if canAddTable then
            let nextName = createNewTableName tables
            let nextTable = ArcTable.init nextName

            tables.Add(nextTable)
            setArcFile (refreshArcFileRef arcFile)
            setActiveView (PreviewActiveView.Table (tables.Count - 1))

    let footerTabBaseClasses =
        "swt:btn swt:btn-sm swt:border swt:!border-white swt:hover:!border-white swt:rounded-none"

    Html.div [
        prop.className "swt:flex swt:gap-0 swt:p-2 swt:bg-base-200 swt:border-t swt:border-base-300 swt:overflow-x-auto"
        prop.children [|
            // Metadata tab
            Html.button [
                prop.className [
                    footerTabBaseClasses
                    if activeView = PreviewActiveView.Metadata then
                        "swt:btn-primary swt:border-2 swt:!border-white"
                    else
                        "swt:btn-ghost"
                ]
                prop.onClick (fun _ ->
                    setActiveView PreviewActiveView.Metadata
                )
                prop.children [|
                    Html.span [ prop.className "swt:i-fluent--info-24-regular" ]
                    Html.span [ prop.text "Metadata" ]
                |]
            ]
            // Table tabs
            for i = 0 to tables.Count - 1 do
                let table = tables.[i]

                Html.button [
                    prop.key (string i)
                    prop.className [
                        footerTabBaseClasses
                        if activeView = PreviewActiveView.Table i then
                            "swt:btn-primary swt:border-2 swt:!border-white"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView (PreviewActiveView.Table i))
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--table-24-regular" ]
                        Html.span [ prop.text table.Name ]
                    |]
                ]
            if canAddTable then
                Html.button [
                    prop.key "new-table-button"
                    prop.title "+"
                    prop.className
                        "swt:btn swt:btn-sm swt:btn-outline swt:items-center swt:border swt:!border-white swt:hover:!border-white swt:rounded-none"
                    prop.onClick (fun _ -> addNewTable ())
                    prop.children [|
                        Html.span [ prop.text "+" ]
                    |]
                ]
            // DataMap tab
            match arcFile with
            | ArcFiles.Assay a when a.DataMap.IsSome ->
                Html.button [
                    prop.className [
                        footerTabBaseClasses
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary swt:border-2 swt:!border-white"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | ArcFiles.Study(s, _) when s.DataMap.IsSome ->
                Html.button [
                    prop.className [
                        footerTabBaseClasses
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary swt:border-2 swt:!border-white"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | ArcFiles.Run r when r.DataMap.IsSome ->
                Html.button [
                    prop.className [
                        footerTabBaseClasses
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary swt:border-2 swt:!border-white"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | ArcFiles.DataMap _ ->
                Html.button [
                    prop.className [
                        footerTabBaseClasses
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary swt:border-2 swt:!border-white"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | _ -> ()
        |]
    ]

/// Render metadata view based on ArcFile type using editable form components
[<ReactComponent>]
let CreateMetadataPreview (arcFile: ArcFiles, setArcFile: ArcFiles -> unit) =
    Html.div [
        prop.className "swt:p-4 swt:h-full"
        prop.children [
            match arcFile with
            | ArcFiles.Investigation inv ->
                InvestigationMetadata(inv, fun updated -> setArcFile (ArcFiles.Investigation updated))
            | ArcFiles.Study(study, assays) ->
                StudyMetadata(study, fun updated -> setArcFile (ArcFiles.Study(updated, assays)))
            | ArcFiles.Assay assay -> AssayMetadata(assay, fun updated -> setArcFile (ArcFiles.Assay updated))
            | ArcFiles.Run run -> RunMetadata(run, fun updated -> setArcFile (ArcFiles.Run updated))
            | ArcFiles.Workflow workflow ->
                WorkflowMetadata(workflow, fun updated -> setArcFile (ArcFiles.Workflow updated))
            | ArcFiles.DataMap(_, datamap) -> DataMapMetadata(datamap)
            | ArcFiles.Template template ->
                TemplateMetadata(template, fun updated -> setArcFile (ArcFiles.Template updated))
        ]
    ]

let CreateTableView activeView arcFileState setArcFileState =
    match activeView with
    | PreviewActiveView.Metadata -> CreateMetadataPreview(arcFileState, setArcFileState)
    | PreviewActiveView.Table index ->
        let tables = arcFileState.Tables()

        if index < tables.Count then
            CreateTablePreview(tables.[index])
        else
            Html.div [
                prop.className "swt:p-4 swt:text-error"
                prop.text "Table not found"
            ]
    | PreviewActiveView.DataMap ->
        match arcFileState with
        | ArcFiles.Assay a when a.DataMap.IsSome ->
            let dm, setDm = React.useState a.DataMap.Value
            DataMapTable.DataMapTable(dm, setDm)
        | ArcFiles.Study(s, _) when s.DataMap.IsSome ->
            let dm, setDm = React.useState s.DataMap.Value
            DataMapTable.DataMapTable(dm, setDm)
        | ArcFiles.Run r when r.DataMap.IsSome ->
            let dm, setDm = React.useState r.DataMap.Value
            DataMapTable.DataMapTable(dm, setDm)
        | ArcFiles.DataMap(_, datamap) ->
            let dm, setDm = React.useState datamap
            DataMapTable.DataMapTable(dm, setDm)
        | _ ->
            Html.div [
                prop.className "swt:p-4 swt:text-error"
                prop.text "No DataMap available"
            ]
