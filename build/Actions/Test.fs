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

    let All () = async {
        printGreenfn "Running all tests..."
        printGreenfn "Running server tests..."
        let! serverResult = server
        printGreenfn "Running client tests..."
        let! clientResult = client
        printGreenfn "Running component tests..."
        let! componentsResult = components

        match serverResult, clientResult, componentsResult with
        | Ok(), Ok(), Ok() ->
            printGreenfn "All tests passed!"
            exit 0
        | _ ->
            if serverResult.IsError then
                printRedfn "Server tests failed."

            if clientResult.IsError then
                printRedfn "Client tests failed."

            if componentsResult.IsError then
                printRedfn "Component tests failed."

            exit 1

    }