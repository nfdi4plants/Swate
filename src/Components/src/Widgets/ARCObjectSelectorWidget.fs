namespace Swate.Components

open ARCtrl
open Feliz
open Fable.Core

module private ARCObjectSelectorWidgetHelper =

    let buttonIcon (target: ARCObjectTarget) =
        match target with
        | ARCObjectTarget.Metadata ->
            Html.span [ prop.className "swt:i-fluent--info-24-regular swt:text-lg" ]
        | ARCObjectTarget.TableView _ ->
            Html.span [ prop.className "swt:i-fluent--table-24-regular swt:text-lg" ]
        | ARCObjectTarget.DataMap ->
            Html.span [ prop.className "swt:i-fluent--database-24-regular swt:text-lg" ]

    let targetKey (target: ARCObjectTarget) =
        match target with
        | ARCObjectTarget.Metadata -> "metadata"
        | ARCObjectTarget.TableView tableIndex -> $"table-{tableIndex}"
        | ARCObjectTarget.DataMap -> "datamap"

[<Erase; Mangle(false)>]
type ARCObjectSelectorWidget =

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-3 swt:p-2 swt:min-w-80 swt:max-w-[95vw]"

    static member private disabledState (message: string) =
        Html.div [
            prop.className ARCObjectSelectorWidget.WidgetContainerClass
            prop.children [
                Html.h3 [
                    prop.className "swt:text-xl swt:font-bold"
                    prop.text "ARC Object Selector"
                ]
                Html.div [
                    prop.className "swt:alert swt:alert-warning swt:text-sm"
                    prop.children [ Html.text message ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main
        (
            arcFileState: ArcFiles option,
            selectedTarget: ARCObjectTarget option,
            onSelectTarget: ARCObjectTarget -> unit
        ) =

        let widgetCtx = WidgetContext.useWidgetController ()

        match arcFileState with
        | None ->
            ARCObjectSelectorWidget.disabledState "Open an ARC file first."
        | Some arcFile ->
            let availableTargets = ARCObjectTarget.availableTargets arcFile

            let selectedTargetLabel =
                selectedTarget
                |> Option.map (ARCObjectTarget.label arcFile)
                |> Option.defaultValue "None"

            Html.div [
                prop.className ARCObjectSelectorWidget.WidgetContainerClass
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-wrap swt:items-end swt:gap-2"
                        prop.children [
                            Html.h3 [
                                prop.className "swt:text-xl swt:font-bold"
                                prop.text "ARC Object Selector"
                            ]
                            Html.span [
                                prop.className "swt:text-xs swt:opacity-70 swt:ml-auto"
                                prop.textf "%d target(s)" availableTargets.Length
                            ]
                        ]
                    ]
                    Html.p [
                        prop.className "swt:text-sm swt:opacity-80"
                        prop.text "Choose which ARC object should be shown in the preview."
                    ]
                    Html.div [
                        prop.className "swt:text-xs swt:opacity-70"
                        prop.textf "Current target: %s" selectedTargetLabel
                    ]
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-2"
                        prop.children [
                            for target in availableTargets do
                                let isSelected = selectedTarget = Some target
                                let label = ARCObjectTarget.label arcFile target

                                Html.button [
                                    prop.key (ARCObjectSelectorWidgetHelper.targetKey target)
                                    prop.className [
                                        "swt:btn swt:justify-start swt:h-auto swt:min-h-12 swt:px-3"
                                        if isSelected then
                                            "swt:btn-primary"
                                        else
                                            "swt:btn-outline"
                                    ]
                                    prop.onClick (fun _ ->
                                        onSelectTarget target
                                        widgetCtx.closeWidget WidgetType.ARCObjectSelector
                                    )
                                    prop.children [
                                        ARCObjectSelectorWidgetHelper.buttonIcon target
                                        Html.span [
                                            prop.className "swt:flex-1 swt:text-left"
                                            prop.text label
                                        ]
                                        if isSelected then
                                            Html.span [
                                                prop.className "swt:badge swt:badge-neutral"
                                                prop.text "Current"
                                            ]
                                    ]
                                ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:gap-2"
                        prop.children [
                            Html.button [
                                prop.className "swt:btn swt:btn-outline"
                                prop.text "Close"
                                prop.onClick (fun _ -> widgetCtx.closeWidget WidgetType.ARCObjectSelector)
                            ]
                        ]
                    ]
                ]
            ]
