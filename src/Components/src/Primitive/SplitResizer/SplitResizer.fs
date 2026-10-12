namespace Swate.Components.Primitive.SplitResizer

open Fable.Core
open Browser.Types
open Feliz
open Swate.Components

[<Erase; Mangle(false)>]
type SplitResizer =

    /// A handle that sits on the border between two panes. While the user drags it, the ratio of
    /// the pointer position within the container goes to onRatio, clamped to 0.15 to 0.85. The
    /// handle is absolutely positioned, so the parent places it on the border.
    [<ReactComponent>]
    static member SplitResizer
        (containerRef: IRefValue<option<HTMLElement>>, vertical: bool, onRatio: float -> unit, ?testId: string)
        =
        let draggingRef = React.useRef false
        let draggingUi, setIsDraggingUi = React.useState false
        let pointerPosition, setPointerPosition = React.useState (None: int option)
        let throttledPointerPosition = React.useThrottle (pointerPosition, 20)

        // Using effect to watch over throttledPointerPosition. This is the only dependency outputting a float, so no chance to enter loops or unexpected behavior.
        React.useEffect (
            (fun () ->
                match containerRef.current, pointerPosition with
                | None, _
                | Some null, _
                | _, None -> ()
                | Some container, Some pointerPos ->
                    let rect = container.getBoundingClientRect ()

                    let directionalPointerPosition =
                        if vertical then
                            (float pointerPos - rect.top) / rect.height
                        else
                            (float pointerPos - rect.left) / rect.width

                    let clampedRatio =
                        match throttledPointerPosition with
                        | Some pos -> Some(max 0.15 (min 0.85 directionalPointerPosition))
                        | None -> None

                    match clampedRatio with
                    | Some clamped -> onRatio clamped
                    | None -> ()
            ),
            [| box throttledPointerPosition |]
        )

        // Register the drag handler event
        // This is not throttled, therefore minimal logic inside the event listener
        React.useEffectOnce (
            (fun () ->
                let onMove (e: PointerEvent) =
                    if draggingRef.current then
                        setPointerPosition (Some(int (if vertical then e.clientY else e.clientX)))

                let stop (_: PointerEvent) =
                    draggingRef.current <- false
                    setIsDraggingUi false

                Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.addEventListener ("pointerup", unbox stop)

                FsReact.createDisposable (fun () ->
                    Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                    Browser.Dom.document.removeEventListener ("pointerup", unbox stop)
                )
            )
        )

        Html.div [
            if testId.IsSome then
                prop.testId testId.Value
            prop.onPointerDown (fun _ ->
                draggingRef.current <- true
                setIsDraggingUi true
            )
            prop.data ("active", if draggingUi then "true" else "false")
            prop.className [
                "swt:shrink-0 swt:select-none swt:transition-colors swt:bg-transparent swt:hover:bg-primary swt:z-10 swt:absolute swt:data-[active=true]:bg-primary"
                if vertical then
                    "swt:h-1.5 swt:cursor-row-resize swt:w-full"
                else
                    "swt:w-1.5 swt:cursor-col-resize swt:h-full swt:top-0 swt:bottom-0 swt:-left-1"
            ]
        ]
