module OfficeAddin.Tests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Mocha
open ExcelJS.Fable.Excel

// ⚠️ These tests do only work on node.js as the office-addin-mock library sents user data to microsoft !!! ⚠️
// Check out https://github.com/OfficeDev/Office-Addin-Scripts/issues/905


// 💡 Read More
// - https://learn.microsoft.com/en-us/office/dev/add-ins/testing/unit-testing
// - RequestContext object: https://learn.microsoft.com/en-us/javascript/api/excel/excel.requestcontext?view=excel-js-preview

let private TestsBasic = testList "Basic tests" [
    testCaseAsync "develop mock" <| async {
        let testContext: RequestContext = importDefault "./OfficeMockObjects/ExampleObject.js"
        let! result = OfficeInterop.ExcelUtil.getSelectedRangeAdress testContext |> Async.AwaitPromise
        Expect.equal result "C2:G3" "Verify correct setup"
    }
    testCaseAsync "develop mock2" <| async {
        let testContext: RequestContext = importDefault "./OfficeMockObjects/ExampleObject.js"
        let! result = OfficeInterop.ExcelUtil.getSelectedRangeAdress testContext |> Async.AwaitPromise
        Expect.equal result "C2:G3" "Verify correct setup"
    }
]

let Main = testList "OfficeAddin" [
    TestsBasic
]