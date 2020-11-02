#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard"


open System
open Fake
open Fake.Core
open Fake.DotNet
open Fake.IO
open Farmer
open Farmer.Builders

Target.initEnvironment ()

let sharedPath = Path.getFullName "./src/Shared"
let serverPath = Path.getFullName "./src/Server"
let deployDir = Path.getFullName "./deploy"
let clientPath = Path.getFullName "./src/Client"
let clientDeployPath = Path.combine clientPath "deploy"
let sharedTestsPath = Path.getFullName "./tests/Shared"
let serverTestsPath = Path.getFullName "./tests/Server"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

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

let nodeTool = platformTool "node" "node.exe"
let npmTool = platformTool "npm" "npm.cmd"
let npxTool = platformTool "npx" "npx.cmd"
let dockerComposeTool = platformTool "docker-compose" "docker-compose.exe"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let npm args workingDir =
    let npmPath =
        match ProcessUtils.tryFindFileOnPath "npm" with
        | Some path -> path
        | None ->
            "npm was not found in path. Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
            |> failwith

    let arguments = args |> String.split ' ' |> Arguments.OfArgs

    Command.RawCommand (npmPath, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    Command.ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result = Fake.DotNet.DotNet.exec (Fake.DotNet.DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

Target.create "Clean" (fun _ ->
    [ deployDir
      clientDeployPath ]
    |> Shell.cleanDirs
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Npm version:"
    runTool npmTool "--version"  __SOURCE_DIRECTORY__
    runTool npmTool "install" __SOURCE_DIRECTORY__
)

Target.create "Build" (fun _ ->
    runDotNet "build" serverPath
    Shell.regexReplaceInFileWithEncoding
        "let app = \".+\""
       ("let app = \"" + release.NugetVersion + "\"")
        System.Text.Encoding.UTF8
        (Path.combine clientPath "Version.fs")
    runTool npxTool "webpack-cli -p" __SOURCE_DIRECTORY__
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runTool npxTool "webpack-dev-server" __SOURCE_DIRECTORY__
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "https://localhost:5000"
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [ if not safeClientOnly then yield server
          yield client
          if not vsCodeSession then yield browser ]

    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "StartMySqlDocker" (fun _ ->
    runTool dockerComposeTool "-f .db\docker-compose.yml up" __SOURCE_DIRECTORY__
)


Target.create "OfficeDebug" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let officeDebug = async {
         runTool npxTool "office-addin-debugging start manifest.xml desktop --debug-method web" __SOURCE_DIRECTORY__
    }
    let client = async {
        runTool npxTool "webpack-dev-server" __SOURCE_DIRECTORY__
    }

    let mySqlDocker = async {
        Trace.trace "Start MySql+Adminer Docker"
        runTool dockerComposeTool "-f .db\docker-compose.yml up" __SOURCE_DIRECTORY__
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [
          yield officeDebug 
          yield client
          yield mySqlDocker
          if not safeClientOnly then yield server
          ]
    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "OfficeDebugRemote" (fun _ ->
   
    let officeDebug = async {
         runTool npxTool "office-addin-debugging start tests/manifest.xml desktop --debug-method web" __SOURCE_DIRECTORY__
    }

    officeDebug 
    |> Async.RunSynchronously
    |> ignore
)

Target.create "InstallOfficeAddinTooling" (fun _ ->

    printfn "Installing office addin tooling"

    runTool npmTool "install -g office-addin-dev-certs" __SOURCE_DIRECTORY__
    runTool npmTool "install -g office-addin-debugging" __SOURCE_DIRECTORY__
    runTool npmTool "install -g office-addin-manifest" __SOURCE_DIRECTORY__
)

Target.create "WebpackConfigSetup" (fun _ ->
    let userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

    Shell.replaceInFiles
        [
            "{USERFOLDER}",userPath.Replace("\\","/")
        ]
        [
            (Path.combine __SOURCE_DIRECTORY__ "webpack.config.js")
        ]

)

// https://fake.build/core-targets.html#Targets-with-arguments
Target.create "LocalConnectionStringSetup" (fun conf ->

    let pwOpt =
        conf.Context.Arguments
        |> List.tryFind (fun x -> x.StartsWith "pw:")

    let msg =
        if pwOpt.IsNone then "Default MySql password set" else "Custom MySql password set."

    let pw =
        if pwOpt.IsNone then "example" else pwOpt.Value.Replace("pw:","").Trim()

    Shell.replaceInFiles
        [
            "{PASSWORD}",pw
        ]
        [
            (Path.combine __SOURCE_DIRECTORY__ ".db/docker-compose.yml")
            (Path.combine __SOURCE_DIRECTORY__ "src/Server/dev.json")
        ]
    Trace.trace msg
)

Target.create "SetLoopbackExempt" (fun _ ->
    Command.RawCommand("CheckNetIsolation.exe",Arguments.ofList [
        "LoopbackExempt"
        "-a"
        "-n=\"microsoft.win32webviewhost_cw5n1h2txyewy\""
    ])
    |> CreateProcess.fromCommand
    |> Proc.run
    |> ignore
)


Target.create "CreateDevCerts" (fun _ ->
    runTool npxTool "office-addin-dev-certs install --days 365" __SOURCE_DIRECTORY__

    let certPath =
        Path.combine
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            ".office-addin-dev-certs/ca.crt"
        

    let psi = new System.Diagnostics.ProcessStartInfo(FileName = certPath, UseShellExecute = true)
    System.Diagnostics.Process.Start(psi) |> ignore

)

//Target.create "Bundle" (fun _ ->
//    let serverDir = Path.combine deployDir "Server"
//    let clientDir = Path.combine deployDir "Client"
//    let publicDir = Path.combine clientDir "public"
//    let publishArgs = sprintf "publish -c Release -o \"%s\"" serverDir
//    runDotNet publishArgs serverPath

//    Shell.copyDir publicDir clientDeployPath FileFilter.allFiles
//)

Target.create "Bundle" (fun _ ->
    runDotNet (sprintf "publish -c Release -o \"%s\"" deployDir) serverPath
    npm "run build" "."
)

Target.create "Bundle-Linux" (fun _ ->
    runDotNet (sprintf "publish -c Release -r linux-x64 -o \"%s\"" deployDir) serverPath
    npm "run build" "."
)

Target.create "Setup" ignore 

Target.create "RunTests" (fun _ ->
    runDotNet "build" sharedTestsPath
    [ async { runDotNet "watch run" serverTestsPath }
      async { npm "run test:live" "." } ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

open Fake.Core.TargetOperators

"Clean"
    ==> "InstallClient"
    ==> "Build"

"Clean"
    ==> "InstallClient"
    ==> "Build"
    ==> "Bundle"

"Clean"
    ==> "InstallClient"
    ==> "Run"

"Clean"
    ==> "InstallClient"
    ==> "OfficeDebug"

"InstallOfficeAddinTooling"
    ==> "WebpackConfigSetup"
    ==> "LocalConnectionStringSetup"
    ==> "CreateDevCerts"
    ==> "SetLoopbackExempt"
    ==> "Setup"

"Clean"
    ==> "InstallClient"
    ==> "RunTests"

Target.runOrDefaultWithArguments "Bundle"
