namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open Feliz

type private SelectContext = {|
    activeIndex: int option
    selectedIndices: Set<int>
    optionCount: int
    getItemProps: obj -> obj
    handleSelect: int option -> unit
|}

module private SelectHelper =

    let SelectContext =
        React.createContext (
            "SelectContext",
            {|
                activeIndex = None
                selectedIndex = None
                getItemProps = fun () -> ()
                handleSelect = fun _ -> ()
            |}
            |> unbox<SelectContext>
        )

    [<Literal>]
    let SelectAllIndex = -1

[<Erase; Mangle(false)>]
type Select =

    static member private InnerBaseOptionRender
        (label: string, isSelected: bool, ?ref: IRefValue<option<Browser.Types.HTMLInputElement>>)
        =
        React.fragment [
            Html.div [
                prop.custom ("data-selectoption", label)
                prop.className
                    "swt:text-sm swt:font-normal swt:text-success swt:min-w-4 swt:h-full swt:flex swt:items-center"
                prop.children [
                    Html.input [
                        prop.ref (if ref.IsSome then ref.Value else unbox null)
                        prop.tabIndex -1
                        prop.className "swt:checkbox swt:checkbox-xs"
                        prop.type'.checkbox
                        prop.isChecked isSelected
                        prop.readOnly true
                    ]
                ]
            ]
            Html.div label
        ]

    static member private OuterBaseOptionRender
        (
            isActive: bool,
            isSelected: bool,
            key: obj,
            listItem:
                {|
                    index: int
                    ref: IRefValue<option<Browser.Types.HTMLElement>>
                |},
            selectContext: SelectContext,
            toggleSelect: bool -> unit,
            children: ReactElement
        ) =
        Html.li [
            prop.key (unbox<string> key)
            prop.ref listItem.ref
            prop.role.option
            prop.ariaSelected (isActive && isSelected)
            prop.tabIndex 0
            prop.className [
                "swt:list-row swt:rounded-none swt:p-1 swt:border-l-4 swt:border-transparent swt:focus-within:outline-none swt:cursor-pointer"
                if isActive then
                    "swt:!border-primary swt:bg-base-content/10"
                if isSelected then
                    "swt:border-accent"
            ]
            yield!
                prop.spread
                <| selectContext.getItemProps (
                    {|
                        onClick = fun () -> toggleSelect isSelected
                        onKeyDown =
                            fun (e: Browser.Types.KeyboardEvent) ->
                                if e.code = kbdEventCode.enter then
                                    e.stopPropagation ()
                                    e.preventDefault ()
                                    toggleSelect isSelected
                    |}
                )
            prop.children children
        ]

    [<ReactComponent>]
    static member private SelectAll(setSelectIndices: Set<int> -> unit, key: string) =
        let selectContext = React.useContext SelectHelper.SelectContext
        let listItem = FloatingUI.useListItem ()
        let allIndices: Set<int> = Set(List.init selectContext.optionCount id)

        let isActive = selectContext.activeIndex = Some listItem.index
        let isSelected = selectContext.selectedIndices = allIndices

        let checkboxRef = React.useInputRef ()

        React.useEffect (
            (fun () ->
                if selectContext.selectedIndices.IsEmpty then
                    checkboxRef.current |> Option.iter (fun x -> x.indeterminate <- false)
                elif not isSelected && selectContext.selectedIndices.IsSubsetOf allIndices then
                    checkboxRef.current |> Option.iter (fun x -> x.indeterminate <- true)
                else
                    checkboxRef.current |> Option.iter (fun x -> x.indeterminate <- false)
            ),
            [| box selectContext.selectedIndices |]
        )

        let toggleSelect =
            fun (_) ->
                if isSelected then
                    setSelectIndices (Set.empty)
                else
                    setSelectIndices (allIndices)

        Select.OuterBaseOptionRender(
            isActive,
            isSelected,
            key,
            listItem,
            selectContext,
            toggleSelect,
            Select.InnerBaseOptionRender("Select all", isSelected, ref = checkboxRef)
        )

    [<ReactComponent>]
    static member SelectItem<'a>
        (option: SelectItem<'a>, key: int, ?optionRenderFn: SelectItemRender<'a> -> ReactElement)
        =
        let OptionRender =
            optionRenderFn
            |> Option.defaultValue (fun (props: SelectItemRender<'a>) ->
                Select.InnerBaseOptionRender(props.item.label, props.isSelected)
            )

        let index = key

        let selectContext = React.useContext SelectHelper.SelectContext
        let listItem = FloatingUI.useListItem ()

        let isActive = selectContext.activeIndex = Some listItem.index
        let isSelected = selectContext.selectedIndices.Contains index

        let toggleSelect = fun (_) -> selectContext.handleSelect (Some index)

        Select.OuterBaseOptionRender(
            isActive,
            isSelected,
            key,
            listItem,
            selectContext,
            toggleSelect,
            OptionRender {|
                isActive = isActive
                isSelected = isSelected
                item = option
            |}
        )
    // Html.li [
    //     prop.key key
    //     prop.ref listItem.ref
    //     prop.role.option
    //     prop.ariaSelected (isActive && isSelected)
    //     prop.tabIndex 0
    //     prop.className [
    //         "swt:list-row swt:rounded-none swt:p-1 swt:border-l-4 swt:border-transparent swt:focus-within:outline-none swt:cursor-pointer"
    //         if isActive then
    //             "swt:!border-primary swt:bg-base-content/10"
    //         if isSelected then
    //             "swt:border-accent"
    //     ]
    //     yield!
    //         prop.spread
    //         <| selectContext.getItemProps (
    //             {|
    //                 onClick = fun () -> toggleSelect isSelected
    //                 onKeyDown =
    //                     fun (e: Browser.Types.KeyboardEvent) ->
    //                         if e.code = kbdEventCode.enter then
    //                             e.stopPropagation ()
    //                             e.preventDefault ()
    //                             toggleSelect isSelected
    //             |}
    //         )
    //     prop.children (
    //
    //     )
    // ]

    [<ReactComponent(true)>]
    static member Select<'a>
        (
            options: SelectItem<'a>[],
            selectedIndices: Set<int>,
            setSelectedIndices: Set<int> -> unit,
            ?onSelect: int option -> unit,
            ?triggerRenderFn: {| isOpen: bool |} -> ReactElement,
            ?optionRenderFn: SelectItemRender<'a> -> ReactElement,
            ?dropdownPlacement: FloatingUI.Placement,
            ?middleware: FloatingUI.IMiddleware[]
        ) =

        let mkLabel (indices: int seq) =
            indices |> Seq.map (fun i -> options.[i].label) |> String.concat ", "

        let isOpen, setIsOpen = React.useState (false)
        let activeIndex, setActiveIndex = React.useState (None: int option)

        let flui =
            FloatingUI.useFloating (
                placement = defaultArg dropdownPlacement FloatingUI.Placement.BottomStart,
                ``open`` = isOpen,
                onOpenChange = setIsOpen,
                whileElementsMounted = FloatingUI.autoUpdate,
                middleware = defaultArg middleware [| FloatingUI.Middleware.flip () |]
            )

        let elementsRef = React.useRef<Browser.Types.HTMLElement option[]> ([||])

        let labelsRef = React.useRef<string option[]> ([||])

        let handleSelect =
            (fun (index: int option) ->
                onSelect |> Option.iter (fun f -> f index)

                if index.IsSome then
                    let nextIndices =
                        if selectedIndices.Contains index.Value then
                            selectedIndices.Remove index.Value
                        else
                            selectedIndices.Add index.Value

                    setSelectedIndices (nextIndices)
            )

        let handleTypeaheadMatch =
            fun (index: int option) ->
                if isOpen then
                    setActiveIndex (index)

        let listNav =
            FloatingUI.useListNavigation (
                flui.context,
                FloatingUI.UseListNavigationProps(
                    listRef = elementsRef,
                    activeIndex = activeIndex,
                    onNavigate = setActiveIndex
                )
            )

        let typeahead =
            FloatingUI.useTypeahead (
                flui.context,
                FloatingUI.UseTypeaheadProps(
                    listRef = labelsRef,
                    activeIndex = activeIndex,
                    onMatch = handleTypeaheadMatch
                )
            )

        let click = FloatingUI.useClick (flui.context)
        let dismiss = FloatingUI.useDismiss (flui.context)

        let role =
            FloatingUI.useRole (flui.context, FloatingUI.UseRoleProps(role = FloatingUI.RoleAttribute.Listbox))

        let interactions =
            FloatingUI.useInteractions ([| listNav; typeahead; click; dismiss; role |])

        let selectContext: SelectContext =
            React.useMemo (
                (fun () -> {|
                    activeIndex = activeIndex
                    selectedIndices = selectedIndices
                    getItemProps = interactions.getItemProps
                    handleSelect = handleSelect
                    optionCount = options.Length
                |}),
                [|
                    activeIndex
                    selectedIndices
                    interactions.getItemProps
                    handleSelect
                    options.Length
                |]
            )

        let TriggerRender =
            triggerRenderFn
            |> Option.defaultValue (fun _ ->
                Html.button [
                    prop.tabIndex -1
                    prop.className [ "swt:btn swt:w-fit swt:pointer-events-none" ]
                    prop.text (
                        if selectedIndices.Count = 0 then
                            "Select an option"
                        else
                            mkLabel selectedIndices
                    )
                ]
            )

        let floatingStyle =
            let entries =
                JS.Constructors.Object.entries flui.floatingStyles
                |> Seq.choose (
                    function
                    | key, value when not (isNull value) -> Some(style.custom (key, value))
                    | _ -> None
                )
                |> Seq.toList

            entries @ [ style.zIndex 999 ]

        React.fragment [
            Html.div [
                prop.className "swt:size-fit swt:cursor-pointer swt:select-none"
                prop.ref (unbox flui.refs.setReference)
                prop.tabIndex 0
                yield! prop.spread <| interactions.getReferenceProps (null)
                prop.children (TriggerRender {| isOpen = isOpen |})
            ]
            React.contextProvider (
                SelectHelper.SelectContext,
                selectContext,
                React.fragment [
                    if isOpen then
                        FloatingUI.FloatingPortal(
                            FloatingUI.FloatingFocusManager(
                                flui.context,
                                modal = false,
                                children =
                                    Html.div [
                                        prop.ref (unbox flui.refs.setFloating)
                                        prop.style floatingStyle
                                        yield! prop.spread <| interactions.getFloatingProps (null)
                                        prop.children [
                                            FloatingUI.FloatingList(
                                                elementsRef = elementsRef,
                                                labelsRef = labelsRef,
                                                children =
                                                    Html.ul [
                                                        prop.className [
                                                            "swt:list swt:p-2"
                                                            "swt:bg-base-100 swt:shadow-sm swt:rounded-xs"
                                                            "swt:overflow-y-auto swt:max-h-[400px]"
                                                            "swt:border-2 swt:border-base-content/50"
                                                        ]
                                                        prop.children [

                                                            Select.SelectAll(setSelectedIndices, key = "select-all")

                                                            for i in 0 .. options.Length - 1 do
                                                                let option = options.[i]

                                                                Select.SelectItem(
                                                                    option,
                                                                    key = i,
                                                                    ?optionRenderFn = optionRenderFn
                                                                )
                                                        ]
                                                    ]
                                            )
                                        ]
                                    ]
                            )
                        )
                ]
            )
        ]



    [<ReactComponent>]
    static member Entry() =

        let options: SelectItem<{| givenName: string; age: int |}>[] = [|
            {|
                label = "Kevin Frey"
                item = {| givenName = "Kevin Frey"; age = 30 |}
            |}
            {|
                label = "John Doe"
                item = {| givenName = "John Doe"; age = 25 |}
            |}
            {|
                label = "Jane Smith"
                item = {| givenName = "Jane Smith"; age = 28 |}
            |}
            {|
                label = "Alice Johnson"
                item = {|
                    givenName = "Alice Johnson"
                    age = 22
                |}
            |}
            {|
                label = "Bob Brown"
                item = {| givenName = "Bob Brown"; age = 35 |}
            |}
            {|
                label = "Charlie White"
                item = {|
                    givenName = "Charlie White"
                    age = 40
                |}
            |}
            {|
                label = "Diana Green"
                item = {|
                    givenName = "Diana Green"
                    age = 32
                |}
            |}
            {|
                label = "Ethan Black"
                item = {|
                    givenName = "Ethan Black"
                    age = 29
                |}
            |}
            {|
                label = "Fiona Blue"
                item = {| givenName = "Fiona Blue"; age = 27 |}
            |}
        // Shoutout to my ai for the mock data
        |]

        let selectedIndices, setSelectedIndices = React.useState (Set.empty: Set<int>)

        Select.Select(options, selectedIndices, setSelectedIndices)