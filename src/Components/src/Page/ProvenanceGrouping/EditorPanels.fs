namespace Swate.Components.Page.ProvenanceGrouping

open System
open System.Globalization
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.FolderedDraggableList
open Swate.Components.Composite.FolderedDraggableList.Types
open Swate.Components.JsBindings
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Page.ProvenanceGrouping.Types

/// Alert and detail panels rendered around the main grouping surface.
module EditorPanels =

    let errorAlert (error: string) =
        Html.div [
            prop.className "swt:alert swt:alert-error"
            prop.text error
        ]

    let assignmentBatchWarning debug (pending: PendingAssignmentBatch) onConfirm onCancel =
        let overwriteCount = pending.AffectedValueCount
        let sideCount = pending.AffectedSideCount
        let isFanOutApply = pending.Batch.Overwrites.IsEmpty

        let headers =
            [
                yield! pending.Batch.Overwrites |> List.map (fun w -> w.Header.Category.Name)
                yield! pending.Batch.Adds |> List.map (fun a -> a.Header.Category.Name)
            ]
            |> List.distinct

        let headerText = headers |> List.tryHead |> Option.defaultValue "property"

        let valueText =
            pending.Batch.Overwrites
            |> List.tryHead
            |> Option.map (fun w -> Formatting.formatValue w.Value w.Unit)
            |> Option.orElse (
                pending.Batch.Adds
                |> List.tryHead
                |> Option.map (fun a -> Formatting.formatValue a.Value a.Unit)
            )
            |> Option.defaultValue "new value"

        let heading =
            if isFanOutApply then
                $"Apply {headerText} value to {pending.AffectedGroupCount} selected groups?"
            else
                match headers with
                | _ :: _ :: _ -> $"Overwrite {overwriteCount} values across {headers.Length} properties?"
                | _ when overwriteCount > 1 -> $"Overwrite {overwriteCount} {headerText} values?"
                | _ -> $"Overwrite {headerText} value?"

        let body =
            if isFanOutApply then
                $"Adds {valueText} to {pending.AffectedEntityCount} entities across the selected groups."
            else
                match headers with
                | _ :: _ :: _ ->
                    let headerList = headers |> String.concat ", "
                    $"The selected targets already have values for {headerList}. Confirm to replace them across {sideCount} side(s)."
                | _ ->
                    $"The selected targets already have a {headerText} value. Confirm to replace it with {valueText} across {sideCount} side(s)."

        Html.div [
            prop.className [
                "swt:alert swt:flex-wrap swt:items-start"
                if isFanOutApply then
                    "swt:alert-info"
                else
                    "swt:alert-warning"
            ]
            if debug then
                if isFanOutApply then
                    prop.testId "provenance-apply-batch-prompt"
                else
                    prop.testId "provenance-overwrite-warning"
            prop.children [
                Html.i [
                    prop.className [
                        "swt:iconify swt:size-5"
                        if isFanOutApply then
                            "swt:fluent--info-20-regular"
                        else
                            "swt:fluent--warning-20-regular"
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.strong [ prop.text heading ]
                        Html.span [ prop.className "swt:text-sm"; prop.text body ]
                    ]
                ]
                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:btn-sm"
                                if isFanOutApply then
                                    "swt:btn-primary"
                                else
                                    "swt:btn-warning"
                            ]
                            if debug then
                                if isFanOutApply then
                                    prop.testId "provenance-confirm-apply"
                                else
                                    prop.testId "provenance-confirm-overwrite"
                            prop.onPointerUp (fun _ -> onConfirm pending)
                            prop.onClick (fun _ -> onConfirm pending)
                            prop.text (if isFanOutApply then "Apply" else "Overwrite")
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-ghost swt:btn-sm"
                            prop.onClick (fun _ -> onCancel ())
                            prop.text "Cancel"
                        ]
                    ]
                ]
            ]
        ]

    let memberResolutionPrompt debug (pending: PendingMemberResolution) onAllToAll onManual onCancel =
        let memberText count side =
            if count = 1 then
                $"{count} {side} member"
            else
                $"{count} {side} members"

        let inputMemberText = memberText pending.InputMemberCount "input"
        let outputMemberText = memberText pending.OutputMemberCount "output"

        Html.div [
            prop.className "swt:alert swt:alert-warning swt:flex-wrap swt:items-start"
            if debug then
                prop.testId "provenance-member-resolution-prompt"
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--text-paragraph-24-regular swt:size-5"
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.strong "Resolve member mismatch"
                        Html.span [
                            prop.className "swt:text-sm"
                            prop.text $"This connection has {inputMemberText} and {outputMemberText}."
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-warning swt:btn-sm"
                            prop.ariaLabel "Create all-to-all connections"
                            if debug then
                                prop.testId "provenance-member-resolution-all-to-all"
                            prop.onClick (fun _ -> onAllToAll pending)
                            prop.text "All-to-all"
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-outline swt:btn-sm"
                            prop.ariaLabel "Resolve manually"
                            if debug then
                                prop.testId "provenance-member-resolution-manual"
                            prop.onPointerUp (fun _ -> onManual pending)
                            prop.onClick (fun _ -> onManual pending)
                            prop.text "Resolve manually"
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-ghost swt:btn-sm"
                            prop.ariaLabel "Cancel member resolution"
                            if debug then
                                prop.testId "provenance-member-resolution-cancel"
                            prop.onClick (fun _ -> onCancel ())
                            prop.text "Cancel"
                        ]
                    ]
                ]
            ]
        ]

    let connectionDetails debug (connections: DisplayConnection list) detail =
        match detail with
        | Some(ProvenanceDetail.Connection connectionId) ->
            let resolved = connections |> List.tryFind (fun c -> c.Id = connectionId)

            match resolved with
            | Some conn ->
                Html.div [
                    prop.className
                        "swt:mx-4 swt:mt-4 swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3 swt:motion-pop-in"
                    if debug then
                        prop.testId "provenance-connection-details"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:grow swt:font-semibold swt:text-primary"
                                    prop.text $"Connection: {connectionId}"
                                ]
                            ]
                        ]
                        Html.p [
                            prop.className "swt:text-sm"
                            prop.text $"Source: {conn.SourceGroupId}"
                        ]
                        Html.p [
                            prop.className "swt:text-sm"
                            prop.text $"Target: {conn.TargetGroupId}"
                        ]
                        let connectionIds = conn.ConnectionIds |> String.concat ", "

                        Html.p [
                            prop.className "swt:text-sm"
                            prop.text $"Connection IDs: {connectionIds}"
                        ]
                    ]
                ]
            | None -> Html.none
        | _ -> Html.none
