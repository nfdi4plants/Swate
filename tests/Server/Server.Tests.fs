module Server.Tests

open System
open System.Text.Json
open Expecto

open ARCtrl.Json
open Swate.Components.Shared
open Server

let templateApi =
    testList "Template API" [
        testCaseAsync "loads templates without a Fable runtime"
        <| async {
            let template = ARCtrl.Template.init "Test template"

            let templatesJson =
                ARCtrl.Json.Templates.encoder [| template |]
                |> Thoth.Json.Newtonsoft.Encode.toString 0

            let mutable downloadCount = 0

            let download () = async {
                downloadCount <- downloadCount + 1
                return templatesJson
            }

            let api = TemplateApi.create download

            let! allTemplates = api.getTemplates ()
            let! compressedTemplate = api.getTemplateById (string template.Id)

            use document = JsonDocument.Parse compressedTemplate

            let selectedTemplateId =
                document.RootElement.GetProperty("object").GetProperty("id").GetString()
                |> Guid.Parse

            Expect.equal allTemplates templatesJson "The downloaded template collection should be returned unchanged"
            Expect.equal selectedTemplateId template.Id "The requested template should be serialized"
            Expect.equal downloadCount 2 "Each API request should download the current template release"
        }
    ]

let server =
    testList "Server" [
        testCase "Message returned correctly"
        <| fun _ ->
            let expectedResult = "Hello from SAFE!"
            let result = Server.getMessage ()
            Expect.equal result expectedResult "Result should be ok"
    ]

let all = testList "All" [ Tests.shared; templateApi; server ]

[<EntryPoint>]
let main _ = runTestsWithCLIArgs [] [||] all
