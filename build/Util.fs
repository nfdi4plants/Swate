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
open System.Threading

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

// let runAsyncColored prefix color (cmd: string) (args: seq<string>) (workingDir: string) = async {
//     // Start the process directly (no cmd/bash wrapper) so killing the tree is reliable
//     let psi = ProcessStartInfo()
//     psi.FileName <- cmd

//     for a in args do
//         psi.ArgumentList.Add(a)

//     psi.WorkingDirectory <- workingDir
//     psi.RedirectStandardOutput <- false
//     psi.RedirectStandardError <- false
//     psi.RedirectStandardInput <- false
//     psi.UseShellExecute <- false
//     psi.CreateNoWindow <- true

//     let! ct = Async.CancellationToken

//     use proc = new Process()
//     proc.StartInfo <- psi

//     let oldColor = Console.ForegroundColor

//     let print (isError: bool) (line: string) =
//         if not (String.IsNullOrWhiteSpace line) then
//             Console.ForegroundColor <- color
//             Console.Write($"[{prefix}] ")
//             Console.ForegroundColor <- if isError then ConsoleColor.Red else oldColor
//             Console.WriteLine(line)
//             Console.ResetColor()

//     proc.OutputDataReceived.Add(fun e ->
//         if e.Data <> null then
//             print false e.Data
//     )

//     proc.ErrorDataReceived.Add(fun e ->
//         if e.Data <> null then
//             print true e.Data
//     )

//     // Ensure we kill the full process tree on cancellation (with a short grace period), with a Windows fallback
//     // use _killReg =
//     //     ct.Register(fun () ->
//     //         // Always log the intent so both client and server show a line
//     //         print true $"Killing {prefix} (pid {proc.Id})..."

//     //         if not proc.HasExited then

//     //             if not proc.HasExited then
//     //                 try
//     //                     proc.Kill(entireProcessTree = true)
//     //                     proc.WaitForExit(5000) |> ignore
//     //                 with _ ->
//     //                     ()

//     //             if not proc.HasExited && OperatingSystem.IsWindows() then
//     //                 try
//     //                     let tk = ProcessStartInfo()
//     //                     tk.FileName <- "taskkill"
//     //                     tk.Arguments <- $"/PID {proc.Id} /T /F"
//     //                     tk.UseShellExecute <- false
//     //                     tk.CreateNoWindow <- true
//     //                     use p = Process.Start(tk)

//     //                     if not (isNull p) then
//     //                         p.WaitForExit(5000) |> ignore
//     //                 with _ ->
//     //                     ()
//     //     )

//     try
//         try
//             if not (proc.Start()) then
//                 failwithf "Failed to start %s" cmd

//             proc.BeginOutputReadLine()
//             proc.BeginErrorReadLine()

//             do! proc.WaitForExitAsync(ct) |> Async.AwaitTask

//             if proc.ExitCode = 0 then
//                 return Ok()
//             else
//                 print true $"Exited with code {proc.ExitCode}"
//                 return Error(exn (sprintf "Process exited with code %d" proc.ExitCode))
//         with ex ->
//             if not ct.IsCancellationRequested then
//                 print true $"Exception: {ex.Message}"

//             return Error(ex)
//     finally
//         Console.ResetColor()
// }


let runParallel (tasks: Async<Result<unit, exn>> list) =
    let cts = new CancellationTokenSource()
    Console.ResetColor()

    Console.CancelKeyPress.Add(fun e ->
        e.Cancel <- true
        cts.Cancel()
    )

    async {
        // Print immediately when cancellation is requested
        use! onCancel =
            Async.OnCancel(fun () ->
                printRedfn "Ctrl+C pressed → cancelling all tasks..."
                Console.ResetColor()
            )

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
    |> fun c ->
        try
            Async.RunSynchronously(c, cancellationToken = cts.Token)
        with :? OperationCanceledException ->
            // Swallow here; each child has already handled its own cancellation and printed
            ()


let getEnvironementVariableOrFail (name: string) =
    let value = Environment.GetEnvironmentVariable(name)

    if String.IsNullOrWhiteSpace value then
        failwithf "Environment variable %s is not set or empty" name
        exit 1
    else
        value