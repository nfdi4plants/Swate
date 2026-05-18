namespace Swate.Components.Page.ARCObjectExplorer.GraphExplorer

open Fable.Core
open Feliz
open Swate.Components.Page.ARCObjectExplorer
open Swate.Components.Page.ARCObjectExplorer.GraphExplorer.Model
open Swate.Components.Shared

type private GraphDetailValue =
    | Text of string
    | Code of string

type private GraphDetailRow = {
    Label: string
    Value: GraphDetailValue
}

module private GraphObjectDetailsHelper =

    let toRows
        (valueFactory: string -> GraphDetailValue)
        (rows: (string * string) list)
        =
        rows
        |> List.map (fun (label, value) -> {
            Label = label
            Value = valueFactory value
        })

    let valueText (value: GraphDetailValue) =
        match value with
        | GraphDetailValue.Text text
        | GraphDetailValue.Code text ->
            text

    let resolveSelectionKind
        (selectedNode: ArcExplorerNode)
        (selectedMeta: GraphNodeMeta option)
        =
        selectedMeta
        |> Option.map _.GraphKind
        |> Option.map GraphExplorerNodeKind.label
        |> Option.defaultValue (GraphExplorerNodeKind.label (GraphExplorerNodeKind.ofArcExplorerNodeKind selectedNode.kind))

    let resolveSelectionRole
        (selectedNode: ArcExplorerNode)
        (selectedMeta: GraphNodeMeta option)
        =
        selectedMeta
        |> Option.map _.RoleLabel
        |> Option.defaultValue (if selectedNode.isReference then "Reference" else "Canonical")

    let buildSelectionRows
        (selectedNode: ArcExplorerNode)
        (selectionKind: string)
        (selectionRole: string)
        =
        [
            "Type", selectionKind
            "Role", selectionRole
            "Path", (selectedNode.path |> Option.defaultValue "Virtual")
            "Children", (string selectedNode.children.Length)
        ]

    let selectableRelatives (nodes: ArcExplorerNode list) =
        nodes
        |> List.filter (fun node -> node.isSelectable)

[<Erase; Mangle(false)>]
type GraphObjectDetails =

    [<ReactComponent>]
    static member private ValueView(value: GraphDetailValue) =
        match value with
        | GraphDetailValue.Text value ->
            Html.span [
                prop.className "swt:text-sm swt:whitespace-pre-wrap swt:break-words"
                prop.text value
            ]
        | GraphDetailValue.Code value ->
            Html.pre [
                prop.className
                    "swt:rounded-md swt:border swt:border-base-300 swt:bg-base-200/70 swt:p-2 swt:text-xs swt:whitespace-pre-wrap swt:break-words"
                prop.children [ Html.code [ prop.text value ] ]
            ]

    [<ReactComponent>]
    static member private DetailsTable(rows: GraphDetailRow list) =
        Html.table [
            prop.className "swt:table swt:table-xs swt:w-full"
            prop.children [
                Html.tbody [
                    prop.children [
                        for rowIndex, row in rows |> List.indexed do
                            let valueKey = GraphObjectDetailsHelper.valueText row.Value

                            Html.tr [
                                prop.key $"{rowIndex}:{row.Label}:{valueKey}"
                                prop.children [
                                    Html.th [
                                        prop.className "swt:w-40 swt:align-top swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"
                                        prop.text row.Label
                                    ]
                                    Html.td [
                                        prop.children [ GraphObjectDetails.ValueView(row.Value) ]
                                    ]
                                ]
                            ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private RelationSection(
        title: string,
        items: ArcExplorerNode list,
        onSelectNodeId: string -> unit
    ) =
        match items with
        | [] ->
            Html.none
        | items ->
            ArcObjectDetailsLayout.Section(
                title,
                [
                    Html.div [
                        prop.className "swt:flex swt:flex-wrap swt:gap-2"
                        prop.children (
                            items
                            |> List.map (fun item ->
                                Html.button [
                                    prop.key item.id
                                    prop.type'.button
                                    prop.className
                                        "swt:btn swt:btn-sm swt:btn-outline swt:normal-case"
                                    prop.text item.name
                                    prop.onClick (fun _ -> onSelectNodeId item.id)
                                ])
                        )
                    ]
                ]
            )

    [<ReactComponent>]
    static member GraphObjectDetails(
        selectedNode: ArcExplorerNode option,
        selectedAncestors: ArcExplorerNode list,
        nodeMetaById: Map<string, GraphNodeMeta>,
        onSelectNodeId: string -> unit
    ) =
        match selectedNode with
        | None ->
            ArcObjectDetailsLayout.EmptyState("Select a graph object to inspect details.")
        | Some selectedNode ->
            let selectedMeta = nodeMetaById |> Map.tryFind selectedNode.id

            let selectionKind =
                GraphObjectDetailsHelper.resolveSelectionKind
                    selectedNode
                    selectedMeta

            let selectionRole =
                GraphObjectDetailsHelper.resolveSelectionRole
                    selectedNode
                    selectedMeta

            let selectionRows =
                GraphObjectDetailsHelper.buildSelectionRows
                    selectedNode
                    selectionKind
                    selectionRole

            let ancestorItems =
                GraphObjectDetailsHelper.selectableRelatives
                    selectedAncestors

            let childItems =
                GraphObjectDetailsHelper.selectableRelatives
                    selectedNode.children

            let metadataSectionContent =
                match selectedMeta with
                | Some meta ->
                    let metadataRows =
                        [
                            "Kind", (GraphExplorerNodeKind.label meta.GraphKind)
                            "Role", meta.RoleLabel
                            yield! meta.Rows
                        ]
                        |> GraphObjectDetailsHelper.toRows GraphDetailValue.Text

                    [
                        GraphObjectDetails.DetailsTable(metadataRows)
                        match meta.Description with
                        | Some description ->
                            Html.p [
                                prop.className "swt:text-sm swt:opacity-80"
                                prop.text description
                            ]
                        | None ->
                            Html.none
                    ]
                | None ->
                    [
                        Html.p [
                            prop.className "swt:text-sm swt:opacity-70"
                            prop.text "No metadata was registered for this graph node."
                        ]
                    ]

            let caseExamplesSection =
                selectedMeta
                |> Option.filter (fun meta -> List.isEmpty meta.CaseExamples |> not)
                |> Option.map (fun meta ->
                    ArcObjectDetailsLayout.Section(
                        "Case Examples",
                        [
                            GraphObjectDetails.DetailsTable(meta.CaseExamples |> GraphObjectDetailsHelper.toRows GraphDetailValue.Code)
                        ]
                    ))
                |> Option.defaultValue Html.none

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                prop.children [
                    ArcObjectDetailsLayout.Section(
                        "Selection",
                        [
                            Html.h4 [
                                prop.className "swt:text-base swt:font-semibold swt:break-words"
                                prop.text selectedNode.name
                            ]
                            GraphObjectDetails.DetailsTable(selectionRows |> GraphObjectDetailsHelper.toRows GraphDetailValue.Text)
                        ]
                    )
                    GraphObjectDetails.RelationSection(
                        "Ancestors",
                        ancestorItems,
                        onSelectNodeId
                    )
                    GraphObjectDetails.RelationSection(
                        "Children",
                        childItems,
                        onSelectNodeId
                    )
                    ArcObjectDetailsLayout.Section(
                        "Metadata",
                        metadataSectionContent
                    )
                    caseExamplesSection
                ]
            ]

