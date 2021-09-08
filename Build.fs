open Fake.Core
open Fake.IO

open Helpers
open System

initializeContext()

let sharedPath = Path.getFullName "src/Shared"
let serverPath = Path.getFullName "src/Server"
let clientPath = Path.getFullName "src/Client"
let deployPath = Path.getFullName "deploy"
let sharedTestsPath = Path.getFullName "tests/Shared"
let serverTestsPath = Path.getFullName "tests/Server"
let clientTestsPath = Path.getFullName "tests/Client"

let dockerComposePath = Path.getFullName ".db\docker-compose.yml"

module ProjectInfo =
    
    let gitOwner = "nfdi4plants"
    let gitName = "Swate"

module ReleaseNoteTasks =

    open Fake.Extensions.Release

    let createAssemblyVersion = Target.create "createvfs" (fun _ ->
        AssemblyVersion.create ProjectInfo.gitName
    )

    let updateReleaseNotes = Target.create "release" (fun config ->
        Release.exists()

        Release.update(ProjectInfo.gitOwner, ProjectInfo.gitName, config)

        let newRelease = ReleaseNotes.load "RELEASE_NOTES.md"
        
        let releaseDate =
            if newRelease.Date.IsSome then newRelease.Date.Value.ToShortDateString() else "WIP"
        
        Fake.DotNet.AssemblyInfoFile.createFSharp  "src/Server/Version.fs"
            [   Fake.DotNet.AssemblyInfo.Title "SWATE"
                Fake.DotNet.AssemblyInfo.Version newRelease.AssemblyVersion
                Fake.DotNet.AssemblyInfo.Metadata ("ReleaseDate",releaseDate)
            ]

        Trace.trace "Update Version.fs done!"
        
        /// Update maniefest.xmls
        Trace.trace "Update manifest.xml"
        
        let _ =
            let newVer = sprintf "<Version>%i.%i.%i</Version>" newRelease.SemVer.Major newRelease.SemVer.Minor newRelease.SemVer.Patch
            Shell.regexReplaceInFilesWithEncoding
                "<Version>[0-9]+.[0-9]+.[0-9]+</Version>"
                newVer
                System.Text.Encoding.UTF8
                [
                    (Path.combine __SOURCE_DIRECTORY__ @".assets\assets\manifest.xml")
                    (Path.combine __SOURCE_DIRECTORY__ "manifest.xml")
                ]
        
        Trace.trace "Update manifest.xml done!"
    )

    let githubDraft = Target.create "GithubDraft" (fun config ->

        let body = "We are ready to go for the first release!"

        Github.draft(
            ProjectInfo.gitOwner,
            ProjectInfo.gitName,
            (Some body),
            None,
            config
        )
    )

Target.create "InstallOfficeAddinTooling" (fun _ ->

    printfn "Installing office addin tooling"

    run npm "install -g office-addin-dev-certs" __SOURCE_DIRECTORY__
    run npm "install -g office-addin-debugging" __SOURCE_DIRECTORY__
    run npm "install -g office-addin-manifest" __SOURCE_DIRECTORY__
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
    run npx "office-addin-dev-certs install --days 365" __SOURCE_DIRECTORY__

    let certPath =
        Path.combine
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            ".office-addin-dev-certs/ca.crt"
        

    let psi = new System.Diagnostics.ProcessStartInfo(FileName = certPath, UseShellExecute = true)
    System.Diagnostics.Process.Start(psi) |> ignore
)

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployPath
    run dotnet "fable clean --yes" clientPath // Delete *.fs.js files created by Fable
)

Target.create "InstallClient" (fun _ ->
    Trace.trace "Node version:"
    run node "--version" "."
    Trace.trace "Npm version:"
    run npm "--version"  "."
    run npm "install" "."
)

Target.create "Bundle" (fun _ ->
    [ "server", dotnet $"publish -c Release -o \"{deployPath}\"" serverPath
      "client", dotnet "fable --run webpack -p" clientPath ]
    |> runParallel
)

Target.create "Run" (fun _ ->
    run dotnet "build" sharedPath
    [ "server", dotnet "watch run" serverPath
      "client", dotnet "fable watch --run webpack-dev-server" clientPath ]
    |> runParallel
)

Target.create "officedebug" (fun _ ->
    run dotnet "build" sharedPath
    [ "server", dotnet "watch run" serverPath
      "client", dotnet "fable watch --run webpack-dev-server" clientPath
      /// start up mysql db from docker-compose
      "database", dockerCompose $"-f {dockerComposePath} up" __SOURCE_DIRECTORY__
      /// sideload webapp in excel
      "officedebug", npx "office-addin-debugging start manifest.xml desktop --debug-method web" __SOURCE_DIRECTORY__
      ]
    |> runParallel
)

Target.create "RunTests" (fun _ ->
    run dotnet "build" sharedTestsPath
    [ "server", dotnet "watch run" serverTestsPath
      "client", dotnet "fable watch --run webpack-dev-server --config ../../webpack.tests.config.js" clientTestsPath ]
    |> runParallel
)

Target.create "run-db" (fun _ ->
    run dockerCompose $"-f {dockerComposePath} up" __SOURCE_DIRECTORY__
)

Target.create "Format" (fun _ ->
    run dotnet "fantomas . -r" "src"
)

Target.create "Setup" ignore 

Target.create "Ignore" (fun _ ->
    Trace.trace "Hit ignore"
)

let testFake = Target.create "testfake" (fun config ->
    Trace.trace "Hit testfake"
    Trace.traceImportant (sprintf "%A" config.Context.Arguments)
    Trace.trace "Finish test"
)

open Fake.Core.TargetOperators

let dependencies = [
    "Clean"
        ==> "InstallClient"
        ==> "Bundle"

    "Clean"
        ==> "InstallClient"
        ==> "Run"

    "Clean"
        ==> "InstallClient"
        ==> "officedebug"

    "InstallClient"
        ==> "RunTests"

    "InstallOfficeAddinTooling"
        ==> "WebpackConfigSetup"
        ==> "LocalConnectionStringSetup"
        ==> "CreateDevCerts"
        ==> "SetLoopbackExempt"
        ==> "Setup"

    "run-db"

    "release"

    "testfake"
    "Ignore"
]

[<EntryPoint>]
let main args = runOrDefault args