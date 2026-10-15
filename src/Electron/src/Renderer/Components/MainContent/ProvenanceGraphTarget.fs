module Renderer.Components.MainContent.ProvenanceGraphTarget

open Feliz
open Swate.Components.Primitive
open Swate.Components.Primitive.ErrorBoundary
open Swate.Components.Shared.ProvenanceGraph.Types
open Swate.Components.Shared.ProvenanceGraph.BuildIndex

/// What the page shows. The index is built from the IPC data once, when the data arrives, because the IPC
/// copy has no lookups.
[<RequireQualifiedAccess>]
type private LoadState =
    | Loading
    | Failed of string
    | Loaded of ProvenanceIndex

/// An alert with an icon, what failed and a hint. A page open shows the ARC as it was at that time, so the hint
/// sends the user to close the graph and open it again after a fix.
[<ReactComponent>]
let private ErrorAlert (message: string) =
    Html.div [
        prop.className "swt:w-full swt:min-w-0 swt:p-3"
        prop.testId "provenance-graph-error"
        prop.children [
            Html.div [
                prop.className "swt:alert swt:alert-error"
                prop.role.alert
                prop.children [
                    Html.i [
                        prop.className "swt:iconify swt:fluent--error-circle-24-regular swt:size-5"
                    ]
                    Html.div [
                        Html.p [ prop.className "swt:m-0"; prop.text message ]
                        Html.p [
                            prop.className "swt:m-0 swt:text-xs"
                            prop.text "After you fix the ARC, close and open the graph again."
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Loads the provenance data once per page open and shows the graph. A load error shows its message in the page.
/// The page catches the errors of its views itself. The app around the page keeps working.
[<ReactComponent(true)>]
let ProvenanceGraphTarget () =
    let state, setState = React.useState LoadState.Loading

    React.useEffect (
        (fun () ->
            let mutable isDisposed = false

            promise {
                // The call can reject as well as return an error, and neither may leave the page.
                let! result = Api.ipcArcVaultApi.loadProvenanceGraph () |> Promise.catch Error

                if not isDisposed then
                    match result with
                    | Ok data -> setState (LoadState.Loaded(ProvenanceIndex.ofData data))
                    | Error exn -> setState (LoadState.Failed $"Could not load the provenance graph: {exn.Message}")
            }
            |> Promise.catch (fun exn ->
                if not isDisposed then
                    setState (LoadState.Failed $"Could not load the provenance graph: {exn.Message}")
            )
            |> Promise.start

            fun () -> isDisposed <- true
        ),
        [||]
    )

    let content =
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
        | LoadState.Failed message -> ErrorAlert message
        | LoadState.Loaded index -> Swate.Components.Page.ProvenanceGraph.ProvenanceGraph.ProvenanceGraph(index)

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
        prop.testId "provenance-graph-page"
        prop.children [ content ]
    ]

/// The main content area of the provenance graph page. The request id changes with every open of the page, and a
/// new id mounts everything inside again, so every open starts from scratch. The boundary catches what the
/// target cannot catch itself, such as a chunk of the lazy target that fails to load, and shows what failed.
let ProvenanceGraphArea (requestId: string) (content: ReactElement) : ReactElement =
    React.KeyedFragment(
        requestId,
        [
            ErrorBoundary.ErrorBoundary(
                content,
                (fun error -> ErrorAlert $"The provenance graph could not be shown: {error.Message}")
            )
        ]
    )
