module Renderer.components.Widgets.SearchComponents

open Feliz
open ARCtrl
open Model.BuildingBlock

[<ReactComponent>]
let CreateDropdown (state: Model) headerOptions ioTypeOptions setHeaderIOType setHeaderCellType =

    Html.div [
        prop.className "swt:join swt:w-full"
        prop.children [
            Html.select [
                prop.className "swt:select swt:join-item swt:border-current"
                prop.valueOrDefault (state.HeaderCellType.ToString())
                prop.onChange (fun (value: string) ->
                    headerOptions
                    |> Array.tryFind (fun (label, _) -> label = value)
                    |> Option.iter (fun (_, next) -> setHeaderCellType next)
                )
                prop.children [
                    for label, headerType in headerOptions do
                        Html.option [
                            prop.value label
                            prop.text (headerType.ToString())
                        ]
                ]
            ]

            if state.HeaderCellType.HasIOType() then
                let selectedIOTypeLabel: string =
                    state.TryHeaderIO()
                    |> Option.map (fun ioType ->
                        ioTypeOptions
                        |> Array.tryFind (fun (_, candidate) ->
                            match ioType, candidate with
                            | IOType.FreeText _, IOType.FreeText _ -> true
                            | _ -> ioType = candidate
                        )
                        |> Option.map fst
                        |> Option.defaultValue "Free Text"
                    )
                    |> Option.defaultValue "Source"

                Html.select [
                    prop.className "swt:select swt:join-item swt:border-current"
                    prop.valueOrDefault selectedIOTypeLabel
                    prop.onChange (fun (value: string) ->
                        ioTypeOptions
                        |> Array.tryFind (fun (label, _) -> label = value)
                        |> Option.iter (fun (_, ioType) ->
                            setHeaderIOType state.HeaderCellType ioType
                        )
                    )
                    prop.children [
                        for label, ioType in ioTypeOptions do
                            Html.option [
                                prop.value label
                                prop.text (
                                    match ioType with
                                    | IOType.FreeText _ -> "Free Text"
                                    | _ -> ioType.ToString()
                                )
                            ]
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let HeaderComponent (state: Model) setState setHeaderTerm setHeaderIOType =

    if state.HeaderCellType = CompositeHeaderDiscriminate.Comment then
        Html.input [
            prop.className "swt:input swt:border-current"
            prop.valueOrDefault state.CommentHeader
            prop.placeholder "Comment header"
            prop.onChange (fun (value: string) ->
                setState { state with CommentHeader = value }
            )
        ]
    elif state.HeaderCellType.HasOA() then
        Swate.Components.TermSearch.TermSearch(
            (state.TryHeaderOA() |> Option.map (fun oa -> oa.ToTerm())),
            setHeaderTerm,
            classNames =
                Swate.Components.Types.TermSearchStyle(
                    Fable.Core.U2.Case1 "swt:border-current swt:join-item swt:w-full"
                )
        )
    elif state.HeaderCellType.HasIOType() then
        match state.TryHeaderIO() with
        | Some(IOType.FreeText freeText) ->
            Html.input [
                prop.className "swt:input swt:border-current"
                prop.valueOrDefault freeText
                prop.placeholder "Input/Output text"
                prop.onChange (fun (value: string) ->
                    setHeaderIOType state.HeaderCellType (IOType.FreeText value)
                )
            ]
        | Some ioType ->
            Html.input [
                prop.className "swt:input swt:border-current"
                prop.readOnly true
                prop.valueOrDefault (ioType.ToString())
            ]
        | None -> Html.none
    else
            Html.none

[<ReactComponent>]
let SearchBuildingBockHeaderElement (state: Model) setState headerOptions ioTypeOptions setHeaderIOType setHeaderCellType setHeaderTerm =

    Html.div [
        prop.style [ style.position.relative ]
        prop.children [
            Html.div [
                prop.className "swt:join swt:w-full"
                prop.children [
                    CreateDropdown
                        state
                        headerOptions
                        ioTypeOptions
                        setHeaderIOType
                        setHeaderCellType

                    HeaderComponent state setState setHeaderTerm setHeaderIOType
                ]
            ]
        ]
    ]
