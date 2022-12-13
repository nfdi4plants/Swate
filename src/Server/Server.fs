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

let serviceApi = {
    getAppVersion = fun () -> async { return System.AssemblyVersionInformation.AssemblyVersion }
}

open ISADotNet
open Microsoft.AspNetCore.Http

let dagApiv1 = {
    parseAnnotationTablesToDagHtml = fun worksheetBuildingBlocks -> async {
        let assay =  JsonExport.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        let processSequence = Option.defaultValue [] assay.ProcessSequence
        let dag = Viz.DAG.fromProcessSequence (processSequence,Viz.Schema.NFDIBlue) |> CyjsAdaption.MyHTML.toEmbeddedHTML
        return dag
    }
}

let swateJsonAPIv1 = {
    parseAnnotationTableToAssayJson = fun (worksheetName,buildingblocks) -> async {
        let assay = JsonExport.parseBuildingBlockToAssay worksheetName buildingblocks
        let parsedJsonStr = ISADotNet.Json.Assay.toString assay
        return parsedJsonStr
    }
    parseAnnotationTableToProcessSeqJson = fun (worksheetName,buildingblocks) -> async {
        let assay = JsonExport.parseBuildingBlockToAssay worksheetName buildingblocks
        let parsedJsonStr = ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence.Value
        return parsedJsonStr
    }
    //parseAnnotationTableToTableJson = fun (worksheetName,buildingblocks) -> async {
    //    let factors, protocol, assay = JsonExport.parseBuildingBlockToAssay worksheetName buildingblocks
    //    let parsedJsonStr = (ISADotNet.Json.AssayCommonAPI.RowWiseAssay.fromAssay >> ISADotNet.Json.AssayCommonAPI.RowWiseAssay.toString) assay
    //    return parsedJsonStr
    //}
    parseAnnotationTablesToAssayJson = fun worksheetBuildingBlocks -> async {
        let assay =  JsonExport.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        let parsedJsonStr = ISADotNet.Json.Assay.toString assay
        return parsedJsonStr
    }
    parseAnnotationTablesToProcessSeqJson = fun worksheetBuildingBlocks -> async {
        let assay =  JsonExport.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        let parsedJsonStr = ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence.Value
        return parsedJsonStr
    }
    // [<System.ObsoleteAttribute>]
    //parseAnnotationTablesToTableJson = fun worksheetBuildingBlocks -> async {
    //    let factors, protocol, assay =  JsonExport.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
    //    let parsedJsonStr = (ISADotNet.Json.AssayCommonAPI.RowWiseAssay.fromAssay >> ISADotNet.Json.AssayCommonAPI.RowWiseAssay.toString) assay
    //    return parsedJsonStr
    //}
    parseAssayJsonToBuildingBlocks = fun jsonString -> async {
        let table = JsonImport.assayJsonToTable jsonString
        if table.Sheets.Length = 0 then failwith "Unable to find any Swate annotation table information! Please check if uploaded json and chosen json import type match."
        let buildingBlocks =
            table.Sheets
            |> Array.ofList
            |> Array.map(fun s ->
                let ibb = s.toInsertBuildingBlockList |> Array.ofList
                //printfn "%A" ibb
                s.SheetName, ibb
        )
        return buildingBlocks
    }
    // [<System.ObsoleteAttribute>]
    //parseTableJsonToBuildingBlocks = fun jsonString -> async {
    //    let table = JsonImport.tableJsonToTable jsonString
    //    if table.Sheets.Length = 0 then failwith "Unable to find any Swate annotation table information! Please check if uploaded json and chosen json import type match."
    //    let buildingBlocks = table.Sheets |> Array.ofList |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
    //    return buildingBlocks
    //}
    parseProcessSeqToBuildingBlocks = fun jsonString -> async {
        let table = JsonImport.processSeqJsonToTable jsonString
        if table.Sheets.Length = 0 then failwith "Unable to find any Swate annotation table information! Please check if uploaded json and chosen json import type match."
        let buildingBlocks = table.Sheets |> Array.ofList |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
        return buildingBlocks
    }
    tryParseToBuildingBlocks = fun jsonString -> async {
        let table = JsonImport.tryToTable jsonString
        if table.Sheets.Length = 0 then failwith "Unable to find any Swate annotation table information! Please check if uploaded json and chosen json import type match."
        let buildingBlocks = table.Sheets |> Array.ofList |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
        return buildingBlocks
    }
}

let isaDotNetCommonAPIv1 : IISADotNetCommonAPIv1 =
    let assayFromByteArray (byteArray: byte []) =
        let ms = new MemoryStream(byteArray)
        let jsonStr = ISADotNet.XLSX.AssayFile.Assay.fromStream ms
        jsonStr
    let investigationFromByteArray (byteArray: byte []) =
        let ms = new MemoryStream(byteArray)
        let jsonStr =
            ISADotNet.XLSX.Investigation.fromStream ms
        jsonStr
    {
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Assay.
        toAssayJson = fun byteArray -> async {
            let assay = assayFromByteArray byteArray |> fun (_,assay) -> assay
            return box assay
        }
        // This functions reads an ISA-XLSX protocol template as byte [] and returns template metadata and the correlated assay.json.
        // This is the main interop function for SWOBUP.
        toSwateTemplateJson = fun byteArray -> async {
            let metadata = TemplateMetadata.parseDynMetadataFromByteArr byteArray
            let ms = new MemoryStream(byteArray)
            let doc = FsSpreadsheet.ExcelIO.Spreadsheet.fromStream ms false
            let tableName = metadata.TryGetValue "Table"
            let assay = ISADotNet.Assay.fromTemplateSpreadsheet (doc, string tableName.Value) 
            let assayJson = ISADotNet.Json.Assay.toString assay.Value
            metadata.SetValue("TemplateJson",assayJson)
            return metadata |> box
        }
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Investigation.
        toInvestigationJson = fun byteArray -> async {
            let investigation = investigationFromByteArray byteArray
            return box investigation
        }
        toProcessSeqJson = fun byteArray -> async {
            let assay = assayFromByteArray byteArray 
            let processList = assay |> fun (_,assay) -> Option.defaultValue [] assay.ProcessSequence
            return box processList
        }
        // [<System.ObsoleteAttribute>]
        //toTableJson = fun byteArray -> async {
        //    let assay = assayFromByteArray byteArray 
        //    let table = assay |> fun (_,_,_,assay) -> assay |> ISADotNet.Json.AssayCommonAPI.RowWiseAssay.fromAssay
        //    return box table
        //}
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Assay.
        toAssayJsonStr = fun byteArray -> async {
            let assayJsonString = assayFromByteArray byteArray |> fun (_,assay) -> ISADotNet.Json.Assay.toString assay
            return assayJsonString
        }
        // This functions reads an ISA-XLSX protocol template as byte [] and returns template metadata and the correlated assay.json.
        toSwateTemplateJsonStr = fun byteArray -> async {
            let metadata = TemplateMetadata.parseDynMetadataFromByteArr byteArray
            let ms = new MemoryStream(byteArray)
            let doc = FsSpreadsheet.ExcelIO.Spreadsheet.fromStream ms false
            let tableName = metadata.TryGetValue "Table"
            let assay = ISADotNet.Assay.fromTemplateSpreadsheet (doc, string tableName.Value) 
            let assayJson = ISADotNet.Json.Assay.toString assay.Value
            metadata.SetValue("TemplateJson",assayJson)
            let jsonExp = metadata.toJson()
            return jsonExp
        }
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Investigation.
        toInvestigationJsonStr = fun byteArray -> async {
            let investigationJson = investigationFromByteArray byteArray |> ISADotNet.Json.Investigation.toString
            return investigationJson
        }
        toProcessSeqJsonStr = fun byteArray -> async {
            let assay = assayFromByteArray byteArray 
            let processJSon = assay |> fun (_,assay) -> Option.defaultValue "" (Option.map ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence) 
            return processJSon
        }
        // [<System.ObsoleteAttribute>]
        //toTableJsonStr = fun byteArray -> async {
        //    let assay = assayFromByteArray byteArray 
        //    let processJSon = assay |> fun (_,_,_,assay) -> assay |> (ISADotNet.Json.AssayCommonAPI.RowWiseAssay.fromAssay >> ISADotNet.Json.AssayCommonAPI.RowWiseAssay.toString) 
        //    return processJSon
        //}
        testPostNumber = fun num -> async {
            let res = $"Hey you just sent us a number. Is this your number {num}?"
            return res
        }
        getTestNumber = fun () -> async {
            return "42"
        }
    }

open Database

let protocolApi credentials = {
    getAllProtocolsWithoutXml = fun () -> async {
        let protocols = Template.Queries.Template(credentials).getAll() |> Array.ofSeq
        return protocols
    }

    getProtocolById = fun templateId -> async { return Template.Queries.Template(credentials).getById(templateId) }

    increaseTimesUsedById = fun templateId -> async {
        let _ = Template.Queries.Template(credentials).increaseTimesUsed(templateId)
        return ()
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
        let exmp = Term.Term(c).getByName(termName,sourceOntologyName="ms")
        return "Info", sprintf "%A" (exmp |> Seq.length)
    }
}

let createIProtocolApiv1 credentials =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (protocolApi credentials)
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

///// due to a bug in Fable.Remoting this does currently not work as inteded and is ignored. (https://github.com/Zaid-Ajaj/Fable.Remoting/issues/198)
//let mainApiController = router {

    ////
    //forward @"/IOntologyAPIv1" (fun next ctx ->
    //    let cString = 
    //        let settings = ctx.GetService<IConfiguration>()
    //        settings.["Swate:ConnectionString"]
    //    createIOntologyApiv1 cString next ctx
    //)

//    //
//    forward @"/IServiceAPIv1" (fun next ctx ->
//        createIServiceAPIv1 next ctx
//    )
//}

let getMessage() = "Hello from SAFE!"

let getNeo4JCredentials (ctx: HttpContext) =
    let settings = ctx.GetService<IConfiguration>()
    let credentials : Helper.Neo4JCredentials = {
        User        = settings.[Helper.Neo4JCredentials.UserVarString]
        Pw          = settings.[Helper.Neo4JCredentials.PwVarString]
        BoltUrl     = settings.[Helper.Neo4JCredentials.UriVarString]
        DatabaseName= settings.[Helper.Neo4JCredentials.DBNameVarString]
    }
    credentials

let topLevelRouter = router {
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
        createIProtocolApiv1 credentials next ctx
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

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.StaticFiles

// https://cors-test.codehappy.dev/?url=https%3A%2F%2Fswate.nfdi4plants.org%2Fapi%2FIOntologyAPIv2%2FgetAllOntologies&method=get
/// Enable CORS. Makes external access of Swate API possible
let configureServices (services:IServiceCollection) =
    services
        .AddCors()
        .AddGiraffe()

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
    url "http://0.0.0.0:5000" //"http://localhost:5000/"
    use_router topLevelRouter
    app_config config
    service_config configureServices
    memory_cache
    use_static "public"
    use_gzip
    logging (fun (builder: ILoggingBuilder) -> builder.SetMinimumLevel(LogLevel.Debug) |> ignore)
}

app
    .ConfigureAppConfiguration(
        System.Action<Microsoft.Extensions.Hosting.HostBuilderContext,IConfigurationBuilder> ( fun ctx config ->
            config.AddUserSecrets("6de80bdf-2a05-4cf7-a1a8-d08581dfa887") |> ignore
            config.AddJsonFile("dev.json",true,true)            |> ignore
            config.AddJsonFile("production.json",true,true)     |> ignore
        )
)
|> run

