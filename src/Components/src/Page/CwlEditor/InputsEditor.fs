namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open Swate.Components.Page.CwlEditor.UiHelpers
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

[<AutoOpen>]
module private InputsEditorHelpers =

    let eventTargetValue (ev: Browser.Types.FocusEvent) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

[<Erase; Mangle(false)>]
type InputsEditor =

    [<ReactComponent>]
    static member InputsEditor
        (
            version: int,
            inputs: InputModel list,
            activeIndex: int option,
            setActiveIndex: int option -> unit,
            onRenameInput: InputId -> string -> unit,
            onSetInputType: InputId -> string option -> unit,
            onSetInputPrefix: InputId -> string -> unit,
            onSetInputPosition: InputId -> string -> unit,
            onSetInputOptional: InputId -> bool -> unit,
            onAddInput: unit -> unit,
            onRemoveInput: InputId -> unit,
            onMoveInputUp: InputId -> unit,
            onMoveInputDown: InputId -> unit,
            ?onInteract: unit -> unit
        ) : ReactElement =
        let inputEditor =
            match activeIndex |> Option.bind (fun index -> inputs |> List.tryItem index) with
            | Some input ->
                let index = inputs |> List.findIndex (fun item -> item.Id = input.Id)

                let binding =
                    input.InputBinding |> Option.defaultValue { Prefix = None; Position = None }

                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.h4 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text (sprintf "Input %s details" input.Name)
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Name" ]
                                DraftTextField.DraftTextField(
                                    string input.Id,
                                    input.Name,
                                    (fun nextName -> onRenameInput input.Id nextName),
                                    testId = sprintf "cwl-input-name-%d" index
                                )
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Type" ]
                                Html.select [
                                    prop.testId (sprintf "cwl-input-type-%d" index)
                                    prop.className "swt:select swt:select-sm swt:w-full"
                                    prop.value (input.CwlType |> Option.defaultValue "")
                                    prop.onChange (fun selectedType ->
                                        onSetInputType
                                            input.Id
                                            (if System.String.IsNullOrWhiteSpace selectedType then
                                                 None
                                             else
                                                 Some selectedType)
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
                                    prop.key (sprintf "input-prefix-%O" input.Id)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (binding.Prefix |> Option.defaultValue "")
                                    prop.placeholder "--input"
                                    prop.onBlur (fun ev -> onSetInputPrefix input.Id (eventTargetValue ev))
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Position" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-input-position-%d" index)
                                    prop.key (sprintf "input-position-%O" input.Id)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (binding.Position |> Option.map string |> Option.defaultValue "")
                                    prop.placeholder "1"
                                    prop.onBlur (fun ev -> onSetInputPosition input.Id (eventTargetValue ev))
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
                                    prop.isChecked input.Optional
                                    prop.onChange (onSetInputOptional input.Id)
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
                                        onInteract |> Option.iter (fun callback -> callback ())
                                        onAddInput ()
                                    )
                                ]
                                Html.button [
                                    prop.testId "cwl-input-remove"
                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                    prop.text "Remove"
                                    prop.disabled activeIndex.IsNone
                                    prop.onClick (fun _ ->
                                        match
                                            activeIndex |> Option.bind (fun index -> inputs |> List.tryItem index)
                                        with
                                        | Some input ->
                                            let nextIndex =
                                                match activeIndex with
                                                | Some _ when inputs.Length <= 1 -> None
                                                | Some index when index >= inputs.Length - 1 -> Some(inputs.Length - 2)
                                                | Some index -> Some(index + 1)
                                                | None -> None

                                            onInteract |> Option.iter (fun callback -> callback ())
                                            onRemoveInput input.Id
                                            setActiveIndex nextIndex
                                        | None -> ()
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
                Html.ul [
                    prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                    prop.children [
                        for index, input in inputs |> List.indexed do
                            Html.li [
                                prop.testId (sprintf "cwl-input-item-%d" index)
                                prop.key (string input.Id)
                                prop.className [
                                    if activeIndex = Some index then
                                        "swt:menu-active"
                                ]
                                prop.onClick (fun _ ->
                                    onInteract |> Option.iter (fun callback -> callback ())
                                    setActiveIndex (Some index)
                                )
                                prop.text (
                                    match input.CwlType with
                                    | Some cwlType -> sprintf "%s : %s" input.Name cwlType
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
                                match activeIndex |> Option.bind (fun index -> inputs |> List.tryItem index) with
                                | Some input ->
                                    onMoveInputUp input.Id
                                    setActiveIndex activeIndex
                                | None -> ()
                            )
                        ]
                        Html.button [
                            prop.testId "cwl-input-move-down"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move down"
                            prop.disabled (activeIndex.IsNone || activeIndex = Some(inputs.Length - 1))
                            prop.onClick (fun _ ->
                                match activeIndex |> Option.bind (fun index -> inputs |> List.tryItem index) with
                                | Some input ->
                                    onMoveInputDown input.Id
                                    setActiveIndex activeIndex
                                | None -> ()
                            )
                        ]
                    ]
                ]
                inputEditor
            ]
        ]
