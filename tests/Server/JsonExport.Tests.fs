module JsonExport.Tests

open Expecto

open Shared
open Server
open ISADotNet
open JsonImport
open System.IO

open OfficeInteropTypes

let x = 0
//let tests_jsonExport_singleTable = testList "JsonExport - Test single table Assay.json export" [
//    testCase "Import .xlsx, export .json" <| fun _ ->
//        let expected = File.ReadAllText("./files/20220802_TermCols_Assay.json")
//        let buildingBlocks =
//            let table = JsonImport.assayJsonToTable expected
//            table.Sheets
//            |> Array.ofList
//            |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
//        Expect.hasLength buildingBlocks 1 "hasLength"
//        let sheetName, bbs = buildingBlocks.[0]
//        let assay = JsonExport.parseBuildingBlockToAssay sheetName bbs 
//        let parsedJsonStr = ISADotNet.Json.Assay.toString assay
//        let actual =
//            let ms = new MemoryStream(excelFileBytes)
//            let persons, assay = ISADotNet.XLSX.AssayFile.Assay.fromStream ms
//            ISADotNet.Json.Assay.toString assay
//        Expect.equal actual expected "Compare imported .xlsx -> .json with presaved .json"
//]