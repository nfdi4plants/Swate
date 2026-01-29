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
            prop.className "swt:w-screen"
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

[<ReactComponent>]
let CreateARCitectNavbar (activeView: PreviewActiveView) addWidget =
    let state, setState = React.useState (SidebarComponents.Navbar.NavbarState.init)

    //let inline toggleMetdadataModal _ =
    //    {
    //        state with
    //            ExcelMetadataModalActive = not state.ExcelMetadataModalActive
    //    }
    //    |> setState

    Components.BaseNavbar.Glow [
        CreateARCitectWidgetNavbarList activeView addWidget
    ]

[<ReactComponent>]
let CreateARCitectFooter (arcFile: ArcFiles) (activeView: PreviewActiveView) (setActiveView: PreviewActiveView -> unit) =
    let tables = arcFile.Tables()

    Html.div [
        prop.className "swt:flex swt:gap-1 swt:p-2 swt:bg-base-200 swt:border-t swt:border-base-300 swt:overflow-x-auto"
        prop.children [|
            // Metadata tab
            Html.button [
                prop.className [
                    "swt:btn swt:btn-sm"
                    if activeView = PreviewActiveView.Metadata then
                        "swt:btn-primary"
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
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.Table i then
                            "swt:btn-primary"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView (PreviewActiveView.Table i))
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--table-24-regular" ]
                        Html.span [ prop.text table.Name ]
                    |]
                ]
            // DataMap tab
            match arcFile with
            | ArcFiles.Assay a when a.DataMap.IsSome ->
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
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
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
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
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
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
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
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
