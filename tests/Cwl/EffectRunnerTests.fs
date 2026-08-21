module Swate.Tests.Cwl.EffectRunnerTests

open System.Collections.Generic
open Fable.Core
open Expecto
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.HostTypes
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.Effects
open Swate.Components.Shared.Cwl.State.EffectRunner

type private ImmediatePromise<'T>(result: Result<'T, obj>) =
    interface JS.Promise<'T> with
        member _.``then``(resolve, reject) =
            match result with
            | Ok value ->
                match resolve with
                | Some onResolve -> ImmediatePromise<'U>(Ok(onResolve value)) :> JS.Promise<'U>
                | None -> ImmediatePromise<'U>(Ok Unchecked.defaultof<'U>) :> JS.Promise<'U>
            | Error error ->
                match reject with
                | Some onReject -> ImmediatePromise<'U>(Ok(onReject error)) :> JS.Promise<'U>
                | None -> ImmediatePromise<'U>(Error error) :> JS.Promise<'U>

        member _.catch(reject) =
            match result with
            | Ok value -> ImmediatePromise<'T>(Ok value) :> JS.Promise<'T>
            | Error error ->
                match reject with
                | Some onReject -> ImmediatePromise<'T>(Ok(onReject error)) :> JS.Promise<'T>
                | None -> ImmediatePromise<'T>(Error error) :> JS.Promise<'T>

module private TestPorts =
    let private resolved value =
        ImmediatePromise<_>(Ok value) :> JS.Promise<_>

    let private defaultDialog = { Canceled = true; FilePath = None }

    let private defaultLoad = {
        Success = false
        Yaml = None
        ResolvedYaml = None
        FilePath = ""
        Error = Some "Not configured"
    }

    let private defaultSave = {
        Success = false
        FilePath = ""
        Error = Some "Not configured"
    }

    let private defaultTimerPort = {
        SetTimeout =
            fun _ callback ->
                callback ()
                0.0
        ClearTimeout = ignore
    }

    let withSave save = {
        HostApi = {
            ShowOpenDialog = fun () -> resolved defaultDialog
            ShowSaveDialog = fun () -> resolved defaultDialog
            LoadCwlFile = fun _ -> resolved defaultLoad
            SaveCwlFile = save
        }
        Timers = defaultTimerPort
    }

    let empty () =
        withSave (fun _ _ -> resolved defaultSave)

let effectRunnerTests =
    testList "Renderer effect runner" [
        test "FocusMainWindow effect is a no-op because Swate hosts do not implement window focus" {
            let dispatched = List<AppAction>()
            let ports = TestPorts.empty ()

            run ports dispatched.Add (FocusMainWindow "session.entry")

            Expect.equal dispatched.Count 0 "Focus effect should not dispatch any document action"
        }

        test "SaveCwlFile effect dispatches SaveSucceeded with request id and revision" {
            let requestId = System.Guid.Parse "11111111-1111-1111-1111-111111111111"
            let revision = Revision 3
            let dispatched = List<AppAction>()

            let ports =
                TestPorts.withSave (fun _path _yaml ->
                    ImmediatePromise<_>(
                        Ok {
                            Success = true
                            FilePath = "saved.cwl"
                            Error = None
                        }
                    )
                    :> JS.Promise<_>
                )

            run ports dispatched.Add (SaveCwlFile(requestId, revision, "saved.cwl", "class: CommandLineTool"))

            Expect.containsAll
                dispatched
                [ SaveSucceeded(requestId, revision, "saved.cwl") ]
                "Save effect should dispatch SaveSucceeded with the same request id and revision"
        }
    ]

[<Tests>]
let allTests = effectRunnerTests
