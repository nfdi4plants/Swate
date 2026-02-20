module Api

open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Remoting.Renderer
open Fable.Core
open Fable.Core.JsInterop

let arcVaultApi = Remoting.init |> Remoting.buildClient<IArcVaultsApi>

// Event-first IPC methods must be invoked from renderer without sending a placeholder event argument.
let openARC () : JS.Promise<Result<string, exn>> =
    emitJsExpr arcVaultApi "$0.openARC()"

let createARC (identifier: string) : JS.Promise<Result<string, exn>> =
    emitJsExpr (arcVaultApi, identifier) "$0.createARC($1)"

let getOpenPath () : JS.Promise<string option> =
    emitJsExpr arcVaultApi "$0.getOpenPath()"

let openFile (path: string) : JS.Promise<Result<PreviewData, exn>> =
    emitJsExpr (arcVaultApi, path) "$0.openFile($1)"

let createExperimentFromLanding
    (request: CreateExperimentRequest)
    : JS.Promise<Result<CreateExperimentResponse, exn>> =
    emitJsExpr (arcVaultApi, request) "$0.createExperimentFromLanding($1)"

let saveArcFile (request: SaveArcFileRequest) : JS.Promise<Result<PreviewData, exn>> =
    emitJsExpr (arcVaultApi, request) "$0.saveArcFile($1)"
