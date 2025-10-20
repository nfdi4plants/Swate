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