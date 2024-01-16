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
    // Use `fromRawCommand` rather than `fromRawCommandLine`, as its behaviour is less likely to be misunderstood.
    // See https://github.com/SAFE-Stack/SAFE-template/issues/551.
    CreateProcess.fromRawCommand exe arg
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.ensureExitCode

let dotnet args dir = createProcess "dotnet" args dir

let docker args dir = createProcess "docker" args dir

let dockerCompose args dir = createProcess "docker-compose" args dir

let git args dir = createProcess "git" args dir

let npm args dir =
    let npmPath =
        match ProcessUtils.tryFindFileOnPath "npm" with
        | Some path -> path
        | None ->
            "npm was not found in path. Please install it and make sure it's available from your path. "
            + "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
            |> failwith

    createProcess npmPath args dir

let npx args dir =
    let npxPath =
        match ProcessUtils.tryFindFileOnPath "npx" with
        | Some path -> path
        | None ->
            "npx was not found in path. Please install it and make sure it's available from your path."
            |> failwith
    createProcess npxPath args dir

let node args dir =
    let nodePath =
        match ProcessUtils.tryFindFileOnPath "node" with
        | Some path -> path
        | None ->
            "node was not found in path. Please install it and make sure it's available from your path."
            |> failwith
    createProcess nodePath args dir

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

let run proc arg dir = proc arg dir |> Proc.run |> ignore

let runParallel processes = processes |> Proc.Parallel.run |> ignore

let prompt (msg:string) =
    System.Console.Write(msg)
    System.Console.ReadLine().Trim()
    |> function | "" -> None | s -> Some s
    |> Option.map (fun s -> s.Replace ("\"","\\\""))

let rec promptYesNo msg =
    match prompt (sprintf "%s [Yn]: " msg) with
    | Some "Y" | Some "y" -> true
    | Some "N" | Some "n" -> false
    | _ -> System.Console.WriteLine("Sorry, invalid answer"); promptYesNo msg

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
