namespace Swate.Components

open Feliz

module internal GitTextComparisonRendering =

    open GitTextComparisonCore

    module Rendering =

        type ComparisonSideStyle = {
            ChangedLineNumberClass: string
            ChangedLineClass: string
            ChangedSegmentClass: string
        }

        type ComparisonTheme = {
            LeftChanged: ComparisonSideStyle
            RightChanged: ComparisonSideStyle
        }

        let DiffTheme = {
            LeftChanged = {
                ChangedLineNumberClass = "swt:text-base-content/60 swt:bg-error/8"
                ChangedLineClass = "swt:bg-error/12"
                ChangedSegmentClass = "swt:bg-error/22"
            }
            RightChanged = {
                ChangedLineNumberClass = "swt:text-base-content/60 swt:bg-success/8"
                ChangedLineClass = "swt:bg-success/12"
                ChangedSegmentClass = "swt:bg-success/22"
            }
        }

        let ChoiceTheme = {
            LeftChanged = {
                ChangedLineNumberClass = "swt:text-base-content/60 swt:bg-info/10"
                ChangedLineClass = "swt:bg-info/12"
                ChangedSegmentClass = "swt:bg-info/22"
            }
            RightChanged = {
                ChangedLineNumberClass = "swt:text-base-content/60 swt:bg-secondary/12"
                ChangedLineClass = "swt:bg-secondary/12"
                ChangedSegmentClass = "swt:bg-secondary/20"
            }
        }

        [<ReactComponent>]
        let private ComparisonSegments (segments: InlineSegment list, changedSegmentClass: string) =
            if List.isEmpty segments then
                Html.span [ prop.text " " ]
            else
                segments
                |> List.mapi (fun index segment ->
                    Html.span [
                        prop.key $"segment-{index}-{segment.Kind}"

                        match segment.Kind with
                        | LineChangeKind.Neutral -> ()
                        | _ -> prop.className changedSegmentClass

                        prop.text segment.Text
                    ]
                )
                |> React.Fragment

        [<ReactComponent>]
        let private ComparisonSide (side: SideLine, changedStyle: ComparisonSideStyle) =
            let lineNumberClassName =
                match side.Kind with
                | LineChangeKind.Neutral -> "swt:text-base-content/45 swt:bg-base-100"
                | _ -> changedStyle.ChangedLineNumberClass

            let lineClassName =
                match side.Kind with
                | LineChangeKind.Neutral -> "swt:bg-base-100"
                | _ -> changedStyle.ChangedLineClass

            Html.div [
                prop.className "swt:grid swt:grid-cols-[3.5rem_minmax(0,1fr)] swt:min-w-0"
                prop.children [
                    Html.div [
                        prop.className [
                            "swt:px-3 swt:py-1 swt:text-right swt:text-xs swt:leading-5 swt:select-none swt:border-r swt:border-base-content/10"
                            lineNumberClassName
                        ]
                        prop.text (side.Number |> Option.map string |> Option.defaultValue "")
                    ]
                    Html.div [
                        prop.className [
                            "swt:px-3 swt:py-1 swt:min-w-0 swt:font-mono swt:text-xs swt:leading-5"
                            lineClassName
                        ]
                        prop.style [
                            style.whitespace.pre
                            style.overflowWrap.anywhere
                        ]
                        prop.children [ ComparisonSegments(side.Segments, changedStyle.ChangedSegmentClass) ]
                    ]
                ]
            ]

        [<ReactComponent>]
        let private ComparisonHeader (sideLabel: string, title: string, lineCount: int, testId: string option) =
            Html.div [
                if testId.IsSome then
                    prop.testId testId.Value
                prop.className "swt:flex swt:items-start swt:justify-between swt:gap-3 swt:px-4 swt:py-3 swt:bg-base-200 swt:border-b swt:border-base-content/10"
                prop.children [
                    Html.div [
                        prop.className "swt:min-w-0 swt:flex swt:flex-col swt:gap-0.5"
                        prop.children [
                            Html.span [
                                prop.className "swt:text-[11px] swt:uppercase swt:tracking-wide swt:text-base-content/60"
                                prop.text sideLabel
                            ]
                            Html.span [
                                prop.className "swt:truncate swt:text-sm swt:font-semibold"
                                prop.text title
                            ]
                        ]
                    ]
                    Html.span [
                        prop.className "swt:badge swt:badge-ghost swt:badge-sm swt:shrink-0"
                        prop.text $"{lineCount} lines"
                    ]
                ]
            ]

        [<ReactMemoComponent(AreEqualFn.FsEquals)>]
        let ComparisonGrid
            (
                rows: DiffRow list,
                leftHeader: string * (string * int),
                rightHeader: string * (string * int),
                leftHeaderTestId: string option,
                rightHeaderTestId: string option,
                scrollTestId: string option,
                maxHeightPx: int option,
                theme: ComparisonTheme option
            ) =
            let rows = if List.isEmpty rows then [ Rows.emptyRow () ] else rows
            let maxHeightPx = defaultArg maxHeightPx 640
            let theme = defaultArg theme DiffTheme

            Html.div [
                if scrollTestId.IsSome then
                    prop.testId scrollTestId.Value
                prop.className "swt:overflow-auto swt:scrollbar-fade"
                prop.style [ style.maxHeight maxHeightPx ]
                prop.children [
                    Html.div [
                        prop.className "swt:min-w-[58rem]"
                        prop.children [
                            Html.div [
                                prop.className "swt:sticky swt:top-0 swt:z-10 swt:grid swt:grid-cols-2 swt:divide-x swt:divide-base-content/10"
                                prop.children [
                                    ComparisonHeader(fst leftHeader, (snd leftHeader |> fst), (snd leftHeader |> snd), leftHeaderTestId)
                                    ComparisonHeader(fst rightHeader, (snd rightHeader |> fst), (snd rightHeader |> snd), rightHeaderTestId)
                                ]
                            ]
                            Html.div [
                                prop.children (
                                    rows
                                    |> List.mapi (fun index row ->
                                        Html.div [
                                            prop.key $"comparison-row-{index}"
                                            prop.className "swt:grid swt:grid-cols-2 swt:divide-x swt:divide-base-content/10"
                                            prop.children [
                                                ComparisonSide(row.Left, theme.LeftChanged)
                                                ComparisonSide(row.Right, theme.RightChanged)
                                            ]
                                        ]
                                    )
                                )
                            ]
                        ]
                    ]
                ]
            ]
