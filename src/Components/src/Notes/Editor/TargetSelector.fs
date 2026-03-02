namespace Swate.Components.Notes.Editor

open System
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
        let studyTargets =
            availableTargets
            |> Seq.filter (fun target -> target.Kind = NotesTargetKind.Study)
            |> Seq.sortBy _.Name
            |> Seq.toArray

        let assayTargets =
            availableTargets
            |> Seq.filter (fun target -> target.Kind = NotesTargetKind.Assay)
            |> Seq.sortBy _.Name
            |> Seq.toArray

        let hasTargets = studyTargets.Length > 0 || assayTargets.Length > 0

        let selectedValue =
            selectedTarget
            |> Option.map _.Name
            |> Option.defaultValue ""

        Html.div [
            prop.className "swt:space-y-2"
            prop.children [
                Html.select [
                    prop.testId "notes-existing-target-select"
                    prop.className "swt:select swt:select-bordered swt:w-full"
                    prop.disabled (isSubmitting || not hasTargets)
                    prop.valueOrDefault selectedValue
                    prop.onChange (fun (value: string) ->
                        if String.IsNullOrWhiteSpace value then
                            setSelectedTarget None
                        else
                            availableTargets
                            |> Seq.toArray
                            |> Array.tryFind (fun target -> target.Name = value)
                            |> setSelectedTarget)
                    prop.children [
                        Html.option [
                            prop.value ""
                            prop.text (
                                if not hasTargets then
                                    "No targets available"
                                else
                                    "Select Study or Assay target"
                            )
                        ]
                        if studyTargets.Length > 0 then
                            Html.optgroup [
                                prop.label "Study"
                                prop.children [
                                    for target in studyTargets do
                                        Html.option [
                                            prop.key $"{target.Kind}-{target.Name}"
                                            prop.value target.Name
                                            prop.text target.Name
                                        ]
                                ]
                            ]
                        if assayTargets.Length > 0 then
                            Html.optgroup [
                                prop.label "Assay"
                                prop.children [
                                    for target in assayTargets do
                                        Html.option [
                                            prop.key $"{target.Kind}-{target.Name}"
                                            prop.value target.Name
                                            prop.text target.Name
                                        ]
                                ]
                            ]
                    ]
                ]
            ]
        ]
