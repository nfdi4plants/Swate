namespace Swate.Components

open Fable.Core
open Feliz
open ARCtrl

module TemplateMocks =

    let mkStella () =
        ARCtrl.Person(
            firstName = "Stella",
            lastName = "Eggels",
            email = "stella.eggels@example.com",
            orcid = "0000-0002-1825-0097"
        )

    let mkDominik () =
        ARCtrl.Person(
            firstName = "Dominik",
            lastName = "Brilhaus",
            email = "dominiks@example.com",
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

    open System

    /// This is a fable StringEnum and can be replaced by any `unbox` string
    [<StringEnum>]
    type FilterTokenType =
        | Tag
        | Repository
        | Organisation
        | Author
        | ORCID

    type FilterToken = {|
        Type: FilterTokenType
        NameText: string
        Id: string
        Payload: obj option
    |}

    let mkFullName (author: ARCtrl.Person) =
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
                Id = tag.NameText
                Payload = Some tag
            |})

        let erTags: seq<FilterToken> =
            templates
            |> ResizeArray
            |> ARCtrl.Templates.getDistinctEndpointRepositories
            |> Seq.map (fun repo -> {|
                Type = FilterTokenType.Repository
                NameText = repo.NameText
                Id = repo.NameText
                Payload = Some repo
            |})

        let authorsRefs: seq<FilterToken> =
            templates
            |> Seq.collect (fun template ->
                template.Authors
                |> Seq.collect (fun author ->
                    let fullname = mkFullName author

                    [
                        {|
                            Type = FilterTokenType.Author
                            NameText = fullname
                            Id = fullname
                            Payload = Some author
                        |}
                        if author.ORCID.IsSome then
                            {|
                                Type = FilterTokenType.ORCID
                                NameText = author.ORCID.Value
                                Id = author.ORCID.Value
                                Payload = Some author
                            |}
                    ]
                )
            )
            |> Seq.distinctBy (fun x -> x.Id)
            |> unbox

        let organisations: seq<FilterToken> =
            templates
            |> Seq.map (fun template -> template.Organisation)
            |> Seq.distinct
            |> Seq.map (fun org -> {|
                Type = FilterTokenType.Organisation
                NameText = org.ToString()
                Id = org.ToString()
                Payload = Some org
            |})

        ra.AddRange(tags)
        ra.AddRange(erTags)
        ra.AddRange(authorsRefs)
        ra.AddRange(organisations)
        ra

// let filter
//     (tagFilter: ResizeArray<OntologyAnnotation>, communityFilter: Organisation, searchString, templates: Template[])
//     =
//     promise {
//         let filterByTag (template: Template) =
//             if tagFilter.Count = 0 then
//                 true
//             else
//                 tagFilter
//                 |> Seq.exists (fun tag -> template.Tags |> Seq.exists (fun t -> t = tag))

//         let filterByCommunity (template: Template) = template.Organisation = communityFilter

//         let filterBySearchString (template: Template) =
//             if System.String.IsNullOrWhiteSpace searchString then
//                 true
//             else
//                 template.Name.Contains(searchString)
//                 || template.Authors
//                    |> Seq.exists (fun author -> mkFullName author |> fun x -> x.Contains searchString)

//         return
//             templates
//             |> Array.filter (fun template ->
//                 filterByTag template
//                 && filterByCommunity template
//                 && filterBySearchString template
//             )
//     }


[<Erase; Mangle(false)>]
type TemplateFilter =

    static member TagBadge(tag: OntologyAnnotation, ?key: obj) =
        Html.div [ prop.className "swt:badge swt:badge-secondary"; prop.text tag.NameText ]

    static member RepoBadge(repo: OntologyAnnotation, ?key: obj) =
        Html.div [ prop.className "swt:badge swt:badge-accent"; prop.text repo.NameText ]

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
                    prop.children [
                        for tag in template.Tags do
                            TemplateFilter.TagBadge(tag)
                        for repo in template.EndpointRepositories do
                            TemplateFilter.RepoBadge(repo)
                    ]
                ]
            ]
        ]

    [<ReactComponent(true)>]
    static member TemplateFilter(templates: Template[], ?key: obj) =

        /// This constant is used to display available tags in the combo box
        let filterTokens =
            React.useMemo ((fun () -> TemplateFilterAux.mkFilterTokens templates), [| box templates |])

        let inputValue, setInputValue = React.useState ""

        let searchFn =
            fun
                (props:
                    {|
                        item: TemplateFilterAux.FilterToken
                        search: string
                    |}) -> props.item.NameText.ToLower().Contains(props.search.ToLower())

        let transformFn = fun (item: TemplateFilterAux.FilterToken) -> item.NameText

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
                        "swt:list-row swt:rounded-none swt:p-1"
                        if props.isActive then
                            "swt:bg-base-content swt:text-base-300"
                    ]
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center"
                            prop.children [
                                match props.item.Type with
                                | TemplateFilterAux.FilterTokenType.Tag -> Icons.Tag("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.Repository -> Icons.CloudUpload("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.Organisation -> Icons.Institution("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.Author -> Icons.User("swt:size-4")
                                | TemplateFilterAux.FilterTokenType.ORCID -> Icons.ORCID("swt:size-4")
                            ]
                        ]
                        Html.div props.item.NameText
                        if props.item.Type = TemplateFilterAux.FilterTokenType.ORCID then
                            Html.div [
                                prop.text (TemplateFilterAux.mkFullName (unbox props.item.Payload: ARCtrl.Person))
                                prop.className "swt:ml-2 swt:text-xs swt:opacity-60"
                            ]
                    ]
                    yield! props.props
                ]

        ComboBox.ComboBox<TemplateFilterAux.FilterToken>(
            inputValue,
            setInputValue,
            Array.ofSeq filterTokens,
            searchFn,
            transformFn,
            itemRenderer = itemRenderFn
        )

    [<ReactComponent>]
    static member Entry() =

        let templates, setTemplates = React.useState (TemplateMocks.mkTemplates ())

        TemplateFilter.TemplateFilter(templates, key = "template-filter")

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