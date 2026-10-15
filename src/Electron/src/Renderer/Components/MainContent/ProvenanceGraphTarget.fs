module Renderer.Components.MainContent.ProvenanceGraphTarget

open Feliz
open Swate.Components.Primitive
open Swate.Components.Shared.ProvenanceGraph.Types
open Swate.Components.Shared.ProvenanceGraph.BuildIndex

/// What the page shows. The index is built from the IPC data once, when the data arrives, because the IPC
/// copy has no lookups.
[<RequireQualifiedAccess>]
type private LoadState =
    | Loading
    | Failed of string
    | Loaded of ProvenanceIndex

/// Loads the provenance data once per page open and shows the graph.
[<ReactComponent(true)>]
let ProvenanceGraphTarget () =
    let state, setState = React.useState LoadState.Loading

    React.useEffect (
        (fun () ->
            let mutable isDisposed = false

            promise {
                let! result = Api.ipcArcVaultApi.loadProvenanceGraph ()

                if not isDisposed then
                    match result with
                    | Ok data -> setState (LoadState.Loaded(ProvenanceIndex.ofData data))
                    | Error exn -> setState (LoadState.Failed $"Could not load the provenance graph: {exn.Message}")
            }
            |> Promise.start

            fun () -> isDisposed <- true
        ),
        [||]
    )

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
        prop.testId "provenance-graph-page"
        prop.children [
            match state with
            | LoadState.Loading ->
                Html.div [
                    prop.className "swt:flex swt:size-full swt:items-center swt:justify-center"
                    prop.children [
                        Swate.Components.Primitive.LoadingSpinner.LoadingSpinner.LoadingSpinner(
                            size = DaisyuiSize.XL,
                            color = DaisyuiColors.Primary,
                            text = "Loading provenance graph..."
                        )
                    ]
                ]
            | LoadState.Failed message ->
                Html.div [
                    prop.className "swt:p-3"
                    prop.children [
                        Html.div [
                            prop.className "swt:alert swt:alert-error"
                            prop.children [
                                Html.i [
                                    prop.className "swt:iconify swt:fluent--error-circle-24-regular swt:size-5"
                                ]
                                Html.span message
                            ]
                        ]
                    ]
                ]
            | LoadState.Loaded index -> Swate.Components.Page.ProvenanceGraph.ProvenanceGraph.ProvenanceGraph(index)
        ]
    ]
