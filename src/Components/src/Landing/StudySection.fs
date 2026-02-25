namespace Swate.Components.Landing

open Feliz
open Swate.Components.Metadata

[<RequireQualifiedAccess>]
module StudySection =

    let private boxedHelperContent (content: ReactElement) =
        Html.div [
            prop.className "swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
            prop.children [ content ]
        ]

    [<ReactComponent>]
    let Main(draft: LandingDraft, setDraft: LandingDraft -> unit) =
        Html.div [
            prop.className "swt:space-y-3"
            prop.children [
                boxedHelperContent (
                    FormComponents.PublicationsInput(
                        draft.Publications,
                        (fun pubs -> setDraft { draft with Publications = pubs }),
                        label = "Publications"
                    )
                )
                FormComponents.DateTimeInput(
                    draft.SubmissionDate,
                    (fun dateText -> setDraft { draft with SubmissionDate = dateText }),
                    label = "Submission Date"
                )
                FormComponents.DateTimeInput(
                    draft.PublicReleaseDate,
                    (fun dateText -> setDraft { draft with PublicReleaseDate = dateText }),
                    label = "Public Release Date"
                )
                boxedHelperContent (
                    FormComponents.OntologyAnnotationsInput(
                        draft.StudyDesignDescriptors,
                        (fun descriptors -> setDraft { draft with StudyDesignDescriptors = descriptors }),
                        label = "Study Design Descriptors"
                    )
                )
            ]
        ]
