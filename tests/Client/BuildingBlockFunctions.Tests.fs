module BuildingBlockFunctions

open Fable.Mocha
open Client

open Shared.OfficeInteropTypes
open Shared.TermTypes


let case_newTableColumns : Column [] = [|
    {
        Index = 0
        Header = { SwateColumnHeader = "Source Name" }
        Cells = [|
            {
                Index = 1
                Value = Some ""
                Unit = None
            }
        |]
    };
    {
        Index = 1
        Header = { SwateColumnHeader = "Sample Name" }
        Cells = [|
            {
                Index = 1
                Value = Some ""
                Unit = None
            }
        |]
    }
|]

let case_tableWithParameterAndValue : Column [] = [|
    {
        Index = 0
        Header = { SwateColumnHeader = "Source Name" }
        Cells = [|{
            Index = 1
            Value = Some "test/source/path"
            Unit = None
        }|]
    }; {
        Index = 1
        Header = { SwateColumnHeader = "Parameter [instrument model]" }
        Cells = [|{
            Index = 1
            Value = Some "SCIEX instrument model"
            Unit = None
        }|]
    }; {
        Index = 2
        Header = { SwateColumnHeader = "Term Source REF (MS:1000031)" }
        Cells = [|{
            Index = 1
            Value = Some "MS"
            Unit = None
        }|]
    }; {
        Index = 3
        Header = { SwateColumnHeader = "Term Accession Number (MS:1000031)" }
        Cells = [|{
            Index = 1
            Value = Some "http://purl.obolibrary.org/obo/MS_1000121"
            Unit = None
        }|]
    }; {
        Index = 4
        Header = { SwateColumnHeader = "Sample Name"}
        Cells = [|{
            Index = 1
            Value = Some "test/sink/path"
            Unit = None
        }|]
    }
|]

let case_tableWithParamsWithUnit : Column [] =
    [|{
        Index = 5
        Header = { SwateColumnHeader = "Factor [temperature]" }
        Cells = [|{
            Index = 1
            Value = Some "30"
            Unit = Some {
                Name = "degree Celsius"
                TermAccession = ""
            }
        }|]
    }; {
        Index = 6
        Header = { SwateColumnHeader = "Unit" }
        Cells = [|{
            Index = 1
            Value = Some ""
            Unit = None
        }|]
    }; {
        Index = 7
        Header = { SwateColumnHeader = "Term Source REF (PATO:0000146)" }
        Cells = [|{
            Index = 1
            Value = Some ""
            Unit = None }|]
    }; {
        Index = 8
        Header = { SwateColumnHeader = "Term Accession Number (PATO:0000146)" }
        Cells = [|{
            Index = 1
            Value = Some "" 
            Unit = None
        }|]
    }|]

let case_tableWithFeaturedCol : Column [] =
    [|{
        Index = 9
        Header = { SwateColumnHeader = "Protocol Type" }
        Cells = [|{
            Index = 1
            Value = Some "extract protocol"
            Unit = None
        }|]
    }; {
        Index = 10
        Header = { SwateColumnHeader = "Term Source REF (NFDI4PSO:1000161)" }
        Cells = [|{
            Index = 1
            Value = Some "KF_PH" 
            Unit = None
        }|]
    }; {
        Index = 11
        Header = { SwateColumnHeader = "Term Accession Number (NFDI4PSO:1000161)" }
        Cells = [|{
            Index = 1
            Value = Some "http://purl.obolibrary.org/obo/KF_PH_002" 
            Unit = None
        }|]
    }|]

let case_tableWithSingleCol : Column [] =
    [|{
        Index = 12
        Header = { SwateColumnHeader = "Protocol REF" }
        Cells = [|{
            Index = 1
            Value = Some "My Fancy Protocol Name"
            Unit = None
        }|]
    }|]

open OfficeInterop.BuildingBlockFunctions.Aux_GetBuildingBlocksPostSync


let tests_buildingBlockFunctions = testList "BuildingBlockFunctions" [
    testCase "BuildingBlocks from case_newTableColumns" <| fun _ ->
        let buildingBlocks =
            sortColsIntoBuildingBlocks case_newTableColumns
            |> List.rev
            |> Array.ofList

        let buildingBlock_withMainColumnTerms =
            buildingBlocks
            |> Array.map getMainColumnTerm

        Expect.equal buildingBlocks.Length 2 "Different number of BuildingBlocks expected."
        Expect.equal buildingBlocks.[0].MainColumn.Header.SwateColumnHeader "Source Name" "First Building Block must be 'Source Name'."
        Expect.equal buildingBlocks.[1].MainColumn.Header.SwateColumnHeader "Sample Name" "Second/last Building Block must be 'Sample Name'."
        Expect.equal buildingBlock_withMainColumnTerms buildingBlocks "Sample Name and Source Name should not change when updating record type with main column terms."

    testCase "BuildingBlocks from case_tableWithParameterAndValue" <| fun _ ->
        let buildingBlocks =
            sortColsIntoBuildingBlocks case_tableWithParameterAndValue
            |> List.rev
            |> Array.ofList

        let buildingBlock_withMainColumnTerms =
            buildingBlocks
            |> Array.map getMainColumnTerm

        Expect.equal buildingBlocks.Length 3 "Different number of BuildingBlocks expected."
        // Source Name
        Expect.equal buildingBlocks.[0].MainColumn.Header.SwateColumnHeader "Source Name" "First Building Block must be 'Source Name'."
        Expect.equal buildingBlocks.[0].MainColumn.Cells.[0].Value.Value "test/source/path" ""
        Expect.equal buildingBlocks.[0] buildingBlock_withMainColumnTerms.[0] "Source Name MUST not change after update with main column term."
        // Parameter [instrument model]
        Expect.equal buildingBlocks.[1].MainColumn.Header.SwateColumnHeader "Parameter [instrument model]" "Second Building Block must be 'Parameter [instrument model]'."
        Expect.isTrue buildingBlocks.[1].hasCompleteTSRTAN "'Parameter [instrument model]' must have complete TSR and TAN"
        Expect.notEqual buildingBlocks.[1] buildingBlock_withMainColumnTerms.[1] "'Parameter [instrument model]' MUST change after update with main column term."
        Expect.isTrue buildingBlock_withMainColumnTerms.[1].hasTerm "'Parameter [instrument model]' MUST have term after 'getMainColumnTerm'."
        Expect.isTrue buildingBlock_withMainColumnTerms.[1].hasCompleteTerm "'Parameter [instrument model]' MUST have complete term after 'getMainColumnTerm'."
            // check main column term correct
        Expect.equal buildingBlock_withMainColumnTerms.[1].MainColumnTerm (TermMinimal.create "instrument model" "MS:1000031" |> Some) "MainColumnTerm for 'Parameter [instrument model]' differs from expected value."
            // check value correct
        Expect.equal buildingBlock_withMainColumnTerms.[1].MainColumn.Cells.[0].Value.Value "SCIEX instrument model" "Value of 'Parameter [instrument model]' differs from expected."
        // Sample Name
        Expect.equal buildingBlocks.[2].MainColumn.Header.SwateColumnHeader "Sample Name" "Last Building Block must be 'Sample Name'."
        Expect.equal buildingBlocks.[2].MainColumn.Cells.[0].Value.Value "test/sink/path" ""
        Expect.equal buildingBlocks.[2] buildingBlock_withMainColumnTerms.[2] "Sample Name MUST not change after update with main column term."

    testCase "BuildingBlocks from case_tableWithParamsWithUnit" <| fun _ ->
        let buildingBlocks =
            sortColsIntoBuildingBlocks case_tableWithParamsWithUnit
            |> List.rev
            |> Array.ofList

        let buildingBlock_withMainColumnTerms =
            buildingBlocks
            |> Array.map getMainColumnTerm

        Expect.equal buildingBlocks.Length 1 "Different number of BuildingBlocks expected."
        // Factor [temperature]
        Expect.equal buildingBlocks.[0].MainColumn.Header.SwateColumnHeader "Factor [temperature]" "Building Block must be 'Factor [temperature]'."
        Expect.isTrue buildingBlocks.[0].hasCompleteTSRTAN "Factor [temperature] hasCompleteTSRTAN"
        Expect.isTrue buildingBlocks.[0].hasUnit "Factor [temperature] hasUnit"
        Expect.equal buildingBlocks.[0].MainColumn.Cells.[0].Value.Value "30" "Factor [temperature] MainColumn.Cells.[0].Value.Value"
        Expect.isTrue buildingBlocks.[0].MainColumn.Cells.[0].Unit.IsSome "Factor [temperature] MainColumn.Cells.[0].Unit.IsSome"
        Expect.equal buildingBlocks.[0].MainColumn.Cells.[0].Unit.Value {Name = "degree Celsius";TermAccession = ""} "Factor [temperature] MainColumn.Cells.[0].Unit.Value"
        Expect.notEqual buildingBlocks.[0] buildingBlock_withMainColumnTerms.[0] "'Factor [temperature]' MUST change after update with main column term."
            // check main column term correct
        Expect.isTrue buildingBlock_withMainColumnTerms.[0].hasCompleteTerm "Factor [temperature] hasCompleteTerm"
        Expect.equal buildingBlock_withMainColumnTerms.[0].MainColumnTerm (TermMinimal.create "temperature" "PATO:0000146" |> Some) "MainColumnTerm for 'Factor [temperature]' differs from expected value."

    testCase "BuildingBlocks from case_tableWithFeaturedCol" <| fun _ ->
        let buildingBlocks =
            sortColsIntoBuildingBlocks case_tableWithFeaturedCol
            |> List.rev
            |> Array.ofList

        let buildingBlock_withMainColumnTerms =
            buildingBlocks
            |> Array.map getMainColumnTerm

        Expect.equal buildingBlocks.Length 1 "Different number of BuildingBlocks expected."
        // //
        Expect.equal buildingBlocks.[0].MainColumn.Header.SwateColumnHeader "Protocol Type" "Building Block must be 'Protocol Type'."
        Expect.isTrue buildingBlocks.[0].hasCompleteTSRTAN "Protocol Type hasCompleteTSRTAN"
        Expect.equal buildingBlocks.[0].MainColumn.Cells.[0].Value.Value "extract protocol" "Protocol Type MainColumn.Cells.[0].Value.Value"
        Expect.isTrue buildingBlocks.[0].MainColumn.Cells.[0].Unit.IsNone "Protocol Type MainColumn.Cells.[0].Unit.IsNone"
        Expect.notEqual buildingBlocks.[0] buildingBlock_withMainColumnTerms.[0] "Protocol Type MUST change after update with main column term."
            // check main column term correct
        Expect.isTrue buildingBlock_withMainColumnTerms.[0].hasCompleteTerm "Protocol Type hasCompleteTerm"
        Expect.equal buildingBlock_withMainColumnTerms.[0].MainColumnTerm (TermMinimal.create "protocol type" "NFDI4PSO:1000161" |> Some) "MainColumnTerm for 'Protocol Type' differs from expected value"

    testCase "BuildingBlocks from case_tableWithSingleCol" <| fun _ ->
        let buildingBlocks =
            sortColsIntoBuildingBlocks case_tableWithSingleCol
            |> List.rev
            |> Array.ofList

        let buildingBlock_withMainColumnTerms =
            buildingBlocks
            |> Array.map getMainColumnTerm

        Expect.equal buildingBlocks.Length 1 "Different number of BuildingBlocks expected."
        Expect.equal buildingBlocks.[0].MainColumn.Header.SwateColumnHeader "Protocol REF" "First Building Block must be 'Protocol REF'."
        Expect.isTrue buildingBlocks.[0].MainColumn.Header.isSingleCol "First Building Block must be 'isSingleCol'."
        Expect.equal buildingBlock_withMainColumnTerms buildingBlocks "Protocol REF should not change when updating record type with main column terms."

    testCase "BuildingBlocks from combined cases" <| fun _ ->

        let combinedCases =
            // Array.append adds second arr to the end of first arr, so we need to build backwards using the pipe operator
            case_tableWithSingleCol
            |> Array.append case_tableWithFeaturedCol
            |> Array.append case_tableWithParamsWithUnit
            |> Array.append case_tableWithParameterAndValue

        let buildingBlocks =
            sortColsIntoBuildingBlocks combinedCases
            |> List.rev
            |> Array.ofList

        let buildingBlock_withMainColumnTerms =
            buildingBlocks
            |> Array.map getMainColumnTerm

        Expect.equal buildingBlocks.Length 6 "Different number of BuildingBlocks expected."
        // Source Name
        Expect.equal buildingBlocks.[0].MainColumn.Header.SwateColumnHeader "Source Name" "First Building Block must be 'Source Name'."
        Expect.equal buildingBlocks.[0].MainColumn.Cells.[0].Value.Value "test/source/path" ""
        Expect.equal buildingBlocks.[0] buildingBlock_withMainColumnTerms.[0] "Source Name MUST not change after update with main column term."
        // Parameter [instrument model]
        Expect.equal buildingBlocks.[1].MainColumn.Header.SwateColumnHeader "Parameter [instrument model]" "Second Building Block must be 'Parameter [instrument model]'."
        Expect.isTrue buildingBlocks.[1].hasCompleteTSRTAN "'Parameter [instrument model]' must have complete TSR and TAN"
        Expect.notEqual buildingBlocks.[1] buildingBlock_withMainColumnTerms.[1] "'Parameter [instrument model]' MUST change after update with main column term."
        Expect.isTrue buildingBlock_withMainColumnTerms.[1].hasTerm "'Parameter [instrument model]' MUST have term after 'getMainColumnTerm'."
        Expect.isTrue buildingBlock_withMainColumnTerms.[1].hasCompleteTerm "'Parameter [instrument model]' MUST have complete term after 'getMainColumnTerm'."
            // check main column term correct
        Expect.equal buildingBlock_withMainColumnTerms.[1].MainColumnTerm (TermMinimal.create "instrument model" "MS:1000031" |> Some) "MainColumnTerm for 'Parameter [instrument model]' differs from expected value."
            // check value correct
        Expect.equal buildingBlock_withMainColumnTerms.[1].MainColumn.Cells.[0].Value.Value "SCIEX instrument model" "Value of 'Parameter [instrument model]' differs from expected."
        // Sample Name
        Expect.equal buildingBlocks.[2].MainColumn.Header.SwateColumnHeader "Sample Name" "Last Building Block must be 'Sample Name'."
        Expect.equal buildingBlocks.[2].MainColumn.Cells.[0].Value.Value "test/sink/path" ""
        Expect.equal buildingBlocks.[2] buildingBlock_withMainColumnTerms.[2] "Sample Name MUST not change after update with main column term."
        // Factor [temperature]
        Expect.equal buildingBlocks.[3].MainColumn.Header.SwateColumnHeader "Factor [temperature]" "Building Block must be 'Factor [temperature]'."
        Expect.isTrue buildingBlocks.[3].hasCompleteTSRTAN "Factor [temperature] hasCompleteTSRTAN"
        Expect.isTrue buildingBlocks.[3].hasUnit "Factor [temperature] hasUnit"
        Expect.equal buildingBlocks.[3].MainColumn.Cells.[0].Value.Value "30" "Factor [temperature] MainColumn.Cells.[0].Value.Value"
        Expect.isTrue buildingBlocks.[3].MainColumn.Cells.[0].Unit.IsSome "Factor [temperature] MainColumn.Cells.[0].Unit.IsSome"
        Expect.equal buildingBlocks.[3].MainColumn.Cells.[0].Unit.Value {Name = "degree Celsius";TermAccession = ""} "Factor [temperature] MainColumn.Cells.[0].Unit.Value"
        Expect.notEqual buildingBlocks.[3] buildingBlock_withMainColumnTerms.[3] "'Factor [temperature]' MUST change after update with main column term."
            // check main column term correct
        Expect.isTrue buildingBlock_withMainColumnTerms.[3].hasCompleteTerm "Factor [temperature] hasCompleteTerm"
        Expect.equal buildingBlock_withMainColumnTerms.[3].MainColumnTerm (TermMinimal.create "temperature" "PATO:0000146" |> Some) "MainColumnTerm for 'Factor [temperature]' differs from expected value."

        // Protocol Type
        Expect.equal buildingBlocks.[4].MainColumn.Header.SwateColumnHeader "Protocol Type" "Building Block must be 'Protocol Type'."
        Expect.isTrue buildingBlocks.[4].hasCompleteTSRTAN "Protocol Type hasCompleteTSRTAN"
        Expect.equal buildingBlocks.[4].MainColumn.Cells.[0].Value.Value "extract protocol" "Protocol Type MainColumn.Cells.[0].Value.Value"
        Expect.isTrue buildingBlocks.[4].MainColumn.Cells.[0].Unit.IsNone "Protocol Type MainColumn.Cells.[0].Unit.IsNone"
        Expect.notEqual buildingBlocks.[4] buildingBlock_withMainColumnTerms.[4] "Protocol Type MUST change after update with main column term."
            // check main column term correct
        Expect.isTrue buildingBlock_withMainColumnTerms.[4].hasCompleteTerm "Protocol Type hasCompleteTerm"
        Expect.equal buildingBlock_withMainColumnTerms.[4].MainColumnTerm (TermMinimal.create "protocol type" "NFDI4PSO:1000161" |> Some) "MainColumnTerm for 'Protocol Type' differs from expected value"

        // Protocol REF
        Expect.equal buildingBlocks.[5].MainColumn.Header.SwateColumnHeader "Protocol REF" "First Building Block must be 'Protocol REF'."
        Expect.isTrue buildingBlocks.[5].MainColumn.Header.isSingleCol "First Building Block must be 'isSingleCol'."
]