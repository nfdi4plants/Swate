namespace Swate.Components.Notes.Editor

open Feliz
open Swate.Components
open Swate.Components.Metadata
open Swate.Components.MarkdownText

[<RequireQualifiedAccess>]
module NoteFormFields =

    [<ReactComponent>]
    let Main (draft: NotesDraft, setDraft: NotesDraft -> unit) =
        let dateInputValue =
            draft.DateCreated
            |> Option.map (fun dateValue -> dateValue.ToString("yyyy-MM-dd"))
            |> Option.defaultValue ""

        React.Fragment [
            Html.div [
                prop.testId "notes-title-field"
                prop.className "swt:fieldset swt:grow"
                prop.children [
                    Generic.FieldTitle "Title (Required)"
                    Html.input [
                        prop.className "swt:input swt:input-bordered swt:w-full"
                        prop.type'.text
                        prop.valueOrDefault draft.Title
                        prop.placeholder "Note title"
                        prop.onChange (fun value -> setDraft { draft with Title = value })
                    ]
                ]
            ]
            Html.div [
                prop.testId "notes-date-field"
                prop.className "swt:fieldset swt:w-full swt:max-w-[9rem]"
                prop.children [
                    Generic.FieldTitle "Date Created (Required)"
                    Html.input [
                        prop.className "swt:input swt:input-bordered swt:w-full"
                        prop.type'.date
                        prop.valueOrDefault dateInputValue
                        prop.onChange (fun value ->
                            let parsedDate = Validation.tryParseDateCreated value
                            setDraft { draft with DateCreated = parsedDate })
                    ]
                ]
            ]
            Html.div [
                prop.testId "notes-tags-field"
                prop.children [
                    FormComponents.OntologyAnnotationsInput(
                        draft.Tags,
                        (fun tags -> setDraft { draft with Tags = tags }),
                        label = "Tags (Optional)"
                    )
                ]
            ]
            Html.div [
                prop.testId "notes-main-text-field"
                prop.children [
                    TextInputWithMarkdown.TextInputWithMarkdown(
                        draft.MainText,
                        (fun value -> setDraft { draft with MainText = value }),
                        label = "Main Text",
                        placeholder = "Write note markdown...",
                        height = 360
                    )
                ]
            ]
        ]
