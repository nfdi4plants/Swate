module Renderer.Context.ArcStateContext

open Fable.Core
open Feliz
open Swate.Components.Hooks.UseMutableStore
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Shared

type ArcState = {
    arcFile: ArcFiles option
    mutate: (ArcFiles -> unit) -> unit
    replace: ArcFiles -> unit
    clear: unit -> unit
}

let ArcStateCtx =
    React.createContext<ArcState> {
        arcFile = None
        mutate = ignore
        replace = ignore
        clear = ignore
    }

[<Hook>]
let useArcStateCtx () = React.useContext ArcStateCtx

[<ReactComponent>]
let ArcStateProvider (persistArcFile: ArcFiles -> JS.Promise<Result<unit, exn>>, children: ReactElement) =
    let arcFile, mutateStore, setStore, version =
        useMutableStore (None: ArcFiles option)

    let errorModal = useErrorModalCtx ()

    let persist (nextArcFile: ArcFiles) =
        promise {
            match! persistArcFile nextArcFile with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not update ARC in memory"))
        }
        |> Promise.start

    let mutate (update: ArcFiles -> unit) =
        mutateStore (fun current ->
            match current with
            | Some arcFile ->
                update arcFile
                persist arcFile
            | None -> ()
        )

    let replace (nextArcFile: ArcFiles) =
        setStore (Some nextArcFile)
        persist nextArcFile

    let clear () = setStore None

    let state =
        React.useMemo (
            (fun _ -> {
                arcFile = arcFile
                mutate = mutate
                replace = replace
                clear = clear
            }),
            [| box arcFile; box version |]
        )

    ArcStateCtx.Provider(state, children)
