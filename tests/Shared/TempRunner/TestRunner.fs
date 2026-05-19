module TempRunner

open Expecto
open Swate.Components.Shared.Tests

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv shared
