namespace Swate.Components.Composite.Widgets.DataAnnotator

open System
open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Composite.Widgets.Context
open Swate.Components.Composite.Widgets.DataAnnotator.Types
open Swate.Components.Composite.Widgets.DataAnnotator.Helper

[<Erase; Mangle(false)>]
type DataAnnotatorWidget =

    [<ReactComponent>]
    static member private Table
        (
            parsedFile: ParsedDataFile,
            selectedTargets: Set<DataTarget>,
            columnCount: int,
            toggleTarget: DataTarget -> unit
        ) =
        Html.table [
            prop.className "swt:table swt:table-xs swt:table-pin-rows"
            prop.children [
                Html.thead [
                    Html.tr [
                        Html.th [ prop.className "swt:w-24"; prop.text "#" ]
                        for columnIndex in 0 .. columnCount - 1 do
                            let target = DataTarget.Column columnIndex
                            let isSelected = selectedTargets.Contains target

                            let label =
                                parsedFile.HeaderRow
                                |> Option.bind (fun row -> row |> Array.tryItem columnIndex)
                                |> Option.defaultValue $"C{columnIndex + 1}"

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
                    for rowIndex in 0 .. parsedFile.BodyRows.Length - 1 do
                        let row = parsedFile.BodyRows.[rowIndex]
                        let rowTarget = DataTarget.Row rowIndex
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
                                    prop.text (string (rowIndex + 1))
                                ]
                                for columnIndex in 0 .. columnCount - 1 do
                                    let cellTarget = DataTarget.Cell(columnIndex, rowIndex)

                                    let isDirectSelection = selectedTargets.Contains cellTarget

                                    let isInheritedSelection =
                                        selectedTargets.Contains(DataTarget.Column columnIndex)
                                        || selectedTargets.Contains(DataTarget.Row rowIndex)

                                    let isSelected = isDirectSelection || isInheritedSelection

                                    let value = if columnIndex < row.Length then row.[columnIndex] else ""

                                    Html.td [
                                        prop.key (cellTarget.ToReactKey())
                                        prop.className [
                                            "swt:cursor-pointer swt:max-w-64 swt:truncate"
                                            if isSelected then
                                                "swt:bg-primary/70 swt:text-primary-content"
                                        ]
                                        prop.title "Toggle cell selector"
                                        prop.onClick (fun _ -> toggleTarget cellTarget)
                                        prop.text (if String.IsNullOrWhiteSpace value then " " else value)
                                    ]
                            ]
                        ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private FileControllerElements
        (
            pickFile: unit -> JS.Promise<unit>,
            loading: bool,
            selectedFileName: string option,
            dataFile: DataFile option,
            reset: unit -> unit
        ) =
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
                    prop.value (selectedFileName |> Option.defaultValue "")
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-outline swt:btn-sm"
                    prop.disabled dataFile.IsNone
                    prop.text "Reset"
                    prop.onClick (fun _ -> reset ())
                ]
            ]
        ]

    [<ReactComponent(true)>]
    static member Main(setAnnotationInput: AnnotationInput -> unit, ?onError: string -> unit) =

        let onError =
            defaultArg onError (fun message -> console.error ("DataAnnotatorWidget error: " + message))

        let dataFile, setDataFile = React.useState (None: DataFile option)

        let parsedFile, setParsedFile = React.useState (None: ParsedDataFile option)

        let selectedTargets, setSelectedTargets =
            React.useStateWithUpdater (Set.empty<DataTarget>)

        let separatorInput, setSeparatorInput = React.useState ""
        let selectedFileName, setSelectedFileName = React.useState (None: string option)

        let targetColumn, setTargetColumn = React.useState TargetColumn.Autodetect

        let loading, setLoading = React.useState false
        let widgetCtx = useWidgetControllerCtx ()

        let reset () =
            setDataFile None
            setParsedFile None
            setSelectedTargets (fun _ -> Set.empty)
            setSeparatorInput ""
            setSelectedFileName None
            setTargetColumn TargetColumn.Autodetect

        let setLoadedFile (fileName: string) (nextDataFile: DataFile) (nextParsedFile: ParsedDataFile option) =
            setSelectedFileName (Some fileName)
            setDataFile (Some nextDataFile)
            setParsedFile nextParsedFile
            setSeparatorInput (separatorToInput nextDataFile.ExpectedSeparator)
            setSelectedTargets (fun _ -> Set.empty)

        let loadImportedFile (importedFile: ImportedTextFile) = promise {
            setLoading true

            try
                let loadedDataFile =
                    DataFile.create (
                        importedFile.Name,
                        fileTypeFromName importedFile.Name,
                        importedFile.Content,
                        float importedFile.Content.Length
                    )

                match parseDataFileBySeparator loadedDataFile.ExpectedSeparator loadedDataFile with
                | Ok parsed -> setLoadedFile importedFile.Name loadedDataFile (Some parsed)
                | Error message ->
                    setLoadedFile importedFile.Name loadedDataFile None
                    onError message
            finally
                setLoading false
        }

        let onPickTextFiles: unit -> JS.Promise<Result<ImportedTextFile[], string>> =
            React.useCallback (
                fun () -> promise { return Error "File picker function not provided." }
                , [| widgetCtx |]
            )

        let pickFile () = promise {

            let! importResult = onPickTextFiles ()

            match importResult with
            | Error message when message <> "Cancelled" -> onError $"Failed to pick file: {message}"
            | Error _ -> ()
            | Ok importedFiles when importedFiles.Length = 0 -> ()
            | Ok importedFiles -> do! loadImportedFile importedFiles.[0]
        }

        let applySeparator () =
            match dataFile with
            | None -> ()
            | Some loadedDataFile ->
                if String.IsNullOrWhiteSpace separatorInput then
                    onError "Separator must not be empty."
                else
                    match parseDataFileBySeparator separatorInput loadedDataFile with
                    | Ok parsed ->
                        setParsedFile (Some parsed)
                        setSelectedTargets (fun _ -> Set.empty)
                    | Error message ->
                        setParsedFile None
                        onError message

        let toggleHeader () =
            match parsedFile with
            | None -> ()
            | Some currentParsedFile ->
                setParsedFile (Some(currentParsedFile.ToggleHeader()))
                setSelectedTargets (fun _ -> Set.empty)

        let toggleTarget (target: DataTarget) =
            setSelectedTargets (fun currentTargets ->
                if currentTargets.Contains target then
                    currentTargets.Remove target
                else
                    currentTargets.Add target
            )

        let submit () =
            if selectedTargets.IsEmpty then
                onError "Select at least one target in the preview table."
            else
                match dataFile, parsedFile with
                | Some loadedDataFile, Some currentParsedFile ->
                    let selectors =
                        selectorsFromTargets currentParsedFile.HeaderRow.IsSome selectedTargets

                    let input: AnnotationInput = {
                        Selectors = selectors
                        FileName = loadedDataFile.DataFileName
                        FileType = loadedDataFile.DataFileType
                        TargetColumn = targetColumn
                    }

                    setAnnotationInput input
                | _ -> onError "Load a file first."

        let previewSection =
            match parsedFile with
            | None ->
                Html.div [
                    prop.className
                        "swt:rounded-box swt:border swt:border-base-300 swt:flex swt:items-center swt:justify-center swt:text-sm swt:opacity-70 swt:min-h-24 swt:px-3 swt:py-4"
                    prop.text "Load a data file to preview selectable targets."
                ]
            | Some currentParsedFile ->
                let headerCount =
                    currentParsedFile.HeaderRow |> Option.map _.Length |> Option.defaultValue 0

                let bodyCount =
                    currentParsedFile.BodyRows
                    |> Array.fold (fun count row -> max count row.Length) 0

                let columnCount = max headerCount bodyCount

                Html.div [
                    prop.className "swt:overflow-auto swt:rounded-box swt:border swt:border-base-300 swt:max-h-[55vh]"
                    prop.children [
                        if currentParsedFile.BodyRows.Length = 0 || columnCount = 0 then
                            Html.div [
                                prop.className
                                    "swt:flex swt:items-center swt:justify-center swt:h-full swt:text-sm swt:opacity-70"
                                prop.text "Parsed file contains no data rows."
                            ]
                        else
                            DataAnnotatorWidget.Table(currentParsedFile, selectedTargets, columnCount, toggleTarget)
                    ]
                ]

        let selectedTargetCount = selectedTargets.Count

        let canSubmit =
            dataFile.IsSome && parsedFile.IsSome && (not loading) && selectedTargetCount > 0

        Html.div [
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
                DataAnnotatorWidget.FileControllerElements(pickFile, loading, selectedFileName, dataFile, reset)
                if dataFile.IsSome then
                    Html.div [
                        prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:items-center"
                        prop.children [
                            let hasCustomSeparator =
                                (DefaultSeparatorOptions
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

                                    for separator, label in DefaultSeparatorOptions do
                                        Html.option [ prop.value separator; prop.text label ]
                                ]
                            ]

                            Html.input [
                                prop.className "swt:input swt:input-sm swt:w-32"
                                prop.placeholder "separator"
                                prop.value separatorInput
                                prop.onChange setSeparatorInput
                                prop.onKeyDown (key.enter, fun _ -> applySeparator ())
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
                                    | Some currentParsedFile when currentParsedFile.HeaderRow.IsSome -> "Header: On"
                                    | _ -> "Header: Off"
                                )
                                prop.onClick (fun _ -> toggleHeader ())
                                prop.disabled parsedFile.IsNone
                            ]

                            Html.select [
                                prop.className "swt:select swt:select-sm"
                                prop.valueOrDefault (string targetColumn)
                                prop.onChange (fun (value: string) -> setTargetColumn (TargetColumn.fromString value))
                                prop.children [
                                    Html.option [
                                        prop.value (string TargetColumn.Autodetect)
                                        prop.text (string TargetColumn.Autodetect)
                                    ]
                                    Html.option [
                                        prop.value (string TargetColumn.Input)
                                        prop.text (string TargetColumn.Input)
                                    ]
                                    Html.option [
                                        prop.value (string TargetColumn.Output)
                                        prop.text (string TargetColumn.Output)
                                    ]
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