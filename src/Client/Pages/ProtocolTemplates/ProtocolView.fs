namespace Protocol

open System

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

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =

        let isOpen, setIsOpen = React.useState (false)
        let selectedOrganisations, setSelectedOrganisations = React.useState(Set.empty<Model.Protocol.CommunityFilter>)

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

        communities
        |> Array.iter (fun item ->
            printfn "options: %s" (item.ToString())
        )

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
                Html.div [
                    Html.p "Select community"

                    Components.BaseDropdown.Main(
                        isOpen,
                        setIsOpen,
                        Html.button [
                            prop.onClick (fun _ -> setIsOpen (not isOpen))
                            prop.role "button"
                            prop.type'.button
                            prop.className "swt:btn swt:btn-primary swt:border swt:!border-base-content swt:join-item swt:flex-nowrap"
                            prop.children [ Icons.Filter() ]
                        ],

                        communities
                        |> Array.map (fun community ->
                            Html.label [
                                prop.className "swt:flex swt:items-center swt:gap-x-2 swt:cursor-pointer"
                                prop.children[
                                    Templates.CheckBoxOrganisations(communities, community, true, selectedOrganisations, setSelectedOrganisations)
                                    Html.text (community.ToStringRdb())
                                ]
                            ]
                        )
                    )
                ]
                if model.ProtocolState.ShowSearch then
                    Protocol.SearchContainer.Main(model, dispatch)
                else
                    Modals.SelectiveTemplateFromDB.Main(model, dispatch, false)
            ]
        ]