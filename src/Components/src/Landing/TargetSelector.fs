namespace Swate.Components.Landing

open Feliz

[<RequireQualifiedAccess>]
module TargetSelector =

    [<ReactComponent>]
    let Main
        (
            selectedTarget: LandingTarget option,
            setSelectedTarget: LandingTarget -> unit,
            isSubmitting: bool
        ) =
        let targetSelectButton (label: string) (target: LandingTarget) =
            Html.button [
                prop.className [
                    "swt:btn swt:flex-1"
                    if selectedTarget = Some target then
                        "swt:btn-primary"
                    else
                        "swt:btn-outline"
                ]
                prop.disabled isSubmitting
                prop.onClick (fun _ -> setSelectedTarget target)
                prop.text label
            ]

        Html.div [
            prop.className "swt:flex swt:gap-3"
            prop.children [
                targetSelectButton "Study" LandingTarget.Study
                targetSelectButton "Assay" LandingTarget.Assay
            ]
        ]
