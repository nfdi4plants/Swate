namespace Protocol

open System

open ARCtrl

open Fable
open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types
open SpreadsheetInterface
open Messages
open Elmish

open Feliz
open Feliz.DaisyUI

open Swate.Components

type Templates =

    static member Tag
        (
            tag: OntologyAnnotation,
            color: IReactProperty,
            isRemovable: bool,
            onclick: (Browser.Types.MouseEvent -> unit) option
        ) =
        Html.div [
            color
            prop.className [
                "swt:badge"
                if onclick.IsSome then
                    "swt:cursor-pointer"
                "swt:text-nowrap"
            ]
            if onclick.IsSome then
                prop.onClick (onclick.Value)
            prop.children [
                if isRemovable then
                    Svg.svg [
                        svg.xmlns "http://www.w3.org/2000/svg"
                        svg.fill "none"
                        svg.viewBox (0, 0, 24, 24)
                        svg.className "swt:inline-block swt:h-4 swt:w-4 swt:stroke-current"
                        svg.children [
                            Svg.path [
                                svg.strokeLineCap "round"
                                svg.strokeLineJoin "round"
                                svg.strokeWidth 2
                                svg.d "M6 18L18 6M6 6l12 12"
                            ]
                        ]
                    ]
                Html.div tag.NameText
            ]
            prop.title tag.TermAccessionShort
        ]

    static member TagContainer
        (
            tagList: OntologyAnnotation seq,
            title: string option,
            updateToggle: (OntologyAnnotation -> unit) option,
            badgeColor
        ) =
        React.fragment [
            if title.IsSome then
                Html.div [
                    prop.className "swt:divider"
                    prop.text title.Value
                ]
            Html.div [
                prop.className "swt:flex swt:flex-wrap swt:gap-2"
                prop.children [
                    for tagSuggestion in tagList do
                        Templates.Tag(
                            tagSuggestion,
                            badgeColor,
                            false,
                            updateToggle |> Option.map (fun f -> fun _ -> f tagSuggestion)
                        )
                ]
            ]
        ]

    static member TagQueryField (model: Model, state: TemplateFilterConfig, setState: TemplateFilterConfig -> unit) =

        let ErBadgeColor = badge.primary
        let TagBadgeColor = badge.accent

        let allTags =
            model.ProtocolState.Templates
            |> Seq.collect (fun x -> x.Tags)
            |> Seq.distinct
            |> Seq.filter (fun x -> state.ProtocolFilterTags |> List.contains x |> not)
            |> Array.ofSeq

        let allErTags =
            model.ProtocolState.Templates
            |> Seq.collect (fun x -> x.EndpointRepositories)
            |> Seq.distinct
            |> Seq.filter (fun x -> state.ProtocolFilterErTags |> List.contains x |> not)
            |> Array.ofSeq

        let hitTagList, hitErTagList =
            if state.ProtocolTagSearchQuery <> "" then
                let queryBigram =
                    state.ProtocolTagSearchQuery
                    |> Swate.Components.Shared.SorensenDice.createBigrams

                let getMatchingTags (allTags: OntologyAnnotation[]) =
                    allTags
                    |> Array.map (fun oa ->
                        oa.NameText
                        |> Swate.Components.Shared.SorensenDice.createBigrams
                        |> Swate.Components.Shared.SorensenDice.calculateDistance queryBigram,
                        oa)
                    |> Array.filter (fun x -> fst x >= 0.3 || (snd x).TermAccessionShort = state.ProtocolTagSearchQuery)
                    |> Array.sortByDescending fst
                    |> Array.map snd

                let sortedTags = getMatchingTags allTags
                let sortedErTags = getMatchingTags allErTags
                sortedTags, sortedErTags
            else
                [||], [||]

        Html.div [
            Html.p "Tags"
            Html.div [
                prop.className "swt:relative"
                prop.children [
                    Html.label [
                        prop.className "swt:label swt:input swt:input-sm swt:input-primary swt:flex swt:w-full swt:items-center"
                        prop.children [
                            Html.input [
                                prop.placeholder ".. protocol tag"
                                prop.valueOrDefault state.ProtocolTagSearchQuery
                                prop.onChange (fun (e: string) ->
                                    {
                                        state with
                                            ProtocolTagSearchQuery = e
                                    }
                                    |> setState)
                            ]
                            Icons.MagnifyingClass()
                        ]
                    ]
                    Html.div [
                        prop.className "swt:absolute swt:bg-base-300 swt:shadow-lg rounded-md swt:w-full swt:z-10 swt:p-2 swt:text-base-content"
                        prop.style [
                            if hitTagList |> Array.isEmpty && hitErTagList |> Array.isEmpty then
                                style.display.none
                        ]
                        prop.children [
                            if hitErTagList <> [||] then
                                let updateToggle =
                                    (fun tagSuggestion ->
                                        let nextState = {
                                            state with
                                                ProtocolFilterErTags = tagSuggestion :: state.ProtocolFilterErTags
                                                ProtocolTagSearchQuery = ""
                                        }

                                        setState nextState)

                                Templates.TagContainer(
                                    hitErTagList,
                                    Some "Endpoint Repositories",
                                    Some updateToggle,
                                    ErBadgeColor
                                )
                            if hitTagList <> [||] then
                                let updateToggle =
                                    (fun tagSuggestion ->
                                        let nextState = {
                                            state with
                                                ProtocolFilterTags = tagSuggestion :: state.ProtocolFilterTags
                                                ProtocolTagSearchQuery = ""
                                        }

                                        setState nextState)

                                Templates.TagContainer(hitTagList, Some "Tags", Some updateToggle, TagBadgeColor)
                        ]
                    ]
                ]
            ]
        ]

    static member ToggleOrganisation (selectedOrganisations:Set<Model.Protocol.CommunityFilter>, organisation: Model.Protocol.CommunityFilter) =
        if Set.contains organisation selectedOrganisations then
            Set.remove organisation selectedOrganisations
        else
            Set.add organisation selectedOrganisations

    static member CheckBoxOrganisations
        (organisations: Array, organisationIndex, isActive: bool, selectedOrganisations, setSelectedOrganisations) =

        let isChecked = selectedOrganisations |> Set.contains organisationIndex

        Html.div [
            prop.className "swt:flex swt:justify-center"
            prop.children [
                Html.input [
                    prop.type'.checkbox
                    prop.className "swt:checkbox swt:checkbox-neutral"
                    prop.disabled (not isActive)
                    prop.isChecked isChecked
                    prop.onChange (fun (_: bool) ->
                        if organisations.Length > 0 then
                            let nextImportConfig = Templates.ToggleOrganisation(selectedOrganisations, organisationIndex)
                            setSelectedOrganisations nextImportConfig)
                ]
            ]
        ]

    static member CommunityButton(isOpen, setIsOpen)=
        Html.button [
            prop.onClick (fun _ -> setIsOpen (not isOpen))
            prop.role "button"
            prop.type'.button
            prop.className "swt:btn swt:btn-primary swt:border swt:!border-base-content swt:join-item swt:flex-nowrap"
            prop.children [ Icons.Filter() ]
        ]

    static member Communities(model, selectedOrganisations, setSelectedOrganisations) =
        let communityNames =
            model.ProtocolState.Templates
            |> Array.choose (fun t -> Model.Protocol.CommunityFilter.CommunityFromOrganisation t.Organisation)
            |> Array.distinct
            |> List.ofArray
        let communities =
            [
                Model.Protocol.CommunityFilter.All
                Model.Protocol.CommunityFilter.OnlyCurated
            ]
            @ communityNames
            |> Array.ofList

        [
            Html.p [
                prop.text "Communities"
            ]
            yield!
                communities
                |> Array.map (fun community ->
                    Html.label [
                        prop.className "swt:flex swt:items-center swt:gap-x-2 swt:py-1 swt:cursor-pointer"
                        prop.children[
                            Templates.CheckBoxOrganisations(communities, community, true, selectedOrganisations, setSelectedOrganisations)
                            Html.text (community.ToStringRdb())
                        ]
                    ]
                )
            ]

    static member FilterTemplates(model:Model, button:ReactElement, isOpen, setIsOpen, config, configSetter: TemplateFilterConfig -> unit) =
        let selectedOrganisations, setSelectedOrganisations = React.useState(Set.empty<Model.Protocol.CommunityFilter>)
        Html.div [
            Components.BaseDropdown.Main(
                isOpen,
                setIsOpen,
                button,
                [
                    Templates.TagQueryField(model, config, configSetter)
                    yield! Templates.Communities(model, selectedOrganisations, setSelectedOrganisations)
                ]
            )
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch, ?button) =

        let isOpen, setIsOpen = React.useState (false)
        let config, setConfig = React.useState (TemplateFilterConfig.init)

        let button =
            if button.IsSome then
                button.Value
            else
                Templates.CommunityButton(isOpen, setIsOpen)

        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "Templates"
            SidebarComponents.SidebarLayout.Description(
                Html.p [
                    Html.b "Search the database for templates."
                    Html.text " The building blocks from these templates can be inserted into the Swate table. "
                    Html.span [
                        prop.className "swt:text-error"
                        prop.text "Only missing building blocks will be added."
                    ]
                ]
            )
            SidebarComponents.SidebarLayout.LogicContainer [
                Templates.FilterTemplates(model, button, isOpen, setIsOpen, config, setConfig)
                if model.ProtocolState.ShowSearch then
                    Protocol.SearchContainer.Main(model, dispatch)
                else
                    Modals.SelectiveTemplateFromDB.Main(model, dispatch, false)
            ]
        ]