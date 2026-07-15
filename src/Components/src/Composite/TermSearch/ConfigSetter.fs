namespace Swate.Components.Composite.TermSearch

open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.TermSearch.Context
open Swate.Components.Composite.TermSearch.Types
open Swate.Components.Primitive
open Swate.Components.Primitive.Select
open Swate.Components.Primitive.Select.Types

module private ConfigSetterTypes =

    let sources = [| TermSearchSource.OLS; TermSearchSource.TIB |]

    let description =
        function
        | TermSearchSource.OLS -> "Choose complete terminology collections from the TS4NFDI OLS gateway."
        | TermSearchSource.TIB -> "Choose the TIB terminology collections to include in term searches."

[<Erase; Mangle(false)>]
type TermSearchConfigSetter =

    [<ReactComponent>]
    static member private SourceTriggerRender(source: TermSearchSource, activeKeys: string[]) =
        let sourceName = source.ToString()

        Html.button [
            prop.testId $"term-search-config-setter-{sourceName.ToLowerInvariant()}-trigger"
            prop.tabIndex -1
            prop.className [
                "swt:btn swt:w-fit swt:btn-primary swt:pointer-events-none"
            ]
            prop.children [
                Icons.SearchPlus("swt:size-4")
                Html.text (
                    if Array.isEmpty activeKeys then
                        $"Select {sourceName} collections"
                    else
                        activeKeys |> Array.truncate 3 |> String.concat ", "
                )
                Html.text (if activeKeys.Length > 3 then " ..." else "")
            ]
        ]

    [<ReactComponent>]
    static member private CollectionSelector
        (source: TermSearchSource, allKeys: Set<string>, activeKeys: string[], setActiveKeys: string[] -> unit)
        =
        let keys =
            allKeys |> Seq.filter (TermSearchSourceKey.belongsTo source) |> Array.ofSeq

        let activeSourceKeys, otherKeys =
            activeKeys |> Array.partition (TermSearchSourceKey.belongsTo source)

        let selectedIndices =
            activeSourceKeys
            |> Array.choose (fun key -> keys |> Array.tryFindIndex ((=) key))
            |> Set

        let selectItems: SelectItem<string>[] =
            keys |> Array.map (fun key -> {| label = key; item = key |})

        let setSelectedIndices selectedIndices =
            let selectedKeys =
                selectedIndices |> Seq.map (fun index -> keys.[index]) |> Array.ofSeq

            setActiveKeys (Array.append otherKeys selectedKeys)

        let TriggerRender =
            fun _ -> TermSearchConfigSetter.SourceTriggerRender(source, activeSourceKeys)

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
        let activeKeysCtx = useTermSearchActiveKeysCtx ()
        let allKeysCtx = useTermSearchAllKeysCtx ()

        let activeKeysState =
            if isNullOrUndefined (box activeKeysCtx.state) then
                TermSearchActiveKeysContext.init (Set.empty)
            else
                activeKeysCtx.state

        let activeKeys =
            activeKeysState.activeKeys |> Option.ofObj |> Option.defaultValue [||]

        let setActiveKeys nextActiveKeys =
            activeKeysCtx.setState {|
                activeKeysState with
                    activeKeys = nextActiveKeys
            |}

        let defaultSearchActive = not activeKeysState.disableDefault

        let renderCollectionSource source =
            let sourceName = source.ToString()

            renderer {|
                title = $"Configure {sourceName} search"
                settingElement =
                    TermSearchConfigSetter.CollectionSelector(source, allKeysCtx, activeKeys, setActiveKeys)
                description =
                    React.Fragment [
                        Html.p (ConfigSetterTypes.description source)
                        Html.p [
                            prop.text "Selecting multiple collections may impact search performance."
                        ]
                    ]
            |}

        React.Fragment [

            //Debugging component, storing data for storybook tests
            Html.div [
                prop.className "swt:hidden"
                prop.ariaHidden true
                prop.testId "term-search-config-setter"
                prop.custom ("data-activekeyscount", activeKeys.Length)
                prop.custom ("data-defaultdisables", activeKeysState.disableDefault)
                prop.custom ("data-activekeys", activeKeys |> Array.sort |> String.concat "; ")
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
                            activeKeysCtx.setState {|
                                activeKeysState with
                                    disableDefault = not b
                            |}
                        )
                    ]
                description = Html.p "When you deactivate this, the default search will not be used."
            |}

            yield! ConfigSetterTypes.sources |> Array.map renderCollectionSource
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
                React.Fragment [
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
                Html.legend [
                    prop.className "swt:fieldset-legend"
                    prop.text "Configure Term Search"
                ]
                TermSearchConfigSetter.TermSearchConfigSetter renderer
            ]
        ]
