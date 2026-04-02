namespace Swate.Components.Landing

open Fable.Core
open Feliz
open ARCtrl
open Swate.Components.Metadata

[<RequireQualifiedAccess>]
module SharedFields =

    [<ReactComponent>]
    let BoxedHelperField (title: string) (content: ReactElement) =
        Html.fieldSet [
            prop.className "swt:fieldset"
            prop.children [
                Html.legend [
                    prop.className "swt:fieldset-legend"
                    prop.text title
                ]
                Html.div [
                    prop.className "swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                    prop.children [ content ]
                ]
            ]
        ]

    [<ReactComponent>]
    let Main
        (
            draft: LandingDraft,
            setDraft: LandingDraft -> unit,
            onImportPersons: (unit -> JS.Promise<Person[]>) option
        ) =
        React.Fragment [
            FormComponents.TextInput(
                draft.Identifier,
                (fun value -> setDraft { draft with Identifier = value }),
                label = "Identifier (Optional)",
                placeholder = "Auto-generated if empty"
            )
            FormComponents.TextInput(
                draft.Title,
                (fun value -> setDraft { draft with Title = value }),
                label = "Title (Required)",
                placeholder = "Experiment title"
            )
            FormComponents.TextInput(
                draft.Description,
                (fun value -> setDraft { draft with Description = value }),
                label = "Description (Required)",
                isArea = true,
                placeholder = "Experiment description"
            )
            BoxedHelperField "Involved People" (
                FormComponents.PersonsInput(
                    draft.InvolvedPeople,
                    (fun persons -> setDraft { draft with InvolvedPeople = persons }),
                    ?onImportPersons = onImportPersons
                )
            )
            BoxedHelperField "Comments" (
                FormComponents.CommentsInput(
                    draft.Comments,
                    (fun comments -> setDraft { draft with Comments = comments })
                )
            )
            FormComponents.TextInput(
                draft.MainText,
                (fun value -> setDraft { draft with MainText = value }),
                label = "Main Text",
                isArea = true,
                placeholder = "Will be saved as protocol markdown"
            )
            Html.fieldSet [
                prop.className "swt:fieldset"
                prop.children [
                    Html.legend [
                        prop.className "swt:fieldset-legend"
                        prop.text "Files"
                    ]
                    Html.div [
                        prop.className "swt:border swt:border-dashed swt:border-base-content/30 swt:rounded-box swt:p-4 swt:bg-base-100"
                        prop.children [
                            Html.p [
                                prop.className "swt:opacity-70"
                                prop.text "File picker is temporarily unavailable here."
                            ]
                        ]
                    ]
                ]
            ]
        ]
