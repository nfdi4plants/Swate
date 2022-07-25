module JsonImport.Tests

open Expecto

open Shared
open Server
open ISADotNet
open JsonImport



let tests_jsonImport = testList "JsonImport" [
    testCase "Test single table Assay.json import." <| fun _ ->
        /// File was created from a single table
        /// TODO: FILE BROKEN DOES NOT CONTAIN COMPONENT [INSTRUMENT MODEL]
        let jsonString = System.IO.File.ReadAllText("./files/20224821_070754_Assay.json") 
        let buildingBlocks =
            let table = JsonImport.assayJsonToTable jsonString
            table.Sheets
            |> Array.ofList
            |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
        Expect.equal buildingBlocks.Length 1 "This file should only include one table."
        let name_table, buildingBlocks_table = buildingBlocks.[0]
        Expect.equal name_table "Sheet1" "Table protocol name should be 'Sheet1_0'"
        Expect.equal buildingBlocks_table.Length 5 "File should contain 5 building blocks"
        // Component [instrument model], with values
        // Parameter [Bio entity], with free text values
        // Parameter [Biosource amount], with unit milligram + values
        // Parameter [Extraction method]
        // Parameter [Extraction buffer]
        // Parameter [Extraction buffer volume], with unit microliter

]

