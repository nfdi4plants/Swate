module TemplateApi

open System.Collections.Generic
open System.Net.Http

open ARCtrl
open ARCtrl.Json
open Swate.Components.Shared

[<Literal>]
let TemplateUrl =
    "https://github.com/nfdi4plants/Swate-templates/releases/download/latest/templates_v2.0.0.json"

let private httpClient = new HttpClient()

let downloadTemplates () =
    httpClient.GetStringAsync(TemplateUrl) |> Async.AwaitTask

let private parseTemplates json =
    Thoth.Json.Newtonsoft.Decode.unsafeFromString ARCtrl.Json.Templates.decoder json

let private serializeCompressedTemplate (template: Template) =
    let stringTable = Dictionary<string, int>()
    let oaTable = Dictionary<OntologyAnnotation, int>()
    let cellTable = Dictionary<CompositeCell, int>()

    let encodedTemplate =
        ARCtrl.Json.Template.encoderCompressed stringTable oaTable cellTable template

    // Encoding the template first populates the lookup tables used below.
    Thoth.Json.Newtonsoft.Encode.toString 0 encodedTemplate |> ignore

    Thoth.Json.Core.Encode.object [
        "cellTable",
        ARCtrl.Json.CellTable.arrayFromMap cellTable
        |> ARCtrl.Json.CellTable.encoder stringTable oaTable
        "oaTable",
        ARCtrl.Json.OATable.arrayFromMap oaTable
        |> ARCtrl.Json.OATable.encoder stringTable
        "stringTable",
        ARCtrl.Json.StringTable.arrayFromMap stringTable
        |> ARCtrl.Json.StringTable.encoder
        "object", encodedTemplate
    ]
    |> Thoth.Json.Newtonsoft.Encode.toString 0

let create (download: unit -> Async<string>) : ITemplateAPIv1 = {
    getTemplates = download

    getTemplateById =
        fun id -> async {
            let! templatesJson = download ()

            return
                templatesJson
                |> parseTemplates
                |> Seq.find (fun template -> template.Id = System.Guid id)
                |> serializeCompressedTemplate
        }
}
