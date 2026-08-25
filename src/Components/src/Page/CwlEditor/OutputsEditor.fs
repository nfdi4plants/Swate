namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open Swate.Components.Page.CwlEditor.UiHelpers
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

[<AutoOpen>]
module private OutputsEditorHelpers =

    let eventTargetValue (ev: Browser.Types.FocusEvent) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

[<Erase; Mangle(false)>]
type OutputsEditor =

    [<ReactComponent>]
    static member OutputsEditor
        (
            version: int,
            outputs: OutputModel list,
            activeIndex: int option,
            setActiveIndex: int option -> unit,
            onRenameOutput: OutputId -> string -> unit,
            onSetOutputType: OutputId -> string option -> unit,
            onSetOutputGlob: OutputId -> string -> unit,
            onAddOutput: unit -> unit,
            onRemoveOutput: OutputId -> unit,
            onMoveOutputUp: OutputId -> unit,
            onMoveOutputDown: OutputId -> unit,
            ?onInteract: unit -> unit
        ) : ReactElement =
        let outputEditor =
            match activeIndex |> Option.bind (fun index -> outputs |> List.tryItem index) with
            | Some output ->
                let index = outputs |> List.findIndex (fun item -> item.Id = output.Id)
                let binding = output.OutputBinding |> Option.defaultValue { Glob = None }

                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.h4 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text (sprintf "Output %s details" output.Name)
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Name" ]
                                DraftTextField.DraftTextField(
                                    string output.Id,
                                    output.Name,
                                    (fun nextName -> onRenameOutput output.Id nextName),
                                    testId = sprintf "cwl-output-name-%d" index
                                )
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Type" ]
                                Html.select [
                                    prop.testId (sprintf "cwl-output-type-%d" index)
                                    prop.className "swt:select swt:select-sm swt:w-full"
                                    prop.value (output.CwlType |> Option.defaultValue "")
                                    prop.onChange (fun selectedType ->
                                        onSetOutputType
                                            output.Id
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
                                Html.span [ prop.text "Glob" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-output-glob-%d" index)
                                    prop.key (sprintf "output-glob-%O" output.Id)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (binding.Glob |> Option.defaultValue "")
                                    prop.placeholder "*.txt"
                                    prop.onBlur (fun ev -> onSetOutputGlob output.Id (eventTargetValue ev))
                                ]
                            ]
                        ]
                    ]
                ]
            | None ->
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text "Select an output to edit details."
                ]

        Html.section [
            prop.testId "cwl-outputs-editor"
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text "Outputs"
                        ]
                        Html.div [
                            prop.className "swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.testId "cwl-output-add"
                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                    prop.text "Add"
                                    prop.onClick (fun _ ->
                                        onInteract |> Option.iter (fun callback -> callback ())
                                        onAddOutput ()
                                    )
                                ]
                                Html.button [
                                    prop.testId "cwl-output-remove"
                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                    prop.text "Remove"
                                    prop.disabled activeIndex.IsNone
                                    prop.onClick (fun _ ->
                                        match
                                            activeIndex |> Option.bind (fun index -> outputs |> List.tryItem index)
                                        with
                                        | Some output ->
                                            let nextIndex =
                                                match activeIndex with
                                                | Some _ when outputs.Length <= 1 -> None
                                                | Some index when index >= outputs.Length - 1 ->
                                                    Some(outputs.Length - 2)
                                                | Some index -> Some(index + 1)
                                                | None -> None

                                            onInteract |> Option.iter (fun callback -> callback ())
                                            onRemoveOutput output.Id
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
                        for index, output in outputs |> List.indexed do
                            Html.li [
                                prop.testId (sprintf "cwl-output-item-%d" index)
                                prop.key (string output.Id)
                                prop.className [
                                    if activeIndex = Some index then
                                        "swt:menu-active"
                                ]
                                prop.onClick (fun _ ->
                                    onInteract |> Option.iter (fun callback -> callback ())
                                    setActiveIndex (Some index)
                                )
                                prop.text (
                                    match output.CwlType with
                                    | Some cwlType -> sprintf "%s : %s" output.Name cwlType
                                    | None -> sprintf "%s : (unset)" output.Name
                                )
                            ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-output-move-up"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move up"
                            prop.disabled (activeIndex.IsNone || activeIndex = Some 0)
                            prop.onClick (fun _ ->
                                match activeIndex |> Option.bind (fun index -> outputs |> List.tryItem index) with
                                | Some output ->
                                    onMoveOutputUp output.Id
                                    setActiveIndex activeIndex
                                | None -> ()
                            )
                        ]
                        Html.button [
                            prop.testId "cwl-output-move-down"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move down"
                            prop.disabled (activeIndex.IsNone || activeIndex = Some(outputs.Length - 1))
                            prop.onClick (fun _ ->
                                match activeIndex |> Option.bind (fun index -> outputs |> List.tryItem index) with
                                | Some output ->
                                    onMoveOutputDown output.Id
                                    setActiveIndex activeIndex
                                | None -> ()
                            )
                        ]
                    ]
                ]
                outputEditor
            ]
        ]
