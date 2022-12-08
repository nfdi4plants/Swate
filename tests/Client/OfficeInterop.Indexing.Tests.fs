module OfficeInterop.Indexing.Tests

open Fable.Mocha

open OfficeInterop.Indexing
open Shared.TermTypes
open Shared.OfficeInteropTypes

let Instrument_Model =
    InsertBuildingBlock.create
        (BuildingBlockNamePrePrint.create BuildingBlockType.Component "instrument model")
        (TermMinimal.create "instrument model" "MS:1000031" |> Some)
        None
        [||]

let Centrifugation_Time_MinuteUnit =
    InsertBuildingBlock.create
        (BuildingBlockNamePrePrint.create BuildingBlockType.Parameter "Centrifugation Time")
        (TermMinimal.create "Centrifugation Time" "NCIT:C178881" |> Some)
        (TermMinimal.create "minute" "UO:0000031" |> Some)
        [||]

/// iteratively create column names
let rec loop (names:string []) (buildingBlocks: InsertBuildingBlock []) (ind:int) =
    if ind >= buildingBlocks.Length then
        names
    else
        let position = buildingBlocks.[ind]
        let nextNames = createColumnNames position names |> Array.append names
        loop nextNames buildingBlocks (ind+1)

let tests_OfficeInterop_Indexing = testList "OfficeInterop_Indexing" [
    testCase "createUnit" <| fun _ ->
        let res = createUnit()
        let expected = "Unit"
        Expect.equal res expected ""
    testCase "createTSR" <| fun _ ->
        let res_Instrument_Model = createTSR Instrument_Model
        let res_Centrifugation_Time_MinuteUnit = createTSR Centrifugation_Time_MinuteUnit
        let expected1 = "Term Source REF (MS:1000031)"
        let expected2 = "Term Source REF (NCIT:C178881)"
        Expect.equal res_Instrument_Model expected1 "expected1"
        Expect.equal res_Centrifugation_Time_MinuteUnit expected2 "expected2"
    testCase "createTAN" <| fun _ ->
        let res_Instrument_Model = createTAN Instrument_Model
        let res_Centrifugation_Time_MinuteUnit = createTAN Centrifugation_Time_MinuteUnit
        let expected1 = "Term Accession Number (MS:1000031)"
        let expected2 = "Term Accession Number (NCIT:C178881)"
        Expect.equal res_Instrument_Model expected1 "expected1"
        Expect.equal res_Centrifugation_Time_MinuteUnit expected2 "expected2"
    testCase "toAnnotationHeader" <| fun _ ->
        let res_Instrument_Model = Instrument_Model.ColumnHeader.toAnnotationTableHeader()
        let res_Centrifugation_Time_MinuteUnit = Centrifugation_Time_MinuteUnit.ColumnHeader.toAnnotationTableHeader()
        let expected1 = "Component [instrument model]"
        let expected2 = "Parameter [Centrifugation Time]"
        Expect.equal res_Instrument_Model expected1 "expected1"
        Expect.equal res_Centrifugation_Time_MinuteUnit expected2 "expected2"
    let existingNames = loop [||] [|Instrument_Model; Centrifugation_Time_MinuteUnit|] 0
    testCase "checkExistingNames" <| fun _ ->
        Expect.hasLength existingNames 7 "hasLength"
        Expect.exists existingNames (fun x -> x = "Component [instrument model]") "exists: Component [instrument model]"
        Expect.exists existingNames (fun x -> x = "Parameter [Centrifugation Time]") "exists: Parameter [Centrifugation Time]"
        Expect.exists existingNames (fun x -> x = "Unit") "exists: Unit"
    testCase "extendName_mainColumn" <| fun _ ->
        let baseName = Instrument_Model.ColumnHeader.toAnnotationTableHeader()
        let res = extendName existingNames baseName
        Expect.equal res "Component [instrument model] " ""
    testCase "extendName_unit" <| fun _ ->
        let baseName = createUnit()
        let res = extendName existingNames baseName
        Expect.equal res "Unit " ""
    testCase "extendName_TSR" <| fun _ ->
        let res_Instrument_Model = createTSR Instrument_Model
        let res_Centrifugation_Time_MinuteUnit = createTSR Centrifugation_Time_MinuteUnit
        let res1 = extendName existingNames res_Instrument_Model
        let res2 = extendName existingNames res_Centrifugation_Time_MinuteUnit
        let expected1 = "Term Source REF (MS:1000031) "
        let expected2 = "Term Source REF (NCIT:C178881) "
        Expect.equal res1 expected1 "expected1"
        Expect.equal res2 expected2 "expected2"
    testCase "extendName_deep" <| fun _ ->
        let existingNames = loop [||] [|Instrument_Model; Centrifugation_Time_MinuteUnit; Centrifugation_Time_MinuteUnit; Centrifugation_Time_MinuteUnit; Centrifugation_Time_MinuteUnit|] 0
        let expected_deep_0 = "Parameter [Centrifugation Time]"
        let expected_deep_1 = "Parameter [Centrifugation Time] "
        let expected_deep_2 = "Parameter [Centrifugation Time]  "
        let expected_deep_3 = "Parameter [Centrifugation Time]   "
        let expected_deep_4 = "Parameter [Centrifugation Time]    " // should not exist
        Expect.exists existingNames (fun x -> x = expected_deep_0) "deep 0"
        Expect.exists existingNames (fun x -> x = expected_deep_1) "deep 1"
        Expect.exists existingNames (fun x -> x = expected_deep_2) "deep 2"
        Expect.exists existingNames (fun x -> x = expected_deep_3) "deep 3"
        Expect.all existingNames (fun x -> x <> expected_deep_4) "deep 4"
]