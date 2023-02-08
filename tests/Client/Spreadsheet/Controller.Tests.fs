module Spreadsheet.Controller.Tests

open Fable.Mocha

open OfficeInterop.Indexing
open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet

let tests_SpreadsheetController = testList "Spreadsheet.Controller" [
    testCase "CreateTable_Empty" <| fun _ ->
        let state = Spreadsheet.Model.init()
        let nextState = Controller.createAnnotationTable state
        Expect.equal nextState.ActiveTableIndex 0 "nextState.ActiveTableIndex"
        Expect.equal nextState.Tables.Count 1 "nextState.Tables.Count"
    ]