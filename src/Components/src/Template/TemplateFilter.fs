namespace Swate.Components

open Fable.Core
open Feliz
open ARCtrl

module TemplateMocks =

    let mkStella () =
        ARCtrl.Person(
            firstName = "Riley",
            lastName = "Morgan",
            email = "riley.morgan@samplemail.com",
            orcid = "0000-0002-1825-0097"
        )

    let mkDominik () =
        ARCtrl.Person(
            firstName = "Jordan",
            lastName = "Avery",
            email = "jordan.avery@webmail.org",
            orcid = "0000-0002-1825-0098"
        )

    let mkMax () =
        ARCtrl.Person(
            firstName = "Max",
            lastName = "Mustermann",
            email = "max.mustermann@example.com",
            orcid = "0000-0002-1825-0099"
        )

    let mkLisa () =
        ARCtrl.Person(
            firstName = "Lisa",
            lastName = "Müller",
            email = "lisa.mueller@example.com",
            orcid = "0000-0002-1825-0100"
        )

    let mkTemplates () = [|

        ARCtrl.Template.create (
            id = System.Guid.NewGuid(),
            name = "Template 1",
            description = "This is the first template.",
            organisation = Organisation.Other "Custom Org",
            table = ARCtrl.ArcTable.init (name = "Table 1"),
            version = "1.0.0",
            tags =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("tag1", "t", "t:00001")
                        OntologyAnnotation.create ("tag2", "t", "t:00002")
                        OntologyAnnotation.create ("tag3", "t", "t:00003")
                        OntologyAnnotation.create ("tag4", "t", "t:00004")
                    ]
                ),
            repos =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("repo1", "r", "r:00001")
                        OntologyAnnotation.create ("repo2", "r", "r:00002")
                        OntologyAnnotation.create ("repo3", "r", "r:00003")
                        OntologyAnnotation.create ("repo4", "r", "r:00004")
                    ]
                ),
            authors = ResizeArray<Person>([| mkStella (); mkDominik () |])
        )

        ARCtrl.Template.create (
            id = System.Guid.NewGuid(),
            name = "Template 2",
            description = "This is the second template.",
            organisation = Organisation.Other "Another Org",
            table = ARCtrl.ArcTable.init (name = "Table 2"),
            version = "1.0.1",
            tags =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("tagA", "t", "t:00005")
                        OntologyAnnotation.create ("tagB", "t", "t:00006")
                    ]
                ),
            repos =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("repoA", "r", "r:00005")
                        OntologyAnnotation.create ("repoB", "r", "r:00006")
                    ]
                ),
            authors = ResizeArray<Person>([| mkStella (); mkLisa () |])
        )

        ARCtrl.Template.create (
            id = System.Guid.NewGuid(),
            name = "Template 3",
            description = "This is the third template.",
            organisation = Organisation.DataPLANT,
            table = ARCtrl.ArcTable.init (name = "Table 3"),
            version = "2.0.0",
            tags =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("tagX", "t", "t:00007")
                        OntologyAnnotation.create ("tagY", "t", "t:00008")
                    ]
                ),
            repos =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("repoX", "r", "r:00007")
                        OntologyAnnotation.create ("repoY", "r", "r:00008")
                    ]
                ),
            authors = ResizeArray<Person>([| mkDominik (); mkLisa (); mkMax () |])
        )

        ARCtrl.Template.create (
            id = System.Guid.NewGuid(),
            name = "Template 4",
            description = "This is the fourth template.",
            organisation = Organisation.Other "Custom Org",
            table = ARCtrl.ArcTable.init (name = "Table 4"),
            version = "1.2.0",
            tags =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("tagAlpha", "t", "t:00009")
                        OntologyAnnotation.create ("tagBeta", "t", "t:00010")
                    ]
                ),
            repos =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("repoAlpha", "r", "r:00009")
                        OntologyAnnotation.create ("repoBeta", "r", "r:00010")
                    ]
                ),
            authors = ResizeArray<Person>([| mkStella (); mkDominik () |])
        )

        ARCtrl.Template.create (
            id = System.Guid.NewGuid(),
            name = "Template 5",
            description = "This is the fifth template.",
            organisation = Organisation.DataPLANT,
            table = ARCtrl.ArcTable.init (name = "Table 5"),
            version = "3.0.0",
            tags =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("tagGamma", "t", "t:00011")
                        OntologyAnnotation.create ("tagDelta", "t", "t:00012")
                    ]
                ),
            repos =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("repoGamma", "r", "r:00011")
                        OntologyAnnotation.create ("repoDelta", "r", "r:00012")
                    ]
                ),
            authors = ResizeArray<Person>([| mkDominik () |])
        )

        ARCtrl.Template.create (
            id = System.Guid.NewGuid(),
            name = "Template 6",
            description = "This is the sixth template.",
            organisation = Organisation.Other "Custom Org",
            table = ARCtrl.ArcTable.init (name = "Table 6"),
            version = "1.3.0",
            tags =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("tagEpsilon", "t", "t:00013")
                        OntologyAnnotation.create ("tagZeta", "t", "t:00014")
                    ]
                ),
            repos =
                ResizeArray<OntologyAnnotation>(
                    [
                        OntologyAnnotation.create ("repoEpsilon", "r", "r:00013")
                        OntologyAnnotation.create ("repoZeta", "r", "r:00014")
                    ]
                ),
            authors = ResizeArray<Person>([| mkLisa () |])
        )
    |]

module TemplateFilterAux =

    let FilteredTemplateContext =
        React.createContext<Context<Template[]>> (
            "TemplateFilterCtx",
            {
                data = [||]
                setData = fun _ -> console.warn "No setData function provided"
            }
        )

    open System

    /// This is a fable StringEnum and can be replaced by any `unbox` string
    [<StringEnum>]
    type FilterTokenType =
        | Tag
        | Repository
        | Name
        | Author
        | ORCID

    type FilterToken = {|
        Type: FilterTokenType
        NameText: string
        Id: string
        Payload: obj option
    |}

    module FilterTokenType =

        [<RequireQualifiedAccess>]
        module FilterTokenPrefixes =
            [<Literal>]
            let Tag = "tag"

            [<Literal>]
            let Repository = "repo"

            [<Literal>]
            let Author = "author"

            [<Literal>]
            let ORCID = "orcid"

        let toString (tokenType: FilterTokenType) =
            match tokenType with
            | Tag -> FilterTokenPrefixes.Tag
            | Repository -> FilterTokenPrefixes.Repository
            | Name -> "name"
            | Author -> FilterTokenPrefixes.Author
            | ORCID -> FilterTokenPrefixes.ORCID

        let toPrefix (tokenType: FilterTokenType) =
            match tokenType with
            | Tag -> FilterTokenPrefixes.Tag + ":"
            | Repository -> FilterTokenPrefixes.Repository + ":"
            | Name -> ""
            | Author -> FilterTokenPrefixes.Author + ":"
            | ORCID -> FilterTokenPrefixes.ORCID + ":"

        let fromPrefix (prefix: string) =
            match prefix.ToLower() with
            | FilterTokenPrefixes.Tag -> Tag
            | FilterTokenPrefixes.Repository -> Repository
            | FilterTokenPrefixes.Author -> Author
            | FilterTokenPrefixes.ORCID -> ORCID
            | "" -> Name
            | _ -> failwithf "Unknown filter token prefix: %s" prefix

        let fromString (str: string) =
            match str.ToLower() with
            | "tag" -> Tag
            | "repo" -> Repository
            | "name" -> Name
            | "author" -> Author
            | "orcid" -> ORCID
            | _ -> failwithf "Unknown filter token type: %s" str

    let mkFullAuthorName (author: ARCtrl.Person) =
        [ author.FirstName; author.LastName; author.MidInitials ]
        |> List.choose id
        |> String.concat " "

    let mkFilterTokens (templates: Template[]) =
        let ra = ResizeArray<FilterToken>()

        let tags: seq<FilterToken> =
            templates
            |> ResizeArray
            |> ARCtrl.Templates.getDistinctTags
            |> Seq.map (fun tag -> {|
                Type = FilterTokenType.Tag
                NameText = tag.NameText
                Id = string FilterTokenType.Tag + tag.NameText
                Payload = Some tag
            |})

        let erTags: seq<FilterToken> =
            templates
            |> ResizeArray
            |> ARCtrl.Templates.getDistinctEndpointRepositories
            |> Seq.map (fun repo -> {|
                Type = FilterTokenType.Repository
                NameText = repo.NameText
                Id = string FilterTokenType.Repository + repo.NameText
                Payload = Some repo
            |})

        let authorsRefs: seq<FilterToken> =
            templates
            |> Seq.collect (fun template ->
                template.Authors
                |> Seq.collect (fun author ->
                    let fullname = mkFullAuthorName author

                    [
                        {|
                            Type = FilterTokenType.Author
                            NameText = fullname
                            Id = string FilterTokenType.Author + fullname
                            Payload = Some author
                        |}
                        if author.ORCID.IsSome then
                            {|
                                Type = FilterTokenType.ORCID
                                NameText = author.ORCID.Value
                                Id = string FilterTokenType.ORCID + author.ORCID.Value
                                Payload = Some author
                            |}
                    ]
                )
            )
            |> Seq.distinctBy (fun x -> x.Id)
            |> unbox

        let names: seq<FilterToken> =
            templates
            |> Seq.map (fun template -> template.Name)
            |> Seq.distinct
            |> Seq.map (fun name -> {|
                Type = FilterTokenType.Name
                NameText = name
                Id = name
                Payload = Some name
            |})

        ra.AddRange(tags)
        ra.AddRange(erTags)
        ra.AddRange(authorsRefs)
        ra.AddRange(names)
        ra.Sort(fun a b -> String.Compare(a.NameText, b.NameText, StringComparison.OrdinalIgnoreCase))
        ra

    let filter =
        fun (templates: Template[]) (selectedOrgs: Organisation[]) (filterTokens: ResizeArray<FilterToken>) ->
            templates
            |> Array.filter (fun template ->
                let orgMatch =
                    if selectedOrgs.Length = 0 then
                        false
                    else
                        selectedOrgs |> Array.exists (fun org -> template.Organisation = org)

                let tokenMatch =
                    if filterTokens.Count = 0 then
                        true
                    elif orgMatch = false then
                        false
                    else
                        filterTokens
                        |> Seq.forall (fun token ->
                            match token.Type with
                            | FilterTokenType.Tag ->
                                template.Tags
                                |> Seq.exists (fun tag -> tag.NameText.ToLower().Contains(token.NameText.ToLower()))
                            | FilterTokenType.Repository ->
                                template.EndpointRepositories
                                |> Seq.exists (fun repo -> repo.NameText.ToLower().Contains(token.NameText.ToLower()))
                            | FilterTokenType.Name -> template.Name.ToLower().Contains(token.NameText.ToLower())
                            | FilterTokenType.Author ->
                                template.Authors
                                |> Seq.exists (fun author -> (mkFullAuthorName author) = token.NameText)
                            | FilterTokenType.ORCID ->
                                template.Authors
                                |> Seq.exists (fun author -> author.ORCID = Some token.NameText)
                        )

                orgMatch && tokenMatch
            )

[<Erase; Mangle(false)>]
type TemplateFilter =

    static member TokenBadge
        (token: TemplateFilterAux.FilterToken, remove: TemplateFilterAux.FilterToken -> unit, ?key: obj)
        =
        Html.div [
            prop.className "swt:h-(--size) swt:flex swt:items-center"
            prop.children [
                Html.div [
                    prop.key (token.Id)
                    prop.className [
                        "swt:badge swt:flex swt:items-center swt:gap-2 swt:bg-base-content/50 swt:text-base-300"
                    ]
                    prop.children [
                        Html.div token.NameText
                        Html.button [
                            prop.className "swt:btn swt:btn-ghost swt:btn-circle swt:btn-xs"
                            prop.onClick (fun _ -> remove (token))
                            prop.children [ Icons.Close() ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member TemplateItem(template: Template, ?key: obj) =
        Html.li [
            prop.key (template.Id)
            prop.className "swt:py-2 swt:px-4"
            prop.children [
                Html.div [
                    prop.className "swt:grow swt:font-semibold swt:text-lg swt:truncate"
                    prop.text template.Name
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-2 swt:items-center swt:text-xs swt:opacity-60"
                    prop.children [
                        Html.div [
                            prop.className "swt:text-xs font-semibold"
                            prop.text (template.Organisation.ToString())
                        ]
                        Html.div [
                            prop.className "swt:text-xs"
                            prop.text (sprintf "Version: %s" template.Version)
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-2 swt:items-center swt:text-xs swt:opacity-60"
                    prop.children [
                        for author in template.Authors do
                            let givenName =
                                [ author.FirstName; author.LastName; author.MidInitials ]
                                |> List.choose id
                                |> String.concat " "

                            Html.div [ prop.text givenName ]
                    ]
                ]
                Html.div [ prop.className "swt:py-2 swt:text-xs"; prop.text template.Description ]
                Html.div [
                    prop.className
                        "swt:flex swt:flex-row swt:flex-wrap swt:gap-2 swt:items-center swt:text-xs swt:opacity-60"
                // prop.children [
                //     for tag in template.Tags do
                //         TemplateFilter.TagBadge(tag)
                //     for repo in template.EndpointRepositories do
                //         TemplateFilter.RepoBadge(repo)
                // ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member TemplateSearch
        (
            availableTokens: ResizeArray<TemplateFilterAux.FilterToken>,
            tokens: ResizeArray<TemplateFilterAux.FilterToken>,
            setTokens:
                (ResizeArray<TemplateFilterAux.FilterToken> -> ResizeArray<TemplateFilterAux.FilterToken>) -> unit,
            ?key: obj
        ) =

        let inputValue, setInputValue = React.useState ""

        let searchFn =
            fun
                (props:
                    {|
                        item: TemplateFilterAux.FilterToken
                        search: string
                    |}) ->
                let str = props.search.ToLower()
                props.item.NameText.ToLower().Contains(str)

        let transformFn = fun (item: TemplateFilterAux.FilterToken) -> item.NameText

        let onChangeFn =
            fun (_: int) (item: TemplateFilterAux.FilterToken) ->

                setInputValue ("")

                setTokens (fun tokens ->
                    if tokens.Contains item then
                        tokens
                    else
                        let next = ResizeArray(tokens)
                        next.Add item
                        next
                )

        let itemRenderFn =
            fun
                (props:
                    {|
                        index: int
                        isActive: bool
                        item: TemplateFilterAux.FilterToken
                        props: ResizeArray<IReactProperty>
                    |}) ->
                Html.li [
                    prop.className [
                        "swt:border-l-4 swt:border-transparent swt:list-row swt:rounded-none swt:p-1"
                        if props.isActive then
                            "swt:!border-primary swt:bg-base-content/10"
                    ]
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center"
                            prop.children [
                                match props.item.Type with
                                | TemplateFilterAux.FilterTokenType.Tag -> Icons.Tag("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.Repository -> Icons.CloudUpload("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.Name -> Icons.Header("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.Author -> Icons.User("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.ORCID -> Icons.ORCID("swt:size-4")
                            ]
                        ]
                        Html.div props.item.NameText
                        if props.item.Type = TemplateFilterAux.FilterTokenType.ORCID then
                            Html.div [
                                prop.text (TemplateFilterAux.mkFullAuthorName (unbox props.item.Payload: ARCtrl.Person))
                                prop.className "swt:ml-2 swt:text-xs swt:opacity-60"
                            ]
                    ]
                    yield! props.props
                ]

        let InputLeadingBadges =
            tokens
            |> Seq.map (fun token ->
                TemplateFilter.TokenBadge(
                    token,
                    (fun t ->
                        setTokens (fun tokens ->
                            let next = ResizeArray(tokens)
                            next.Remove t |> ignore
                            next
                        )
                    ),
                    key = token.Id
                )
            )
            |> React.fragment

        let onKeyDown =
            fun (ev: Browser.Types.KeyboardEvent) ->
                if ev.code = kbdEventCode.backspace && inputValue = "" then
                    // Remove the last token when backspace is pressed and input is empty
                    setTokens (fun tokens ->
                        if tokens.Count > 0 then
                            let next = ResizeArray(tokens)
                            next.RemoveAt(next.Count - 1) |> ignore
                            next
                        else
                            tokens
                    )
                else
                    ()

        let availableTokens =
            React.useMemo (
                (fun () ->
                    availableTokens
                    |> Array.ofSeq
                    |> Array.filter (fun token -> tokens.Contains token |> not)
                ),
                [| box availableTokens; box tokens |]
            )

        ComboBox.ComboBox<TemplateFilterAux.FilterToken>(
            inputValue,
            setInputValue,
            Array.ofSeq availableTokens,
            searchFn,
            transformFn,
            onChange = onChangeFn,
            itemRenderer = itemRenderFn,
            inputLeadingVisual = InputLeadingBadges,
            labelClassName =
                "swt:has-[:focus]:input-primary swt:flex-wrap swt:h-fit swt:min-h-(--size) swt:flex-row swt:w-fit swt:*:min-h-(--size) swt:*:w-auto swt:gap-y-0 swt:gap-x-1",
            onKeyDown = onKeyDown
        )


    [<ReactComponent>]
    static member OrganisationFilter
        (organisations: Organisation[], selectedIndices: Set<int>, setSelectedIndices: Set<int> -> unit, ?key: obj)
        =

        let communities: SelectItem<ARCtrl.Organisation>[] =
            React.useMemo (
                (fun () -> organisations |> Array.map (fun org -> {| label = org.ToString(); item = org |})),
                [| organisations |]
            )

        let TriggerRenderFn =
            fun _ ->
                Html.button [
                    prop.tabIndex -1
                    prop.className "swt:btn swt:btn-square swt:btn-info swt:pointer-events-none"
                    prop.children [ Html.div selectedIndices.Count; Icons.Institution(className = "swt:size-4") ]
                ]

        Select.Select(
            communities,
            selectedIndices,
            setSelectedIndices,
            triggerRenderFn = TriggerRenderFn,
            dropdownPlacement = FloatingUI.Placement.BottomEnd,
            middleware = [| FloatingUI.Middleware.flip (); FloatingUI.Middleware.offset (10) |]
        )

    /// <summary>
    /// This component is used to filter templates by search, community, and tags.
    ///
    /// <param name="templates">The list of templates to filter. This list should not be modified by this component.</param>
    /// <param name="key">An optional key for the component.</param>
    [<ReactComponent(true)>]
    static member TemplateFilter(templates: Template[], ?key: obj, ?setCommunityFilter) =

        let tokens, setTokens =
            React.useStateWithUpdater (ResizeArray<TemplateFilterAux.FilterToken>())

        let selectedOrgIndices, setSelectedOrgIndices = React.useState Set.empty<int>

        /// This context is used to provide the filtered templates to the rest of the application
        let filteredTemplatesCtx =
            React.useContext TemplateFilterAux.FilteredTemplateContext

        /// This constant is used to display available tags in the combo box
        let availableTokens =
            React.useMemo ((fun () -> TemplateFilterAux.mkFilterTokens templates), [| box templates |])

        /// This constant is used to display available communities in the community filter
        let availableCommunities: Organisation[] =
            React.useMemo (
                (fun () ->
                    templates
                    |> Array.map (fun template -> template.Organisation)
                    |> Array.distinct
                    |> Array.sortBy (fun org -> org.IsOfficial() |> not, org.ToString())
                ),
                [| templates |]
            )

        let officialIndices =
            availableCommunities
            |> Array.filter (fun org -> org.IsOfficial())
            |> Array.mapi (fun index _ -> index)
            |> Set.ofArray

        React.useEffect(
            (fun () ->
                if setCommunityFilter.IsSome && selectedOrgIndices.IsEmpty && templates.Length > 0 then
                    if officialIndices.IsEmpty then
                        setSelectedOrgIndices(Set.empty)
                    else
                        setSelectedOrgIndices(officialIndices)
            ), [| box templates; box availableCommunities |]
        )

        React.useEffect(
            (fun () ->
                if setCommunityFilter.IsSome && not selectedOrgIndices.IsEmpty then
                    selectedOrgIndices
                    |> List.ofSeq
                    |> List.map (fun index ->
                        availableCommunities.[index])
                    |> setCommunityFilter.Value
            ), [| box selectedOrgIndices |])

        let filter =
            React.useCallback (
                (fun (templates: Template[]) ->
                    let orgs =
                        selectedOrgIndices |> Seq.map (fun i -> availableCommunities.[i]) |> Array.ofSeq

                    TemplateFilterAux.filter templates orgs tokens
                ),
                [| box availableCommunities; box selectedOrgIndices; box tokens |]
            )

        React.useEffect (
            (fun () ->

                let nextTemplates = filter templates

                filteredTemplatesCtx.setData nextTemplates
            ),
            [| box selectedOrgIndices; box tokens |]
        )

        Html.div [
            prop.className "swt:flex swt:flex-row swt:gap-2"
            prop.children [
                TemplateFilter.TemplateSearch(availableTokens, tokens, setTokens, key = "template-filter")
                TemplateFilter.OrganisationFilter(
                    availableCommunities,
                    selectedOrgIndices,
                    setSelectedOrgIndices,
                    key = "community-filter"
                )
            ]
        ]

    [<ReactComponent>]
    static member TemplateFilterProvider(children: ReactElement) =
        let filteredTemplates, setFilteredTemplatesFn = React.useState ([||])

        React.contextProvider (
            TemplateFilterAux.FilteredTemplateContext,
            {
                data = filteredTemplates
                setData = setFilteredTemplatesFn
            },
            children
        )

    [<ReactComponent>]
    static member FilteredTemplateRenderer(children: Template[] -> ReactElement) =
        let filteredTemplatesCtx =
            React.useContext<Context<Template[]>> TemplateFilterAux.FilteredTemplateContext

        let templates = filteredTemplatesCtx.data

        children templates

    [<ReactComponent>]
    static member Entry() =

        let templates, _ = React.useState (TemplateMocks.mkTemplates)

        TemplateFilter.TemplateFilterProvider(
            React.fragment [
                TemplateFilter.TemplateFilter(templates, key = "template-filter-provider")
                TemplateFilter.FilteredTemplateRenderer(fun templates ->
                    Html.div [ prop.text (sprintf "%d templates found" templates.Length) ]
                )
            ]
        )

// Html.ul [
//     prop.className
//         "swt:bg-base-100 swt:rounded-box swt:shadow-md swt:max-w-lg swt:max-h-[500px] swt:overflow-y-scroll"
//     prop.children [

//         Html.li [
//             prop.className "swt:p-4 swt:pb-2 swt:text-xs swt:opacity-60 swt:tracking-wide"
//             prop.text (
//                 if loading then
//                     "...loading..."
//                 else
//                     $"{localTemplates.Length} templates found"
//             )
//         ]

//         for template in localTemplates do
//             TemplateFilter.TemplateItem(template, key = template.Id)
//     ]
// ]