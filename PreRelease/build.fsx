#r "paket: groupref build //"
#load "././.fake/build.fsx/intellisense.fsx"
#r "netstandard"


open System
open Fake
open Fake.Core
open Fake.DotNet
open Fake.IO
open Farmer
open Farmer.Builders

Target.initEnvironment ()

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg


let npmTool = platformTool "npm" "npm.cmd"
let npxTool = platformTool "npx" "npx.cmd"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore


Target.create "InstallClient" (fun _ ->
    printfn "Npm version:"
    runTool npmTool "--version"  __SOURCE_DIRECTORY__
    printfn "Npx version"
    runTool npxTool "--version" __SOURCE_DIRECTORY__
)

Target.create "OfficeDebugRemote" (fun _ ->
   
    let officeDebug = async {
         runTool npxTool "office-addin-debugging start manifest.xml desktop --debug-method web" __SOURCE_DIRECTORY__
    }

    officeDebug 
    |> Async.RunSynchronously
    |> ignore
)


open Fake.Core.TargetOperators


"InstallClient"
    ==> "OfficeDebugRemote"

Target.runOrDefaultWithArguments "OfficeDebugRemote"
