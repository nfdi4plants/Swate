namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open Feliz
open Swate.Components.Shared

[<RequireQualifiedAccess>]
type GraphObjectDetails =

    [<ReactComponent>]
    static member private Section(title: string, children: ReactElement list) =
        Html.div [
            prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
            prop.children [
                Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-3"; prop.text title ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-3"
                    prop.children children
                ]
            ]
        ]

    [<ReactComponent>]
    static member private PropertyTable(rows: (string * string) list) =
        Html.dl [
            prop.className "swt:grid swt:grid-cols-1 swt:gap-y-3"
            prop.children [
                for label, value in rows do
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.dt [
                                prop.className "swt:text-xs swt:font-semibold swt:uppercase swt:tracking-wide swt:opacity-60"
                                prop.text label
                            ]
                            Html.dd [
                                prop.className "swt:text-sm swt:whitespace-pre-wrap swt:break-words"
                                prop.text value
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private CaseExampleTable(examples: (string * string) list) =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3"
            prop.children [
                for caseName, example in examples do
                    Html.div [
                        prop.key $"{caseName}:{example}"
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.p [
                                prop.className "swt:text-xs swt:font-semibold swt:uppercase swt:tracking-wide swt:opacity-60"
                                prop.text caseName
                            ]
                            Html.pre [
                                prop.className
                                    "swt:rounded-md swt:border swt:border-base-300 swt:bg-base-200/70 swt:p-2 swt:text-xs swt:whitespace-pre-wrap swt:break-words"
                                prop.children [ Html.code [ prop.text example ] ]
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member Main(
        selectedNode: ArcExplorerNode option,
        selectedAncestors: ArcExplorerNode list,
        nodeMetaById: Map<string, GraphNodeMeta>,
        onSelectNodeId: string -> unit
    ) =
        match selectedNode with
        | None ->
            Html.div [
                prop.className
                    "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
                prop.children [
                    Html.p [
                        prop.className "swt:text-sm swt:text-center swt:opacity-70"
                        prop.text "Select a graph object to inspect details."
                    ]
                ]
            ]
        | Some selectedNode ->
            let selectedMeta = nodeMetaById |> Map.tryFind selectedNode.id
            let selectionKind =
                selectedMeta
                |> Option.map _.KindLabel
                |> Option.defaultValue (ArcExplorerNodeKind.label selectedNode.kind)
            let selectionRole =
                selectedMeta
                |> Option.map _.RoleLabel
                |> Option.defaultValue (if selectedNode.isReference then "Reference" else "Canonical")

            let selectionRows = [
                "Kind", selectionKind
                "Role", selectionRole
                "Path", (selectedNode.path |> Option.defaultValue "Virtual")
                "Children", (string selectedNode.children.Length)
            ]

            let ancestorItems =
                selectedAncestors
                |> List.filter (fun ancestor -> ancestor.isSelectable)

            let childItems =
                selectedNode.children
                |> List.filter (fun child -> child.isSelectable)

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                prop.children [
                    GraphObjectDetails.Section(
                        "Selection",
                        [
                            Html.h4 [
                                prop.className "swt:text-base swt:font-semibold swt:break-words"
                                prop.text selectedNode.name
                            ]
                            GraphObjectDetails.PropertyTable(selectionRows)
                        ]
                    )
                    match ancestorItems with
                    | [] -> Html.none
                    | items ->
                        GraphObjectDetails.Section(
                            "Ancestors",
                            [
                                Html.div [
                                    prop.className "swt:flex swt:flex-wrap swt:gap-2"
                                    prop.children (
                                        items
                                        |> List.map (fun ancestor ->
                                            Html.button [
                                                prop.key ancestor.id
                                                prop.type'.button
                                                prop.className
                                                    "swt:btn swt:btn-sm swt:btn-outline swt:normal-case"
                                                prop.text ancestor.name
                                                prop.onClick (fun _ -> onSelectNodeId ancestor.id)
                                            ])
                                    )
                                ]
                            ]
                        )
                    match childItems with
                    | [] -> Html.none
                    | items ->
                        GraphObjectDetails.Section(
                            "Children",
                            [
                                Html.div [
                                    prop.className "swt:flex swt:flex-wrap swt:gap-2"
                                    prop.children (
                                        items
                                        |> List.map (fun child ->
                                            Html.button [
                                                prop.key child.id
                                                prop.type'.button
                                                prop.className
                                                    "swt:btn swt:btn-sm swt:btn-outline swt:normal-case"
                                                prop.text child.name
                                                prop.onClick (fun _ -> onSelectNodeId child.id)
                                            ])
                                    )
                                ]
                            ]
                        )
                    match selectedMeta with
                    | Some meta ->
                        GraphObjectDetails.Section(
                            "Metadata",
                            [
                                GraphObjectDetails.PropertyTable(
                                    [
                                        "Kind", meta.KindLabel
                                        "Role", meta.RoleLabel
                                        yield! meta.Rows
                                    ]
                                )
                                match meta.Description with
                                | Some description ->
                                    Html.p [
                                        prop.className "swt:text-sm swt:opacity-80"
                                        prop.text description
                                    ]
                                | None -> Html.none
                            ]
                        )
                    | None ->
                        GraphObjectDetails.Section(
                            "Metadata",
                            [
                                Html.p [
                                    prop.className "swt:text-sm swt:opacity-70"
                                    prop.text "No metadata was registered for this graph node."
                                ]
                            ]
                        )
                    match selectedMeta with
                    | Some meta when List.isEmpty meta.CaseExamples |> not ->
                        GraphObjectDetails.Section(
                            "Case Examples",
                            [
                                GraphObjectDetails.CaseExampleTable(meta.CaseExamples)
                            ]
                        )
                    | _ -> Html.none
                ]
            ]
