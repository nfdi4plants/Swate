namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

[<Erase; Mangle(false)>]
type TermSearchConfigSetter =

    static member private TriggerRender(activeKeys: string[]) =
        Html.button [
            prop.testId "term-search-config-setter-tib-trigger"
            prop.tabIndex -1
            prop.className [ "swt:btn swt:w-fit swt:btn-primary swt:pointer-events-none" ]
            prop.children [
                Icons.SearchPlus("swt:size-4")
                Html.text (
                    if Array.isEmpty activeKeys then
                        "Select tib queries"
                    else
                        activeKeys |> Array.truncate 3 |> String.concat ", "
                )
                Html.text (if activeKeys.Length > 3 then " ..." else "")
            ]
        ]


    [<ReactComponentAttribute(true)>]
    static member TermSearchConfigSetter
        (renderer:
            {|
                title: string
                settingElement: ReactElement
                description: ReactElement
            |}
                -> ReactElement)
        =
        let activeKeysCtx = React.useContext (Contexts.TermSearch.TermSearchActiveKeysCtx)
        let allKeysCtx = React.useContext (Contexts.TermSearch.TermSearchAllKeysCtx)

        let selectedIndices =
            activeKeysCtx.data.aktiveKeys
            |> Array.choose (fun key -> allKeysCtx |> Seq.tryFindIndex (fun activeKey -> activeKey = key))
            |> Set

        let selectItems: SelectItem<string>[] =
            allKeysCtx |> Seq.map (fun key -> {| label = key; item = key |}) |> Array.ofSeq

        let setSelectedIndices =
            fun selectedIndices ->
                let nextActiveKeys = selectedIndices |> Seq.map (fun i -> allKeysCtx |> Seq.item i)

                activeKeysCtx.setData {
                    activeKeysCtx.data with
                        aktiveKeys = Array.ofSeq nextActiveKeys
                }

        let defaultSearchActive = not activeKeysCtx.data.disableDefault

        let TriggerRender =
            fun _ -> TermSearchConfigSetter.TriggerRender(activeKeysCtx.data.aktiveKeys)

        React.fragment [

            Html.div [
                prop.className "swt:hidden"
                prop.ariaHidden true
                prop.testId "term-search-config-setter"
                prop.custom ("data-activekeyscount", activeKeysCtx.data.aktiveKeys.Length)
                prop.custom ("data-defaultdisables", activeKeysCtx.data.disableDefault)
                prop.custom ("data-activekeys", activeKeysCtx.data.aktiveKeys |> Array.sort |> String.concat "; ")
            ]

            renderer {|
                title = "Enable Swate default search"
                settingElement =
                    Html.input [
                        prop.className [
                            if defaultSearchActive then
                                "swt:toggle-primary"
                            "swt:toggle"
                        ]
                        prop.type'.checkbox
                        prop.isChecked defaultSearchActive
                        prop.onChange (fun (b: bool) ->
                            activeKeysCtx.setData {
                                activeKeysCtx.data with
                                    disableDefault = not b
                            }
                        )
                    ]
                description = Html.p "When you deactivate this, the default search will not be used."
            |}

            renderer {|
                title = "Configure TIB search"
                settingElement =
                    Select.Select<string>(
                        selectItems,
                        selectedIndices,
                        setSelectedIndices,
                        triggerRenderFn = TriggerRender,
                        middleware = [|
                            FloatingUI.Middleware.flip ()
                            FloatingUI.Middleware.shift ()
                            FloatingUI.Middleware.offset (4)
                        |],
                        dropdownPlacement = FloatingUI.Placement.BottomEnd
                    )
                description =
                    React.fragment [
                        Html.p
                            "Adds support for high performance TIB term search. Choose a collection of terms to search through."
                        Html.p [ prop.text "Selecting multiple collections may impact search performance." ]
                    ]
            |}
        ]

    [<ReactComponent>]
    static member Entry() =

        let renderer =
            fun
                (props:
                    {|
                        title: string
                        settingElement: ReactElement
                        description: ReactElement
                    |}) ->
                React.fragment [
                    Html.label [ prop.className "swt:label"; prop.text props.title ]
                    props.settingElement
                    Html.div [
                        prop.className "swt:text-xs swt:base-content/60 swt:md:col-span-2 swt:prose"
                        prop.children props.description
                    ]
                ]

        Html.fieldSet [
            prop.className "swt:fieldset swt:bg-base-200 swt:border-base-300 swt:rounded-box swt:border swt:p-4"
            prop.children [
                Html.legend [ prop.className "swt:fieldset-legend"; prop.text "Configure Term Search" ]
                TermSearchConfigSetter.TermSearchConfigSetter renderer
            ]
        ]