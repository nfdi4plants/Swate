module Renderer.Components.Composite.ArcOpening.Helper

open Fable.Core

type RequestGate = {
    tryBegin: unit -> bool
    finish: unit -> unit
}

/// Opens a selected ARC while reporting progress. Concurrent requests are ignored,
/// and progress is cleared even when opening fails.
let openPathWithProgress
    (requestGate: RequestGate)
    (arcPath: string)
    (openArcByPath: string -> JS.Promise<bool>)
    (setIsOpeningArc: bool -> unit)
    =
    promise {
        if requestGate.tryBegin () then
            try
                setIsOpeningArc true

                try
                    let! _ = openArcByPath arcPath
                    ()
                finally
                    setIsOpeningArc false
            finally
                requestGate.finish ()
    }

/// Selects an ARC directory before reporting progress, so cancelling the picker does
/// not briefly show the opening modal. Concurrent requests are ignored.
let openWithProgress
    (requestGate: RequestGate)
    (pickDirectory: unit -> JS.Promise<Result<string, exn>>)
    (openArcByPath: string -> JS.Promise<bool>)
    (onError: string -> unit)
    (setIsOpeningArc: bool -> unit)
    =
    promise {
        if requestGate.tryBegin () then
            try
                match! pickDirectory () with
                | Error error when error.Message = "Cancelled" -> ()
                | Error error -> onError error.Message
                | Ok arcPath ->
                    setIsOpeningArc true

                    try
                        let! _ = openArcByPath arcPath
                        ()
                    finally
                        setIsOpeningArc false
            finally
                requestGate.finish ()
    }
