module OfficeInteropTypes

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Shared.OfficeInteropTypes

let buildingBlockTypes = testList "buildingBlockTypes" [
    
    testCase "Source Name" <| fun _ ->
        let header_SourceName = {SwateColumnHeader = BuildingBlockType.Source.toString}
        Expect.isTrue header_SourceName.isMainColumn ""
        Expect.isTrue header_SourceName.isSingleCol ""
        Expect.isFalse header_SourceName.isFeaturedCol ""
        Expect.isTrue header_SourceName.isInputCol ""
        Expect.isFalse header_SourceName.isOutputCol ""
        Expect.isFalse header_SourceName.isUnitCol ""
        Expect.isFalse header_SourceName.isTANCol ""
        Expect.isFalse header_SourceName.isTSRCol ""

    testCase "Protocol Type" <| fun _ ->
        let header_ProtocolType = {SwateColumnHeader = BuildingBlockType.ProtocolType.toString}
        Expect.isTrue header_ProtocolType.isMainColumn ""
        Expect.isFalse header_ProtocolType.isSingleCol ""
        Expect.isTrue header_ProtocolType.isFeaturedCol ""
        Expect.isFalse header_ProtocolType.isInputCol ""
        Expect.isFalse header_ProtocolType.isOutputCol ""
        Expect.isFalse header_ProtocolType.isUnitCol ""
        Expect.isFalse header_ProtocolType.isTANCol ""
        Expect.isFalse header_ProtocolType.isTSRCol ""

        Expect.equal header_ProtocolType.getFeaturedColAccession "NFDI4PSO:1000161" ""
]