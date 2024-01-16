module Shared.Tests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open System
open System.Text.RegularExpressions

open Shared
open Shared.Regex


let example_tests = testList "example" [
    testCase "One" <| fun _ ->
        Expect.equal 1 1 ""
]

let shared = testList "Shared" [
    example_tests
]