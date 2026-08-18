namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type Header =

    [<ReactComponent>]
    static member Header
        (
            version: int,
            kindLabel: string,
            fileLabel: string,
            isDirty: bool,
            isSaving: bool,
            onPreview: unit -> unit,
            onSave: unit -> unit,
            onBackToStart: unit -> unit
        ) : ReactElement =
        Html.header [
            prop.className "swt:navbar swt:bg-base-200 swt:border-b swt:border-base-300 swt:gap-2"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:grow swt:min-w-0"
                    prop.children [
                        Html.h2 [ prop.text (sprintf "Editing %s" kindLabel) ]
                        Html.p [
                            prop.text (
                                sprintf "%s | version %d%s" fileLabel version (if isDirty then " (unsaved)" else "")
                            )
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-editor-preview"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Preview .cwl"
                            prop.onClick (fun _ -> onPreview ())
                        ]
                        Html.button [
                            prop.testId "cwl-editor-save"
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text (if isSaving then "Saving..." else "Save .cwl")
                            prop.disabled isSaving
                            prop.onClick (fun _ -> onSave ())
                        ]
                        Html.button [
                            prop.testId "cwl-editor-back-to-start"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Back to Start"
                            prop.onClick (fun _ -> onBackToStart ())
                        ]
                    ]
                ]
            ]
        ]
