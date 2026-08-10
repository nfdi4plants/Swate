namespace App

open Feliz
open ARCtrl
open Types
open Fable.Core
open Swate.Components.Composite.TermSearch.Types
open Swate.Components.Primitive.Dropdown
open Model.BuildingBlock

module private Helperfuncs =
    let updateAnnotation
        (func: Annotation -> Annotation, indx: int, annoState: Annotation list, setState: Annotation list -> unit)
        =
        let nextA = func annoState[indx]
        annoState |> List.mapi (fun i a -> if i = indx then nextA else a) |> setState

[<AutoOpen>]
module private ARCtrlExtensions =
    type CompositeCell with
        member this.UpdateWithString(s: string) =
            match this with
            | CompositeCell.Unitized(_, oa) -> CompositeCell.Unitized(s, oa)
            | _ -> this

module Searchblock =

    let TermOrUnitizedSwitch (a: int, annoState: Annotation list, setState: Annotation list -> unit) =
        React.Fragment [
            Html.div [
                prop.className "swt:flex swt:w-full swt:gap-2"
                prop.children [
                    let isTermActive = annoState[a].Search.Body.isTerm

                    Html.button [
                        prop.className (
                            if isTermActive then
                                "swt:flex-1 swt:rounded-md swt:border swt:border-info swt:bg-info swt:px-3 swt:py-1 swt:text-white"
                            else
                                "swt:flex-1 swt:rounded-md swt:border swt:border-gray-300 swt:bg-white swt:px-3 swt:py-1 swt:text-gray-700"
                        )
                        prop.onClick (fun _ ->
                            (annoState
                             |> List.mapi (fun i e ->
                                 if i = a then
                                     {
                                         e with
                                             Search.Body = e.Search.Body.ToTermCell()
                                     }
                                 elif i = a then
                                     {
                                         e with
                                             Search.Body = e.Search.Body.UpdateWithString("")
                                     }
                                 else
                                     e
                             ))
                            |> setState

                        )
                        prop.text "Term"
                    ]

                    let isUnitizedActive = annoState[a].Search.Body.isUnitized

                    Html.button [
                        prop.className (
                            if isUnitizedActive then
                                "swt:flex-1 swt:rounded-md swt:border swt:border-info swt:bg-info swt:px-3 swt:py-1 swt:text-white"
                            else
                                "swt:flex-1 swt:rounded-md swt:border swt:border-gray-300 swt:bg-white swt:px-3 swt:py-1 swt:text-gray-700"
                        )
                        prop.onClick (fun _ ->
                            (annoState
                             |> List.mapi (fun i e ->
                                 if i = a then
                                     {
                                         e with
                                             Search.Body = e.Search.Body.ToUnitizedCell()
                                     }
                                 else
                                     e
                             ))
                            |> setState
                        )
                        prop.text "Unit"
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    let SearchElementKey (ui, setUi, annoState: Annotation list, setAnnoState, a) =
        let element = React.useElementRef ()
        let keyTypeDropdownOpen, setKeyTypeDropdownOpen = React.useState false

        Html.div [
            prop.ref element
            prop.className "swt:relative"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:w-full swt:flex-wrap swt:gap-2 swt:z-20 swt:text-white"
                    prop.children [
                        let setKeyType (cHDOpt: CompositeHeaderDiscriminate option) =
                            let cHD = cHDOpt |> Option.defaultValue CompositeHeaderDiscriminate.Parameter

                            Helperfuncs.updateAnnotation (
                                (fun anno -> { anno with Search.KeyType = cHD }),
                                a,
                                annoState,
                                setAnnoState
                            )

                        let keyTypeOptions = [
                            CompositeHeaderDiscriminate.Parameter
                            CompositeHeaderDiscriminate.Factor
                            CompositeHeaderDiscriminate.Characteristic
                            CompositeHeaderDiscriminate.Component
                            CompositeHeaderDiscriminate.Comment
                            CompositeHeaderDiscriminate.Date
                            CompositeHeaderDiscriminate.Performer
                            CompositeHeaderDiscriminate.ProtocolDescription
                            CompositeHeaderDiscriminate.ProtocolREF
                            CompositeHeaderDiscriminate.ProtocolType
                            CompositeHeaderDiscriminate.ProtocolUri
                            CompositeHeaderDiscriminate.ProtocolVersion
                            CompositeHeaderDiscriminate.Input
                            CompositeHeaderDiscriminate.Output
                        ]

                        Dropdown.Main(
                            isOpen = keyTypeDropdownOpen,
                            setIsOpen = setKeyTypeDropdownOpen,
                            toggle =
                                Html.button [
                                    prop.type'.button
                                    prop.className "swt:btn swt:btn-sm swt:btn-outline swt:text-black"
                                    prop.text (annoState[a].Search.KeyType.ToString())
                                    prop.onClick (fun _ -> setKeyTypeDropdownOpen (not keyTypeDropdownOpen))
                                ],
                            children =
                                React.Fragment [
                                    for keyType in keyTypeOptions ->
                                        Html.li [
                                            Html.a [
                                                prop.text (keyType.ToString())
                                                prop.onClick (fun _ ->
                                                    setKeyType (Some keyType)
                                                    setKeyTypeDropdownOpen false
                                                )
                                            ]
                                        ]
                                ],
                            dropdownClassName = "swt:dropdown-end"
                        )

                        let setter (termOpt: Term option) =
                            let nextOA =
                                termOpt
                                |> Option.map OntologyAnnotation.from
                                |> Option.defaultValue (OntologyAnnotation())

                            Helperfuncs.updateAnnotation (
                                (fun anno -> { anno with Search.Key = nextOA }),
                                a,
                                annoState,
                                setAnnoState
                            )

                        let input = annoState[a].Search.Key.ToTerm() |> Some

                        Swate.Components.Composite.TermSearch.TermSearch.TermSearch(
                            input,
                            setter,
                            classNames = TermSearchStyle(U2.Case1 "swt:w-full swt:min-w-[14rem]"),
                            ?parentId = None
                        )
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    let SearchElementBody (a, annoState, setAnnoState) =
        let element = React.useElementRef ()

        Html.div [
            prop.ref element
            prop.className "swt:relative"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:w-full swt:flex-wrap swt:gap-2 swt:z-1! swt:text-white"
                    prop.children [
                        TermOrUnitizedSwitch(a, annoState, setAnnoState)
                        let setter (termOpt: Term option) =
                            Helperfuncs.updateAnnotation (
                                (fun anno ->
                                    let nextOA =
                                        termOpt
                                        |> Option.map OntologyAnnotation.from
                                        |> Option.defaultValue (OntologyAnnotation())

                                    let nextCell = anno.Search.Body.UpdateWithOA(nextOA)
                                    { anno with Search.Body = nextCell }

                                ),
                                a,
                                annoState,
                                setAnnoState
                            )

                        let input =
                            annoState[a].Search.Body.ToOA() |> Some |> Option.map (fun oa -> oa.ToTerm())

                        Swate.Components.Composite.TermSearch.TermSearch.TermSearch(
                            input,
                            setter,
                            classNames = TermSearchStyle(U2.Case1 "swt:w-full swt:min-w-[14rem]"),
                            ?parentId = None
                        )
                    ]
                ]
            ]
        ]

type Components =

    [<ReactComponent>]
    static member AnnoBlockwithSwate
        (
            annoState: Annotation list,
            setState: Annotation list -> unit,
            index: int,
            highlight: Highlight,
            setHighlight: Highlight -> unit
        ) =

        let (ui: BuildingBlockUIState, setUi) = React.useState (BuildingBlockUIState.init)

        let a = annoState.[index]

        let mapOfSameHeight =

            annoState |> List.groupBy (fun a -> floor a.Height) |> Map.ofList

        let deleteButton (specIndex: int) =
            Html.span [
                prop.className "swt:mt-0 swt:cursor-pointer swt:hover:text-error swt:transition-colors"
                prop.onClick (fun _ ->
                    annoState |> List.filter ((<>) annoState[specIndex]) |> setState

                    let newHighlight = {
                        Keys = highlight.Keys |> Map.remove annoState[specIndex].Height
                        Terms = highlight.Terms |> Map.remove annoState[specIndex].Height
                        Values = highlight.Values |> Map.remove annoState[specIndex].Height
                    }

                    setHighlight newHighlight
                )
                prop.children [
                    Html.span [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--delete-24-regular swt:size-4"
                        ]
                    ]
                ]
            ]

        let closeButton (specIndex: int) =
            Html.div [
                prop.className "swt:cursor-pointer swt:hover:text-info"
                prop.onClick (fun e ->
                    Helperfuncs.updateAnnotation ((fun a -> a.ToggleOpen()), specIndex, annoState, setState)
                )
                prop.children [
                    Html.span [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--chevron-left-20-regular swt:size-4"
                        ]
                    ]
                ]
            ]

        let valueInput (specIndex: int) =
            if annoState[specIndex].Search.Body.isUnitized then
                Html.div [
                    prop.className "swt:max-w-32"
                    prop.children [
                        Html.div [
                            prop.className
                                "swt:flex swt:items-center swt:gap-2 swt:relative swt:rounded-md swt:border swt:border-gray-300 swt:bg-white swt:px-3 swt:py-2"
                            prop.children [
                                Html.input [
                                    prop.className "swt:grow swt:text-black swt:outline-none"
                                    prop.placeholder "Value..."
                                    prop.onChange (fun (s: string) ->
                                        Helperfuncs.updateAnnotation (
                                            (fun anno -> {
                                                anno with
                                                    Search.Body = anno.Search.Body.UpdateWithString(s)
                                            }),
                                            specIndex,
                                            annoState,
                                            setState
                                        )
                                    )
                                    match annoState.[specIndex].Search.Body with
                                    | CompositeCell.Unitized(v, _) -> prop.valueOrDefault v
                                    | _ -> ()
                                ]
                            ]
                        ]
                    ]
                ]
            else
                Html.none

        let annotationNote (specIndex: int) (hasChevron: bool) =
            Html.div [
                prop.className "swt:bg-[#ffe699] swt:p-3 swt:text-black swt:w-fit"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-row"
                        prop.children [
                            if hasChevron then
                                closeButton specIndex
                            Html.div [
                                prop.className "swt:space-y-2 swt:flex swt:flex-col swt:gap-2"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex swt:flex-row swt:justify-end"
                                        prop.children [ deleteButton specIndex ]
                                    ]
                                    Searchblock.SearchElementKey(ui, setUi, annoState, setState, specIndex)
                                    if annoState[specIndex].Search.KeyType.IsTermColumn() then
                                        Searchblock.SearchElementBody(specIndex, annoState, setState)
                                        valueInput specIndex
                                ]
                            ]
                        ]
                    ]
                ]
            ]


        Html.div [
            prop.style [ style.position.absolute; style.top (int a.Height) ]
            prop.children [
                if mapOfSameHeight[floor a.Height].Length = 1 && a.IsOpen = false then
                    Html.div [
                        prop.className "swt:z-0!"
                        prop.children [
                            Html.button [
                                prop.className "swt:cursor-pointer"
                                prop.children [
                                    Html.i [
                                        prop.className "swt:iconify swt:fluent--comment-24-regular swt:size-4"
                                        prop.style [ style.color "#ffe699" ]
                                        prop.onClick (fun e ->
                                            Helperfuncs.updateAnnotation (
                                                (fun e -> e.ToggleOpen()),
                                                index,
                                                annoState,
                                                setState
                                            )

                                            let updatedAnnos =
                                                annoState
                                                |> List.mapi (fun i anno ->
                                                    if i = index then
                                                        anno.ToggleOpen()
                                                    else
                                                        { anno with IsOpen = false }
                                                )

                                            setState updatedAnnos
                                        )
                                    ]
                                ]
                            ]
                        ]
                    ]

                elif mapOfSameHeight[floor a.Height].Length = 1 && a.IsOpen = true then
                    Html.div [
                        prop.className "swt:z-1! swt:relative"
                        prop.children [ annotationNote index true ]
                    ]

                elif
                    mapOfSameHeight[floor a.Height].Length > 1
                    && not (mapOfSameHeight[floor a.Height] |> List.exists (fun anno -> anno.IsOpen))
                then
                    Html.div [
                        prop.className "swt:z-0! swt:relative"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:items-center swt:gap-2"
                                prop.children [
                                    Html.span [
                                        prop.className
                                            "swt:inline-flex swt:h-6 swt:w-6 swt:items-center swt:justify-center swt:rounded-full swt:bg-secondary swt:px-2 swt:py-0.5 swt:text-xs swt:font-semibold swt:text-white"
                                        prop.text (string mapOfSameHeight[floor a.Height].Length)
                                    ]
                                    Html.button [
                                        prop.className "swt:cursor-pointer"
                                        prop.children [
                                            Html.i [
                                                prop.className "swt:iconify swt:fluent--comment-24-regular swt:size-4"
                                                prop.style [ style.color "#ffe699" ]
                                                prop.onClick (fun e ->
                                                    let updatedAnnos =
                                                        annoState
                                                        |> List.mapi (fun i anno ->
                                                            if i = index then
                                                                anno.ToggleOpen()
                                                            else
                                                                { anno with IsOpen = false }
                                                        )

                                                    setState updatedAnnos
                                                )
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]

                elif mapOfSameHeight[floor a.Height].Length > 1 && a.IsOpen = true then //
                    Html.div [
                        prop.className "swt:z-1! swt:relative"
                        prop.children [
                            Html.div [
                                prop.className
                                    "swt:flex swt:flex-col swt:gap-2 swt:border-3 swt:border-secondary swt:p-2 swt:bg-white"
                                prop.children [
                                    closeButton index
                                    for i in 0 .. mapOfSameHeight[floor a.Height].Length - 1 do
                                        let newIndex =
                                            annoState
                                            |> List.findIndex (fun anno -> anno = mapOfSameHeight[floor a.Height][i])

                                        annotationNote newIndex false
                                ]
                            ]
                        ]
                    ]
            ]
        ]
