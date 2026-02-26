namespace Swate.Components.Notes.Editor

open Feliz

[<RequireQualifiedAccess>]
module Actions =

    [<ReactComponent>]
    let Main
        (
            isSubmitting: bool,
            showExistingTargetSelector: bool,
            onToggleExistingTargetSelector: unit -> unit,
            onCreateNew: unit -> unit,
            error: string option
        ) =
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
                                if isSubmitting then
                                    "swt:btn-disabled"
                            ]
                            prop.disabled isSubmitting
                            prop.onClick (fun _ -> onToggleExistingTargetSelector ())
                            prop.text (
                                if showExistingTargetSelector then
                                    "Close existing Assay/Study"
                                else
                                    "Add to existing Assay/Study"
                            )
                        ]
                        Html.button [
                            prop.testId "notes-create-new-button"
                            prop.className [
                                "swt:btn swt:btn-primary"
                                if isSubmitting then
                                    "swt:btn-disabled"
                            ]
                            prop.disabled isSubmitting
                            prop.onClick (fun _ -> onCreateNew ())
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
