namespace Swate.Components.Composite.Notes.Editor

open Feliz

[<RequireQualifiedAccess>]
module Actions =

    [<ReactComponent>]
    let Main
        (
            isSubmitting: bool,
            canSubmitDraft: bool,
            onOpenExistingTargetSelector: unit -> unit,
            onCreateNew: unit -> unit,
            error: string option
        ) =
        let isDisabled = isSubmitting || not canSubmitDraft

        Html.div [
            prop.className "swt:space-y-2"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-3"
                    prop.children [
                        Html.button [
                            prop.testId "notes-add-existing-button"
                            prop.className [
                                "swt:btn swt:btn-secondary"
                                if isDisabled then
                                    "swt:btn-disabled"
                            ]
                            prop.disabled isDisabled
                            prop.onClick (fun _ ->
                                if not isDisabled then
                                    onOpenExistingTargetSelector ()
                            )
                            prop.text "Add to existing Assay/Study"
                        ]
                        Html.button [
                            prop.testId "notes-create-new-button"
                            prop.className [
                                "swt:btn swt:btn-primary"
                                if isDisabled then
                                    "swt:btn-disabled"
                            ]
                            prop.disabled isDisabled
                            prop.onClick (fun _ ->
                                if not isDisabled then
                                    onCreateNew ()
                            )
                            prop.text "Create new note"
                        ]
                    ]
                ]
                match error with
                | Some message ->
                    Html.span [
                        prop.testId "notes-error-text"
                        prop.className "swt:text-error"
                        prop.text message
                    ]
                | None -> Html.none
            ]
        ]
