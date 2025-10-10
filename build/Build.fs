open System
open System.IO

let DEFINE_SWATE_ENVIRONMENT_FABLE = [ "--define"; "SWATE_ENVIRONMENT" ]

let sharedPath = Path.GetFullPath "src/Shared"
let serverPath = Path.GetFullPath "src/Server"
let clientPath = Path.GetFullPath "src/Client"
let deployPath = Path.GetFullPath "deploy"

let sharedTestsPath = Path.GetFullPath "tests/Shared"
let serverTestsPath = Path.GetFullPath "tests/Server"
let clientTestsPath = Path.GetFullPath "tests/Client"
let componentTestsPath = Path.GetFullPath "src/Components"

let dockerComposePath = Path.GetFullPath ".db/docker-compose.yml"

let ChangelogArtifactPath = Path.GetFullPath "artifacts/changelog.json"

let developmentUrl = "https://localhost:3000"

module ProjectInfo =

    let gitOwner = "nfdi4plants"
    let project = "Swate"
    let projectRepo = $"https://github.com/{gitOwner}/{project}"

[<AutoOpenAttribute>]
module ConsoleUtility =

    open System

    let inline printGreenfn fmt =
        Printf.kprintf
            (fun s ->
                let oldColor = Console.ForegroundColor
                Console.ForegroundColor <- ConsoleColor.Green
                Console.WriteLine s
                Console.ForegroundColor <- oldColor
            )
            fmt

    let inline printRedfn fmt =
        Printf.kprintf
            (fun s ->
                let oldColor = Console.ForegroundColor
                Console.ForegroundColor <- ConsoleColor.Red
                Console.WriteLine s
                Console.ForegroundColor <- oldColor
            )
            fmt

[<AutoOpen>]
module RunUtil =

    open SimpleExec
    open System.Diagnostics
    open System.IO
    open System.Threading.Tasks

    let private shellCommand cmd args =
        let argStr = args |> String.concat " "

        if OperatingSystem.IsWindows() then
            "cmd", $"/c {cmd} {argStr}"
        else
            "/bin/bash", $"-c \"{cmd} {argStr}\""

    let run (cmd: string) (args: seq<string>) (workingDir: string) =
        try
            Command.Run(cmd, args = args, workingDirectory = workingDir)
        with ex ->
            printRedfn "Error while running command: %s" ex.Message
            exit 1

    let runAsync (prefix: string) (cmd: string) (args: seq<string>) (workingDir: string) = async {
        try
            do!
                Command.RunAsync(cmd, args = args, workingDirectory = workingDir, echoPrefix = prefix)
                |> Async.AwaitTask

            return Ok()
        with ex ->
            printRedfn "[%s] Error: %s" prefix ex.Message
            return Error ex
    }

    let runAsyncColored prefix color cmd args workingDir = async {
        let shell, arguments = shellCommand cmd args
        let psi = ProcessStartInfo()
        psi.FileName <- shell
        psi.Arguments <- arguments
        psi.WorkingDirectory <- workingDir
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true

        let proc = new Process()
        proc.StartInfo <- psi
        let oldColor = Console.ForegroundColor

        let print (isError: bool) (line: string) =
            if not (String.IsNullOrWhiteSpace line) then
                Console.ForegroundColor <- color
                Console.Write($"[{prefix}] ")
                Console.ForegroundColor <- if isError then ConsoleColor.Red else oldColor
                Console.WriteLine(line)
                Console.ResetColor()

        proc.OutputDataReceived.Add(fun e ->
            if e.Data <> null then
                print false e.Data
        )

        proc.ErrorDataReceived.Add(fun e ->
            if e.Data <> null then
                print false e.Data
        )

        try
            if not (proc.Start()) then
                failwithf "Failed to start %s" cmd

            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()

            do! proc.WaitForExitAsync() |> Async.AwaitTask

            if proc.ExitCode = 0 then
                proc.Close()
                Console.ResetColor()
                return Ok()
            else
                print true $"Exited with code {proc.ExitCode}"
                proc.Close()
                Console.ResetColor()
                return Error(exn (sprintf "Process exited with code %d" proc.ExitCode))
        with ex ->
            print true $"Exception: {ex.Message}"
            proc.Close()
            Console.ResetColor()
            return Error(ex)
    }


    let runParallel (tasks: Async<Result<unit, exn>> list) =
        async {
            let! results = tasks |> Async.Parallel

            let errors =
                results
                |> Array.choose (
                    function
                    | Error e -> Some e
                    | Ok _ -> None
                )

            if errors.Length > 0 then
                printRedfn "%d task(s) failed" errors.Length
                errors |> Array.iter (fun e -> printRedfn " - %s" e.Message)
                exit 1
            else
                printGreenfn "All tasks completed successfully"
        }
        |> Async.RunSynchronously

// By @MangelMaxime: https://github.com/easybuild-org/EasyBuild.PackageReleaseNotes.Tasks/blob/main/src/LastVersionFinder.fs
module Changelog =

    open System
    open System.IO
    open System.Text.RegularExpressions
    open Semver
    open FsToolkit.ErrorHandling
    open System.Text.Json

    type Version = {
        Version: SemVersion
        Date: DateTime option
        Body: string
    } with

        static member Zero = {
            Version = SemVersion(0, 0, 0)
            Date = None
            Body = ""
        }

    type private VersionLineData = { Version: string; Date: string option }

    let private tryCheckVersionLine (line: string) =
        let m =
            Regex.Match(
                line,
                "^##\\s+\\[?v?(?<version>[^\\]\\s]+)\\]?(\\s-\\s(?<date>\\d{4}-\\d{2}-\\d{2}))?$",
                RegexOptions.Multiline
            )

        // I don't know why, but empty lines are matched
        if m.Success then
            let version = m.Groups.["version"].Value
            // Skip Unreleased versions for KeepAChangelog format
            if version = "Unreleased" then
                None
            else
                let date =
                    if m.Groups.["date"].Success then
                        Some m.Groups.["date"].Value
                    else
                        None

                { Version = version; Date = date } |> Some
        else
            None

    type Errors =
        | NoVersionFound
        | InvalidVersionFormat of line: string
        | InvalidDate of string

        member this.ToText() =
            match this with
            | NoVersionFound -> "No version found"
            | InvalidVersionFormat version -> $"Invalid version format: %s{version}"
            | InvalidDate date -> $"Invalid date format: %s{date}"

    let tryFindLastVersion (content: string) =
        let lines = content.Replace("\r\n", "\n").Split('\n') |> Seq.toList

        let rec apply (lines: string list) =
            match lines with
            | [] -> Error NoVersionFound
            | line :: rest ->
                match tryCheckVersionLine line with
                | Some data -> result {
                    let! version =
                        match SemVersion.TryParse(data.Version, SemVersionStyles.Strict) with
                        | true, version -> Ok version
                        | false, _ -> Error(InvalidVersionFormat data.Version)

                    let! date =
                        match data.Date with
                        | Some date ->
                            match DateTime.TryParse(date) with
                            | true, date -> Ok(Some date)
                            | false, _ -> Error(InvalidDate date)
                        | None -> Ok None

                    let body =
                        rest
                        |> List.takeWhile (fun line ->
                            match tryCheckVersionLine line with
                            | Some _ -> false
                            | None -> true
                        )
                        // Remove leading and trailing empty lines (not pretty but it works)
                        |> List.skipWhile (fun line -> line.Trim() = "")
                        |> List.rev
                        |> List.skipWhile (fun line -> line.Trim() = "")
                        |> List.rev
                        |> String.concat "\n"

                    return {
                        Version = version
                        Date = date
                        Body = body
                    }
                  }
                | None -> apply rest

        apply lines

    let getChangelogFile () : string =
        let changelogPath = Path.GetFullPath "CHANGELOG.md"

        if not (File.Exists changelogPath) then
            failwithf "CHANGELOG.md not found at %s" changelogPath
            exit 1

        System.IO.File.ReadAllText changelogPath

    let getLatestVersion () =
        let changelogLines = getChangelogFile ()

        match tryFindLastVersion changelogLines with
        | Ok version ->
            printGreenfn "Latest version found: %O" version.Version
            version
        | Error err ->
            printRedfn "Error: %s" (err.ToText())
            exit 1

    let writeLatestToFile (changelog: Version) =
        let json =
            JsonSerializer.Serialize(
                {|
                    version = changelog.Version.ToString()
                    changelog = changelog.Body
                    date = changelog.Date |> Option.map (fun d -> d.ToString("yyyy-MM-dd"))
                |},
                JsonSerializerOptions(WriteIndented = true)
            )

        Directory.CreateDirectory("artifacts") |> ignore
        File.WriteAllText(ChangelogArtifactPath, json, Text.Encoding.UTF8)
        printGreenfn "Wrote %s" ChangelogArtifactPath


// let mutable prereleaseSuffix = ""
// let mutable prereleaseTag: string = ""
// let mutable isPrerelease = false

// Fake.Extensions.Release.ReleaseNotes.ensure ()
// let releaseNotesPath = "RELEASE_NOTES.md"
// let release = ReleaseNotes.load releaseNotesPath
// let stableVersion = SemVer.parse release.NugetVersion

// let stableVersionTag =
//     (sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch)

// module ReleaseNoteTasks =

//     open Fake.Extensions.Release

//     let createVersionFile (version: string, commit: bool) =
//         let releaseDate = System.DateTime.UtcNow.ToShortDateString()

//         Fake.DotNet.AssemblyInfoFile.createFSharp "src/Server/Version.fs" [
//             Fake.DotNet.AssemblyInfo.Title "Swate"
//             Fake.DotNet.AssemblyInfo.Version version
//             Fake.DotNet.AssemblyInfo.Metadata("Version", version)
//             Fake.DotNet.AssemblyInfo.Metadata("ReleaseDate", releaseDate)
//         ]

//         Fake.DotNet.AssemblyInfoFile.createFSharp "src/Client/Version.fs" [
//             Fake.DotNet.AssemblyInfo.Title "Swate"
//             Fake.DotNet.AssemblyInfo.Version version
//             Fake.DotNet.AssemblyInfo.Metadata("Version", version)
//             Fake.DotNet.AssemblyInfo.Metadata("ReleaseDate", releaseDate)
//         ]

//         if commit then
//             run git [ "add"; "." ] ""
//             run git [ "commit"; "-m"; (sprintf "Release %s :bookmark:" ProjectInfo.prereleaseTag) ] ""

//     let updateReleaseNotes =
//         Target.create
//             "releasenotes"
//             (fun config ->
//                 ReleaseNotes.ensure ()

//                 ReleaseNotes.update (ProjectInfo.gitOwner, ProjectInfo.project, config)

//                 let newRelease = ReleaseNotes.load "RELEASE_NOTES.md"
//                 createVersionFile (newRelease.AssemblyVersion, false)

//                 Trace.trace "Update Version.fs done!"

//                 // Update maniefest.xmls
//                 Trace.trace "Update manifest.xml"

//                 let _ =
//                     let newVer =
//                         sprintf
//                             "<Version>%i.%i.%i</Version>"
//                             newRelease.SemVer.Major
//                             newRelease.SemVer.Minor
//                             newRelease.SemVer.Patch

//                     Shell.regexReplaceInFilesWithEncoding
//                         "<Version>[0-9]+.[0-9]+.[0-9]+</Version>"
//                         newVer
//                         System.Text.Encoding.UTF8
//                         [
//                             System.IO.Path.Combine(__SOURCE_DIRECTORY__, @"..\.assets\assets\manifest.xml")
//                             System.IO.Path.Combine(__SOURCE_DIRECTORY__, @"..\.assets\assets\core_manifest.xml")
//                             System.IO.Path.Combine(__SOURCE_DIRECTORY__, @"..\.assets\assets\experts_manifest.xml")
//                             System.IO.Path.Combine(__SOURCE_DIRECTORY__, "manifest.xml")
//                         ]

//                 Trace.trace "Update manifest.xml done!"
//             )

//     let githubDraft =
//         Target.create
//             "GithubDraft"
//             (fun config ->

//                 let zipFolderPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, @"..\.assets")

//                 let cleanFolder =
//                     Trace.trace $"Clean existing .zip files from '{Path.getFullName (zipFolderPath)}'!"

//                     Fake.IO.DirectoryInfo.ofPath zipFolderPath
//                     |> Fake.IO.DirectoryInfo.getFiles
//                     |> Array.filter (fun x -> x.Name.EndsWith ".zip")
//                     |> Array.map (fun x -> x.FullName)
//                     |> File.deleteAll

//                 let assetPath = System.IO.Path.Combine(zipFolderPath, "assets")
//                 let assetDir = Fake.IO.DirectoryInfo.ofPath assetPath

//                 let allFiles, quickstartFiles =
//                     let assetsPaths = Fake.IO.DirectoryInfo.getFiles assetDir

//                     assetsPaths |> Array.map (fun x -> x.FullName),
//                     assetsPaths
//                     |> Array.filter (fun x -> x.Name = "core_manifest.xml")
//                     |> Array.map (fun x -> x.FullName)

//                 let zipFile = System.IO.Path.Combine(zipFolderPath, "swate-win.zip")

//                 let quickStartZipFile =
//                     System.IO.Path.Combine(zipFolderPath, "swate-b-quickstart.zip")

//                 Zip.zip assetDir.FullName zipFile allFiles

//                 Zip.zip assetDir.FullName quickStartZipFile (quickstartFiles)

//                 Trace.trace "Assets zipped!"

//                 let bodyText =
//                     [
//                         ""
//                         "You can check our [release notes](https://github.com/nfdi4plants/Swate/blob/developer/RELEASE_NOTES.md) to see a list of all new features."
//                         "If you decide to test Swate in the current state, please take the time to set up a Github account to report your issues and suggestions [here](https://github.com/nfdi4plants/Swate/issues/new/choose)."
//                         ""
//                         "You can also search existing issues for solutions for your questions and/or discussions about your suggestions."
//                         ""
//                         "If you want to start using Swate follow these easy instructions: [Swate installation](https://github.com/nfdi4plants/Swate#installuse)"
//                         ""
//                     ]
//                     |> String.concat "\n"

//                 Github.draft (
//                     ProjectInfo.gitOwner,
//                     ProjectInfo.project,
//                     (Some bodyText),
//                     (Some <| zipFolderPath),
//                     config
//                 )
//             )

// module Docker =

//     let dockerImageName = "freymaurer/swate"
//     let dockerContainerName = "swate"
//     let port = "5000"

//     let dockerCreateImage (tag: string option) =
//         run
//             docker
//             [
//                 "build"
//                 "-t"
//                 if tag.IsSome then
//                     $"{dockerContainerName}:{tag.Value}"
//                 else
//                     dockerContainerName
//                 "-f"
//                 "build/Dockerfile.publish"
//                 "."
//             ]
//             ""

//     let dockerTestImage (tag: string option) =
//         run
//             docker
//             [
//                 "run"
//                 "-it"
//                 "-p"
//                 $"{port}:{port}"
//                 if tag.IsSome then
//                     $"{dockerContainerName}:{tag.Value}"
//                 else
//                     dockerContainerName
//             ]
//             ""

//     /// <summary>
//     /// Runs full docker compose stack with the swate:new image.
//     /// </summary>
//     let DockerTestNewStack () =
//         let dockerComposeNewPath = Path.GetFullPath ".db/docker.compose.new.yml"
//         run dockerCompose [ "-f"; dockerComposeNewPath; "up" ] __SOURCE_DIRECTORY__

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

// module Release =

//     open System.Diagnostics

//     let GetLatestGitTag () : string =

//         executeCommand <| "describe --abbrev=0 --tags".Trim()

//     let SetPrereleaseTag () =
//         printfn "Please enter pre-release package suffix"
//         let suffix = System.Console.ReadLine()
//         ProjectInfo.prereleaseSuffix <- suffix

//         ProjectInfo.prereleaseTag <-
//             (sprintf
//                 "v%i.%i.%i-%s"
//                 ProjectInfo.release.SemVer.Major
//                 ProjectInfo.release.SemVer.Minor
//                 ProjectInfo.release.SemVer.Patch
//                 suffix)

//         ProjectInfo.isPrerelease <- true

//     let CreateTag () =
//         if promptYesNo (sprintf "tagging branch with %s OK?" ProjectInfo.stableVersionTag) then
//             Git.Branches.tag "" ProjectInfo.stableVersionTag
//             Git.Branches.pushTag "" ProjectInfo.projectRepo ProjectInfo.stableVersionTag
//         else
//             failwith "aborted"

//     let CreatePrereleaseTag () =
//         if promptYesNo (sprintf "Tagging branch with %s OK?" ProjectInfo.prereleaseTag) then
//             run git [ "tag"; "-f"; ProjectInfo.prereleaseTag ] ""
//             Git.Branches.pushTag "" ProjectInfo.projectRepo ProjectInfo.prereleaseTag
//         else
//             failwith "aborted"

//     let ForcePushNightly () =
//         if promptYesNo "Ready to force push release to nightly branch?" then
//             run git [ "push"; "-f"; "origin"; "HEAD:nightly" ] ""
//         else
//             failwith "aborted"

//     let ForcePushLatest () =
//         if promptYesNo "Ready to force push release to latest branch?" then
//             run git [ "push"; "-f"; "origin"; "HEAD:latest" ] ""
//         else
//             failwith "aborted"


// Target.create
//     "InstallOfficeAddinTooling"
//     (fun _ ->

//         printfn "Installing office addin tooling"

//         run npm [| "install"; "-g"; "office-addin-dev-certs" |] __SOURCE_DIRECTORY__
//         run npm [| "install"; "-g"; "office-addin-debugging" |] __SOURCE_DIRECTORY__
//         run npm [| "install"; "-g"; "office-addin-manifest" |] __SOURCE_DIRECTORY__
//     )

// Target.create
//     "SetLoopbackExempt"
//     (fun _ ->
//         Command.RawCommand(
//             "CheckNetIsolation.exe",
//             Arguments.ofList [ "LoopbackExempt"; "-a"; "-n=\"microsoft.win32webviewhost_cw5n1h2txyewy\"" ]
//         )
//         |> CreateProcess.fromCommand
//         |> Proc.run
//         |> ignore
//     )

// Target.create
//     "CreateDevCerts"
//     (fun _ ->
//         run npx [| "office-addin-dev-certs"; "install"; "--days"; "365" |] ""

//         let certPath =
//             Path.combine
//                 (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
//                 ".office-addin-dev-certs/ca.crt"


//         let psi =
//             new System.Diagnostics.ProcessStartInfo(FileName = certPath, UseShellExecute = true)

//         System.Diagnostics.Process.Start(psi) |> ignore
//     )

// Target.create
//     "Clean"
//     (fun _ ->
//         Shell.cleanDir deployPath
//         run dotnet [ "fable"; "clean"; "--yes" ] clientPath // Delete *.fs.js files created by Fable
//     )

// Target.create "InstallClient" (fun _ -> run npm [ "install" ] ".")

// let InstallClient () = run npm [ "install" ] "."

// type Bundle =
//     static member Client(?forSwate: bool) =
//         let forSwate = defaultArg forSwate true

//         run
//             dotnet
//             [
//                 "fable"
//                 "-o"
//                 "output"
//                 "-s"
//                 "-e"
//                 "fs.js"
//                 if forSwate then
//                     yield! DEFINE_SWATE_ENVIRONMENT_FABLE
//                 "--run"
//                 "npx"
//                 "vite"
//                 "build"
//             ]
//             clientPath

//     static member All() =
//         [
//             "server", dotnet [ "publish"; "-c"; "Release"; "-o"; deployPath ] serverPath
//             "client",
//             dotnet
//                 [
//                     "fable"
//                     "-o"
//                     "output"
//                     "-s"
//                     "-e"
//                     "fs.js"
//                     yield! DEFINE_SWATE_ENVIRONMENT_FABLE
//                     "--run"
//                     "npx"
//                     "vite"
//                     "build"
//                 ]
//                 clientPath
//         ]
//         |> runParallel

// type Run =

//     static member ClientArgs = [
//         "fable"
//         "watch"
//         "-o"
//         "output"
//         "-s"
//         "-e"
//         "fs.js"
//         yield! DEFINE_SWATE_ENVIRONMENT_FABLE
//         "--run"
//         "npx"
//         "vite"
//         "--debug"
//         "transform"
//     ]


//     static member All(db: bool) =
//         [
//             "server", dotnet [ "watch"; "run" ] serverPath
//             "client", dotnet Run.ClientArgs clientPath
//             if db then
//                 "database", dockerCompose [ "-f"; dockerComposePath; "up"; "-d" ] __SOURCE_DIRECTORY__
//         ]
//         |> runParallel

// //Target.create "officedebug" (fun config ->
// //    let args = config.Context.Arguments
// //    run dotnet [ "build" ] sharedPath
// //    if args |> List.contains "--open" then openBrowser developmentUrl
// //    [ "server", dotnet "watch run" serverPath
// //      "client", dotnet "fable watch src/Client -o src/Client/output -e .fs.js -s --run webpack-dev-server" ""
// //      // start up db + Swobup from docker-compose
// //      "database", dockerCompose $"-f {dockerComposePath} up" __SOURCE_DIRECTORY__
// //      // sideload webapp in excel
// //      if args |> List.contains "--excel" then "officedebug", npx "office-addin-debugging start build/manifest.xml desktop --debug-method web" ""
// //      ]
// //    |> runParallel
// //)

// // Target.create "RunDB" (fun _ -> run dockerCompose [ "-f"; dockerComposePath; "up"; "-d" ] __SOURCE_DIRECTORY__)

[<RequireQualifiedAccess>]
module Tests =

    let buildSharedTests () =
        run "dotnet" [ "build" ] sharedTestsPath

    /// This disables microsoft data collection for using `office-addin-mock`
    let disableUserData () =
        run "npx" [ "office-addin-usage-data"; "off" ] __SOURCE_DIRECTORY__

    let Watch () =
        [
            runAsync "server" "dotnet" [ "watch"; "run" ] serverTestsPath
            // This below will start web ui for tests, but cannot execute due to office-addin-mock
            runAsync
                "client"
                "dotnet"
                [
                    "fable"
                    "watch"
                    "-o"
                    "output"
                    "-s"
                    yield! DEFINE_SWATE_ENVIRONMENT_FABLE
                    "--run"
                    "npx"
                    "mocha"
                    $"{clientTestsPath}/output/Client.Tests.js"
                    "--watch"
                ]
                clientTestsPath
            runAsync "components" "npm" [ "run"; "test" ] componentTestsPath
        ]
        |> runParallel

    let WatchJs () =
        [
            runAsync
                "client"
                "dotnet"
                [
                    "fable"
                    "watch"
                    "-o"
                    "output"
                    "-s"
                    yield! DEFINE_SWATE_ENVIRONMENT_FABLE
                    "--run"
                    "npx"
                    "mocha"
                    $"{clientTestsPath}/output/Client.Tests.js"
                    "--watch"
                    "--parallel"
                ]
                clientTestsPath
        ]
        |> runParallel

    let Run () =
        [
            runAsyncColored "server" ConsoleColor.Blue "dotnet" [ "run" ] serverTestsPath
            runAsyncColored
                "client"
                ConsoleColor.Magenta
                "dotnet"
                [
                    "fable"
                    "-o"
                    "output"
                    "-s"
                    yield! DEFINE_SWATE_ENVIRONMENT_FABLE
                    "--run"
                    "npx"
                    "mocha"
                    $"{clientTestsPath}/output/Client.Tests.js"
                ]
                clientTestsPath
            runAsyncColored "components" ConsoleColor.Yellow "npm" [ "run"; "test:run" ] componentTestsPath
        ]
        |> runParallel

// Target.create "Format" (fun _ -> run dotnet [ "fantomas"; "."; "-r" ] "src")

// Target.create "Setup" ignore

// Target.create "Ignore" (fun _ -> Trace.trace "Hit ignore")

[<EntryPoint>]
let main args =
    let argv = args |> Array.map (fun x -> x.ToLower()) |> Array.toList

    match argv with
    // | "bundle" :: a ->
    //     InstallClient()

    //     match a with
    //     | "client" :: a ->
    //         match a with
    //         | "standalone" :: _ ->
    //             Bundle.Client(false)
    //             0
    //         | _ ->
    //             Bundle.Client(true)
    //             0
    //     | _ ->
    //         Bundle.All()
    //         0
    // | "run" :: a ->
    //     match a with
    //     | "db" :: a ->
    //         Run.All(true)
    //         0
    //     | "client" :: a ->
    //         run dotnet Run.ClientArgs clientPath
    //         0
    //     | _ ->
    //         Run.All(false)
    //         0
    | "test" :: a ->
        Tests.disableUserData ()
        Tests.buildSharedTests ()

        match a with
        | "watch" :: _ ->
            Tests.Watch()
            0
        | "js" :: _ ->
            Tests.WatchJs()
            0
        | _ ->
            Tests.Run()
            0
    // | "release" :: a ->
    //     Tests.buildSharedTests ()
    //     Tests.Run()
    //     0
    // // match a with
    // // | "pre" :: a ->
    // //     Release.SetPrereleaseTag()
    // //     Release.CreatePrereleaseTag()
    // //     let version = Release.GetLatestGitTag()
    // //     ReleaseNoteTasks.createVersionFile (version, true)
    // //     Release.ForcePushNightly()
    // //     0
    // // | _ ->
    // //     Release.CreateTag()
    // //     0
    // | "docker" :: a ->
    //     match a with
    //     | "create" :: a ->
    //         Docker.dockerCreateImage (Some "new")
    //         0
    //     | "test" :: a ->
    //         match a with
    //         | "single" :: a ->
    //             Docker.dockerTestImage (Some "new")
    //             0
    //         | _ ->
    //             Docker.DockerTestNewStack()
    //             0
    //     | _ -> runOrDefault args
    // // | "version" :: a ->
    // //     match a with
    // //     | "create-file" :: "from-git" :: a ->
    // //         let version = Release.GetLatestGitTag()
    // //         ReleaseNoteTasks.createVersionFile (version, true)
    // //         0
    // //     | "create-file" :: version :: a ->
    // //         ReleaseNoteTasks.createVersionFile (version, false)
    // //         0
    // //     | _ -> runOrDefault args
    | "changelog" :: a ->
        let changelog = Changelog.getLatestVersion ()
        Changelog.writeLatestToFile changelog
        0
    | "dev" :: a ->
        let changelog = Changelog.getLatestVersion ()
        // run git [ "add"; "." ] ""
        // run git [ "commit"; "-m"; (sprintf "Release v%s" ProjectInfo.prereleaseTag) ] ""
        0
    | _ ->
        Console.WriteLine("No valid argument provided. Please provide a valid target.")
        1