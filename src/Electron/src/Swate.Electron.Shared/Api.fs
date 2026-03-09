module Api

open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes

open Fable.Core
open Fable.Core.JsInterop
open Fable.Remoting.Client
open Fable.Electron.Remoting.Renderer


let arcVaultApi = Remoting.init |> Remoting.buildClient<IArcVaultsApi>

let templateApi: ITemplateAPIv1 =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITemplateAPIv1>

// Event-first IPC methods must be invoked from renderer without sending a placeholder event argument.
let openARC () : JS.Promise<Result<string, exn>> = emitJsExpr arcVaultApi "$0.openARC()"

let createARC (identifier: string) : JS.Promise<Result<string, exn>> =
    emitJsExpr (arcVaultApi, identifier) "$0.createARC($1)"

let getOpenPath () : JS.Promise<string option> =
    emitJsExpr arcVaultApi "$0.getOpenPath()"

let openFile (path: string) : JS.Promise<Result<PageState, exn>> =
    emitJsExpr (arcVaultApi, path) "$0.openFile($1)"

let openARCInNewWindow () : JS.Promise<Result<unit, exn>> =
    emitJsExpr arcVaultApi "$0.openARCInNewWindow()"

let focusExistingARCWindow (arcPath: string) : JS.Promise<Result<unit, exn>> =
    emitJsExpr (arcVaultApi, arcPath) "$0.focusExistingARCWindow($1)"

let getRecentARCs () =
    emitJsExpr arcVaultApi "$0.getRecentARCs()"

let pickPaths () : JS.Promise<Result<string [], exn>> =
    emitJsExpr arcVaultApi "$0.pickPaths()"

let saveArcFile (request: SaveArcFileRequest) : JS.Promise<Result<PageState, exn>> =
    emitJsExpr (arcVaultApi, request) "$0.saveArcFile($1)"

let resolveCloseRequest (decision: SaveBeforeQuitDecision) : JS.Promise<Result<unit, exn>> =
    emitJsExpr (arcVaultApi, decision) "$0.resolveCloseRequest($1)"

let writeFile (request: WriteFileRequest) : JS.Promise<Result<unit, exn>> =
    emitJsExpr (arcVaultApi, request) "$0.writeFile($1)"

let syncARC (request: SaveArcFileRequest) : JS.Promise<Result<unit, exn>> =
    emitJsExpr (arcVaultApi, request) "$0.syncARC($1)"

let runGitLfs (request: GitLfsRequest) : JS.Promise<Result<GitLfsResult, exn>> =
    emitJsExpr (arcVaultApi, request) "$0.runGitLfs($1)"

let cancelGitLfs (requestId: string) : JS.Promise<Result<string, exn>> =
    emitJsExpr (arcVaultApi, requestId) "$0.cancelGitLfs($1)"
