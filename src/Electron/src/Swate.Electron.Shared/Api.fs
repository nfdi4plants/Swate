module Api

// TODO: This file MUST not be part of "SHARED", this MUST be part of the Renderer project, as it contains only Renderer-specific API calls. The "SHARED" project should only contain code that is shared between Renderer and Main processes, which is not the case here.

open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

open Fable.Core
open Fable.Core.JsInterop
open Fable.Remoting.Client
open Fable.Electron.Remoting.Renderer

let gitApi = Remoting.init |> Remoting.buildClient<IGitApi>
let arcVaultApi = Remoting.init |> Remoting.buildClient<IArcVaultsApi>

let templateApi: ITemplateAPIv1 =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITemplateAPIv1>


// TODO: Everything below here is bad practise and MUST be removed ASAP, as it is not type-safe and relies on stringly-typed IPC calls. Instead, all IPC calls MUST be defined in a type-safe manner using Fable.Remoting, similar to how the Git API is defined above. This will ensure that all IPC calls are type-checked at compile time and will prevent runtime errors due to incorrect argument types or missing arguments.

// Event-first IPC methods must be invoked from renderer without sending a placeholder event argument.
let openARC () : JS.Promise<Result<string, exn>> = emitJsExpr arcVaultApi "$0.openARC()"

let createARC (identifier: string) : JS.Promise<Result<string, exn>> =
    emitJsExpr (arcVaultApi, identifier) "$0.createARC($1)"

let getOpenPath () : JS.Promise<string option> =
    emitJsExpr arcVaultApi "$0.getOpenPath()"

let openFile (path: string) : JS.Promise<Result<PageState, exn>> =
    emitJsExpr (arcVaultApi, path) "$0.openFile($1)"

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