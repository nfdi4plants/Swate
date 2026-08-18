namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open ARCtrl.CWL
open Swate.Components.Page.CwlEditor.UiHelpers
open Swate.Components.Shared.Cwl.CommandLineToolMutations

[<AutoOpen>]
module private InputsEditorHelpers =

    let eventTargetValue (ev: Browser.Types.Event) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

[<Erase; Mangle(false)>]
type InputsEditor =

    [<ReactComponent>]
    static member InputsEditor
        (
            version: int,
            inputs: ResizeArray<CWLInput>,
            activeIndex: int option,
            setActiveIndex: int option -> unit,
            commitMutation: (unit -> unit) -> unit,
            addInput: unit -> int,
            ?onInteract: unit -> unit
        ) : ReactElement =
        let inputEditor =
            match activeIndex with
            | Some index ->
                let input = inputs.[index]
                let binding = input.InputBinding |> Option.defaultValue (InputBinding.create ())

                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.h4 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text (sprintf "Input %d details" (index + 1))
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Name" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-input-name-%d" index)
                                    prop.key (sprintf "input-name-%d" index)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue input.Name
                                    prop.onBlur (fun (ev: Browser.Types.FocusEvent) ->
                                        let value = eventTargetValue ev
                                        commitMutation (fun () -> renameInputAt inputs index value)
                                    )
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Type" ]
                                Html.select [
                                    prop.testId (sprintf "cwl-input-type-%d" index)
                                    prop.className "swt:select swt:select-sm swt:w-full"
                                    prop.value (cwlTypeToKey input.Type_)
                                    prop.onChange (fun selectedType ->
                                        commitMutation (fun () ->
                                            setInputTypeAt inputs index (cwlTypeFromKey selectedType)
                                        )
                                    )
                                    prop.children [
                                        for value, label in cwlTypeSelectOptions do
                                            Html.option [ prop.key value; prop.value value; prop.text label ]
                                    ]
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Prefix" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-input-prefix-%d" index)
                                    prop.key (sprintf "input-prefix-%d" index)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (binding.Prefix |> Option.defaultValue "")
                                    prop.placeholder "--input"
                                    prop.onBlur (fun (ev: Browser.Types.FocusEvent) ->
                                        let value = eventTargetValue ev
                                        commitMutation (fun () -> setInputPrefixAt inputs index value)
                                    )
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Position" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-input-position-%d" index)
                                    prop.key (sprintf "input-position-%d" index)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (binding.Position |> Option.map string |> Option.defaultValue "")
                                    prop.placeholder "1"
                                    prop.onBlur (fun (ev: Browser.Types.FocusEvent) ->
                                        let value = eventTargetValue ev
                                        commitMutation (fun () -> setInputPositionAt inputs index value)
                                    )
                                ]
                            ]
                        ]
                        Html.label [
                            prop.testId (sprintf "cwl-input-optional-%d" index)
                            prop.className "swt:label swt:cursor-pointer swt:justify-start swt:gap-2"
                            prop.children [
                                Html.input [
                                    prop.type'.checkbox
                                    prop.className "swt:checkbox swt:checkbox-sm"
                                    prop.isChecked (input.Optional |> Option.defaultValue false)
                                    prop.onChange (fun isChecked ->
                                        commitMutation (fun () -> setInputOptionalAt inputs index isChecked)
                                    )
                                ]
                                Html.span [ prop.text "Optional input" ]
                            ]
                        ]
                    ]
                ]
            | None ->
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text "Select an input to edit details."
                ]

        Html.section [
            prop.testId "cwl-inputs-editor"
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text "Inputs"
                        ]
                        Html.div [
                            prop.className "swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.testId "cwl-input-add"
                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                    prop.text "Add"
                                    prop.onClick (fun _ ->
                                        commitMutation (fun () ->
                                            let nextIndex = addInput ()
                                            setActiveIndex (Some nextIndex)
                                        )
                                    )
                                ]
                                Html.button [
                                    prop.testId "cwl-input-remove"
                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                    prop.text "Remove"
                                    prop.disabled activeIndex.IsNone
                                    prop.onClick (fun _ ->
                                        commitMutation (fun () ->
                                            let nextIndex = removeInput activeIndex inputs
                                            setActiveIndex nextIndex
                                        )
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
                Html.ul [
                    prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                    prop.children [
                        for index, input in inputs |> Seq.indexed do
                            Html.li [
                                prop.testId (sprintf "cwl-input-item-%d" index)
                                prop.key input.Name
                                prop.className [
                                    if activeIndex = Some index then
                                        "swt:menu-active"
                                ]
                                prop.onClick (fun _ -> setActiveIndex (Some index))
                                prop.text (
                                    match input.Type_ with
                                    | Some cwlType -> sprintf "%s : %s" input.Name (cwlTypeToKey (Some cwlType))
                                    | None -> sprintf "%s : (unset)" input.Name
                                )
                            ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-input-move-up"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move up"
                            prop.disabled (activeIndex.IsNone || activeIndex = Some 0)
                            prop.onClick (fun _ ->
                                commitMutation (fun () ->
                                    let nextIndex = moveInputUp activeIndex inputs
                                    setActiveIndex nextIndex
                                )
                            )
                        ]
                        Html.button [
                            prop.testId "cwl-input-move-down"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move down"
                            prop.disabled (activeIndex.IsNone || activeIndex = Some(inputs.Count - 1))
                            prop.onClick (fun _ ->
                                commitMutation (fun () ->
                                    let nextIndex = moveInputDown activeIndex inputs
                                    setActiveIndex nextIndex
                                )
                            )
                        ]
                    ]
                ]
                inputEditor
            ]
        ]
