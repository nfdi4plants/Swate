open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Saturn
open Shared
open Shared.TermTypes

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration

open ARCtrl
open ARCtrl.Json

let serviceApi: IServiceAPIv1 = {
    getAppVersion = fun () -> async { return System.AssemblyVersionInformation.AssemblyMetadata_Version }
}

open Microsoft.AspNetCore.Http

let dagApiv1 = {
    parseAnnotationTablesToDagHtml = fun worksheetBuildingBlocks -> async {
        //let assay =  Export.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        //let processSequence = Option.defaultValue [] assay.ProcessSequence
        //let dag = Viz.DAG.fromProcessSequence (processSequence,Viz.Schema.NFDIBlue) |> CyjsAdaption.MyHTML.toEmbeddedHTML
        //return dag
        return failwith "Not implemented yet"
    }
}

let swateJsonAPIv1 = {
    parseAnnotationTableToAssayJson = fun (worksheetName,buildingblocks) -> async {
        //let assay = Export.parseBuildingBlockToAssay worksheetName buildingblocks
        //let parsedJsonStr = ISADotNet.Json.Assay.toString assay
        //return parsedJsonStr
        return failwith "Not implemented yet"
    }
    parseAnnotationTableToProcessSeqJson = fun (worksheetName,buildingblocks) -> async {
        //let assay = Export.parseBuildingBlockToAssay worksheetName buildingblocks
        //let parsedJsonStr = ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence.Value
        //return parsedJsonStr
        return failwith "Not implemented yet"
    }
    parseAnnotationTablesToAssayJson = fun worksheetBuildingBlocks -> async {
        //let assay = Export.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        //let parsedJsonStr = ISADotNet.Json.Assay.toString assay
        //return parsedJsonStr
        return failwith "Not implemented yet"
    }
    parseAnnotationTablesToProcessSeqJson = fun worksheetBuildingBlocks -> async {
        //let assay =  Export.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        //let parsedJsonStr = ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence.Value
        //return parsedJsonStr
        return failwith "Not implemented yet"
    }
    parseAssayJsonToBuildingBlocks = fun jsonString -> async {
        //let table = Import.Json.fromAssay jsonString
        //if table.Sheets.Length = 0 then failwith "Unable to find any Swate annotation table information! Please check if uploaded json and chosen json import type match."
        //let buildingBlocks =
        //    table.Sheets
        //    |> Array.ofList
        //    |> Array.map(fun s ->
        //        let ibb = s.toInsertBuildingBlockList |> Array.ofList
        //        //printfn "%A" ibb
        //        s.SheetName, ibb
        //)
        //return buildingBlocks
        return failwith "Not implemented yet"
    }
    // [<System.ObsoleteAttribute>]
    //parseTableJsonToBuildingBlocks = fun jsonString -> async {
    //    let table = JsonImport.tableJsonToTable jsonString
    //    if table.Sheets.Length = 0 then failwith "Unable to find any Swate annotation table information! Please check if uploaded json and chosen json import type match."
    //    let buildingBlocks = table.Sheets |> Array.ofList |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
    //    return buildingBlocks
    //}
    parseProcessSeqToBuildingBlocks = fun jsonString -> async {
        //let table = Import.Json.fromProcessSeq jsonString
        //if table.Sheets.Length = 0 then failwith "Unable to find any Swate annotation table information! Please check if uploaded json and chosen json import type match."
        //let buildingBlocks = table.Sheets |> Array.ofList |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
        //return buildingBlocks
        return failwith "Not implemented yet"
    }
}

let isaDotNetCommonAPIv1 : IISADotNetCommonAPIv1 =
    //let assayFromByteArray (byteArray: byte []) =
    //    let ms = new MemoryStream(byteArray)
    //    let jsonStr = ISADotNet.XLSX.AssayFile.Assay.fromStream ms
    //    jsonStr
    //let investigationFromByteArray (byteArray: byte []) =
    //    let ms = new MemoryStream(byteArray)
    //    let jsonStr =
    //        ISADotNet.XLSX.Investigation.fromStream ms
    //    jsonStr
    {
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Assay.
        toAssayJson = fun byteArray -> async {
            //let assay = assayFromByteArray byteArray |> fun (_,assay) -> assay
            //return box assay
            return failwith "Not implemented yet"
        }
        // This functions reads an ISA-XLSX protocol template as byte [] and returns template metadata and the correlated assay.json.
        // This is the main interop function for SWOBUP.
        toSwateTemplateJson = fun byteArray -> async {
            //let metadata = TemplateMetadata.parseDynMetadataFromByteArr byteArray
            //let ms = new MemoryStream(byteArray)
            //let doc = FsSpreadsheet.ExcelIO.Spreadsheet.fromStream ms false
            //let tableName = metadata.TryGetValue "Table"
            //let assay = ISADotNet.Assay.fromTemplateSpreadsheet (doc, string tableName.Value) 
            //let assayJson = ISADotNet.Json.Assay.toString assay.Value
            //metadata.SetValue("TemplateJson",assayJson)
            //return metadata |> box
            return failwith "Not implemented yet"
        }
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Investigation.
        toInvestigationJson = fun byteArray -> async {
            //let investigation = investigationFromByteArray byteArray
            //return box investigation
            return failwith "Not implemented yet"
        }
        toProcessSeqJson = fun byteArray -> async {
            //let assay = assayFromByteArray byteArray 
            //let processList = assay |> fun (_,assay) -> Option.defaultValue [] assay.ProcessSequence
            //return box processList
            return failwith "Not implemented yet"
        }
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Assay.
        toAssayJsonStr = fun byteArray -> async {
            //let assayJsonString = assayFromByteArray byteArray |> fun (_,assay) -> ISADotNet.Json.Assay.toString assay
            //return assayJsonString
            return failwith "Not implemented yet"
        }
        // This functions reads an ISA-XLSX protocol template as byte [] and returns template metadata and the correlated assay.json.
        toSwateTemplateJsonStr = fun byteArray -> async {
            //let metadata = TemplateMetadata.parseDynMetadataFromByteArr byteArray
            //let ms = new MemoryStream(byteArray)
            //let doc = FsSpreadsheet.ExcelIO.Spreadsheet.fromStream ms false
            //let tableName = metadata.TryGetValue "Table"
            //let assay = ISADotNet.Assay.fromTemplateSpreadsheet (doc, string tableName.Value) 
            //let assayJson = ISADotNet.Json.Assay.toString assay.Value
            //metadata.SetValue("TemplateJson",assayJson)
            //let jsonExp = metadata.toJson()
            //return jsonExp
            return failwith "Not implemented yet"
        }
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Investigation.
        toInvestigationJsonStr = fun byteArray -> async {
            //let investigationJson = investigationFromByteArray byteArray |> ISADotNet.Json.Investigation.toString
            //return investigationJson
            return failwith "Not implemented yet"
        }
        toProcessSeqJsonStr = fun byteArray -> async {
            //let assay = assayFromByteArray byteArray 
            //let processJSon = assay |> fun (_,assay) -> Option.map ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence |> Option.defaultValue "" 
            //return processJSon
            return failwith "Not implemented yet"
        }
        testPostNumber = fun num -> async {
            let res = $"Hey you just sent us a number. Is this your number {num}?"
            return res
        }
        getTestNumber = fun () -> async {
            return "42"
        }
    }

open Database

let templateApi credentials = 
    let templateUrl = @"https://github.com/nfdi4plants/Swate-templates/releases/download/latest/templates_v2.0.0.json"
    {
        getTemplates = fun () -> async {
            let! templates = ARCtrl.Template.Web.getTemplates (None)
            let templatesJson = ARCtrl.Json.Templates.toJsonString 0 (Array.ofSeq templates.Values)
            return templatesJson
        }

        getTemplateById = fun id -> async {
            let! templates = ARCtrl.Template.Web.getTemplates (None)
            let template = templates.Values |> Seq.find (fun t -> t.Id = System.Guid(id))
            let templateJson = Template.toCompressedJsonString 0 template
            return templateJson
        }
    }

let testApi (ctx: HttpContext): ITestAPI = {
    test = fun () -> async {
        let c =
            let settings = ctx.GetService<IConfiguration>()
            let credentials : Helper.Neo4JCredentials= {
                User        = settings.[Helper.Neo4JCredentials.UserVarString]
                Pw          = settings.[Helper.Neo4JCredentials.PwVarString]
                BoltUrl     = settings.[Helper.Neo4JCredentials.UriVarString]
                DatabaseName= settings.[Helper.Neo4JCredentials.DBNameVarString]
            }
            credentials
        //let exmp = OntologyDB.Queries.Term(c).getByAdvancedTermSearch(termName="insturment~ -Shimadzu")
        return "Info", "nothing active here"
    }
    postTest = fun (termName) -> async {
        let c =
            let settings = ctx.GetService<IConfiguration>()
            let credentials : Helper.Neo4JCredentials= {
                User        = settings.[Helper.Neo4JCredentials.UserVarString]
                Pw          = settings.[Helper.Neo4JCredentials.PwVarString]
                BoltUrl     = settings.[Helper.Neo4JCredentials.UriVarString]
                DatabaseName= settings.[Helper.Neo4JCredentials.DBNameVarString]
            }
            credentials
        let exmp = Term.Term(c).getByName(termName,sourceOntologyName=Term.AnyOfOntology.Single "ms")
        return "Info", sprintf "%A" (exmp |> Seq.length)
    }
}

let createITemplateApiv1 credentials =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (templateApi credentials)
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler API.Helper.errorHandler
    |> Remoting.buildHttpHandler

let createIServiceAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue serviceApi
    //|> Remoting.withDocs Shared.URLs.DocsApiUrl2 DocsServiceAPIvs1.serviceApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler API.Helper.errorHandler
    |> Remoting.buildHttpHandler

let createISADotNetCommonAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue isaDotNetCommonAPIv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler API.Helper.errorHandler
    |> Remoting.buildHttpHandler

let createExpertAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue swateJsonAPIv1
    //|> Remoting.withDocs "/api/IExpertAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler API.Helper.errorHandler
    |> Remoting.buildHttpHandler

let createDagApiv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue dagApiv1
    //|> Remoting.withDocs "/api/IExpertAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler API.Helper.errorHandler
    |> Remoting.buildHttpHandler

let createTestApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext testApi
    //|> Remoting.withDocs "/api/IExpertAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler API.Helper.errorHandler
    |> Remoting.buildHttpHandler

let getMessage() = "Hello from SAFE!"

let getNeo4JCredentials (ctx: HttpContext) =
    let settings = ctx.GetService<IConfiguration>()
    try 
        let credentials : Helper.Neo4JCredentials = {
            User        = settings.[Helper.Neo4JCredentials.UserVarString]
            Pw          = settings.[Helper.Neo4JCredentials.PwVarString]
            BoltUrl     = settings.[Helper.Neo4JCredentials.UriVarString]
            DatabaseName= settings.[Helper.Neo4JCredentials.DBNameVarString]
        }
        credentials
    with
        | _ -> 
            let credentials : Helper.Neo4JCredentials = {
                User        = settings.[System.Environment.GetEnvironmentVariable(Helper.Neo4JCredentials.UserVarString)]
                Pw          = settings.[System.Environment.GetEnvironmentVariable(Helper.Neo4JCredentials.PwVarString)]
                BoltUrl     = settings.[System.Environment.GetEnvironmentVariable(Helper.Neo4JCredentials.UriVarString)]
                DatabaseName= settings.[System.Environment.GetEnvironmentVariable(Helper.Neo4JCredentials.DBNameVarString)]
            }
            credentials

//// https://cors-test.codehappy.dev/?url=https%3A%2F%2Fswate.nfdi4plants.org%2Fapi%2FIOntologyAPIv2%2FgetAllOntologies&method=get
///// Enable CORS. Makes external access of Swate API possible
//let allow_cors =
//    pipeline {
//        set_header "Access-Control-Allow-Origin" "*"
//        set_header "Access-Control-Allow-Methods" "*"
//        set_header "Access-Control-Allow-Headers" "*"
//    }

let topLevelRouter = router {
    //pipe_through allow_cors
    get "/test/test1" (htmlString "<h1>Hi this is test response 1</h1>")
    get "/test/hello" (getMessage() |> json)

    forward @"" (fun next ctx ->
        let credentials = getNeo4JCredentials ctx
        API.IOntologyAPI.V1.createIOntologyApi credentials next ctx
    )
    forward @"" (fun next ctx ->
        let credentials = getNeo4JCredentials ctx
        API.IOntologyAPI.V2.createIOntologyApi credentials next ctx
    )

    forward @"" (fun next ctx ->
        let credentials = getNeo4JCredentials ctx
        API.IOntologyAPI.V3.createIOntologyApi credentials next ctx
    )

    forward @"" (fun next ctx ->
        let credentials = getNeo4JCredentials ctx
        createITemplateApiv1 credentials next ctx
    )

    forward "" (fun next ctx ->
        API.IExportAPI.V1.createExportApi () next ctx
    )

    //
    forward @"" (fun next ctx ->
        createIServiceAPIv1 next ctx
    )

    forward @"" (fun next ctx ->
        createISADotNetCommonAPIv1 next ctx
    )

    forward @"" (fun next ctx ->
        createExpertAPIv1 next ctx
    )

    forward @""(fun next ctx ->
        createDagApiv1 next ctx
    )

    forward @""(fun next ctx ->
        createTestApi next ctx
    )
}

open Microsoft.AspNetCore.StaticFiles
open Microsoft.AspNetCore.Cors.Infrastructure

/// Enable CORS. Makes external access of Swate API possible
let cors_config = fun (b: CorsPolicyBuilder) ->
    b
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin()
    |> ignore

/// Allows serving .yaml files directly
let config (app:IApplicationBuilder) =
    let provider = new FileExtensionContentTypeProvider()
    provider.Mappings.Add(".yaml", "application/x-yaml")
    app.UseStaticFiles(
        let opt = new StaticFileOptions()
        opt.ContentTypeProvider <- provider
        opt
    ) 

let app = application {
    //url "http://localhost:5000" //"http://localhost:5000/"
    //app_config config
    use_router topLevelRouter
    //use_cors "CORS_CONFIG" cors_config
    memory_cache
    use_static "public"
    use_gzip
    logging (fun (builder: ILoggingBuilder) -> builder.SetMinimumLevel(LogLevel.Debug) |> ignore)
}

app
    .ConfigureAppConfiguration(
        System.Action<Microsoft.Extensions.Hosting.HostBuilderContext,IConfigurationBuilder> (fun ctx config ->
            config.AddUserSecrets("6de80bdf-2a05-4cf7-a1a8-d08581dfa887") |> ignore
            config.AddJsonFile("dev.json",true,true)            |> ignore
            config.AddJsonFile("production.json",true,true)     |> ignore
        )
)
|> run

