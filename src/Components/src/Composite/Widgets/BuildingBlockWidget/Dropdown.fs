namespace Swate.Components.Composite.Widgets.BuildingBlockWidget

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Primitive.Dropdown
open Swate.Components.Shared

module private BuildingBlockDropdownState =

    [<RequireQualifiedAccess>]
    type Page =
        | Main
        | More
        | IOTypes of CompositeHeaderDiscriminate

module private BuildingBlockDropdownHelper =

    let MainHeaderTypes: CompositeHeaderDiscriminate[] = [|
        CompositeHeaderDiscriminate.Parameter
        CompositeHeaderDiscriminate.Factor
        CompositeHeaderDiscriminate.Characteristic
        CompositeHeaderDiscriminate.Component
    |]

    let MoreHeaderTypes: CompositeHeaderDiscriminate[] = [|
        CompositeHeaderDiscriminate.Comment
        CompositeHeaderDiscriminate.Date
        CompositeHeaderDiscriminate.Performer
        CompositeHeaderDiscriminate.ProtocolDescription
        CompositeHeaderDiscriminate.ProtocolREF
        CompositeHeaderDiscriminate.ProtocolType
        CompositeHeaderDiscriminate.ProtocolUri
        CompositeHeaderDiscriminate.ProtocolVersion
    |]

    let IOTypeOptions: IOType[] = [|
        IOType.Source
        IOType.Sample
        IOType.Material
        IOType.Data
        IOType.FreeText ""
    |]

[<Erase; Mangle(false)>]
type BuildingBlockDropdown =

    [<ReactComponent>]
    static member private FreeTextInputElement(onSubmit: string -> unit, ?freeText: string) =
        let initialValue = defaultArg freeText ""
        let inputS, setInput = React.useState initialValue

        Html.div [
            prop.className "swt:flex swt:flex-row swt:gap-0 swt:p-0 swt:join"
            prop.children [
                Html.input [
                    prop.placeholder "..."
                    prop.className
                        "swt:input swt:input-sm swt:join-item swt:grow swt:truncate swt:rounded-l-(--radius-field)! swt:rounded-r-none!"
                    prop.onClick (fun e -> e.stopPropagation ())
                    prop.onChange (fun (value: string) -> setInput value)
                    prop.onKeyDown (
                        key.enter,
                        fun e ->
                            e.preventDefault ()
                            e.stopPropagation ()
                            onSubmit inputS
                    )
                ]
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-accent swt:btn-sm swt:join-item"
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        onSubmit inputS
                    )
                    prop.children [ Icons.Check() ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private SubpageLinkItem(title: string, onSelect: unit -> unit) =
        Html.li [
            prop.onClick (fun e ->
                e.preventDefault ()
                e.stopPropagation ()
                onSelect ()
            )
            prop.onKeyDown (fun e ->
                if e.code = kbdEventCode.enter then
                    e.preventDefault ()
                    e.stopPropagation ()
                    onSelect ()
            )
            prop.children [
                Html.a [
                    prop.onClick (fun e -> e.preventDefault ())
                    prop.className "swt:flex swt:flex-row"
                    prop.children [
                        Html.span title
                        Html.i [
                            prop.className "swt:iconify swt:fluent--arrow-right-20-regular swt:ml-auto"
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private HeaderDropdownItem
        (headerType: CompositeHeaderDiscriminate, onSelectHeaderType: CompositeHeaderDiscriminate -> unit)
        =
        Html.li [
            Html.a [
                prop.onClick (fun e ->
                    e.preventDefault ()
                    e.stopPropagation ()
                    onSelectHeaderType headerType
                )
                prop.onKeyDown (fun e ->
                    if e.code = kbdEventCode.enter then
                        e.preventDefault ()
                        e.stopPropagation ()
                        onSelectHeaderType headerType
                )
                prop.text (headerType.ToString())
            ]
        ]

    [<ReactComponent>]
    static member private IOTypeDropdownItem
        (
            headerType: CompositeHeaderDiscriminate,
            ioType: IOType,
            onSelectHeaderIOType: CompositeHeaderDiscriminate -> IOType -> unit,
            ?freeText: string
        ) =
        let setIOType nextIOType =
            onSelectHeaderIOType headerType nextIOType

        Html.li [
            match ioType with
            | IOType.FreeText _ ->
                prop.children [
                    BuildingBlockDropdown.FreeTextInputElement(
                        (fun value -> setIOType (IOType.FreeText value)),
                        ?freeText = freeText
                    )
                ]
            | _ ->
                prop.children [
                    Html.a [
                        prop.onClick (fun e ->
                            e.preventDefault ()
                            e.stopPropagation ()
                            setIOType ioType
                        )
                        prop.onKeyDown (fun e ->
                            if e.code = kbdEventCode.enter then
                                e.preventDefault ()
                                e.stopPropagation ()
                                setIOType ioType
                        )
                        prop.children [ Html.div [ prop.text (ioType.ToString()) ] ]
                    ]
                ]
        ]

    [<ReactComponent>]
    static member private DropdownInfoFooter(hasBack: bool, onBackToMain: unit -> unit) =
        Html.li [
            prop.className "swt:flex swt:flex-row swt:justify-between swt:pt-1"
            if hasBack then
                prop.onClick (fun e ->
                    e.preventDefault ()
                    e.stopPropagation ()
                    onBackToMain ()
                )
            prop.children [
                if hasBack then
                    Html.a [
                        prop.className "swt:content-center"
                        prop.children [ Icons.ArrowLeft() ]
                    ]
                Html.a [
                    prop.href "#"
                    prop.className "swt:ml-auto swt:link-info"
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        Browser.Dom.window.``open`` (URLs.AnnotationPrinciplesUrl, "_blank") |> ignore
                    )
                    prop.text "info"
                ]
            ]
        ]

    [<ReactComponent>]
    static member private MainPageContent
        (
            onOpenIOTypePage: CompositeHeaderDiscriminate -> unit,
            onSelectHeaderType: CompositeHeaderDiscriminate -> unit,
            onOpenMorePage: unit -> unit
        ) =
        React.Fragment [
            BuildingBlockDropdown.SubpageLinkItem(
                CompositeHeaderDiscriminate.Input.ToString(),
                (fun () -> onOpenIOTypePage CompositeHeaderDiscriminate.Input)
            )
            Html.div [ prop.className "swt:divider swt:mx-2 swt:my-0" ]
            for headerType in BuildingBlockDropdownHelper.MainHeaderTypes do
                BuildingBlockDropdown.HeaderDropdownItem(headerType, onSelectHeaderType)
            BuildingBlockDropdown.SubpageLinkItem("More", onOpenMorePage)
            Html.div [ prop.className "swt:divider swt:mx-2 swt:my-0" ]
            BuildingBlockDropdown.SubpageLinkItem(
                CompositeHeaderDiscriminate.Output.ToString(),
                (fun () -> onOpenIOTypePage CompositeHeaderDiscriminate.Output)
            )
            BuildingBlockDropdown.DropdownInfoFooter(false, fun () -> ())
        ]

    [<ReactComponent>]
    static member private MorePageContent
        (onSelectHeaderType: CompositeHeaderDiscriminate -> unit, onBackToMain: unit -> unit)
        =
        React.Fragment [
            for headerType in BuildingBlockDropdownHelper.MoreHeaderTypes do
                BuildingBlockDropdown.HeaderDropdownItem(headerType, onSelectHeaderType)
            BuildingBlockDropdown.DropdownInfoFooter(true, onBackToMain)
        ]

    [<ReactComponent>]
    static member private IOTypePageContent
        (
            headerType: CompositeHeaderDiscriminate,
            selectedIOType: IOType option,
            onSelectHeaderIOType: CompositeHeaderDiscriminate -> IOType -> unit,
            onBackToMain: unit -> unit
        ) =
        let freeTextValue =
            selectedIOType
            |> Option.bind (fun ioType ->
                match ioType with
                | IOType.FreeText text -> Some text
                | _ -> None
            )

        React.Fragment [
            for ioType in BuildingBlockDropdownHelper.IOTypeOptions do
                match ioType with
                | IOType.FreeText _ ->
                    BuildingBlockDropdown.IOTypeDropdownItem(
                        headerType,
                        ioType,
                        onSelectHeaderIOType,
                        ?freeText = freeTextValue
                    )
                | _ -> BuildingBlockDropdown.IOTypeDropdownItem(headerType, ioType, onSelectHeaderIOType)
            BuildingBlockDropdown.DropdownInfoFooter(true, onBackToMain)
        ]

    [<ReactComponent>]
    static member Main
        (
            selectedHeaderType: CompositeHeaderDiscriminate,
            selectedIOType: IOType option,
            onSelectHeaderType: CompositeHeaderDiscriminate -> unit,
            onSelectHeaderIOType: CompositeHeaderDiscriminate -> IOType -> unit
        ) =
        let isOpen, setIsOpen = React.useState false
        let page, setPage = React.useState BuildingBlockDropdownState.Page.Main

        let closeDropdown () =
            setPage BuildingBlockDropdownState.Page.Main
            setIsOpen false

        let setDropdownOpen nextIsOpen =
            if not nextIsOpen then
                setPage BuildingBlockDropdownState.Page.Main

            setIsOpen nextIsOpen

        let handleHeaderTypeSelect headerType =
            onSelectHeaderType headerType
            closeDropdown ()

        let handleIOTypeSelect headerType ioType =
            onSelectHeaderIOType headerType ioType
            closeDropdown ()

        let content =
            match page with
            | BuildingBlockDropdownState.Page.Main ->
                BuildingBlockDropdown.MainPageContent(
                    (fun ioHeaderType -> setPage (BuildingBlockDropdownState.Page.IOTypes ioHeaderType)),
                    handleHeaderTypeSelect,
                    (fun () -> setPage BuildingBlockDropdownState.Page.More)
                )
            | BuildingBlockDropdownState.Page.More ->
                BuildingBlockDropdown.MorePageContent(
                    handleHeaderTypeSelect,
                    (fun () -> setPage BuildingBlockDropdownState.Page.Main)
                )
            | BuildingBlockDropdownState.Page.IOTypes ioHeaderType ->
                BuildingBlockDropdown.IOTypePageContent(
                    ioHeaderType,
                    selectedIOType,
                    handleIOTypeSelect,
                    (fun () -> setPage BuildingBlockDropdownState.Page.Main)
                )

        Dropdown.Main(
            isOpen,
            setDropdownOpen,
            Html.button [
                prop.type'.button
                prop.onClick (fun _ -> setDropdownOpen (not isOpen))
                prop.role "button"
                prop.className
                    "swt:btn swt:btn-primary swt:border swt:border-base-content! swt:join-item swt:flex-nowrap"
                prop.children [
                    Html.span (selectedHeaderType.ToString())
                    Icons.AngleDown()
                ]
            ],
            content,
            dropdownClassName = "swt:join-item swt:dropdown",
            contentClassName =
                "swt:min-w-64! swt:menu swt:bg-base-200 swt:rounded-box swt:z-99 swt:p-2 swt:shadow-sm swt:top-110%",
            closeOnClick = false
        )
