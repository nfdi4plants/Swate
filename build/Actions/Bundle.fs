[<RequireQualifiedAccessAttribute>]
module Bundle

open ProjectInfo

let Client(forSwate: bool) =

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

let All() =
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