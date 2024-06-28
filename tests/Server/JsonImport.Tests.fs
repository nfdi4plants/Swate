module JsonImport.Tests

open Expecto

open Shared
open Server
let x = 2
//open JsonImport

//open OfficeInteropTypes

//let tests_jsonImport_termCols = testList "JsonImport - Test single table Assay.json import - TermCols" [
//    // // File was created from a single table
//    // Component [instrument model], with values
//    // Parameter [Bio entity], with free text values
//    // Parameter [Biosource amount], with unit milligram + values
//    // Parameter [Extraction method], no value
//    // Parameter [Extraction buffer], no value
//    // Parameter [Extraction buffer volume], with unit microliter, but no value
//    let jsonString = System.IO.File.ReadAllText("./files/20220802_TermCols_Assay.json")
//    let buildingBlocks =
//        let table = JsonImport.assayJsonToTable jsonString
//        table.Sheets
//        |> Array.ofList
//        |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
//    let name_table, buildingBlocks_table = buildingBlocks.[0]
//    testCase "Test file import general." <| fun _ ->
//        Expect.equal buildingBlocks.Length 1 "This file should only include one table."
//        Expect.equal name_table "Sheet1" "Table protocol name should be 'Sheet1_0'"
//        Expect.equal buildingBlocks_table.Length 6 "File should contain 6 building blocks"
//    testCase "Test BuildingBlock Component [instrument model]" <| fun _ ->
//        // // Component [instrument model]
//        let component_instrumentModel = buildingBlocks_table.[0]
//        Expect.equal component_instrumentModel.ColumnHeader.Type BuildingBlockType.Component "component_instrumentModel.ColumnHeader.Type"
//        Expect.equal component_instrumentModel.ColumnHeader.Name "instrument model" "component_instrumentModel.ColumnHeader.Name"
//        Expect.isSome component_instrumentModel.ColumnTerm "component_instrumentModel.ColumnTerm isSome"
//        Expect.equal component_instrumentModel.ColumnTerm.Value.Name "instrument model" "component_instrumentModel.ColumnTerm.Value.Name"
//        Expect.equal component_instrumentModel.ColumnTerm.Value.TermAccession "MS:1000031" "component_instrumentModel.ColumnTerm.Value.TermAccession"
//        Expect.equal component_instrumentModel.Rows.Length 5 "component_instrumentModel.Rows.Length"
//        Expect.equal (component_instrumentModel.Rows |> Array.distinct).Length 1 "component_instrumentModel.Rows -> Array.distinct"
//        Expect.equal component_instrumentModel.Rows.[0].Name "SCIEX instrument model" "component_instrumentModel.Rows.[0].Name"
//        Expect.equal component_instrumentModel.Rows.[0].TermAccession "MS:1000121" "component_instrumentModel.Rows.[0].TermAccession"
//        // // // Unit
//        Expect.isFalse component_instrumentModel.HasUnit "component_instrumentModel.HasUnit"
//    testCase "Test BuildingBlock Parameter [Bio entity]" <| fun _ ->
//        // // Parameter [Bio entity]
//        let parameter_bioEntity = buildingBlocks_table.[1]
//        Expect.equal parameter_bioEntity.ColumnHeader.Type BuildingBlockType.Parameter "parameter_bioEntity.ColumnHeader.Type"
//        Expect.equal parameter_bioEntity.ColumnHeader.Name "Bio entity" "parameter_bioEntity.ColumnHeader.Name"
//        Expect.isSome parameter_bioEntity.ColumnTerm "parameter_bioEntity.ColumnTerm isSome"
//        Expect.equal parameter_bioEntity.ColumnTerm.Value.Name "Bio entity" "parameter_bioEntity.ColumnTerm.Value.Name"
//        Expect.equal parameter_bioEntity.ColumnTerm.Value.TermAccession "NFDI4PSO:0000012" "parameter_bioEntity.ColumnTerm.Value.TermAccession"
//        Expect.equal parameter_bioEntity.Rows.Length 5 "parameter_bioEntity.Rows.Length"
//        Expect.equal (parameter_bioEntity.Rows |> Array.distinct).Length 1 "parameter_bioEntity.Rows -> Array.distinct"
//        Expect.equal parameter_bioEntity.Rows.[0].Name "bananana" "parameter_bioEntity.Rows.[0].Name"
//        Expect.equal parameter_bioEntity.Rows.[0].TermAccession "" "parameter_bioEntity.Rows.[0].TermAccession"
//        // // // Unit
//        Expect.isFalse parameter_bioEntity.HasUnit "parameter_bioEntity.HasUnit"
//    testCase "Test BuildingBlock Parameter [Biosource amount]" <| fun _ ->
//        // // Parameter [Biosource amount]
//        let parameter_biosourceAmount = buildingBlocks_table.[2]
//        Expect.equal parameter_biosourceAmount.ColumnHeader.Type BuildingBlockType.Parameter "parameter_biosourceAmount.ColumnHeader.Type"
//        Expect.equal parameter_biosourceAmount.ColumnHeader.Name "Biosource amount" "parameter_biosourceAmount.ColumnHeader.Name"
//        Expect.isSome parameter_biosourceAmount.ColumnTerm "parameter_biosourceAmount.ColumnTerm isSome"
//        Expect.equal parameter_biosourceAmount.ColumnTerm.Value.Name "Biosource amount" "parameter_biosourceAmount.ColumnTerm.Value.Name"
//        Expect.equal parameter_biosourceAmount.ColumnTerm.Value.TermAccession "NFDI4PSO:0000013" "parameter_biosourceAmount.ColumnTerm.Value.TermAccession"
//        Expect.equal parameter_biosourceAmount.Rows.Length 5 "parameter_biosourceAmount.Rows.Length"
//        Expect.equal (parameter_biosourceAmount.Rows |> Array.distinct).Length 1 "parameter_biosourceAmount.Rows -> Array.distinct"
//        Expect.equal parameter_biosourceAmount.Rows.[0].Name "12" "parameter_biosourceAmount.Rows.[0].Name"
//        Expect.equal parameter_biosourceAmount.Rows.[0].TermAccession "" "parameter_biosourceAmount.Rows.[0].TermAccession"
//        // // // Unit
//        Expect.isTrue parameter_biosourceAmount.HasUnit "parameter_biosourceAmount.HasUnit"
//        Expect.isSome parameter_biosourceAmount.UnitTerm "UnitTerm -> isSome"
//        Expect.equal parameter_biosourceAmount.UnitTerm.Value.Name "milligram" "UnitTerm.Value.Name"
//        Expect.equal parameter_biosourceAmount.UnitTerm.Value.TermAccession "UO:0000022" "UnitTerm.Value.TermAccession"
//        Expect.equal parameter_biosourceAmount.UnitTerm.Value.toNumberFormat "0.00 \"milligram\"" "UnitTerm.Value.toNumberFormat"
//    testCase "Test BuildingBlock Parameter [Extraction method]" <| fun _ ->
//        // // Parameter [Extraction method]
//        let bb = buildingBlocks_table.[3]
//        Expect.equal bb.ColumnHeader.Type BuildingBlockType.Parameter "ColumnHeader.Type"
//        Expect.equal bb.ColumnHeader.Name "Extraction method" "ColumnHeader.Name"
//        Expect.isSome bb.ColumnTerm "ColumnTerm isSome"
//        Expect.equal bb.ColumnTerm.Value.Name "Extraction method" "ColumnTerm.Value.Name"
//        Expect.equal bb.ColumnTerm.Value.TermAccession "NFDI4PSO:0000054" "ColumnTerm.Value.TermAccession"
//        Expect.equal bb.Rows.Length 0 "Rows.Length"
//        // // // Unit
//        Expect.isFalse bb.HasUnit "HasUnit"
//    testCase "Test BuildingBlock Parameter [Extraction buffer]" <| fun _ ->
//        // // Parameter [Extraction buffer]
//        let bb = buildingBlocks_table.[4]
//        Expect.equal bb.ColumnHeader.Type BuildingBlockType.Parameter "ColumnHeader.Type"
//        Expect.equal bb.ColumnHeader.Name "Extraction buffer" "ColumnHeader.Name"
//        Expect.isSome bb.ColumnTerm "ColumnTerm isSome"
//        Expect.equal bb.ColumnTerm.Value.Name "Extraction buffer" "ColumnTerm.Value.Name"
//        Expect.equal bb.ColumnTerm.Value.TermAccession "NFDI4PSO:0000050" "ColumnTerm.Value.TermAccession"
//        Expect.equal bb.Rows.Length 0 "Rows.Length"
//        // // // Unit
//        Expect.isFalse bb.HasUnit "HasUnit"
//    testCase "Test BuildingBlock Parameter [Extraction buffer volume]" <| fun _ ->
//        // // Parameter [Extraction buffer volume]
//        let bb = buildingBlocks_table.[5]
//        Expect.equal bb.ColumnHeader.Type BuildingBlockType.Parameter "ColumnHeader.Type"
//        Expect.equal bb.ColumnHeader.Name "Extraction buffer volume" "ColumnHeader.Name"
//        Expect.isSome bb.ColumnTerm "ColumnTerm isSome"
//        Expect.equal bb.ColumnTerm.Value.Name "Extraction buffer volume" "ColumnTerm.Value.Name"
//        Expect.equal bb.ColumnTerm.Value.TermAccession "NFDI4PSO:0000051" "ColumnTerm.Value.TermAccession"
//        Expect.equal bb.Rows.Length 0 "Rows.Length"
//        // // // Unit
//        Expect.isTrue bb.HasUnit "HasUnit"
//        Expect.isSome bb.UnitTerm "UnitTerm -> isSome"
//        Expect.equal bb.UnitTerm.Value.Name "microliter" "UnitTerm.Value.Name"
//        Expect.equal bb.UnitTerm.Value.TermAccession "UO:0000101" "UnitTerm.Value.TermAccession"
//        Expect.equal bb.UnitTerm.Value.toNumberFormat "0.00 \"microliter\"" "UnitTerm.Value.toNumberFormat"
//]

//let tests_jsonImport_protocolCols = testList "JsonImport - Test single table Assay.json import - ProtocolCols" [
//    // // File was created from a single table
//    // Protocol REF, with values
//    // Protocol Type, with values
//    let jsonString = System.IO.File.ReadAllText("./files/20220803_ProtocolCols_Assay.json")
//    let buildingBlocks =
//        let table = JsonImport.assayJsonToTable jsonString
//        table.Sheets
//        |> Array.ofList
//        |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
//    let name_table, buildingBlocks_table = buildingBlocks.[0]
//    testCase "Test file import general." <| fun _ ->
//        Expect.equal buildingBlocks.Length 1 "This file should only include one table."
//        Expect.equal name_table "Sheet1" "Table protocol name should be 'Sheet1_0'"
//        Expect.equal buildingBlocks_table.Length 2 "File should contain 6 building blocks"
//    testCase "Test Protocol REF" <| fun _ ->
//        // // Protocol REF
//        let bb = buildingBlocks_table.[0]
//        Expect.equal bb.ColumnHeader.Type BuildingBlockType.ProtocolREF "ColumnHeader.Type"
//        Expect.isNone bb.ColumnTerm "ColumnTerm isNone"
//        Expect.equal bb.ColumnHeader.Name "" "ColumnHeader.Name"
//        Expect.isTrue bb.ColumnHeader.isSingleColumn "isSingleColumn"
//        Expect.isFalse bb.ColumnHeader.isFeaturedColumn "isFeaturedColumn"
//        Expect.isFalse bb.ColumnHeader.isInputColumn "isInputColumn"
//        Expect.isFalse bb.ColumnHeader.isOutputColumn "isOutputColumn"
//        Expect.isFalse bb.ColumnHeader.isTermColumn "isTermColumn"
//        Expect.isFalse bb.HasUnit "HasUnit"
//        Expect.equal bb.Rows.Length 6 "Rows.Length"
//        let distinctRows = bb.Rows |> Array.distinct
//        Expect.equal distinctRows.Length 2 "distinctRows"
//        Expect.equal distinctRows.[0].Name "MyProtocol" "distinctRows.[0].Name"
//        Expect.equal distinctRows.[1].Name "MyOtherProtocol" "distinctRows.[1].Name"
//        Expect.equal distinctRows.[0].TermAccession "" "distinctRows.[0].TermAccession"
//        Expect.equal distinctRows.[1].TermAccession "" "distinctRows.[1].TermAccession"
//    testCase "Test Protocol Type" <| fun _ ->
//        // // Protocol Type
//        let bb = buildingBlocks_table.[1]
//        Expect.equal bb.ColumnHeader.Type BuildingBlockType.ProtocolType "ColumnHeader.Type"
//        Expect.isSome bb.ColumnTerm "ColumnTerm"
//        Expect.equal bb.ColumnHeader.Name "" "ColumnHeader.Name"
//        Expect.isTrue bb.ColumnHeader.isFeaturedColumn "isFeaturedColumn"
//        Expect.isFalse bb.ColumnHeader.isTermColumn "isTermColumn"
//        Expect.isFalse bb.ColumnHeader.isSingleColumn "isSingleColumn"
//        Expect.isFalse bb.ColumnHeader.isInputColumn "isInputColumn"
//        Expect.isFalse bb.ColumnHeader.isOutputColumn "isOutputColumn"
//        Expect.isFalse bb.HasUnit "HasUnit"
//        Expect.equal bb.Rows.Length 6 "Rows.Length"
//        let distinctRows = bb.Rows |> Array.distinct
//        Expect.equal distinctRows.Length 2 "distinctRows"
//        Expect.equal distinctRows.[0].Name "Growth Protocol" "distinctRows.[0].Name"
//        Expect.equal distinctRows.[1].Name "Spec Growth Protocol" "distinctRows.[1].Name"
//        Expect.equal distinctRows.[0].TermAccession "" "distinctRows.[0].TermAccession"
//        Expect.equal distinctRows.[1].TermAccession "" "distinctRows.[1].TermAccession"
//]