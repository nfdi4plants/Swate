module BuildingBlockView

open Fable.Mocha
open Client

open Shared.OfficeInteropTypes
open Shared.TermTypes

open BuildingBlock

let tests_BuildingBlockView = testList "BuildingBlockView" [
    testCase "isValidBuildingBlock" <| fun _ ->

        // only columns which require input, the so called term columns, can be invalid if they don't have a name.
        let protocolREF = BuildingBlockNamePrePrint.init(BuildingBlockType.ProtocolREF) |> isValidBuildingBlock
        let source = BuildingBlockNamePrePrint.init(BuildingBlockType.Source) |> isValidBuildingBlock
        let parameter = BuildingBlockNamePrePrint.create BuildingBlockType.Parameter "instrument model" |> isValidBuildingBlock
        let parameter_nonsense = BuildingBlockNamePrePrint.create BuildingBlockType.Parameter "banana balloon animal" |> isValidBuildingBlock
        let characteristic = BuildingBlockNamePrePrint.create BuildingBlockType.Characteristics "strain" |> isValidBuildingBlock
        let factor = BuildingBlockNamePrePrint.create BuildingBlockType.Characteristics "temperature" |> isValidBuildingBlock
        /// Featured column should always be valid
        let protocolType = BuildingBlockNamePrePrint.init BuildingBlockType.ProtocolType |> isValidBuildingBlock

        let term_empty = BuildingBlockNamePrePrint.create BuildingBlockType.Characteristics "" |> isValidBuildingBlock

        Expect.isTrue protocolREF "protocolREF"
        Expect.isTrue source "source"
        Expect.isTrue parameter "parameter"
        Expect.isTrue parameter_nonsense "parameter_nonsense"
        Expect.isTrue characteristic "characteristic"
        Expect.isTrue factor "factor"
        Expect.isTrue protocolType "protocolType"
        Expect.isFalse term_empty "term_empty"
]