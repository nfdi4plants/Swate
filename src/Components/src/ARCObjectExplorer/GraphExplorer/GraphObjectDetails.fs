namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open Fable.Core
open Feliz
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model
open Swate.Components.Shared

type private GraphDetailValue =
    | Text of string
    | Code of string

type private GraphDetailRow = {
    Label: string
    Value: GraphDetailValue
}

module private GraphObjectDetailsHelper =

    let textRows (rows: (string * string) list) =
        rows
        |> List.map (fun (label, value) -> {
            Label = label
            Value = GraphDetailValue.Text value
        })

    let codeRows (rows: (string * string) list) =
        rows
        |> List.map (fun (label, value) -> {
            Label = label
            Value = GraphDetailValue.Code value
        })

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
                        for row in rows do
                            let valueKey =
                                match row.Value with
                                | GraphDetailValue.Text value
                                | GraphDetailValue.Code value -> value

                            Html.tr [
                                prop.key $"{row.Label}:{valueKey}"
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
                selectedMeta
                |> Option.map _.KindLabel
                |> Option.defaultValue (ArcExplorerNodeKind.label selectedNode.kind)

            let selectionRole =
                selectedMeta
                |> Option.map _.RoleLabel
                |> Option.defaultValue (if selectedNode.isReference then "Reference" else "Canonical")

            let selectionRows = [
                "Type", selectionKind
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
                    ArcObjectDetailsLayout.Section(
                        "Selection",
                        [
                            Html.h4 [
                                prop.className "swt:text-base swt:font-semibold swt:break-words"
                                prop.text selectedNode.name
                            ]
                            GraphObjectDetails.DetailsTable(selectionRows |> GraphObjectDetailsHelper.textRows)
                        ]
                    )
                    match ancestorItems with
                    | [] -> Html.none
                    | items ->
                        ArcObjectDetailsLayout.Section(
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
                        ArcObjectDetailsLayout.Section(
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
                        ArcObjectDetailsLayout.Section(
                            "Metadata",
                            [
                                GraphObjectDetails.DetailsTable(
                                    [
                                        "Kind", meta.KindLabel
                                        "Role", meta.RoleLabel
                                        yield! meta.Rows
                                    ]
                                    |> GraphObjectDetailsHelper.textRows
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
                        ArcObjectDetailsLayout.Section(
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
                        ArcObjectDetailsLayout.Section(
                            "Case Examples",
                            [
                                GraphObjectDetails.DetailsTable(meta.CaseExamples |> GraphObjectDetailsHelper.codeRows)
                            ]
                        )
                    | _ -> Html.none
                ]
            ]
