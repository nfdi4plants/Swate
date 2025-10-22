[<AutoOpenAttribute>]
module Util

open System

let inline printGreenfn fmt =
    Printf.kprintf
        (fun s ->
            let oldColor = Console.ForegroundColor
            Console.ForegroundColor <- ConsoleColor.Green
            Console.WriteLine s
            Console.ForegroundColor <- oldColor
        )
        fmt

let inline printRedfn fmt =
    Printf.kprintf
        (fun s ->
            let oldColor = Console.ForegroundColor
            Console.ForegroundColor <- ConsoleColor.Red
            Console.WriteLine s
            Console.ForegroundColor <- oldColor
        )
        fmt

open SimpleExec
open System.Diagnostics
open System.IO
open System.Threading.Tasks

let private shellCommand cmd args =
    let argStr = args |> String.concat " "

    if OperatingSystem.IsWindows() then
        "cmd", $"/c {cmd} {argStr}"
    else
        "/bin/bash", $"-c \"{cmd} {argStr}\""

let run (cmd: string) (args: seq<string>) (workingDir: string) =
    try
        Command.Run(cmd, args = args, workingDirectory = workingDir)
    with ex ->
        printRedfn "Error while running command: %s" ex.Message
        exit 1

let runReadAsync (cmd: string) (args: seq<string>) (workingDir: string) =
    try
        Command.ReadAsync(cmd, args = args, workingDirectory = workingDir)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> _.ToTuple()
    with ex ->
        printRedfn "Error while running command: %s" ex.Message
        exit 1

let runAsync (prefix: string) (cmd: string) (args: seq<string>) (workingDir: string) = async {
    try
        do!
            Command.RunAsync(cmd, args = args, workingDirectory = workingDir, echoPrefix = prefix)
            |> Async.AwaitTask

        return Ok()
    with ex ->
        printRedfn "[%s] Error: %s" prefix ex.Message
        return Error ex
}

let runAsyncColored prefix color cmd args workingDir = async {
    let shell, arguments = shellCommand cmd args
    let psi = ProcessStartInfo()
    psi.FileName <- shell
    psi.Arguments <- arguments
    psi.WorkingDirectory <- workingDir
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    psi.CreateNoWindow <- true

    let proc = new Process()
    proc.StartInfo <- psi
    let oldColor = Console.ForegroundColor

    let print (isError: bool) (line: string) =
        if not (String.IsNullOrWhiteSpace line) then
            Console.ForegroundColor <- color
            Console.Write($"[{prefix}] ")
            Console.ForegroundColor <- if isError then ConsoleColor.Red else oldColor
            Console.WriteLine(line)
            Console.ResetColor()

    proc.OutputDataReceived.Add(fun e ->
        if e.Data <> null then
            print false e.Data
    )

    proc.ErrorDataReceived.Add(fun e ->
        if e.Data <> null then
            print false e.Data
    )

    try
        if not (proc.Start()) then
            failwithf "Failed to start %s" cmd

        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()

        do! proc.WaitForExitAsync() |> Async.AwaitTask

        if proc.ExitCode = 0 then
            proc.Close()
            Console.ResetColor()
            return Ok()
        else
            print true $"Exited with code {proc.ExitCode}"
            proc.Close()
            Console.ResetColor()
            return Error(exn (sprintf "Process exited with code %d" proc.ExitCode))
    with ex ->
        print true $"Exception: {ex.Message}"
        proc.Close()
        Console.ResetColor()
        return Error(ex)
}


let runParallel (tasks: Async<Result<unit, exn>> list) =
    async {
        let! results = tasks |> Async.Parallel

        let errors =
            results
            |> Array.choose (
                function
                | Error e -> Some e
                | Ok _ -> None
            )

        if errors.Length > 0 then
            printRedfn "%d task(s) failed" errors.Length
            errors |> Array.iter (fun e -> printRedfn " - %s" e.Message)
            exit 1
        else
            printGreenfn "All tasks completed successfully"
    }
    |> Async.RunSynchronously

let getEnvironementVariableOrFail (name: string) =
    let value = Environment.GetEnvironmentVariable(name)

    if String.IsNullOrWhiteSpace value then
        failwithf "Environment variable %s is not set or empty" name
        exit 1
    else
        value