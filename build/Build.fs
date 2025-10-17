open System
open System.IO

let DEFINE_SWATE_ENVIRONMENT_FABLE = [ "--define"; "SWATE_ENVIRONMENT" ]

module ProjectPaths =

    let sharedPath = Path.GetFullPath "src/Shared"
    let serverPath = Path.GetFullPath "src/Server"
    let clientPath = Path.GetFullPath "src/Client"
    let componentsPath = Path.GetFullPath "src/Components"
    let deployPath = Path.GetFullPath "deploy"
    let nugetDeployPath = Path.GetFullPath "nupkgs"
    let nugetSln = Path.GetFullPath "Nuget.sln"

    let sharedTestsPath = Path.GetFullPath "tests/Shared"
    let serverTestsPath = Path.GetFullPath "tests/Server"
    let clientTestsPath = Path.GetFullPath "tests/Client"
    let componentTestsPath = Path.GetFullPath "src/Components"

    let dockerComposePath = Path.GetFullPath ".db/docker-compose.yml"
    let dockerFilePath = Path.GetFullPath "build/Dockerfile.publish"

let developmentUrl = "https://localhost:3000"

module ProjectInfo =

    let gitOwner = "nfdi4plants"
    let project = "Swate"
    let projectRepo = $"https://github.com/{gitOwner}/{project}"

module GHActions =
    let setGitHubOutput (name: string) (value: string) =
        match Environment.GetEnvironmentVariable("GITHUB_OUTPUT") with
        | null
        | "" -> printfn "GITHUB_OUTPUT not set — likely running outside GitHub Actions."
        | path -> File.AppendAllText(path, $"{name}={value}\n")

    let setShouldSkip () = setGitHubOutput "should_skip" "true"

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

    let runReadAsync (cmd: string) (args: seq<string>) (workingDir: string) =
        try
            Command.ReadAsync(cmd, args = args, workingDirectory = workingDir)
            |> Async.AwaitTask
            |> Async.RunSynchronously
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

module GIT =

    /// Get all git tags and strip leading 'v' if present
    let getTags () =

        let x = runReadAsync "git" [ "tag" ] ""
        let tags, _ = x.ToTuple()

        tags.Trim().Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun t -> if t.StartsWith("v") then t.Substring 1 else t)

    let createTagAndPush (tag: string) =
        run
            "git"
            [
                "tag"
                if tag = "1.0.0-rc.9" then
                    "-f"
                tag
            ]
            ""

        run
            "git"
            [
                "push"
                "origin"
                if tag = "1.0.0-rc.9" then
                    "-f"
                tag
            ]
            ""

        printGreenfn "Tag %s created and pushed" tag

module VersionTasks =

    open Semver
    open System.Text.RegularExpressions

    // robust function, that looks in string[] for matching line and replaces it if found, returns bool*string []
    // true if replaced, false if not found
    let tryReplace (pattern: Regex) (replacement: string) (content: string[]) =
        let mutable wasReplaced = false

        let nextContent =
            content
            |> Array.map (fun line ->
                if pattern.IsMatch(line) then
                    wasReplaced <- true
                    replacement
                else
                    line
            )

        wasReplaced, nextContent

    let updateVersionFiles (version: Changelog.Version) =
        printGreenfn "Updating version files to %O" version.Version
        let serverPath = "src/Server/Version.fs"
        let clientPath = "src/Client/Version.fs"

        let nextReleaseDate =
            match version.Date with
            | Some date -> date.ToShortDateString()
            | None -> DateTime.UtcNow.ToShortDateString()

        let nextVersion = version.Version.ToString()

        // let AssemblyVersion = "0.0.0"
        let patternAssemblyVersion = @"^let AssemblyVersion = "".*"""
        // let AssemblyMetadata_ReleaseDate = "09.10.2025"
        let patternAssemblyMetadata_ReleaseDate =
            @"^let AssemblyMetadata_ReleaseDate = "".*"""

        let versionRegex = Regex(patternAssemblyVersion)
        let dateRegex = Regex(patternAssemblyMetadata_ReleaseDate)

        let replace (path: string) =
            let content = File.ReadAllLines(path)

            let newContent =
                match
                    content
                    |> tryReplace versionRegex (sprintf $"let AssemblyVersion = \"%s{nextVersion}\"")
                with
                | (true, newContent) ->
                    match
                        newContent
                        |> tryReplace dateRegex (sprintf $"let AssemblyMetadata_ReleaseDate = \"%s{nextReleaseDate}\"")
                    with
                    | (true, finalContent) -> finalContent
                    | (false, _) -> failwithf "Date line not found in version file: %s" path
                | (false, _) -> failwithf "Version line not found in version file: %s" path

            File.WriteAllLines(path, newContent)

        replace serverPath
        printGreenfn "Updated %s" serverPath
        replace clientPath
        printGreenfn "Updated %s" clientPath

    let updateFSharpProjectVersions (version: Changelog.Version) =
        printGreenfn "Updating .fsproj files to version %O" version.Version
        let componentsProjectPath = "src/Components/src/Swate.Components.fsproj"
        let sharedProjectPath = "src/Shared/Swate.Components.Core.fsproj"


        let nextVersion = version.Version.ToString()
        let versionRegexPattern = @"<PackageVersion>.*</PackageVersion>"

        let replace (path: string) =
            let content = File.ReadAllText(path)

            let nextContent =
                match Regex.Count(content, versionRegexPattern) with
                | 1 ->
                    Regex.Replace(
                        content,
                        versionRegexPattern,
                        sprintf "<PackageVersion>%s</PackageVersion>" nextVersion
                    )
                | _ -> failwithf "Version line not found in version file: %s" path

            File.WriteAllText(path, nextContent)

        replace componentsProjectPath
        printGreenfn "Updated %s" componentsProjectPath
        replace sharedProjectPath
        printGreenfn "Updated %s" sharedProjectPath

    let updateComponentsPackageJSONVersion (version: Changelog.Version) =
        run
            "npm"
            [
                "version"
                version.Version.ToString()
                "--allow-same-version"
                "--no-git-tag-version"
            ]
            "src/Components"

        printfn "Updated src/Components/package.json to version %O" version.Version


module GitHub =

    open FSharp.Data
    open System.Text.Json

    type ReleaseResponse = {
        url: string
        assets_url: string
        upload_url: string
        html_url: string
        id: int
        tag_name: string
        name: string
        body: string
    }

    type UpdateReleaseRequest = {
        tag_name: string option
        name: string option
        body: string option
    }

    let mkHeaders token = [
        "Authorization", sprintf "Bearer %s" token
        "Accept", "application/vnd.github+json"
        "X-GitHub-Api-Version", "2022-11-28"
        "user-agent", "Swate build script"
    ]

    let toJson (data: (string * obj) list) = JsonSerializer.Serialize(dict data)

    let mkRelease (token: string) (version: Changelog.Version) =
        let endpoint =
            $"https://api.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases"

        Http.RequestString(
            endpoint,
            httpMethod = "POST",
            headers = mkHeaders token,
            body =
                TextRequest(
                    toJson [
                        "tag_name", version.Version.ToString() :> obj
                        "name", version.Version.ToString() :> obj
                        "body", version.Body :> obj
                        "draft", true :> obj
                        "generate_release_notes ", true
                    ]
                )
        )
        |> JsonSerializer.Deserialize<ReleaseResponse>

    let tryGetRelease (token: string) (version: Changelog.Version) =
        let version = version.Version.ToString()

        let endpoint =
            $"https://api.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases/tags/{version}"

        let response =
            Http.Request(endpoint, httpMethod = "GET", headers = mkHeaders token, silentHttpErrors = true)

        match response.StatusCode with
        | 404 ->
            printfn "[GitHub] Release %O not found" version
            None
        | 200 ->
            printfn "[GitHub] Found Release %O" version

            let jsonStr =
                match response.Body with
                | Text text -> text
                | _ -> failwith "Unexpected response body"

            let response = JsonSerializer.Deserialize<ReleaseResponse>(jsonStr)
            Some response
        | code -> failwithf "Error: unexpected status code %d" code

    let updateRelease (token: string) (version: Changelog.Version) (fn: ReleaseResponse -> UpdateReleaseRequest) =
        let versionStr = version.Version.ToString()

        let id =
            tryGetRelease token version
            |> Option.defaultWith (fun () -> failwithf "Release %s not found" versionStr)

        let request = fn id

        let endpoint =
            $"https://api.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases/{id}"

        Http.Request(
            endpoint,
            httpMethod = "PATCH",
            headers = mkHeaders token,
            body =
                TextRequest(
                    toJson [
                        if request.tag_name.IsSome then
                            "tag_name", request.tag_name.Value :> obj
                        if request.name.IsSome then
                            "name", request.name.Value :> obj
                        if request.body.IsSome then
                            "body", request.body.Value :> obj
                    ]
                )
        )

    let uploadReleaseAsset (token: string) (version: Changelog.Version) (filePath: string) =
        let versionStr = version.Version.ToString()

        let release =
            tryGetRelease token version
            |> Option.defaultWith (fun () -> failwithf "Release %s not found" versionStr)

        let endpoint =
            $"https://uploads.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases/{release.id}/assets"

        let fileName = Path.GetFileName(filePath)

        let fileBytes = File.ReadAllBytes(filePath)

        Http.Request(
            endpoint,
            httpMethod = "POST",
            headers = mkHeaders token @ [ "Content-Type", "application/octet-stream" ],
            query = [ "name", fileName; "label", fileName ],
            body = BinaryUpload fileBytes
        )

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

// let SetPrereleaseTag () =
//     printfn "Please enter pre-release package suffix"
//     let suffix = System.Console.ReadLine()
//     ProjectInfo.prereleaseSuffix <- suffix

//     ProjectInfo.prereleaseTag <-
//         (sprintf
//             "v%i.%i.%i-%s"
//             ProjectInfo.release.SemVer.Major
//             ProjectInfo.release.SemVer.Minor
//             ProjectInfo.release.SemVer.Patch
//             suffix)

//     ProjectInfo.isPrerelease <- true

// let CreateTag () =
//     if promptYesNo (sprintf "tagging branch with %s OK?" ProjectInfo.stableVersionTag) then
//         Git.Branches.tag "" ProjectInfo.stableVersionTag
//         Git.Branches.pushTag "" ProjectInfo.projectRepo ProjectInfo.stableVersionTag
//     else
//         failwith "aborted"

// let CreatePrereleaseTag () =
//     if promptYesNo (sprintf "Tagging branch with %s OK?" ProjectInfo.prereleaseTag) then
//         run git [ "tag"; "-f"; ProjectInfo.prereleaseTag ] ""
//         Git.Branches.pushTag "" ProjectInfo.projectRepo ProjectInfo.prereleaseTag
//     else
//         failwith "aborted"

// let ForcePushNightly () =
//     if promptYesNo "Ready to force push release to nightly branch?" then
//         run git [ "push"; "-f"; "origin"; "HEAD:nightly" ] ""
//     else
//         failwith "aborted"

// let ForcePushLatest () =
//     if promptYesNo "Ready to force push release to latest branch?" then
//         run git [ "push"; "-f"; "origin"; "HEAD:latest" ] ""
//     else
//         failwith "aborted"


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

type Bundle =

    static member Client(?forSwate: bool) =
        let forSwate = defaultArg forSwate true

        run
            "dotnet"
            [
                "fable"
                "-o"
                "output"
                "-s"
                "-e"
                "fs.js"
                if forSwate then
                    yield! DEFINE_SWATE_ENVIRONMENT_FABLE
                "--run"
                "npx"
                "vite"
                "build"
            ]
            ProjectPaths.clientPath

    static member All() =
        [
            runAsync
                "server"
                "dotnet"
                [ "publish"; "-c"; "Release"; "-o"; ProjectPaths.deployPath ]
                ProjectPaths.serverPath
            runAsync
                "client"
                "dotnet"
                [
                    "fable"
                    "-o"
                    "output"
                    "-s"
                    "-e"
                    "fs.js"
                    yield! DEFINE_SWATE_ENVIRONMENT_FABLE
                    "--run"
                    "npx"
                    "vite"
                    "build"
                ]
                ProjectPaths.clientPath
        ]
        |> runParallel

type Run =

    static member ClientArgs = [
        "fable"
        "watch"
        "-o"
        "output"
        "-s"
        "-e"
        "fs.js"
        yield! DEFINE_SWATE_ENVIRONMENT_FABLE
        "--run"
        "npx"
        "vite"
        "--debug"
        "transform"
    ]


    static member All(db: bool) =
        [
            runAsync "server" "dotnet" [ "watch"; "run" ] ProjectPaths.serverPath
            runAsync "client" "dotnet" Run.ClientArgs ProjectPaths.clientPath
            if db then
                runAsync
                    "database"
                    "docker-compose"
                    [ "-f"; ProjectPaths.dockerComposePath; "up"; "-d" ]
                    __SOURCE_DIRECTORY__
        ]
        |> runParallel

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

// // Target.create "RunDB" (fun _ -> run dockerCompose [ "-f"; dockerComposePath; "up"; "-d" ] __SOURCE_DIRECTORY__)

[<RequireQualifiedAccess>]
module Tests =

    let buildSharedTests () =
        run "dotnet" [ "build" ] ProjectPaths.sharedTestsPath

    /// This disables microsoft data collection for using `office-addin-mock`
    let disableUserData () =
        run "npx" [ "office-addin-usage-data"; "off" ] __SOURCE_DIRECTORY__

    let Watch () =
        [
            runAsync "server" "dotnet" [ "watch"; "run" ] ProjectPaths.serverTestsPath
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
                    $"{ProjectPaths.clientTestsPath}/output/Client.Tests.js"
                    "--watch"
                ]
                ProjectPaths.clientTestsPath
            runAsync "components" "npm" [ "run"; "test" ] ProjectPaths.componentTestsPath
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
                    $"{ProjectPaths.clientTestsPath}/output/Client.Tests.js"
                    "--watch"
                    "--parallel"
                ]
                ProjectPaths.clientTestsPath
        ]
        |> runParallel

    let Run () =
        [
            runAsyncColored "server" ConsoleColor.Blue "dotnet" [ "run" ] ProjectPaths.serverTestsPath
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
                    $"{ProjectPaths.clientTestsPath}/output/Client.Tests.js"
                ]
                ProjectPaths.clientTestsPath
            runAsyncColored "components" ConsoleColor.Yellow "npm" [ "run"; "test:run" ] ProjectPaths.componentTestsPath
        ]
        |> runParallel

// Target.create "Format" (fun _ -> run dotnet [ "fantomas"; "."; "-r" ] "src")

// Target.create "Setup" ignore

// Target.create "Ignore" (fun _ -> Trace.trace "Hit ignore")

module Release =

    open SimpleExec

    let npm (key: string) (version: Changelog.Version) (isDryRun: bool) =

        VersionTasks.updateComponentsPackageJSONVersion version

        let isPrerelease = version.Version.IsPrerelease

        async {
            do!
                Command.RunAsync("npm", [ "run"; "build" ], workingDirectory = ProjectPaths.componentsPath)
                |> Async.AwaitTask

            do!
                Command.RunAsync(
                    "npm",
                    [
                        "publish"
                        "--access"
                        "public"
                        if isPrerelease then
                            "--tag"
                            "next"
                        "--otp"
                        if isDryRun then
                            "--dry-run"
                    ],
                    workingDirectory = ProjectPaths.componentsPath
                )
                |> Async.AwaitTask
        }
        |> Async.RunSynchronously

    let nuget (key: string) (version: Changelog.Version) (isDryRun: bool) =

        VersionTasks.updateVersionFiles version
        VersionTasks.updateFSharpProjectVersions version

        let mkCssFile =
            Command.RunAsync("npm", [ "run"; "prebuild:net" ], workingDirectory = ProjectPaths.componentsPath)
            |> Async.AwaitTask

        let pack =
            Command.RunAsync(
                "dotnet",
                [
                    "pack"
                    ProjectPaths.nugetSln
                    "--configuration"
                    "Release"
                    "--output"
                    ProjectPaths.nugetDeployPath
                ]
            )
            |> Async.AwaitTask

        let publish =
            Command.RunAsync(
                "dotnet",
                [
                    "nuget"
                    "push"
                    $"{ProjectPaths.nugetDeployPath}/*.nupkg"
                    "--api-key"
                    key
                    "--source"
                    "https://api.nuget.org/v3/index.json"
                    if isDryRun then
                        "--dry-run"
                ]
            )
            |> Async.AwaitTask

        async {
            do! mkCssFile

            do! pack

            do! publish
        }
        |> Async.RunSynchronously

    let docker (username: string) (key: string) (version: Changelog.Version) (isDryRun: bool) =
        // Placeholder for docker release logic

        let dockerRegistryTarget = "ghcr.io"
        let imageName = "ghcr.io/nfdi4plants/swate"

        let login =
            Command.RunAsync("docker", [ "login"; dockerRegistryTarget; "--username"; username; "--password"; key ])
            |> Async.AwaitTask

        let isPrerelease = version.Version.IsPrerelease

        let imageVersioned = $"{imageName}:{version.Version.ToString()}"
        let imageLatest = $"{imageName}:latest"
        let imageNext = $"{imageName}:next"

        let build =
            Command.RunAsync(
                "docker",
                [
                    "build"
                    "-f"
                    ProjectPaths.dockerFilePath
                    if isPrerelease then
                        "-t"
                        imageNext
                    else
                        "-t"
                        imageVersioned
                        "-t"
                        imageLatest
                ]
            )
            |> Async.AwaitTask

        let push = async {
            if isPrerelease then
                do! Command.RunAsync("docker", [ "push"; imageNext ]) |> Async.AwaitTask
            else
                do! Command.RunAsync("docker", [ "push"; imageVersioned ]) |> Async.AwaitTask
                do! Command.RunAsync("docker", [ "push"; imageLatest ]) |> Async.AwaitTask
        }

        async {
            do! login
            do! build

            if not isDryRun then
                do! push
        }
        |> Async.RunSynchronously

    open System.IO
    open System.IO.Compression

    /// This currently builds the frontend, zips it to add it as asset to github release
    let electron (version: Changelog.Version) (token: string) (isDryRun: bool) =
        VersionTasks.updateVersionFiles version

        let sourceDir = Path.Combine(ProjectPaths.deployPath, "public")
        let targetZip = "./SwateClient.zip"

        async {
            Bundle.Client(false)

            if File.Exists(targetZip) then
                File.Delete(targetZip)

            ZipFile.CreateFromDirectory(sourceDir, targetZip, CompressionLevel.Optimal, includeBaseDirectory = false)

            let response = GitHub.uploadReleaseAsset token version targetZip

            ()

        }
        |> Async.RunSynchronously

[<EntryPoint>]
let main args =
    let argv = args |> Array.map (fun x -> x.ToLower()) |> Array.toList

    match argv with
    | "bundle" :: a ->
        run "npm" [ "install" ] "."

        match a with
        | "client" :: a ->
            match a with
            | "standalone" :: _ ->
                Bundle.Client(false)
                0
            | _ ->
                Bundle.Client(true)
                0
        | _ ->
            Bundle.All()
            0
    | "run" :: a ->
        match a with
        | "db" :: a ->
            Run.All(true)
            0
        | "client" :: a ->
            run "dotnet" Run.ClientArgs ProjectPaths.clientPath
            0
        | _ ->
            Run.All(false)
            0
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
    | "release" :: target :: otherArgs ->
        let latestVersion = Changelog.getLatestVersion ()
        let isDryRun = otherArgs |> List.contains "--dry-run"
        let isCi = otherArgs |> List.contains "--ci"

        if not isCi then
            printRedfn "Currently the worklow only supports CI releases!"
            exit 1

        let ghRelease =
            GitHub.mkRelease (Environment.GetEnvironmentVariable "GITHUB_TOKEN") latestVersion

        match target with
        | "nuget" ->
            let key = Environment.GetEnvironmentVariable "NUGET_KEY"

            if String.IsNullOrWhiteSpace key then
                printRedfn "No nuget key set for environmental variables!"
                exit 1

            Release.nuget key latestVersion isDryRun

            printGreenfn ("Release nuget!")
            0
        | "npm" ->
            let key = Environment.GetEnvironmentVariable "NPM_KEY"

            if String.IsNullOrWhiteSpace key then
                printRedfn "No npm key set for environmental variables!"
                exit 1

            printGreenfn ("Starting NPM release!")

            Release.npm key latestVersion isDryRun

            printGreenfn "Released npm package version %O" latestVersion.Version
            0
        | "docker" ->
            let key = Environment.GetEnvironmentVariable "DOCKER_KEY"

            let user =
                otherArgs
                |> List.tryFind (fun x -> x.StartsWith "--user=")
                |> Option.map (fun x -> x.Substring 7)

            if user.IsNone then
                printRedfn "No docker user set! Please pass user in the format --user=yourusername"
                exit 1

            if String.IsNullOrWhiteSpace key then
                printRedfn "No docker key set for environmental variables!"
                exit 1

            Release.docker user.Value key latestVersion isDryRun

            printGreenfn ("Release docker!")
            0
        | "electron" ->
            printGreenfn ("Release electron!")
            printfn "This currently also does the github Release"
            0
        | _ ->
            printRedfn ("No valid release target provided!")
            1
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
    | "check-release" :: a ->
        let latestVersion = Changelog.getLatestVersion ()
        let tags = GIT.getTags () |> Array.toList
        let nextTag = latestVersion.Version.ToString()

        if tags |> List.contains (nextTag) && nextTag <> "1.0.0-rc.9" then
            printRedfn
                "The latest version %O from CHANGELOG.md is already tagged in git. No release needed."
                latestVersion.Version

            GHActions.setShouldSkip ()
            1
        else
            printGreenfn "The latest version %O from CHANGELOG.md is not yet tagged in git." latestVersion.Version
            GIT.createTagAndPush (nextTag)
            0
    | "dev" :: a ->

        // run git [ "add"; "." ] ""
        // run git [ "commit"; "-m"; (sprintf "Release v%s" ProjectInfo.prereleaseTag) ] ""
        0
    | _ ->
        Console.WriteLine("No valid argument provided. Please provide a valid target.")
        1