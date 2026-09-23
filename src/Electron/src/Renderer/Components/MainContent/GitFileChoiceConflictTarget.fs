module Renderer.Components.MainContent.GitFileChoiceConflictTarget

open Fable.Core
open Feliz
open Renderer.Context.GitWorkflow
open Renderer.Types
open Swate.Electron.Shared.VersionControlTypes

[<ReactComponent>]
let Main (choiceData: VersionControlFileChoicePage) =
    let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()

    let isConfirmingCurrentPath =
        gitStateCtx.state.MergeResolutionPendingPath = Some choiceData.Path

    let isBusy = gitStateCtx.state.BusyOperation.IsSome

    let pickCandidate candidateId =
        if not isBusy then
            gitStateCtx.confirmMergeResolution {
                Path = choiceData.Path
                Handle = choiceData.Handle
                WorkspaceVersion = choiceData.WorkspaceVersion
                Resolution = ConflictResolutionDto.PickCandidate candidateId
            }

    Html.div [
        prop.testId "renderer-git-file-choice-page"
        prop.className "swt:flex swt:h-full swt:w-full swt:min-h-0 swt:items-center swt:justify-center swt:p-8"
        prop.children [
            Html.div [
                prop.className
                    "swt:w-full swt:max-w-2xl swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-100 swt:p-6 swt:shadow-sm"
                prop.children [
                    if isConfirmingCurrentPath then
                        Html.div [
                            prop.testId "renderer-git-file-choice-pending"
                            prop.className
                                "swt:mb-4 swt:border-b swt:border-base-content/10 swt:bg-base-200/70 swt:px-4 swt:py-2 swt:text-sm swt:text-base-content/70"
                            prop.text "Applying merge resolution..."
                        ]

                    Html.h2 [
                        prop.testId "renderer-git-file-choice-heading"
                        prop.className "swt:text-lg swt:font-semibold"
                        prop.text (
                            if choiceData.Mine.IsDeleted || choiceData.Online.IsDeleted then
                                "This file was changed on one side and deleted on the other"
                            else
                                "This file was changed here and online"
                        )
                    ]
                    Html.p [
                        prop.className "swt:mt-2 swt:text-sm swt:text-base-content/70"
                        prop.text "Swate can't show the differences for this file. Choose which version to keep."
                    ]
                    Html.div [
                        prop.className "swt:mt-5 swt:space-y-2 swt:text-sm"
                        prop.children [
                            match versionLine "Your version" false choiceData.Mine with
                            | Some line ->
                                Html.p [
                                    prop.testId "renderer-git-file-choice-mine-version"
                                    prop.text line
                                ]
                            | None -> Html.none
                            match versionLine "Online version" true choiceData.Online with
                            | Some line ->
                                Html.p [
                                    prop.testId "renderer-git-file-choice-online-version"
                                    prop.text line
                                ]
                            | None -> Html.none
                        ]
                    ]
                    Html.div [
                        prop.className "swt:mt-6 swt:grid swt:gap-4"
                        prop.children [
                            Html.div [
                                prop.className "swt:space-y-2"
                                prop.children [
                                    Html.button [
                                        prop.testId "renderer-git-file-choice-keep-mine"
                                        prop.type'.button
                                        prop.className "swt:btn swt:btn-primary"
                                        prop.disabled isBusy
                                        prop.text "Keep my version"
                                        prop.onClick (fun _ -> pickCandidate keepMineCandidateId)
                                    ]
                                    Html.p [
                                        prop.testId "renderer-git-file-choice-keep-mine-hint"
                                        prop.className "swt:text-xs swt:text-base-content/70"
                                        prop.text (
                                            if choiceData.Mine.IsDeleted then
                                                "You deleted this file. Keeping your version deletes it and discards the online changes to it."
                                            else
                                                "Keeps the file as it is on this computer. The online changes to this file are discarded."
                                        )
                                    ]
                                ]
                            ]
                            Html.div [
                                prop.className "swt:space-y-2"
                                prop.children [
                                    Html.button [
                                        prop.testId "renderer-git-file-choice-use-online"
                                        prop.type'.button
                                        prop.className "swt:btn swt:btn-primary"
                                        prop.disabled isBusy
                                        prop.text "Use online version"
                                        prop.onClick (fun _ -> pickCandidate useOnlineCandidateId)
                                    ]
                                    Html.p [
                                        prop.testId "renderer-git-file-choice-use-online-hint"
                                        prop.className "swt:text-xs swt:text-base-content/70"
                                        prop.text (
                                            if choiceData.Online.IsDeleted then
                                                "The online version deleted this file. Choosing it deletes the file here and discards your changes to it."
                                            else
                                                "Replaces your version of this file with the online one. Your changes to this file are discarded."
                                        )
                                    ]
                                ]
                            ]
                            Renderer.Components.Helper.GitMergeAbandonConfirmation.Main(
                                isBusy,
                                gitStateCtx.abandonMerge,
                                "renderer-git-file-choice"
                            )
                        ]
                    ]
                ]
            ]
        ]
    ]
