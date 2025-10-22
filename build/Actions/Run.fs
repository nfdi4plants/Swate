[<RequireQualifiedAccessAttribute>]
module Run

open ProjectInfo

let ClientArgs = [
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


let All(db: bool) =
    [
        runAsync "server" "dotnet" [ "watch"; "run" ] ProjectPaths.serverPath
        runAsync "client" "dotnet" ClientArgs ProjectPaths.clientPath
        if db then
            runAsync
                "database"
                "docker-compose"
                [ "-f"; ProjectPaths.dockerComposePath; "up"; "-d" ]
                __SOURCE_DIRECTORY__
    ]
    |> runParallel