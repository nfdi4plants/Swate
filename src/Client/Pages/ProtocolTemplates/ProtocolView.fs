namespace Protocol

open Model

open Feliz
open ARCtrl

module private TemplatesAux =

    [<Literal>]
    let ColCount = 5

type Templates =

    [<ReactComponent>]
    static member private TemplateItem
        (
            template: Template,
            show: bool,
            setShow: bool -> unit,
            isSelected: bool,
            toggleIsSelected: unit -> unit,
            ?key: obj
        ) =

        let authorStr =
            template.Authors
            |> Seq.map (fun author ->
                let givenName =
                    [ author.FirstName; author.LastName; author.MidInitials ]
                    |> List.choose id
                    |> String.concat " "

                givenName
            )
            |> String.concat ", "

        let sharedClasses = "swt:cursor-pointer"
        let isSelectedClass = "swt:bg-primary/10"
        let sharedProps = [ prop.onClick (fun _ -> toggleIsSelected ()) ]

        React.keyedFragment (
            template.Id,
            [
                Html.tr [
                    prop.role.button
                    prop.ariaRoleDescription "toggle template selection"
                    yield! sharedProps
                    prop.className [
                        sharedClasses
                        if isSelected then
                            isSelectedClass
                        if show then
                            "swt:!border-transparent" // removes bottom border to indicate relation to details row
                    ]
                    prop.key "generic"
                    prop.children [
                        Html.td [
                            Html.input [
                                prop.custom ("readOnly", true)
                                prop.isChecked isSelected
                                prop.type'.checkbox
                                prop.className "swt:checkbox"
                            ]
                        ]
                        Html.td [
                            prop.children [ Html.div [ prop.title template.Name; prop.text template.Name ] ]
                        ]
                        Html.td [
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex-col swt:items-start swt:text-xs swt:opacity-60"
                                    prop.children [
                                        Html.div [
                                            prop.className "swt:text-xs font-semibold"
                                            prop.text (template.Organisation.ToString())
                                        ]
                                        Html.div [
                                            prop.className "swt:text-xs"
                                            prop.text (sprintf "v%s" template.Version)
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.td [
                            Html.p [
                                prop.title authorStr
                                prop.className "swt:flex swt:items-center swt:text-xs swt:opacity-60 swt:!line-clamp-3"
                                prop.text authorStr
                            ]
                        ]
                        Html.td [
                            Swate.Components.Components.CollapseButton(
                                show,
                                (fun _ -> setShow (not show)),
                                classes = "swt:btn-sm"
                            )
                        ]
                    ]
                ]
                if show then
                    Html.tr [
                        prop.className [
                            sharedClasses
                            if isSelected then
                                isSelectedClass
                        ]
                        yield! sharedProps
                        prop.key "details"
                        prop.children [
                            Html.td [
                                prop.className "swt:pt-0"
                                prop.colSpan TemplatesAux.ColCount
                                prop.children [
                                    Html.div [ prop.className "swt:py-2 swt:text-xs"; prop.text template.Description ]
                                    Html.div [
                                        prop.className "swt:flex swt:gap-1"
                                        prop.children [
                                            for tag in template.EndpointRepositories do
                                                Html.span [
                                                    prop.className "swt:badge swt:badge-sm swt:badge-accent"
                                                    prop.text tag.NameText
                                                ]
                                            for tag in template.Tags do
                                                Html.span [
                                                    prop.className "swt:badge swt:badge-sm swt:badge-secondary"
                                                    prop.text tag.NameText
                                                ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
            ]
        )

    [<ReactComponent>]
    static member private RefreshButton (model: Model) dispatch =
        Html.button [
            prop.className "swt:btn swt:btn-sm swt:btn-square"
            prop.onClick (fun _ ->
                Messages.Protocol.GetAllProtocolsForceRequest
                |> Messages.ProtocolMsg
                |> dispatch
            )
            prop.children [ Swate.Components.Icons.ArrowsRotate() ]
        ]

    [<ReactComponent>]
    static member private DisplayTemplates(templates: Template[], model: Model, dispatch, ?maxheight: Styles.ICssUnit) =
        let maxheight = defaultArg maxheight (length.px 600)
        let (showIds: int list), setShowIds = React.useStateWithUpdater ([])

        Html.div [
            prop.style [ style.maxHeight maxheight ]
            prop.className "swt:shrink swt:overflow-y-auto"
            prop.children [
                Html.table [
                    prop.className "swt:table swt:table-pin-cols"
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th [
                                    Html.div [
                                        prop.className "swt:flex swt:items-center"
                                        prop.children [
                                            Swate.Components.Icons.Filter("swt:size-3")
                                            Html.span templates.Length
                                        ]
                                    ]
                                ]
                                Html.th "Template Name"
                                Html.th "Details"
                                Html.th "Authors"
                                Html.th [ Templates.RefreshButton model dispatch ]
                            ]
                        ]
                        Html.tbody [
                            match model.ProtocolState.Loading with
                            | true ->
                                Html.tr [
                                    Html.td [
                                        prop.colSpan TemplatesAux.ColCount
                                        prop.style [ style.textAlign.center ]
                                        prop.children [ Swate.Components.Icons.SpinningSpinner() ]
                                    ]
                                ]
                            | false ->
                                match templates with
                                | [||] ->
                                    Html.tr [
                                        Html.td [
                                            prop.colSpan TemplatesAux.ColCount
                                            prop.children [
                                                Html.div [
                                                    prop.className
                                                        "swt:flex swt:justify-center swt:items-center swt:w-full swt:h-full"
                                                    prop.text "No templates found."
                                                ]
                                            ]
                                        ]
                                    ]
                                | _ ->
                                    for i in 0 .. templates.Length - 1 do
                                        let template = templates.[i]
                                        let isShown = showIds |> List.contains i

                                        let setIsShown (show: bool) =
                                            if show then
                                                setShowIds (fun current -> i :: current)
                                            else
                                                setShowIds (fun current -> current |> List.except [ i ])

                                        let isSelected =
                                            model.ProtocolState.TemplatesSelected
                                            |> List.exists (fun t -> t.Id = template.Id)

                                        let toggleIsSelected =
                                            fun _ ->
                                                Messages.Protocol.ToggleSelectProtocol template
                                                |> Messages.ProtocolMsg
                                                |> dispatch

                                        Templates.TemplateItem(
                                            template,
                                            isShown,
                                            setIsShown,
                                            isSelected,
                                            toggleIsSelected
                                        )
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private ImportTemplatesBtn(model, dispatch) =
        let emptySelected = model.ProtocolState.TemplatesSelected.Length = 0

        Html.div [
            prop.className "swt:flex swt:gap-2"
            prop.children [
                Html.button [
                    prop.className [ "swt:btn swt:btn-primary swt:grow" ]
                    prop.disabled emptySelected
                    prop.textf "Import Templates"
                    prop.onClick (fun _ ->
                        if not model.ProtocolState.TemplatesSelected.IsEmpty then
                            Messages.Protocol.ImportProtocols |> Messages.ProtocolMsg |> dispatch
                    )
                ]
                if not emptySelected then
                    Html.button [
                        prop.title "Reset template selection"
                        prop.className "swt:btn swt:btn-neutral swt:btn-square"
                        prop.onClick (fun _ ->
                            Messages.Protocol.Msg.RemoveSelectedProtocols
                            |> Messages.ProtocolMsg
                            |> dispatch
                        )
                        prop.children [ Swate.Components.Icons.Delete() ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member TemplateSelect(model: Model, dispatch) =

        React.useEffectOnce (fun _ -> Messages.Protocol.GetAllProtocolsRequest |> Messages.ProtocolMsg |> dispatch)

        Swate.Components.TemplateFilter.TemplateFilterProvider(
            React.fragment [

                Templates.ImportTemplatesBtn(model, dispatch)

                Swate.Components.TemplateFilter.TemplateFilter(
                    model.ProtocolState.Templates,
                    templateSearchClassName = "swt:grow"
                )

                Swate.Components.TemplateFilter.FilteredTemplateRenderer(fun filteredTemplates ->
                    Templates.DisplayTemplates(filteredTemplates, model, dispatch, ?maxheight = Some(length.px 600))
                )
            ]
        )

    [<ReactComponent>]
    static member TableSelect(model: Model, dispatch) =

        React.useEffectOnce (fun _ -> Messages.Protocol.GetAllProtocolsRequest |> Messages.ProtocolMsg |> dispatch)

        Swate.Components.TemplateFilter.TemplateFilterProvider(
            React.fragment [

                Templates.ImportTemplatesBtn(model, dispatch)

                Swate.Components.TemplateFilter.TemplateFilter(
                    model.ProtocolState.Templates,
                    templateSearchClassName = "swt:grow"
                )

                Swate.Components.TemplateFilter.FilteredTemplateRenderer(fun filteredTemplates ->
                    Templates.DisplayTemplates(filteredTemplates, model, dispatch, ?maxheight = Some(length.px 600))
                )
            ]
        )

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =
        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "Templates"

            SidebarComponents.SidebarLayout.Description(
                React.fragment [
                    Html.p [
                        Html.b "Search the database for templates."
                        Html.text " The building blocks from these templates can be inserted into the Swate table. "
                    ]
                ]
            )

            SidebarComponents.SidebarLayout.LogicContainer [ Templates.TemplateSelect(model, dispatch) ]
        ]