namespace Swate.Components.Page

open System
open Fable.Core
open Feliz

/// The change a diff shows, when the caller knows it. JavaScript callers pass
/// "added", "deleted" or "modified".
[<StringEnum; RequireQualifiedAccess>]
type GitDiffChangeKind =
    | Added
    | Deleted
    | Modified

[<Erase; Mangle(false)>]
type GitDiffViewer =

    [<ReactComponent(true)>]
    static member Viewer
        (
            wordDiffText: string,
            previousContent: string,
            currentContent: string,
            ?changeKind: GitDiffChangeKind,
            ?previousTitle: string,
            ?currentTitle: string,
            ?testIdPrefix: string
        ) =
        let previousHeaderLabel, currentHeaderLabel, rows, previousLineCount, currentLineCount, changeBadgeText =
            React.useMemo (
                (fun () ->
                    let metadata = GitTextComparisonCore.Metadata.extractDiffMetadata wordDiffText

                    let previousHeaderLabel =
                        GitTextComparisonCore.Metadata.resolveHeaderLabel
                            "Previous version"
                            previousTitle
                            metadata.PreviousPath

                    let currentHeaderLabel =
                        GitTextComparisonCore.Metadata.resolveHeaderLabel
                            "Current version"
                            currentTitle
                            metadata.CurrentPath

                    let rows =
                        GitTextComparisonCore.WordDiff.buildRowsFromWordDiff
                            wordDiffText
                            previousContent
                            currentContent
                        |> List.toArray

                    let previousLineCount =
                        (GitTextComparisonCore.Text.splitContentToLines previousContent).Length

                    let currentLineCount =
                        (GitTextComparisonCore.Text.splitContentToLines currentContent).Length

                    let diffLines =
                        wordDiffText
                        |> GitTextComparisonCore.Text.normalizeLineEndings
                        |> GitTextComparisonCore.Text.splitContentToLines

                    let isAddedFile =
                        diffLines
                        |> Array.exists (fun line ->
                            line.StartsWith("new file mode ", StringComparison.Ordinal)
                            || line.StartsWith("--- /dev/null", StringComparison.Ordinal)
                        )

                    let isDeletedFile =
                        diffLines
                        |> Array.exists (fun line ->
                            line.StartsWith("deleted file mode ", StringComparison.Ordinal)
                            || line.StartsWith("+++ /dev/null", StringComparison.Ordinal)
                        )

                    let inferredChangeBadgeText =
                        match isAddedFile, isDeletedFile, metadata.PreviousPath, metadata.CurrentPath with
                        | true, _, _, _ -> "Added"
                        | _, true, _, _ -> "Deleted"
                        | _, _, None, Some _ -> "Added"
                        | _, _, Some _, None -> "Deleted"
                        | _ when String.IsNullOrWhiteSpace wordDiffText -> "No changes"
                        | _ -> "Changed"

                    let changeBadgeText =
                        match changeKind with
                        | Some GitDiffChangeKind.Added -> "Added"
                        | Some GitDiffChangeKind.Deleted -> "Deleted"
                        | Some GitDiffChangeKind.Modified -> "Changed"
                        | None -> inferredChangeBadgeText

                    previousHeaderLabel, currentHeaderLabel, rows, previousLineCount, currentLineCount, changeBadgeText
                ),
                [|
                    box wordDiffText
                    box previousContent
                    box currentContent
                    box changeKind
                    box previousTitle
                    box currentTitle
                |]
            )

        let rootTestId = testIdPrefix |> Option.map (fun prefix -> prefix + "-root")

        let previousHeaderTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-previous-header")

        let currentHeaderTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-current-header")

        let comparisonScrollTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-comparison-scroll")

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
