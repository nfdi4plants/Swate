open Fake.Core
open Fake.IO

open Helpers
open System
open Fake.Tools

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
    let project = "Swate"
    let projectRepo = $"https://github.com/{gitOwner}/{project}"
    let mutable prereleaseSuffix = ""
    let mutable prereleaseTag: string = ""
    let mutable isPrerelease = false

    Fake.Extensions.Release.ReleaseNotes.ensure()
    let releaseNotesPath = "RELEASE_NOTES.md"
    let release = ReleaseNotes.load releaseNotesPath
    let stableVersion = SemVer.parse release.NugetVersion
    let stableVersionTag = (sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch )
    
module ReleaseNoteTasks =

    open Fake.Extensions.Release

    let createVersionFile(version: string, commit: bool) =
        let releaseDate = System.DateTime.UtcNow.ToShortDateString()
        Fake.DotNet.AssemblyInfoFile.createFSharp "src/Server/Version.fs"
            [   Fake.DotNet.AssemblyInfo.Title "Swate"
                Fake.DotNet.AssemblyInfo.Version version
                Fake.DotNet.AssemblyInfo.Metadata ("Version",version)
                Fake.DotNet.AssemblyInfo.Metadata ("ReleaseDate",releaseDate)
            ]
        if commit then 
            run git ["add"; "."] ""
            run git ["commit"; "-m"; (sprintf "Release v%s :bookmark:" ProjectInfo.prereleaseTag)] ""

    let updateReleaseNotes = Target.create "releasenotes" (fun config ->
        ReleaseNotes.ensure()

        ReleaseNotes.update(ProjectInfo.gitOwner, ProjectInfo.project, config)

        let newRelease = ReleaseNotes.load "RELEASE_NOTES.md"
        createVersionFile(newRelease.AssemblyVersion, false)

        Trace.trace "Update Version.fs done!"
        
        // Update maniefest.xmls
        Trace.trace "Update manifest.xml"
        
        let _ =
            let newVer = sprintf "<Version>%i.%i.%i</Version>" newRelease.SemVer.Major newRelease.SemVer.Minor newRelease.SemVer.Patch
            Shell.regexReplaceInFilesWithEncoding
                "<Version>[0-9]+.[0-9]+.[0-9]+</Version>"
                newVer
                System.Text.Encoding.UTF8
                [
                    System.IO.Path.Combine(__SOURCE_DIRECTORY__, @"..\.assets\assets\manifest.xml")
                    System.IO.Path.Combine(__SOURCE_DIRECTORY__, @"..\.assets\assets\core_manifest.xml")
                    System.IO.Path.Combine(__SOURCE_DIRECTORY__, @"..\.assets\assets\experts_manifest.xml")
                    System.IO.Path.Combine(__SOURCE_DIRECTORY__, "manifest.xml")
                ]
        
        Trace.trace "Update manifest.xml done!"
    )

    let githubDraft = Target.create "GithubDraft" (fun config ->

        let zipFolderPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__,@"..\.assets")

        let cleanFolder =
            Trace.trace $"Clean existing .zip files from '{Path.getFullName(zipFolderPath)}'!"
            Fake.IO.DirectoryInfo.ofPath zipFolderPath
            |> Fake.IO.DirectoryInfo.getFiles
            |> Array.filter (fun x -> x.Name.EndsWith ".zip")
            |> Array.map (fun x -> x.FullName)
            |> File.deleteAll

        let assetPath = System.IO.Path.Combine(zipFolderPath, "assets")
        let assetDir = Fake.IO.DirectoryInfo.ofPath assetPath

        let allFiles, quickstartFiles =
            let assetsPaths = Fake.IO.DirectoryInfo.getFiles assetDir
            assetsPaths |> Array.map (fun x -> x.FullName),
            assetsPaths |> Array.filter (fun x -> x.Name = "core_manifest.xml") |> Array.map (fun x -> x.FullName)

        let zipFile = System.IO.Path.Combine(zipFolderPath, "swate-win.zip")

        let quickStartZipFile = System.IO.Path.Combine(zipFolderPath, "swate-b-quickstart.zip")

        Zip.zip assetDir.FullName zipFile allFiles

        Zip.zip assetDir.FullName quickStartZipFile (quickstartFiles)

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
            ProjectInfo.project,
            (Some bodyText),
            (Some <| zipFolderPath),
            config
        )
    )

module Docker =
    
    open Fake.Extensions.Release

    let dockerImageName = "freymaurer/swate"
    let dockerContainerName = "swate"
    let port = "8080"

    let dockerCreateImage(tag:string option) = 
        run 
            docker 
            [
                "build"; "-t"; 
                if tag.IsSome then $"{dockerContainerName}:{tag.Value}" else dockerContainerName; 
                "-f"; "build/Dockerfile.publish"; "."
            ] 
            ""
    let dockerTestImage(tag:string option) = 
        run 
            docker 
            [
                "run"; "-it"; "-p"; $"{port}:{port}"; 
                if tag.IsSome then $"{dockerContainerName}:{tag.Value}" else dockerContainerName;
            ] 
            ""

    /// <summary>
    /// Runs full docker compose stack with the swate:new image.
    /// </summary>
    let DockerTestNewStack() =
        let dockerComposeNewPath = Path.getFullName ".db\docker.compose.new.yml"
        run dockerCompose ["-f"; dockerComposeNewPath; "up"] __SOURCE_DIRECTORY__

    Target.create "docker-test" (fun _ ->
        dockerCreateImage (Some "new")
        dockerTestImage (Some "new")
    )

    // Create nightly (https://de.wikipedia.org/wiki/Nightly_Build)
    // 1: docker build -t swate -f build/Dockerfile.publish .
    // 2: docker run -it -p 5000:5000 swate
    //      -> http://localhost:5000
    // 3: docker tag swate:latest freymaurer/swate.nightly:latest
    // 4: docker push freymaurer/swate.nightly:latest

    // Change target to github-packages
    // https://docs.github.com/en/actions/publishing-packages/publishing-docker-images
    //Target.create "docker-publish" (fun _ ->
        
    //    let newRelease = ProjectInfo.release
    //    let check = Fake.Core.UserInput.getUserInput($"Is version {newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch} correct? (y/n/true/false)" )

    //    let dockerTagImage() =
    //        run docker ["tag"; sprintf "%s:latest" dockerContainerName; sprintf "%s:%i.%i.%i" dockerContainerName newRelease.SemVer.Major newRelease.SemVer.Minor newRelease.SemVer.Patch] ""
    //        run docker ["tag"; sprintf "%s:latest" dockerContainerName; sprintf "%s:latest" dockerImageName] ""
    //    let dockerPushImage() =
    //        run docker ["push"; sprintf "%s:%i.%i.%i" dockerImageName newRelease.SemVer.Major newRelease.SemVer.Minor newRelease.SemVer.Patch] ""
    //        run docker ["push"; sprintf "%s:latest" dockerImageName] ""
    //    let dockerPublish() =
    //        Trace.trace $"Tagging image with :latest and :{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}"
    //        dockerTagImage()
    //        Trace.trace $"Pushing image to dockerhub with :latest and :{newRelease.SemVer.Major}.{newRelease.SemVer.Minor}.{newRelease.SemVer.Patch}"
    //        dockerPushImage()
    //    // Check if next SemVer is correct
    //    match check with
    //    | "y"|"true"|"Y" ->
    //        Trace.trace "Perfect! Starting with docker publish"
    //        Trace.trace "Creating image"
    //        dockerCreateImage(None)
    //        /// Check if user wants to test image
    //        let testImage = Fake.Core.UserInput.getUserInput($"Want to test the image? (y/n/true/false)" )
    //        match testImage with
    //        | "y"|"true"|"Y" ->
    //            Trace.trace $"Your app on port {port} will open on localhost:{port}."
    //            dockerTestImage(None)
    //            /// Check if user wants the image published
    //            let imageWorkingCorrectly = Fake.Core.UserInput.getUserInput($"Is the image working as intended? (y/n/true/false)" )
    //            match imageWorkingCorrectly with
    //            | "y"|"true"|"Y"    -> dockerPublish()
    //            | "n"|"false"|"N"   -> Trace.traceErrorfn "Cancel docker-publish"
    //            | anythingElse      -> failwith $"""Could not match your input "{anythingElse}" to a valid input. Please try again."""
    //        | "n"|"false"|"N"   -> dockerPublish()
    //        | anythingElse      -> failwith $"""Could not match your input "{anythingElse}" to a valid input. Please try again."""
    //    | "n"|"false"|"N" ->
    //        Trace.traceErrorfn "Please update your SemVer Version in %s" ProjectInfo.releaseNotesPath
    //    | anythingElse -> failwith $"""Could not match your input "{anythingElse}" to a valid input. Please try again."""
    //)

module Build =
    let SharedTests() =
        run dotnet [ "build" ] sharedTestsPath

module Release =

    open System.Diagnostics

    let GetLatestGitTag () : string =
        let executeCommand (command: string) : string =
            let p = new Process()
            p.StartInfo.FileName <- "git"
            p.StartInfo.Arguments <- command
            p.StartInfo.RedirectStandardOutput <- true
            p.StartInfo.UseShellExecute <- false
            p.StartInfo.CreateNoWindow <- true

            p.Start() |> ignore

            let output = p.StandardOutput.ReadToEnd()
            p.WaitForExit()

            output
        executeCommand "describe --abbrev=0 --tags"
        |> String.trim

    let SetPrereleaseTag() =
        printfn "Please enter pre-release package suffix"
        let suffix = System.Console.ReadLine()
        ProjectInfo.prereleaseSuffix <- suffix
        ProjectInfo.prereleaseTag <- (sprintf "%i.%i.%i-%s" ProjectInfo.release.SemVer.Major ProjectInfo.release.SemVer.Minor ProjectInfo.release.SemVer.Patch suffix)
        ProjectInfo.isPrerelease <- true

    let CreateTag() =
        if promptYesNo (sprintf "tagging branch with %s OK?" ProjectInfo.stableVersionTag ) then
            Git.Branches.tag "" ProjectInfo.stableVersionTag
            Git.Branches.pushTag "" ProjectInfo.projectRepo ProjectInfo.stableVersionTag
        else
            failwith "aborted"

    let CreatePrereleaseTag() =
        if promptYesNo (sprintf "Tagging branch with %s OK?" ProjectInfo.prereleaseTag ) then 
            run git ["tag"; "-f"; ProjectInfo.prereleaseTag; ] ""
            Git.Branches.pushTag "" ProjectInfo.projectRepo ProjectInfo.prereleaseTag
        else
            failwith "aborted"

    let ForcePushNightly() =
        if promptYesNo "Ready to force push release to nightly branch?" then 
            run git ["push"; "-f"; "origin"; "HEAD:nightly"] ""
        else
            failwith "aborted"


Target.create "InstallOfficeAddinTooling" (fun _ ->

    printfn "Installing office addin tooling"

    run npm [|"install"; "-g"; "office-addin-dev-certs"|] __SOURCE_DIRECTORY__
    run npm [|"install"; "-g"; "office-addin-debugging"|] __SOURCE_DIRECTORY__
    run npm [|"install"; "-g"; "office-addin-manifest"|] __SOURCE_DIRECTORY__
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
    run npx [|"office-addin-dev-certs"; "install"; "--days"; "365"|] ""

    let certPath =
        Path.combine
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            ".office-addin-dev-certs/ca.crt"
        

    let psi = new System.Diagnostics.ProcessStartInfo(FileName = certPath, UseShellExecute = true)
    System.Diagnostics.Process.Start(psi) |> ignore
)

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployPath
    run dotnet [ "fable"; "clean"; "--yes" ] clientPath // Delete *.fs.js files created by Fable
)

Target.create "InstallClient" (fun _ -> run npm [ "install" ] ".")

let InstallClient() =
    run npm [ "install" ] "."

let Bundle() =
    [
        "server", dotnet [ "publish"; "-c"; "Release"; "-o"; deployPath ] serverPath
        "client", dotnet [ "fable"; "-o"; "output"; "-s"; "-e"; "fs.js"; "--run"; "npx"; "vite"; "build" ] clientPath
    ]
    |> runParallel

let Run(db: bool) =
    [ "server", dotnet [ "watch"; "run" ] serverPath
      "client", dotnet [ "fable"; "watch"; "-o"; "output"; "-s"; "-e"; "fs.js"; "--run"; "npx"; "vite" ] clientPath
      if db then
        "database", dockerCompose ["-f"; dockerComposePath; "up"] __SOURCE_DIRECTORY__
    ] |> runParallel

//Target.create "officedebug" (fun config ->
//    let args = config.Context.Arguments
//    run dotnet [ "build" ] sharedPath
//    if args |> List.contains "--open" then openBrowser developmentUrl
//    [ "server", dotnet "watch run" serverPath
//      "client", dotnet "fable watch src/Client -o src/Client/output -e .fs.js -s --run webpack-dev-server" ""
//      // start up db + Swobup from docker-compose
//      "database", dockerCompose $"-f {dockerComposePath} up" __SOURCE_DIRECTORY__
//      // sideload webapp in excel
//      if args |> List.contains "--excel" then "officedebug", npx "office-addin-debugging start build/manifest.xml desktop --debug-method web" ""
//      ]
//    |> runParallel
//)

Target.create "RunDB" (fun _ ->
    run dockerCompose ["-f"; dockerComposePath; "up"] __SOURCE_DIRECTORY__
)

[<RequireQualifiedAccess>]
module Tests =
    let Watch() = 
        [
            "server", dotnet [ "watch"; "run" ] serverTestsPath
            "client", dotnet [ "fable"; "watch"; "-o"; "output"; "-s"; "--run"; "npx"; "vite" ] clientTestsPath ]
        |> runParallel

    let Run() = 
        [
            "server", dotnet [ "run" ] serverTestsPath
            //"client", dotnet [ "fable"; "-o"; "output"; "-s"; "--run"; "npx"; "mocha"; $"{clientTestsPath}/output/Client.Tests.js" ] clientTestsPath 
        ]|> runParallel

Target.create "RunTests" (fun _ ->
    run dotnet [ "build" ] sharedTestsPath
    [
        "server", dotnet [ "watch"; "run" ] serverTestsPath
        "client", dotnet [ "fable"; "watch"; "-o"; "output"; "-s"; "--run"; "npx"; "vite" ] clientTestsPath ]
    |> runParallel
)

Target.create "Format" (fun _ ->
    run dotnet [ "fantomas"; "."; "-r" ] "src"
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

[<EntryPoint>]
let main args = 
    let argv = args |> Array.map (fun x -> x.ToLower()) |> Array.toList

    match argv with
    | "bundle" :: a -> 
        InstallClient()
        Bundle(); 0
    | "run" :: a ->
        match a with
        | "db" :: a -> Run(true); 0
        | _ -> Run(false); 0
    | "release" :: a ->
        Build.SharedTests()
        Tests.Run()
        match a with
        | "pre" :: a -> 
            Release.SetPrereleaseTag()
            Release.CreatePrereleaseTag()
            let version = Release.GetLatestGitTag()
            ReleaseNoteTasks.createVersionFile(version, true)
            Release.ForcePushNightly()
            0
        | _ -> 
            Release.CreateTag()
            0
    | "docker" :: a ->
        match a with
        | "create" :: a -> Docker.dockerCreateImage(Some "new"); 0
        | "test" :: a -> 
            match a with
            | "single" :: a -> Docker.dockerTestImage(Some "new"); 0
            | _ -> Docker.DockerTestNewStack(); 0
        | _ -> runOrDefault args
    | "version" :: a ->
        match a with
        | "create-file" :: version :: a -> ReleaseNoteTasks.createVersionFile(version, false); 0
        | _ -> runOrDefault args
    | "cmdtest" :: a ->
        run git ["add"; "."] ""
        run git ["commit"; "-m"; (sprintf "Release v%s" ProjectInfo.prereleaseTag)] ""
        0
    | _ -> runOrDefault args

    