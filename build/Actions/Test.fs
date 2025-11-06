[<RequireQualifiedAccessAttribute>]
module Test

open ProjectInfo
open System

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

module Run =
    let server = runAsync "server" "dotnet" [ "run" ] ProjectPaths.serverTestsPath

    let client =
        runAsync
            "client"
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

    let components =
        runAsync "components" "npm" [ "run"; "test:run" ] ProjectPaths.componentTestsPath

    let All () =
        [ server; client; components ] |> runParallel