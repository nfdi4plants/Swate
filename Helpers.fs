module Helpers

open Fake.Core
open System.Runtime.InteropServices

let initializeContext () =
    let execContext = Context.FakeExecutionContext.Create false "build.fsx" [ ]
    Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

module Proc =
    module Parallel =
        open System

        let locker = obj()

        let colors =
            [| ConsoleColor.Blue
               ConsoleColor.Yellow
               ConsoleColor.Magenta
               ConsoleColor.Cyan
               ConsoleColor.DarkBlue
               ConsoleColor.DarkYellow
               ConsoleColor.DarkMagenta
               ConsoleColor.DarkCyan |]

        let print color (colored: string) (line: string) =
            lock locker
                (fun () ->
                    let currentColor = Console.ForegroundColor
                    Console.ForegroundColor <- color
                    Console.Write colored
                    Console.ForegroundColor <- currentColor
                    Console.WriteLine line)

        let onStdout index name (line: string) =
            let color = colors.[index % colors.Length]
            if isNull line then
                print color $"{name}: --- END ---" ""
            else if String.isNotNullOrEmpty line then
                print color $"{name}: " line

        let onStderr name (line: string) =
            let color = ConsoleColor.Red
            if isNull line |> not then
                print color $"{name}: " line

        let redirect (index, (name, createProcess)) =
            createProcess
            |> CreateProcess.redirectOutputIfNotRedirected
            |> CreateProcess.withOutputEvents (onStdout index name) (onStderr name)

        let printStarting indexed =
            for (index, (name, c: CreateProcess<_>)) in indexed do
                let color = colors.[index % colors.Length]
                let wd =
                    c.WorkingDirectory
                    |> Option.defaultValue ""
                let exe = c.Command.Executable
                let args = c.Command.Arguments.ToStartInfo
                print color $"{name}: {wd}> {exe} {args}" ""

        let run cs =
            cs
            |> Seq.toArray
            |> Array.indexed
            |> fun x -> printStarting x; x
            |> Array.map redirect
            |> Array.Parallel.map Proc.run

let createProcess exe arg dir =
    CreateProcess.fromRawCommandLine exe arg
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.ensureExitCode

let dotnet = createProcess "dotnet"
let npm =
    let npmPath =
        match ProcessUtils.tryFindFileOnPath "npm" with
        | Some path -> path
        | None ->
            "npm was not found in path. Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
            |> failwith

    createProcess npmPath
let npx =
    let npxPath =
        match ProcessUtils.tryFindFileOnPath "npx" with
        | Some path -> path
        | None ->
            "npx was not found in path. Please install it and make sure it's available from your path."
            |> failwith
    createProcess npxPath
let node =
    let nodePath =
        match ProcessUtils.tryFindFileOnPath "node" with
        | Some path -> path
        | None ->
            "node was not found in path. Please install it and make sure it's available from your path."
            |> failwith
    createProcess nodePath

    
let dockerCompose = createProcess "docker-compose"

///Choose process to open plots with depending on OS. Thanks to @zyzhu for hinting at a solution (https://github.com/plotly/Plotly.NET/issues/31)
let openBrowser url =
    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
        CreateProcess.fromRawCommand "cmd.exe" [ "/C"; $"start {url}" ] |> Proc.run |> ignore
    elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
        CreateProcess.fromRawCommand "xdg-open" [ url ] |> Proc.run |> ignore
    elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
        CreateProcess.fromRawCommand "open" [ url ] |> Proc.run |> ignore
    else
        failwith "Cannot open Browser. OS not supported."

let run proc arg dir =
    proc arg dir
    |> Proc.run
    |> ignore

let runParallel processes =
    processes
    |> Proc.Parallel.run
    |> ignore

let runOrDefault args =
    Trace.trace (sprintf "%A" args)
    try
        match args with
        | [| target |] -> Target.runOrDefault target
        | arr when args.Length > 1 ->
            Target.run 0 (Array.head arr) ( Array.tail arr |> List.ofArray )
        | _ -> Target.runOrDefault "Ignore" 
        0
    with e ->
        printfn "%A" e
        1
