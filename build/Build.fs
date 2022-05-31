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

let developmentUrl = "https://localhost:3000"

module ProjectInfo =
    
    let gitOwner = "nfdi4plants"
    let gitName = "Swate"

module ReleaseNoteTasks =

    open Fake.Extensions.Release

    //let createAssemblyVersion = Target.create "createvfs" (fun _ ->
    //    AssemblyVersion.create ProjectInfo.gitName
    //)

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
                    (Path.combine __SOURCE_DIRECTORY__ @".assets\assets\core_manifest.xml")
                    (Path.combine __SOURCE_DIRECTORY__ @".assets\assets\experts_manifest.xml")
                    (Path.combine __SOURCE_DIRECTORY__ "manifest.xml")
                ]
        
        Trace.trace "Update manifest.xml done!"
    )

    let githubDraft = Target.create "GithubDraft" (fun config ->

        let zipFolderPath = @".assets"

        let cleanFolder =
            Trace.trace $"Clean existing .zip files from '{Path.getFullName(zipFolderPath)}'!"
            Fake.IO.DirectoryInfo.ofPath zipFolderPath
            |> Fake.IO.DirectoryInfo.getFiles
            |> Array.filter (fun x -> x.Name.EndsWith ".zip")
            |> Array.map (fun x -> x.FullName)
            |> File.deleteAll

        let assetPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__,@".assets\assets")
        let assetDir = Fake.IO.DirectoryInfo.ofPath assetPath

        let allFiles, quickstartFiles =
            let assetsPaths = Fake.IO.DirectoryInfo.getFiles assetDir
            assetsPaths |> Array.map (fun x -> x.FullName),
            assetsPaths |> Array.filter (fun x -> x.Name = "core_manifest.xml") |> Array.map (fun x -> x.FullName)

        Zip.zip assetDir.FullName ".assets\swate-win.zip" allFiles

        Zip.zip assetDir.FullName ".assets\swate-b-quickstart.zip" (quickstartFiles)

        Trace.trace "Assets zipped!"

        let bodyText =
            [
                ""
                "You can check our [release notes](https://github.com/nfdi4plants/Swate/blob/developer/RELEASE_NOTES.md) to see a list of all new features."
                "If you decide to test Swate in the current state, please take the time to set up a Github account to report your issues and suggestions [here](https://github.com/nfdi4plants/Swate/issues/new/choose)."
                ""
                "You can also search existing issues for solutions for your questions and/or discussions about your suggestions."
                ""
                "If you want to start using Swate follow these easy instructions: [Swate installation](https://github.com/nfdi4plants/Swate#installuse)"
                ""
            ] |> String.concat "\n"

        Github.draft(
            ProjectInfo.gitOwner,
            ProjectInfo.gitName,
            (Some bodyText),
            (Some <| zipFolderPath),
            config
        )
    )

module Docker =
    
    open Fake.Extensions.Release

    let dockerImageName = "freymaurer/swate"
    let dockerContainerName = "swate"

    Target.create "docker-publish" (fun _ ->
        let releaseNotesPath = "RELEASE_NOTES.md"
        let port = "5000"

        Release.exists()
        let newRelease = ReleaseNotes.load releaseNotesPath
        let check = Fake.Core.UserInput.getUserInput($"Is version {newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch} correct? (y/n/true/false)" )

        let dockerCreateImage() = run docker $"build -t {dockerContainerName} ." ""
        let dockerTestImage() = run docker $"run -it -p {port}:{port} {dockerContainerName}" ""
        let dockerTagImage() =
            run docker $"tag {dockerContainerName}:latest {dockerImageName}:{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}" ""
            run docker $"tag {dockerContainerName}:latest {dockerImageName}:latest" ""
        let dockerPushImage() =
            run docker $"push {dockerImageName}:{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}" ""
            run docker $"push {dockerImageName}:latest" ""
        let dockerPublish() =
            Trace.trace $"Tagging image with :latest and :{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}"
            dockerTagImage()
            Trace.trace $"Pushing image to dockerhub with :latest and :{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}"
            dockerPushImage()
        /// Check if next SemVer is correct
        match check with
        | "y"|"true"|"Y" ->
            Trace.trace "Perfect! Starting with docker publish"
            Trace.trace "Creating image"
            dockerCreateImage()
            /// Check if user wants to test image
            let testImage = Fake.Core.UserInput.getUserInput($"Want to test the image? (y/n/true/false)" )
            match testImage with
            | "y"|"true"|"Y" ->
                Trace.trace $"Your app on port {port} will open on localhost:{port}."
                dockerTestImage()
                /// Check if user wants the image published
                let imageWorkingCorrectly = Fake.Core.UserInput.getUserInput($"Is the image working as intended? (y/n/true/false)" )
                match imageWorkingCorrectly with
                | "y"|"true"|"Y"    -> dockerPublish()
                | "n"|"false"|"N"   -> Trace.traceErrorfn "Cancel docker-publish"
                | anythingElse      -> failwith $"""Could not match your input "{anythingElse}" to a valid input. Please try again."""
            | "n"|"false"|"N"   -> dockerPublish()
            | anythingElse      -> failwith $"""Could not match your input "{anythingElse}" to a valid input. Please try again."""
        | "n"|"false"|"N" ->
            Trace.traceErrorfn "Please update your SemVer Version in %s" releaseNotesPath
        | anythingElse -> failwith $"""Could not match your input "{anythingElse}" to a valid input. Please try again."""

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

Target.create "bundle" (fun _ ->
    [ "server", dotnet $"publish -c Release -o \"{deployPath}\"" serverPath
      "client", dotnet "fable src/Client -s --run webpack --config webpack.config.js" "" ]
    |> runParallel
)

Target.create "bundle-linux" (fun _ ->
    [ "server", dotnet $"publish -c Release -r linux-x64 -o \"{deployPath}\"" serverPath
      "client", dotnet "fable src/Client -s --run webpack --config webpack.config.js" "" ]
    |> runParallel
)

Target.create "Run" (fun _ ->
    run dotnet "build" sharedPath
    [ "server", dotnet "watch run" serverPath
      "client", dotnet "fable watch src/Client -s --run webpack-dev-server" "" ]
    |> runParallel
)

Target.create "officedebug" (fun config ->
    let args = config.Context.Arguments
    run dotnet "build" sharedPath
    if args |> List.contains "--open" then openBrowser developmentUrl
    [ "server", dotnet "watch run" serverPath
      "client", dotnet "fable watch src/Client -s --run webpack-dev-server" ""
      /// start up mysql db from docker-compose
      "database", dockerCompose $"-f {dockerComposePath} up" __SOURCE_DIRECTORY__
      /// sideload webapp in excel
      if args |> List.contains "--excel" then "officedebug", npx "office-addin-debugging start build/manifest.xml desktop --debug-method web" ""
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
        ==> "CreateDevCerts"
        ==> "SetLoopbackExempt"
        ==> "Setup"

    "run-db"

    "release"

    "docker-publish"

    "testfake"
    "Ignore"
]

[<EntryPoint>]
let main args = runOrDefault args