namespace Swate.Components.Composite.Workspace

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components
open Swate.Components.Composite.Workspace.Types

[<Erase; Mangle(false)>]
type SplitDivider =

    [<ReactComponent>]
    static member SplitDivider
        (
            direction: SplitDirection,
            ratio: float,
            onRatioChange: float -> unit,
            panePath: string,
            ?key: string
        )
        =
        let dragging = React.useRef false

        let storageKey = Keys.mkLocalStorageKey "workspace" "split" panePath

        let (storedRatio, setStoredRatio) =
            React.useLocalStorage (storageKey, ratio)

        React.useEffect (
            (fun () ->
                if storedRatio <> ratio && storedRatio >= 0.15 && storedRatio <= 0.85 then
                    onRatioChange storedRatio
            ),
            [| box storedRatio |]
        )

        React.useEffectOnce (fun () ->

            let onMove (e: PointerEvent) =
                if dragging.current then
                    let parent = (e.target :?> HTMLElement).parentElement

                    match parent with
                    | null -> ()
                    | _ ->
                        let rect = parent.getBoundingClientRect ()
                        let newRatio =
                            match direction with
                            | SplitDirection.Horizontal ->
                                (e.clientX - rect.left) / rect.width
                            | SplitDirection.Vertical ->
                                (e.clientY - rect.top) / rect.height
                        let clamped = max 0.15 (min 0.85 newRatio)
                        onRatioChange clamped
                        setStoredRatio clamped

            let stop (_: PointerEvent) =
                dragging.current <- false

            Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
            Browser.Dom.document.addEventListener ("pointerup", unbox stop)

            FsReact.createDisposable (fun () ->
                Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.removeEventListener ("pointerup", unbox stop)
            )
        )

        Html.div [
            match key with
            | Some k -> prop.key k
            | None -> ()
            prop.onPointerDown (fun _ -> dragging.current <- true)
            prop.className [
                "swt:shrink-0 swt:select-none swt:transition-colors swt:hover:bg-primary swt:z-10"
                match direction with
                | SplitDirection.Horizontal -> "swt:w-1 swt:cursor-col-resize swt:h-full"
                | SplitDirection.Vertical -> "swt:h-1 swt:cursor-row-resize swt:w-full"
            ]
        ]
