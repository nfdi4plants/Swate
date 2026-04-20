module Renderer.Context.GitProgressSubscription

open Elmish
open Renderer.Context.GitWorkflow

let subscribe (_model: GitState) : Sub<Msg> = [
    [ "gitProgress" ],
    fun dispatch ->
        let dispose =
            Renderer.MainUpdateRendererBridge.subscribeGitProgressUpdate (fun progress ->
                dispatch (SetCurrentProgress(Some(mapProgress progress)))
            )

        { new System.IDisposable with
            member _.Dispose() = dispose ()
        }
]
