module Swate.Components.Shared.Tests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

let example_tests = testList "example" [
    testCase "One" <| fun _ ->
        Expect.equal 1 1 ""
]

let shared = testList "Shared" [
    example_tests
]