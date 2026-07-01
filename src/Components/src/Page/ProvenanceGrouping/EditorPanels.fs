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

        let headerText =
            pending.Batch.Overwrites
            |> List.tryHead
            |> Option.map (fun w -> w.Header.Category.Name)
            |> Option.defaultValue "property"

        let valueText =
            pending.Batch.Overwrites
            |> List.tryHead
            |> Option.map (fun w -> Formatting.formatValue w.Value w.Unit)
            |> Option.defaultValue "new value"

        Html.div [
            prop.className "swt:alert swt:alert-warning swt:flex-wrap swt:items-start"
            if debug then
                prop.testId "provenance-overwrite-warning"
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--warning-20-regular swt:size-5"
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.strong [
                            prop.text (
                                if overwriteCount > 1 then
                                    $"Overwrite {overwriteCount} {headerText} values?"
                                else
                                    $"Overwrite {headerText} value?"
                            )
                        ]
                        Html.span [
                            prop.className "swt:text-sm"
                            prop.text
                                $"The selected targets already have {headerText}. Confirm to replace with {valueText} across {sideCount} side(s) using the existing edit path."
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-warning swt:btn-sm"
                            if debug then
                                prop.testId "provenance-confirm-overwrite"
                            prop.onPointerUp (fun _ -> onConfirm pending)
                            prop.onClick (fun _ -> onConfirm pending)
                            prop.text "Overwrite"
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
                        "swt:mx-4 swt:mt-4 swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
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
