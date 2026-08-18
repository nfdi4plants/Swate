namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open ARCtrl.CWL
open Swate.Components.Page.CwlEditor.UiHelpers
open Swate.Components.Shared.Cwl.CommandLineToolMutations

[<AutoOpen>]
module private OutputsEditorHelpers =

    let eventTargetValue (ev: Browser.Types.Event) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

[<Erase; Mangle(false)>]
type OutputsEditor =

    [<ReactComponent>]
    static member OutputsEditor
        (
            version: int,
            outputs: ResizeArray<CWLOutput>,
            activeIndex: int option,
            setActiveIndex: int option -> unit,
            commitMutation: (unit -> unit) -> unit,
            addOutput: unit -> int,
            ?onInteract: unit -> unit
        ) : ReactElement =
        let outputEditor =
            match activeIndex with
            | Some index ->
                let output = outputs.[index]
                let binding = output.OutputBinding |> Option.defaultValue (OutputBinding.create ())

                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.h4 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text (sprintf "Output %d details" (index + 1))
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Name" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-output-name-%d" index)
                                    prop.key (sprintf "output-name-%d" index)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue output.Name
                                    prop.onBlur (fun (ev: Browser.Types.FocusEvent) ->
                                        let value = eventTargetValue ev
                                        commitMutation (fun () -> renameOutputAt outputs index value)
                                    )
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Type" ]
                                Html.select [
                                    prop.testId (sprintf "cwl-output-type-%d" index)
                                    prop.className "swt:select swt:select-sm swt:w-full"
                                    prop.value (cwlTypeToKey output.Type_)
                                    prop.onChange (fun selectedType ->
                                        commitMutation (fun () ->
                                            setOutputTypeAt outputs index (cwlTypeFromKey selectedType)
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
                                Html.span [ prop.text "Glob" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-output-glob-%d" index)
                                    prop.key (sprintf "output-glob-%d" index)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (binding.Glob |> Option.defaultValue "")
                                    prop.placeholder "*.txt"
                                    prop.onBlur (fun (ev: Browser.Types.FocusEvent) ->
                                        let value = eventTargetValue ev
                                        commitMutation (fun () -> setOutputGlobAt outputs index value)
                                    )
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
                                        commitMutation (fun () ->
                                            let nextIndex = addOutput ()
                                            setActiveIndex (Some nextIndex)
                                        )
                                    )
                                ]
                                Html.button [
                                    prop.testId "cwl-output-remove"
                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                    prop.text "Remove"
                                    prop.disabled activeIndex.IsNone
                                    prop.onClick (fun _ ->
                                        commitMutation (fun () ->
                                            let nextIndex = removeOutput activeIndex outputs
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
                        for index, output in outputs |> Seq.indexed do
                            Html.li [
                                prop.testId (sprintf "cwl-output-item-%d" index)
                                prop.key output.Name
                                prop.className [
                                    if activeIndex = Some index then
                                        "swt:menu-active"
                                ]
                                prop.onClick (fun _ -> setActiveIndex (Some index))
                                prop.text (
                                    match output.Type_ with
                                    | Some cwlType -> sprintf "%s : %s" output.Name (cwlTypeToKey (Some cwlType))
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
                                commitMutation (fun () ->
                                    let nextIndex = moveOutputUp activeIndex outputs
                                    setActiveIndex nextIndex
                                )
                            )
                        ]
                        Html.button [
                            prop.testId "cwl-output-move-down"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move down"
                            prop.disabled (activeIndex.IsNone || activeIndex = Some(outputs.Count - 1))
                            prop.onClick (fun _ ->
                                commitMutation (fun () ->
                                    let nextIndex = moveOutputDown activeIndex outputs
                                    setActiveIndex nextIndex
                                )
                            )
                        ]
                    ]
                ]
                outputEditor
            ]
        ]
