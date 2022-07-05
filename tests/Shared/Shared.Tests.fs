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
    let case_SourceName = "Source Name"
    let case_SampleName = "Sample Name"
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
    let case_Underscore = "Term Source REF (KF_PH:001)"
    let case_URI = "http://purl.obolibrary.org/obo/GO_000001"
    let case_numberInIDSPACE = "Term Source REF (nfdi4pso:001)"

let regex = testList "Regex patterns" [
    testCase "CoreNamePattern 'Source Name'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case_SourceName, Pattern.CoreNamePattern)
        let regexValue = regExMatch.Value
        Expect.equal regexValue "Source Name" ""

    testCase "CoreNamePattern 'Characteristic [Sample type]'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case3, Pattern.CoreNamePattern)
        let regexValue = regExMatch.Value.Trim()
        Expect.equal regexValue "Characteristics" ""

    testCase "CoreNamePattern 'Term Source REF (NFDI4PSO:0000064)'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case8, Pattern.CoreNamePattern)
        let regexValue = regExMatch.Value.Trim()
        Expect.equal regexValue "Term Source REF" ""

    testCase "CoreNamePattern 'Term Accession Number (MS:1001809#2)'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case11, Pattern.CoreNamePattern)
        let regexValue = regExMatch.Value.Trim()
        Expect.equal regexValue "Term Accession Number" ""

    testCase "CoreNamePattern 'Unit (#3)'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case13, Pattern.CoreNamePattern)
        let regexValue = regExMatch.Value.Trim()
        Expect.equal regexValue "Unit" ""

    testCase "CoreNamePattern 'Term Accession Number ()'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case14, Pattern.CoreNamePattern)
        let regexValue = regExMatch.Value.Trim()
        Expect.equal regexValue "Term Accession Number" ""

    testCase "parseTermAccession 'http://purl.obolibrary.org/obo/GO_000001'" <| fun _ ->
        let regExMatch = Regex.parseTermAccession TestCases.case_URI
        Expect.equal regExMatch (Some "GO:000001") ""

    testCase "parseTermAccession 'Term Source REF (KF_PH:001)'" <| fun _ ->
        let regExMatch = Regex.parseTermAccession TestCases.case_Underscore
        Expect.equal regExMatch (Some "KF_PH:001") ""

    testCase "TermAccessionPatternURI 'Term Source REF (KF_PH:001)'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case_Underscore, Pattern.TermAccessionPatternURI)
        Expect.equal regExMatch.Success false ""

    testCase "TermAccessionPattern 'Term Source REF (KF_PH:001)'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case_Underscore, Pattern.TermAccessionPattern)
        Expect.equal regExMatch.Value "KF_PH:001" ""

    testCase "TermAccessionPattern 'Term Source REF (nfdi4pso:001)'" <| fun _ ->
        let regExMatch = Regex.Match(TestCases.case_numberInIDSPACE, Pattern.TermAccessionPattern)
        Expect.equal regExMatch.Value "nfdi4pso:001" ""
]


let shared = testList "Shared" [
    regex
    OfficeInteropTypes.buildingBlockTypes
]