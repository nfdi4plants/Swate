module Renderer.components.Widgets.AddDataAnnotatorWidget

open System
open Feliz
open ARCtrl
open Swate.Components
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open DataAnnotator
open DataAnnotatorDataSource

type HostView =
    | Table
    | DataMap
    | Metadata
    | PreviewError

let private widgetContainerClass =
    "swt:flex swt:flex-col swt:gap-3 swt:p-2 swt:w-fit swt:max-w-[95vw]"

let private refreshArcFileRef (arcFile: ArcFiles) =
    match arcFile with
    | ArcFiles.Investigation investigation -> ArcFiles.Investigation investigation
    | ArcFiles.Study(study, assays) -> ArcFiles.Study(study, assays)
    | ArcFiles.Assay assay -> ArcFiles.Assay assay
    | ArcFiles.Run run -> ArcFiles.Run run
    | ArcFiles.Workflow workflow -> ArcFiles.Workflow workflow
    | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap)
    | ArcFiles.Template template -> ArcFiles.Template template

let private fileNameFromPath (path: string) =
    path.Replace("\\", "/").Split('/') |> Array.last

let private fileTypeFromName (fileName: string) =
    if fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) then
        "text/csv"
    elif fileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) then
        "text/tab-separated-values"
    elif fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) then
        "text/plain"
    else
        "text/plain"

let private separatorToInput (separator: string) =
    match separator with
    | "\t" -> "\\t"
    | "\n" -> "\\n"
    | "\r" -> "\\r"
    | "\r\n" -> "\\r\\n"
    | "\f" -> "\\f"
    | "\v" -> "\\v"
    | _ -> separator

let private parseDataFileBySeparator (separator: string) (dataFile: DataFile) =
    match TryParseDataFile separator dataFile with
    | Ok parsed -> Ok parsed
    | Error err ->
        let fallbackSeparator = dataFile.ExpectedSeparator

        if separator <> fallbackSeparator then
            TryParseDataFile fallbackSeparator dataFile
        else
            Error err

let private targetOption (value: TargetColumn) =
    Html.option [
        prop.value (string value)
        prop.text (string value)
    ]

let private defaultSeparatorOptions: (string * string)[] = [|
    "\\t", "Tab (\\t)"
    ",", "Comma (,)"
    ";", "Semicolon (;)"
    "|", "Pipe (|)"
|]

let private inferDisabledMessage (arcFileState: ArcFiles option) (activeView: HostView) (activeTableIndex: int option) =
    match arcFileState with
    | None -> Some "Open an ARC file first."
    | Some arcFile ->
        match activeView with
        | HostView.Metadata -> Some "Switch to a table or datamap tab to use Data Annotator."
        | HostView.PreviewError -> Some "Data Annotator is unavailable while the preview is in an error state."
        | HostView.Table ->
            match activeTableIndex with
            | Some tableIndex when tableIndex >= 0 && tableIndex < arcFile.Tables().Count -> None
            | _ -> Some "Select a table tab to use Data Annotator."
        | HostView.DataMap ->
            let hasDataMap =
                match arcFile with
                | ArcFiles.Assay assay -> assay.DataMap.IsSome
                | ArcFiles.Study(study, _) -> study.DataMap.IsSome
                | ArcFiles.Run run -> run.DataMap.IsSome
                | ArcFiles.DataMap _ -> true
                | _ -> false

            if hasDataMap then
                None
            else
                Some "No DataMap available for this ARC file."

[<ReactComponent>]
let Table parsedFile (selectedTargets: Set<DataTarget>) columnCount toggleTarget =
    Html.table [
        prop.className "swt:table swt:table-xs swt:table-pin-rows"
        prop.children [
            Html.thead [
                Html.tr [
                    Html.th [
                        prop.className "swt:w-24"
                        prop.text "#"
                    ]
                    for ci in 0 .. columnCount - 1 do
                        let target = DataTarget.Column ci
                        let isSelected = selectedTargets.Contains target

                        let label =
                            parsedFile.HeaderRow
                            |> Option.bind (fun row -> row |> Array.tryItem ci)
                            |> Option.defaultValue $"C{ci + 1}"

                        Html.th [
                            prop.key (target.ToReactKey())
                            prop.className [
                                "swt:cursor-pointer swt:max-w-48 swt:truncate"
                                if isSelected then
                                    "swt:bg-primary swt:text-primary-content"
                            ]
                            prop.title "Toggle entire column selector"
                            prop.onClick (fun _ -> toggleTarget target)
                            prop.text label
                        ]
                ]
            ]
            Html.tbody [
                for ri in 0 .. parsedFile.BodyRows.Length - 1 do
                    let row = parsedFile.BodyRows.[ri]
                    let rowTarget = DataTarget.Row ri
                    let isRowSelected = selectedTargets.Contains rowTarget

                    Html.tr [
                        prop.key (rowTarget.ToReactKey())
                        prop.children [
                            Html.th [
                                prop.className [
                                    "swt:cursor-pointer swt:font-mono"
                                    if isRowSelected then
                                        "swt:bg-primary swt:text-primary-content"
                                ]
                                prop.title "Toggle entire row selector"
                                prop.onClick (fun _ -> toggleTarget rowTarget)
                                prop.text (string (ri + 1))
                            ]
                            for ci in 0 .. columnCount - 1 do
                                let cellTarget = DataTarget.Cell(ci, ri)
                                let isDirectSelection = selectedTargets.Contains cellTarget

                                let isInheritedSelection =
                                    selectedTargets.Contains(DataTarget.Column ci)
                                    || selectedTargets.Contains(DataTarget.Row ri)

                                let isSelected = isDirectSelection || isInheritedSelection

                                let value =
                                    if ci < row.Length then row.[ci] else ""

                                Html.td [
                                    prop.key (cellTarget.ToReactKey())
                                    prop.className [
                                        "swt:cursor-pointer swt:max-w-64 swt:truncate"
                                        if isSelected then
                                            "swt:bg-primary/70 swt:text-primary-content"
                                    ]
                                    prop.title "Toggle cell selector"
                                    prop.onClick (fun _ -> toggleTarget cellTarget)
                                    prop.text (
                                        if String.IsNullOrWhiteSpace value then
                                            " "
                                        else
                                            value
                                    )
                                ]
                        ]
                    ]
            ]
        ]
    ]

[<ReactComponent>]
let private FileControllerElements pickFile loading selectedPath (dataFile: DataFile option) reset =
    Html.div [
        prop.className "swt:flex swt:flex-wrap swt:gap-2"
        prop.children [
            Html.button [
                prop.className "swt:btn swt:btn-primary swt:btn-sm"
                prop.disabled loading
                prop.text "Choose File"
                prop.onClick (fun _ -> pickFile () |> Promise.start)
            ]
            Html.input [
                prop.className "swt:input swt:input-sm swt:grow"
                prop.readOnly true
                prop.placeholder "No file selected"
                prop.value (
                    selectedPath
                    |> Option.map fileNameFromPath
                    |> Option.defaultValue ""
                )
            ]
            Html.button [
                prop.className "swt:btn swt:btn-outline swt:btn-sm"
                prop.disabled dataFile.IsNone
                prop.text "Reset"
                prop.onClick (fun _ -> reset ())
            ]
        ]
    ]

[<ReactComponent>]
let Main
    (
        arcFileState: ArcFiles option,
        activeView: HostView,
        activeTableIndex: int option,
        setArcFileState: ArcFiles option -> unit
    ) =

    let dataFile, setDataFile = React.useState (None: DataFile option)
    let parsedFile, setParsedFile = React.useState (None: ParsedDataFile option)
    let selectedTargets, setSelectedTargets = React.useState (Set.empty<DataTarget>)
    let separatorInput, setSeparatorInput = React.useState ""
    let selectedPath, setSelectedPath = React.useState (None: string option)
    let targetColumn, setTargetColumn = React.useState TargetColumn.Autodetect
    let loading, setLoading = React.useState false
    let statusMessage, setStatusMessage = React.useState (None: string option)
    let errorMessage, setErrorMessage = React.useState (None: string option)
    let widgetCtx = WidgetContext.useWidgetController ()

    let disabledMessage = inferDisabledMessage arcFileState activeView activeTableIndex

    let reset () =
        setDataFile None
        setParsedFile None
        setSelectedTargets Set.empty
        setSeparatorInput ""
        setSelectedPath None
        setTargetColumn TargetColumn.Autodetect
        setStatusMessage None
        setErrorMessage None

    let setLoadedFile (path: string) (dataFile: DataFile) (parsedFile: ParsedDataFile option) =
        setSelectedPath (Some path)
        setDataFile (Some dataFile)
        setParsedFile parsedFile
        setSeparatorInput (separatorToInput dataFile.ExpectedSeparator)
        setSelectedTargets Set.empty

    let loadFileFromPath (path: string) =
        promise {
            setLoading true
            setStatusMessage None
            setErrorMessage None

            try
                let! fileResult = Api.ipcArcVaultApi.openFile (unbox null) path

                match fileResult with
                | Error exn ->
                    setErrorMessage (Some $"Failed to open file: {exn.Message}")
                | Ok pageState ->
                    match pageState with
                    | PageState.Text content ->
                        let fileName = fileNameFromPath path
                        let dataFile =
                            DataFile.create (fileName, fileTypeFromName fileName, content, float content.Length)

                        match parseDataFileBySeparator dataFile.ExpectedSeparator dataFile with
                        | Ok parsed ->
                            setLoadedFile path dataFile (Some parsed)
                            setStatusMessage (Some $"Loaded {fileName} ({parsed.BodyRows.Length} rows).")
                        | Error err ->
                            setLoadedFile path dataFile None
                            setErrorMessage (Some err)
                    | _ ->
                        setErrorMessage (
                            Some
                                "Selected file could not be loaded as plain text. Only csv/tsv/txt are supported."
                        )
            finally
                setLoading false
        }

    let pickFile () =
        promise {
            setStatusMessage None
            setErrorMessage None

            let! pathResult = Api.ipcArcVaultApi.pickPaths (unbox null)

            match pathResult with
            | Error exn ->
                if exn.Message <> "Cancelled" then
                    setErrorMessage (Some $"Failed to pick file: {exn.Message}")
            | Ok paths when paths.Length = 0 ->
                setStatusMessage (Some "No file selected.")
            | Ok paths ->
                if paths.Length > 1 then
                    setStatusMessage (Some "Multiple files selected. Using the first one.")

                do! loadFileFromPath paths.[0]
        }

    let applySeparator () =
        match dataFile with
        | None -> ()
        | Some dataFile ->
            if String.IsNullOrWhiteSpace separatorInput then
                setErrorMessage (Some "Separator must not be empty.")
            else
                match parseDataFileBySeparator separatorInput dataFile with
                | Ok parsed ->
                    setParsedFile (Some parsed)
                    setSelectedTargets Set.empty
                    setErrorMessage None
                    setStatusMessage (Some "Separator updated.")
                | Error err ->
                    setParsedFile None
                    setErrorMessage (Some err)

    let toggleHeader () =
        match parsedFile with
        | None -> ()
        | Some parsed ->
            setParsedFile (Some(parsed.ToggleHeader()))
            setSelectedTargets Set.empty
            setStatusMessage None
            setErrorMessage None

    let toggleTarget (target: DataTarget) =
        if selectedTargets.Contains target then
            setSelectedTargets (selectedTargets.Remove target)
        else
            setSelectedTargets (selectedTargets.Add target)

    let submit () =
        if selectedTargets.IsEmpty then
            setErrorMessage (Some "Select at least one target in the preview table.")
        else
            match arcFileState, dataFile, parsedFile with
            | Some arcFile, Some dataFile, Some parsedFile ->
                let selectors = SelectorsFromTargets parsedFile.HeaderRow.IsSome selectedTargets

                let input: AnnotationInput = {
                    Selectors = selectors
                    FileName = dataFile.DataFileName
                    FileType = dataFile.DataFileType
                    TargetColumn = targetColumn
                }

                let applySuccess count =
                    setArcFileState (Some(refreshArcFileRef arcFile))
                    setErrorMessage None
                    setStatusMessage (Some $"Applied {count} data annotation(s).")
                    widgetCtx.closeWidget WidgetType.DataAnnotator

                match activeView with
                | HostView.Table ->
                    match activeTableIndex with
                    | Some tableIndex when tableIndex >= 0 && tableIndex < arcFile.Tables().Count ->
                        let table = arcFile.Tables().[tableIndex]

                        match ApplyToTable table input with
                        | Ok count -> applySuccess count
                        | Error err -> setErrorMessage (Some err)
                    | _ -> setErrorMessage (Some "No active table selected.")
                | HostView.DataMap ->
                    let dataMapOpt =
                        match arcFile with
                        | ArcFiles.Assay assay when assay.DataMap.IsSome -> Some assay.DataMap.Value
                        | ArcFiles.Study(study, _) when study.DataMap.IsSome -> Some study.DataMap.Value
                        | ArcFiles.Run run when run.DataMap.IsSome -> Some run.DataMap.Value
                        | ArcFiles.DataMap(_, dataMap) -> Some dataMap
                        | _ -> None

                    match dataMapOpt with
                    | Some dataMap ->
                        match ApplyToDataMap dataMap input with
                        | Ok count -> applySuccess count
                        | Error err -> setErrorMessage (Some err)
                    | None -> setErrorMessage (Some "No DataMap available.")
                | HostView.Metadata ->
                    setErrorMessage (Some "Data annotation cannot be applied in metadata view.")
                | HostView.PreviewError ->
                    setErrorMessage (Some "Data annotation cannot be applied while preview is in error state.")
            | _ -> setErrorMessage (Some "Load a file first.")

    let canSubmit =
        disabledMessage.IsNone
        && dataFile.IsSome
        && parsedFile.IsSome
        && (loading |> not)

    let previewSection =
        match parsedFile with
        | None ->
            Html.div [
                prop.className
                    "swt:rounded-box swt:border swt:border-base-300 swt:flex swt:items-center swt:justify-center swt:text-sm swt:opacity-70 swt:min-h-24 swt:px-3 swt:py-4"
                prop.text "Load a data file to preview selectable targets."
            ]
        | Some parsedFile ->
            let headerCount =
                parsedFile.HeaderRow
                |> Option.map _.Length
                |> Option.defaultValue 0

            let bodyCount =
                parsedFile.BodyRows
                |> Array.fold (fun acc row -> max acc row.Length) 0

            let columnCount = max headerCount bodyCount

            Html.div [
                prop.className
                    "swt:overflow-auto swt:rounded-box swt:border swt:border-base-300 swt:max-h-[55vh]"
                prop.children [
                    if parsedFile.BodyRows.Length = 0 || columnCount = 0 then
                        Html.div [
                            prop.className
                                "swt:flex swt:items-center swt:justify-center swt:h-full swt:text-sm swt:opacity-70"
                            prop.text "Parsed file contains no data rows."
                        ]
                    else
                        Table parsedFile selectedTargets columnCount toggleTarget
                ]
            ]

    let selectedTargetCount = selectedTargets.Count

    match disabledMessage with
    | Some message ->
        Html.div [
            prop.className widgetContainerClass
            prop.children [
                Html.h3 [
                    prop.className "swt:text-xl swt:font-bold"
                    prop.text "Data Annotator"
                ]
                Html.div [
                    prop.className "swt:alert swt:alert-warning swt:text-sm"
                    prop.children [ Html.text message ]
                ]
            ]
        ]
    | None ->
        Html.div [
            prop.className widgetContainerClass
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:items-end swt:gap-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:text-xl swt:font-bold"
                            prop.text "Data Annotator"
                        ]
                        Html.span [
                            prop.className "swt:text-xs swt:opacity-70 swt:ml-auto"
                            prop.textf "%d target(s) selected" selectedTargetCount
                        ]
                    ]
                ]
                FileControllerElements pickFile loading selectedPath dataFile reset
                if dataFile.IsSome then
                    Html.div [
                        prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:items-center"
                        prop.children [
                            let hasCustomSeparator =
                                (defaultSeparatorOptions
                                 |> Array.exists (fun (separator, _) -> separator = separatorInput)
                                 |> not)
                                && (String.IsNullOrWhiteSpace separatorInput |> not)

                            Html.select [
                                prop.className "swt:select swt:select-sm swt:w-44"
                                prop.valueOrDefault separatorInput
                                prop.onChange setSeparatorInput
                                prop.children [
                                    if hasCustomSeparator then
                                        Html.option [
                                            prop.value separatorInput
                                            prop.text $"Custom ({separatorInput})"
                                        ]

                                    for separator, label in defaultSeparatorOptions do
                                        Html.option [
                                            prop.value separator
                                            prop.text label
                                        ]
                                ]
                            ]
                            Html.input [
                                prop.className "swt:input swt:input-sm swt:w-32"
                                prop.placeholder "separator"
                                prop.value separatorInput
                                prop.onChange setSeparatorInput
                                prop.onKeyDown (
                                    key.enter,
                                    fun _ -> applySeparator ()
                                )
                            ]
                            Html.button [
                                prop.className "swt:btn swt:btn-sm"
                                prop.text "Apply Separator"
                                prop.onClick (fun _ -> applySeparator ())
                                prop.disabled (String.IsNullOrWhiteSpace separatorInput)
                            ]
                            Html.button [
                                prop.className "swt:btn swt:btn-sm"
                                prop.text (
                                    match parsedFile with
                                    | Some parsed when parsed.HeaderRow.IsSome -> "Header: On"
                                    | _ -> "Header: Off"
                                )
                                prop.onClick (fun _ -> toggleHeader ())
                                prop.disabled parsedFile.IsNone
                            ]
                            if activeView = HostView.Table then
                                Html.select [
                                    prop.className "swt:select swt:select-sm"
                                    prop.valueOrDefault (string targetColumn)
                                    prop.onChange (fun (value: string) ->
                                        setTargetColumn (TargetColumn.fromString value)
                                    )
                                    prop.children [
                                        targetOption TargetColumn.Autodetect
                                        targetOption TargetColumn.Input
                                        targetOption TargetColumn.Output
                                    ]
                                ]
                        ]
                    ]
                previewSection
                if loading then
                    Html.div [
                        prop.className "swt:alert swt:alert-info swt:text-xs"
                        prop.children [ Html.text "Loading file..." ]
                    ]
                if statusMessage.IsSome then
                    Html.div [
                        prop.className "swt:alert swt:alert-success swt:text-xs"
                        prop.children [ Html.text statusMessage.Value ]
                    ]
                if errorMessage.IsSome then
                    Html.div [
                        prop.className "swt:alert swt:alert-error swt:text-xs"
                        prop.children [ Html.text errorMessage.Value ]
                    ]
                Html.div [
                    prop.className "swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-outline"
                            prop.text "Cancel"
                            prop.onClick (fun _ -> widgetCtx.closeWidget WidgetType.DataAnnotator)
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-primary swt:ml-auto"
                            prop.disabled (not canSubmit)
                            prop.text "Submit"
                            prop.onClick (fun _ -> submit ())
                        ]
                    ]
                ]
            ]
        ]
