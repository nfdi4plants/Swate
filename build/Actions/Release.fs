[<RequireQualifiedAccessAttribute>]
module Release

open SimpleExec
open ProjectInfo
open System.IO

let npm (version: Changelog.Version) (isDryRun: bool) =

    let isPrerelease = version.Version.IsPrerelease

    try
        run
            "npm"
            [
                "view"
                $"{ProjectInfo.componentsPackageName}@{version.Version.ToString()}"
            ]
            ProjectPaths.componentsPath

        printGreenfn
            $"Package {ProjectInfo.componentsPackageName} version {version.Version.ToString()} already published to npmjs.org, skipping publish step."
    with _ ->
        printGreenfn
            $"Publishing package {ProjectInfo.componentsPackageName} version {version.Version.ToString()} to npmjs.org."

        let version = VersionIO.updateComponentsPackageJSONVersion version

        printGreenfn "Start building npm package"

        let build = run "npm" [ "run"; "build" ] ProjectPaths.componentsPath

        printGreenfn "Start updating .css file to use @layer swt-base instead of @layer base"

        let cssFilePath =
            Path.Combine(ProjectPaths.componentsPath, "dist", "assets", "swate-components.css")

        if not (System.IO.File.Exists(cssFilePath)) then
            failwithf "CSS file not found at %s" cssFilePath

        let content = System.IO.File.ReadAllText(cssFilePath)
        let replacedContent = content.Replace("@layer base", "@layer swt-base")
        System.IO.File.WriteAllText(cssFilePath, replacedContent)

        printGreenfn "Start publishing npm package"

        let publish =
            run
                "npm"
                [
                    "publish"
                    "--access"
                    "public"
                    "--provenance"
                    if isPrerelease then
                        "--tag"
                        "next"
                    if isDryRun then
                        "--dry-run"
                ]
                ProjectPaths.componentsPath

        ()

    ()

let nuget (key: string) (isDryRun: bool) =

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

    VersionIO.updateVersionFiles version
    VersionIO.updateFSharpProjectVersions version

    let dockerRegistryTarget = "ghcr.io"
    let imageName = "ghcr.io/nfdi4plants/swate"

    let login =
        run
            "docker"
            [
                "login"
                "--username"
                username
                "--password-stdin"
                key
                dockerRegistryTarget
            ]
            ""

    let isPrerelease = version.Version.IsPrerelease

    let imageVersioned = $"{imageName}:{version.Version.ToString()}"
    let imageLatest = $"{imageName}:latest"
    let imageNext = $"{imageName}:next"

    // docker build -t ghcr.io/nfdi4plants/swate:next -t ghcr.io/nfdi4plants/swate:2.0.0 -t ghcr.io/nfdi4plants/swate:latest -f ./build/Dockerfile.publish .

    let build =
        run
            "docker"
            [
                "build"
                "-t"
                imageNext
                if not isPrerelease then
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
            run "docker" [ "push"; imageNext ] ""

            if not isPrerelease then
                run "docker" [ "push"; imageVersioned ] ""
                run "docker" [ "push"; imageLatest ] ""

    ()

open System.IO
open System.IO.Compression

/// Builds the frontend and zips it for the GitHub release
let electron () =

    let sourceDir = Path.Combine(ProjectPaths.deployPath, "public")
    let targetZip = "./SwateClient.zip"

    Bundle.Client(false)

    if File.Exists(targetZip) then
        File.Delete(targetZip)

    ZipFile.CreateFromDirectory(sourceDir, targetZip, CompressionLevel.Optimal, includeBaseDirectory = false)

    ()

let electronBinaries (arch: string) =

    run "npm" [ "run"; "fable" ] ProjectPaths.electronPath

    run "npx" [ "electron-forge"; "make"; sprintf "--arch=%s" arch ] ProjectPaths.electronPath

    ()

let storybook () =
    run "npm" [ "run"; "build:storybook" ] ProjectPaths.componentsPath
