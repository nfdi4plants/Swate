namespace Swate.Components.Notes.Editor

open Feliz

[<RequireQualifiedAccess>]
module TargetSelector =

    [<ReactComponent>]
    let Main
        (
            selectedTarget: ExistingTargetRef option,
            setSelectedTarget: ExistingTargetRef option -> unit,
            availableTargets: ResizeArray<ExistingTargetRef>,
            isSubmitting: bool
        ) =
        let activeKind, setActiveKind =
            React.useState (selectedTarget |> Option.map _.Kind |> Option.defaultValue NotesTargetKind.Study)

        React.useEffect (
            (fun () ->
                selectedTarget
                |> Option.iter (fun target -> setActiveKind target.Kind)),
            [| box selectedTarget |]
        )

        let setKind kind =
            setActiveKind kind

            match selectedTarget with
            | Some target when target.Kind <> kind -> setSelectedTarget None
            | _ -> ()

        let filteredTargets =
            availableTargets
            |> Seq.filter (fun target -> target.Kind = activeKind)
            |> Seq.toArray

        let selectedValue =
            selectedTarget
            |> Option.filter (fun target -> target.Kind = activeKind)
            |> Option.map _.Name
            |> Option.defaultValue ""

        let kindButton (label: string) (kind: NotesTargetKind) =
            Html.button [
                prop.className [
                    "swt:btn swt:flex-1"
                    if activeKind = kind then
                        "swt:btn-primary"
                    else
                        "swt:btn-outline"
                ]
                prop.disabled isSubmitting
                prop.onClick (fun _ -> setKind kind)
                prop.text label
            ]

        Html.div [
            prop.className "swt:space-y-2"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:gap-3"
                    prop.children [
                        kindButton "Study" NotesTargetKind.Study
                        kindButton "Assay" NotesTargetKind.Assay
                    ]
                ]
                Html.select [
                    prop.testId "notes-existing-target-select"
                    prop.className "swt:select swt:select-bordered swt:w-full"
                    prop.disabled (isSubmitting || filteredTargets.Length = 0)
                    prop.valueOrDefault selectedValue
                    prop.onChange (fun (value: string) ->
                        filteredTargets
                        |> Array.tryFind (fun target -> target.Name = value)
                        |> setSelectedTarget)
                    prop.children [
                        Html.option [
                            prop.value ""
                            prop.text (
                                if filteredTargets.Length = 0 then
                                    "No targets available"
                                else
                                    "Select target"
                            )
                        ]
                        for target in filteredTargets do
                            Html.option [
                                prop.key $"{target.Kind}-{target.Name}"
                                prop.value target.Name
                                prop.text target.Name
                            ]
                    ]
                ]
            ]
        ]
