module TemplateMetadata

open Newtonsoft.Json.Schema

let resolver = JSchemaUrlResolver()
    
let jsonSchemaPath = @"public/TemplateMetadataSchema.json"

let writeSettings =
    let s = JSchemaWriterSettings()
    s.ReferenceHandling <- JSchemaWriterReferenceHandling.Never
    s

let getJsonSchemaAsXml =
    System.IO.File.ReadAllText jsonSchemaPath
    |> fun x -> JSchema.Parse(x,resolver).ToString(writeSettings)

