namespace Swate.Components

open System
open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type GitDiffViewer =

    [<ReactComponent>]
    static member Viewer
        (
            wordDiffText: string,
            previousContent: string,
            currentContent: string,
            ?previousTitle: string,
            ?currentTitle: string,
            ?testIdPrefix: string
        ) =
        let previousHeaderLabel, currentHeaderLabel, rows, previousLineCount, currentLineCount =
            React.useMemo (
                (fun () ->
                    let metadata = GitTextComparisonCore.Metadata.extractDiffMetadata wordDiffText

                    let previousHeaderLabel =
                        GitTextComparisonCore.Metadata.resolveHeaderLabel "Previous version" previousTitle metadata.PreviousPath

                    let currentHeaderLabel =
                        GitTextComparisonCore.Metadata.resolveHeaderLabel "Current version" currentTitle metadata.CurrentPath

                    let rows =
                        GitTextComparisonCore.WordDiff.buildRowsFromWordDiff wordDiffText previousContent currentContent

                    let previousLineCount = (GitTextComparisonCore.Text.splitContentToLines previousContent).Length
                    let currentLineCount = (GitTextComparisonCore.Text.splitContentToLines currentContent).Length

                    previousHeaderLabel, currentHeaderLabel, rows, previousLineCount, currentLineCount
                ),
                [| box wordDiffText; box previousContent; box currentContent; box previousTitle; box currentTitle |]
            )

        let rootTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-root")

        let previousHeaderTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-previous-header")

        let currentHeaderTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-current-header")

        let comparisonScrollTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-comparison-scroll")

        let changeBadgeText =
            if String.IsNullOrWhiteSpace wordDiffText then
                "No changes"
            else
                "Changed"

        GitComparisonView.PanelShell
            (React.Fragment [
                GitComparisonView.HeaderRow
                    (GitComparisonView.TitleStack
                        (Html.h3 [
                            prop.className "swt:text-sm swt:font-semibold"
                            prop.text "Git Diff"
                        ])
                        None
                        None)
                    (Html.span [
                        prop.className "swt:badge swt:badge-outline swt:badge-sm"
                        prop.text changeBadgeText
                    ])
                    (Some "swt:border-b swt:border-base-content/10 swt:bg-base-100")
                GitTextComparisonRendering.Rendering.ComparisonGrid(
                    rows,
                    ("Previous", (previousHeaderLabel, previousLineCount)),
                    ("Current", (currentHeaderLabel, currentLineCount)),
                    previousHeaderTestId,
                    currentHeaderTestId,
                    comparisonScrollTestId,
                    None,
                    None
                )
            ])
            rootTestId
            (Some "swt:flex swt:h-full swt:w-full swt:min-h-0 swt:min-w-0 swt:flex-col")
            None
