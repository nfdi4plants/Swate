[<RequireQualifiedAccessAttribute>]
module Release

open SimpleExec
open ProjectInfo

let npm (key: string) (version: Changelog.Version) (isDryRun: bool) =

    VersionIO.updateComponentsPackageJSONVersion version

    let isPrerelease = version.Version.IsPrerelease

    let build =
        Command.RunAsync("npm", [ "run"; "build" ], workingDirectory = ProjectPaths.componentsPath)
        |> Async.AwaitTask

    let setConfig =
        Command.RunAsync("npm", [ "config"; "set"; $"//registry.npmjs.org/:_authToken={key}" ])
        |> Async.AwaitTask

    let publish =
        Command.RunAsync(
            "npm",
            [
                "publish"
                "--access"
                "public"
                if isPrerelease then
                    "--tag"
                    "next"
                if isDryRun then
                    "--dry-run"
            ],
            workingDirectory = ProjectPaths.componentsPath
        )
        |> Async.AwaitTask


    async {
        do! build

        do! setConfig

        do! publish
    }
    |> Async.RunSynchronously

let nuget (key: string) (version: Changelog.Version) (isDryRun: bool) =

    VersionIO.updateVersionFiles version
    VersionIO.updateFSharpProjectVersions version

    let installNpmDependencies =
        Command.RunAsync("npm", [ "ci" ], workingDirectory = ProjectPaths.componentsPath)
        |> Async.AwaitTask

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
        Command.RunAsync(
            "docker",
            [
                "login"
                dockerRegistryTarget
                "--username"
                username
                "--password-stdin"
                key
            ]
        )
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
                if isPrerelease then
                    "-t"
                    imageNext
                else
                    "-t"
                    imageVersioned
                    "-t"
                    imageLatest
                ProjectPaths.dockerFilePath
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
    VersionIO.updateVersionFiles version

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

let storybook () =
    run "npm" [ "run"; "build:storybook" ] ProjectPaths.componentsPath