module ElectronRenderer.ArcStateContextTests

open Browser.Dom
open Fable.Core
open Feliz
open ARCtrl
open Renderer.Context.ArcStateContext
open Swate.Components.Primitive.ErrorModal
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Shared
open Vitest

let rec private waitUntil (predicate: unit -> bool, attempts: int) = promise {
    if predicate () then
        return ()
    elif attempts <= 0 then
        failwith "Timed out waiting for React effect."
    else
        do! Promise.sleep 1
        return! waitUntil (predicate, attempts - 1)
}

let private waitForEffect predicate = waitUntil (predicate, 50)

[<ReactComponent>]
let private ArcStateProbe (onArcFile: ArcFiles option -> unit, expose: ArcState -> unit) =
    let arcStateCtx = useArcStateCtx ()

    React.useEffect ((fun () -> expose arcStateCtx), [| box arcStateCtx |])

    React.useEffect ((fun () -> onArcFile arcStateCtx.arcFile), [| box arcStateCtx.arcFile |])

    Html.none

let private makeAssay identifier =
    let assay = ArcAssay.init identifier
    assay.AddTable(ArcTable.init "Test Table")
    ArcFiles.Assay assay

let private renderProvider persist onArcFile expose =
    let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
    document.body.appendChild container |> ignore
    let root = ReactDOM.createRoot container
    root.render (ArcStateProvider(persist, ArcStateProbe(onArcFile, expose)))
    root, container

Vitest.describe (
    "ArcStateContext",
    fun () ->
        Vitest.test (
            "replace publishes and persists a new arc file",
            fun () -> promise {
                let arcFile = makeAssay "replace"
                let persisted = ResizeArray<ArcFiles>()
                let observed = ResizeArray<ArcFiles option>()
                let mutable arcState = Unchecked.defaultof<ArcState>

                let root, container =
                    renderProvider
                        (fun arcFile -> promise {
                            persisted.Add arcFile
                            return Ok()
                        })
                        observed.Add
                        (fun state -> arcState <- state)

                try
                    do! waitForEffect (fun () -> observed.Count > 0)
                    arcState.replace arcFile
                    do! waitForEffect (fun () -> persisted.Count = 1 && observed |> Seq.contains (Some arcFile))
                    Vitest.expect(System.Object.ReferenceEquals(persisted.[0], arcFile)).toBe (true)
                finally
                    root.unmount ()
                    container.remove ()
            }
        )

        Vitest.test (
            "mutate applies in-place updates and persists the same reference",
            fun () -> promise {
                let arcFile = makeAssay "mutate"
                let persisted = ResizeArray<ArcFiles>()
                let observed = ResizeArray<ArcFiles option>()
                let mutable arcState = Unchecked.defaultof<ArcState>

                let root, container =
                    renderProvider
                        (fun arcFile -> promise {
                            persisted.Add arcFile
                            return Ok()
                        })
                        observed.Add
                        (fun state -> arcState <- state)

                try
                    do! waitForEffect (fun () -> observed.Count > 0)
                    arcState.replace arcFile
                    do! waitForEffect (fun () -> persisted.Count = 1)
                    arcState.mutate (fun current -> current.Tables().[0].AddColumn(CompositeHeader.Comment "Added"))
                    do! waitForEffect (fun () -> persisted.Count = 2)
                    Vitest.expect(System.Object.ReferenceEquals(persisted.[1], arcFile)).toBe (true)
                    Vitest.expect(arcFile.Tables().[0].ColumnCount).toBe (1)
                finally
                    root.unmount ()
                    container.remove ()
            }
        )

        Vitest.test (
            "mutate is a no-op while no arc file is open",
            fun () -> promise {
                let persisted = ResizeArray<ArcFiles>()
                let observed = ResizeArray<ArcFiles option>()
                let mutable arcState = Unchecked.defaultof<ArcState>

                let root, container =
                    renderProvider
                        (fun arcFile -> promise {
                            persisted.Add arcFile
                            return Ok()
                        })
                        observed.Add
                        (fun state -> arcState <- state)

                try
                    do! waitForEffect (fun () -> observed.Count > 0)
                    arcState.mutate (fun _ -> failwith "Mutation must not run without an open arc file.")
                    do! Promise.sleep 5
                    Vitest.expect(persisted.Count).toBe (0)
                finally
                    root.unmount ()
                    container.remove ()
            }
        )

        Vitest.test (
            "clear empties the published arc file",
            fun () -> promise {
                let arcFile = makeAssay "clear"
                let persisted = ResizeArray<ArcFiles>()
                let observed = ResizeArray<ArcFiles option>()
                let mutable arcState = Unchecked.defaultof<ArcState>

                let root, container =
                    renderProvider
                        (fun arcFile -> promise {
                            persisted.Add arcFile
                            return Ok()
                        })
                        observed.Add
                        (fun state -> arcState <- state)

                try
                    do! waitForEffect (fun () -> observed.Count > 0)
                    arcState.replace arcFile
                    do! waitForEffect (fun () -> observed |> Seq.contains (Some arcFile))
                    arcState.clear ()
                    do! waitForEffect (fun () -> observed.Count > 0 && observed.[observed.Count - 1] = None)
                finally
                    root.unmount ()
                    container.remove ()
            }
        )

        Vitest.test (
            "persistence failures are reported to the error modal",
            fun () -> promise {
                let arcFile = makeAssay "error"
                let observed = ResizeArray<ArcFiles option>()
                let mutable arcState = Unchecked.defaultof<ArcState>

                let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
                document.body.appendChild container |> ignore
                let root = ReactDOM.createRoot container

                root.render (
                    ErrorModalProvider.ErrorModalProvider(
                        ArcStateProvider(
                            (fun _ -> promise { return Error(exn "IPC unavailable") }),
                            ArcStateProbe(observed.Add, (fun state -> arcState <- state))
                        )
                    )
                )

                try
                    do! waitForEffect (fun () -> observed.Count > 0)
                    arcState.replace arcFile

                    do! waitForEffect (fun () -> document.body.textContent.Contains("Could not update ARC in memory"))
                finally
                    root.unmount ()
                    container.remove ()
            }
        )
)
