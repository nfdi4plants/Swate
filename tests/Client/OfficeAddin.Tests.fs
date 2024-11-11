module OfficeAddin.Tests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Mocha

open ExcelJS.Fable.Excel

open ARCtrl

// ⚠️ These tests do only work on node.js as the we sents user data to microsoft !!! ⚠️
// Check out https://github.com/OfficeDev/Office-Addin-Scripts/issues/905

// 💡 Read More
// - https://learn.microsoft.com/en-us/office/dev/add-ins/testing/unit-testing
// - RequestContext object: https://learn.microsoft.com/en-us/javascript/api/excel/excel.requestcontext?view=excel-js-preview

let compositeColumn =
    let header = CompositeHeader.Parameter (OntologyAnnotation.create("Test"))
    CompositeColumn.create(header)

let private TestsBasic = testList "Basic tests" [
    testCaseAsync "develop mock" <| async {
        let testContext: RequestContext = importDefault "./OfficeMockObjects/ExampleObject.js"
        let! result = OfficeInterop.ExcelUtil.getSelectedRangeAdress testContext |> Async.AwaitPromise
        Expect.equal result "C2:G3" "Verify correct setup"
    }
]

let private TestsSuccessful = testList "Successful tests" [
    testCaseAsync "develop getTablesTest successful" <| async {
        let testContext: RequestContext = importDefault "./OfficeMockObjects/ExampleObject.js"
        let! resultRes = OfficeInterop.Core.getExcelAnnotationTables testContext |> Async.AwaitPromise

        let result = Expect.wantOk (Result.Ok resultRes) "This is a message"
        Expect.equal (Result.toOption result).Value.Count 1 "This is a message"
    }

    testCaseAsync "develop rectifyTermColumnsTest successful" <| async {
        let testContext: RequestContext = importDefault "./OfficeMockObjects/ExampleObject.js"
        let! resultRes = OfficeInterop.Core.rectifyTermColumns (Some testContext) |> Async.AwaitPromise

        let result = Expect.wantOk (Result.Ok resultRes) "This is a message"
        Expect.equal result.Head.MessageTxt "The annotation table annotationTable 1 is valid" "This is a message"
    }

    testCaseAsync "develop addCompositeColumnTest" <| async {
        let testContext: RequestContext = importDefault "./OfficeMockObjects/ExampleObject.js"
        let! resultRes = OfficeInterop.Core.Main.addCompositeColumn (compositeColumn, Some testContext) |> Async.AwaitPromise

        let result = Expect.wantOk (Result.Ok resultRes) "This is a message"
        Expect.equal result.Head.MessageTxt "Error! No annotation table found in active worksheet!" "This is a message"
    }
]

let Main = testList "OfficeAddin" [
    TestsBasic;
    TestsSuccessful
]