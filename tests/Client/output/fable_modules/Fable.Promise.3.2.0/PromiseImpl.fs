[<AutoOpen>]
module PromiseImpl

let promise = Promise.PromiseBuilder()

[<RequireQualifiedAccess>]
module SettledValue =

    /// <summary>
    /// Helper functionm to convert a settled result into an F# result
    /// </summary>
    /// <param name="p">The settled result</param>
    /// <typeparam name="'T"></typeparam>
    /// <returns>The F# result</returns>
    /// <example>
    /// <code lang="fsharp">
    /// Promise.allSettled [success; rejection]
    /// |> Promise.iter (fun results ->
    ///     for result in results do 
    ///       match SettledValue.toResult result  with 
    ///       | Ok value ->
    ///         printfn "Success: %A" value
    ///       | Error err -> 
    ///         eprintfn "Error: %O" err
    /// )
    /// </code>
    /// </example>
    let toResult (p: Promise.SettledValue<'T>) =
        match p.status with 
        | Promise.Fulfilled -> Ok p.value.Value
        | Promise.Rejected -> Error p.reason.Value