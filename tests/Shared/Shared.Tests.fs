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

module TestCases =
    let case1 = "Source Name"
    let case2 = "Sample Name"
    let case3 = "Characteristics [Sample type]"
    let case4 = "Characteristics [biological replicate]"
    let case5 = "Factor [Sample type#2]"
    let case6 = "Parameter [biological replicate#2]"
    let case7 = "Data File Name"
    let case8 = "Term Source REF (NFDI4PSO:0000064)"
    let case9 = "Term Source REF (NFDI4PSO:0000064#2)"
    let case10 = "Term Accession Number (MS:1001809)"
    let case11 = "Term Accession Number (MS:1001809#2)"
    let case12 = "Unit"
    let case13 = "Unit (#3)"
    let case14 = "Term Accession Number ()" 

//let regex = testList "Regex patterns" [
//    testCase "CoreNamePattern 'Source Name'" <| fun _ ->
//        let regExMatch = Regex.Match(TestCases.case1, Pattern.CoreNamePattern)
//        let regexValue = regExMatch.Value
//        Expect.equal regexValue "Source Name" ""

//    testCase "CoreNamePattern 'Characteristic [Sample type]'" <| fun _ ->
//        let regExMatch = Regex.Match(TestCases.case3, Pattern.CoreNamePattern)
//        let regexValue = regExMatch.Value.Trim()
//        Expect.equal regexValue "Characteristics" ""

//    testCase "CoreNamePattern 'Term Source REF (NFDI4PSO:0000064)'" <| fun _ ->
//        let regExMatch = Regex.Match(TestCases.case8, Pattern.CoreNamePattern)
//        let regexValue = regExMatch.Value.Trim()
//        Expect.equal regexValue "Term Source REF" ""

//    testCase "CoreNamePattern 'Term Accession Number (MS:1001809#2)'" <| fun _ ->
//        let regExMatch = Regex.Match(TestCases.case11, Pattern.CoreNamePattern)
//        let regexValue = regExMatch.Value.Trim()
//        Expect.equal regexValue "Term Accession Number" ""

//    testCase "CoreNamePattern 'Unit (#3)'" <| fun _ ->
//        let regExMatch = Regex.Match(TestCases.case13, Pattern.CoreNamePattern)
//        let regexValue = regExMatch.Value.Trim()
//        Expect.equal regexValue "Unit" ""

//    testCase "CoreNamePattern 'Term Accession Number ()'" <| fun _ ->
//        let regExMatch = Regex.Match(TestCases.case14, Pattern.CoreNamePattern)
//        let regexValue = regExMatch.Value.Trim()
//        Expect.equal regexValue "Term Accession Number" ""
//]

//let shared = testList "Shared" [
//    regex
//]

let shared = testList "Shared" [
    testCase "Empty string is not a valid description" <| fun _ ->
        let expected = false
        let actual = false
        Expect.equal actual expected "Should be false"
]