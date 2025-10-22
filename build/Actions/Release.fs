[<RequireQualifiedAccessAttribute>]
module Release

open SimpleExec
open ProjectInfo

let npm (key: string) (version: Changelog.Version) (isDryRun: bool) =

    VersionIO.updateComponentsPackageJSONVersion version

    let isPrerelease = version.Version.IsPrerelease

    try
        run
            "npm"
            [ "view"; $"{ProjectInfo.componentsPackageName}@{version.Version.ToString()}" ]
            ProjectPaths.componentsPath

        printGreenfn
            $"Package {ProjectInfo.componentsPackageName} version {version.Version.ToString()} already published to npmjs.org, skipping publish step."
    with _ ->

        let build = run "npm" [ "run"; "build" ] ProjectPaths.componentsPath

        let setConfig =
            run "npm" [ "config"; "set"; $"//registry.npmjs.org/:_authToken={key}" ] ProjectPaths.componentsPath

        let cssFilePath = @"src/Components/dist/swate-components.css"

        if not (System.IO.File.Exists(cssFilePath)) then
            failwithf "CSS file not found at %s" cssFilePath

        let content = System.IO.File.ReadAllText(cssFilePath)
        let replacedContent = content.Replace("@layer base", "@layer swt-base")
        System.IO.File.WriteAllText(cssFilePath, replacedContent)

        let publish =
            run
                "npm"
                [
                    "publish"
                    "--access"
                    "public"
                    if isPrerelease then
                        "--tag"
                        "next"
                    if isDryRun then
                        "--dry-run"
                ]
                ProjectPaths.componentsPath

        ()

    ()

let nuget (key: string) (version: Changelog.Version) (isDryRun: bool) =

    VersionIO.updateVersionFiles version
    VersionIO.updateFSharpProjectVersions version

    let mkCssFile = run "npm" [ "run"; "prebuild:net" ] ProjectPaths.componentsPath

    let pack =
        run
            "dotnet"
            [
                "pack"
                ProjectPaths.nugetSln
                "--configuration"
                "Release"
                "--output"
                ProjectPaths.nugetDeployPath
            ]
            ""

    let publish =
        if not isDryRun then
            run
                "dotnet"
                [
                    "nuget"
                    "push"
                    $"{ProjectPaths.nugetDeployPath}/*.nupkg"
                    "--api-key"
                    key
                    "--skip-duplicate"
                    "--source"
                    "https://api.nuget.org/v3/index.json"
                ]
                ""

    ()

let docker (username: string) (key: string) (version: Changelog.Version) (isDryRun: bool) =
    // Placeholder for docker release logic

    let dockerRegistryTarget = "ghcr.io"
    let imageName = "ghcr.io/nfdi4plants/swate"

    let login =
        run "docker" [ "login"; "--username"; username; "--password"; key; dockerRegistryTarget ] ""

    let isPrerelease = version.Version.IsPrerelease

    let imageVersioned = $"{imageName}:{version.Version.ToString()}"
    let imageLatest = $"{imageName}:latest"
    let imageNext = $"{imageName}:next"

    let build =
        run
            "docker"
            [
                "build"
                if isPrerelease then
                    "-t"
                    imageNext
                else
                    "-t"
                    imageVersioned
                    "-t"
                    imageLatest
                "-f"
                ProjectPaths.dockerFilePath
                "."
            ]
            ""

    let push =
        if not isDryRun then
            if isPrerelease then
                run "docker" [ "push"; imageNext ] ""
            else
                run "docker" [ "push"; imageVersioned ] ""
                run "docker" [ "push"; imageLatest ] ""

    ()

open System.IO
open System.IO.Compression

/// This currently builds the frontend, zips it to add it as asset to github release
let electron (version: Changelog.Version) (token: string) (isDryRun: bool) =
    VersionIO.updateVersionFiles version

    let sourceDir = Path.Combine(ProjectPaths.deployPath, "public")
    let targetZip = "./SwateClient.zip"

    Bundle.Client(false)

    if File.Exists(targetZip) then
        File.Delete(targetZip)

    ZipFile.CreateFromDirectory(sourceDir, targetZip, CompressionLevel.Optimal, includeBaseDirectory = false)

    let response = GitHub.uploadReleaseAsset token version targetZip

    ()

let storybook () =
    run "npm" [ "run"; "build:storybook" ] ProjectPaths.componentsPath