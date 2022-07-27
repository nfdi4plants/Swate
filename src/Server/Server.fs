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
        let factors, protocol, assay =  JsonExport.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        let processSequence = Option.defaultValue [] assay.ProcessSequence
        let dag = Viz.DAG.fromProcessSequence (processSequence,Viz.Schema.NFDIBlue) |> CyjsAdaption.MyHTML.toEmbeddedHTML
        return dag
    }
}

let swateJsonAPIv1 = {
    parseAnnotationTableToAssayJson = fun (worksheetName,buildingblocks) -> async {
        let factors, protocol, assay = JsonExport.parseBuildingBlockToAssay worksheetName buildingblocks
        printfn "HERE:"
        printfn "%A" assay
        let parsedJsonStr = ISADotNet.Json.Assay.toString assay
        return parsedJsonStr
    }
    parseAnnotationTableToProcessSeqJson = fun (worksheetName,buildingblocks) -> async {
        let factors, protocol, assay = JsonExport.parseBuildingBlockToAssay worksheetName buildingblocks
        let parsedJsonStr = ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence.Value
        return parsedJsonStr
    }
    //parseAnnotationTableToTableJson = fun (worksheetName,buildingblocks) -> async {
    //    let factors, protocol, assay = JsonExport.parseBuildingBlockToAssay worksheetName buildingblocks
    //    let parsedJsonStr = (ISADotNet.Json.AssayCommonAPI.RowWiseAssay.fromAssay >> ISADotNet.Json.AssayCommonAPI.RowWiseAssay.toString) assay
    //    return parsedJsonStr
    //}
    parseAnnotationTablesToAssayJson = fun worksheetBuildingBlocks -> async {
        let factors, protocol, assay =  JsonExport.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
        let parsedJsonStr = ISADotNet.Json.Assay.toString assay
        return parsedJsonStr
    }
    parseAnnotationTablesToProcessSeqJson = fun worksheetBuildingBlocks -> async {
        let factors, protocol, assay =  JsonExport.parseBuildingBlockSeqsToAssay worksheetBuildingBlocks
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
        let buildingBlocks = table.Sheets |> Array.ofList |> Array.map(fun s -> s.SheetName,s.toInsertBuildingBlockList |> Array.ofList)
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
            let assay = assayFromByteArray byteArray |> fun (_,_,_,assay) -> assay
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
            return box metadata
        }
        // This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Investigation.
        toInvestigationJson = fun byteArray -> async {
            let investigation = investigationFromByteArray byteArray
            return box investigation
        }
        toProcessSeqJson = fun byteArray -> async {
            let assay = assayFromByteArray byteArray 
            let processList = assay |> fun (_,_,_,assay) -> Option.defaultValue [] assay.ProcessSequence
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
            let assayJsonString = assayFromByteArray byteArray |> fun (_,_,_,assay) -> ISADotNet.Json.Assay.toString assay
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
            let processJSon = assay |> fun (_,_,_,assay) -> Option.defaultValue "" (Option.map ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence) 
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

let ontologyApi (credentials : Helper.Neo4JCredentials) : IOntologyAPIv1 =
    /// Neo4j prefix query does not provide any measurement on distance between query and result.
    /// Thats why we apply sorensen dice after the database search.
    let sorensenDiceSortTerms (searchStr:string) (terms: Term []) =
        terms |> SorensenDice.sortBySimilarity searchStr (fun term -> term.Name)
    
    {
        //Development

        getTestNumber = fun () -> async { return 42 }

        //Ontology related requests

        getAllOntologies = fun () ->
            async {
                let results = Ontology.Ontology(credentials).getAll() |> Array.ofSeq
                return results
            }

        // Term related requests

        getTermSuggestions = fun (max:int,typedSoFar:string) ->
            async {
                let dbSearchRes =
                    match typedSoFar with
                    | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                        Term.Term(credentials).getByAccession foundAccession.Value
                    /// This suggests we search for a term name
                    | notAnAccession ->
                        Term.Term(credentials).getByName notAnAccession
                    |> Array.ofSeq
                    |> sorensenDiceSortTerms typedSoFar
                let arr = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                return arr
            }

        getTermSuggestionsByParentTerm = fun (max:int,typedSoFar:string,parentTerm:TermMinimal) ->
            async {
                let dbSearchRes =
                    match typedSoFar with
                    | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                        Database.Term.Term(credentials).getByAccession foundAccession.Value
                    | _ ->
                        if parentTerm.TermAccession = ""
                        then
                            Term.Term(credentials).getByNameAndParent_Name(typedSoFar,parentTerm.Name,Helper.FullTextSearch.PerformanceComplete)
                        else
                            Term.Term(credentials).getByNameAndParent(typedSoFar,parentTerm,Helper.FullTextSearch.PerformanceComplete)
                    |> Array.ofSeq
                    |> sorensenDiceSortTerms typedSoFar
                let res = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                return res
            }

        getAllTermsByParentTerm = fun (parentTerm:TermMinimal) ->
            async {
                let searchRes = Database.Term.Term(credentials).getAllByParent(parentTerm,limit=500) |> Array.ofSeq
                return searchRes  
            }

        getTermSuggestionsByChildTerm = fun (max:int,typedSoFar:string,childTerm:TermMinimal) ->
            async {

                let dbSearchRes =
                    match typedSoFar with
                    | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                        Term.Term(credentials).getByAccession foundAccession.Value
                    | _ ->
                        if childTerm.TermAccession = ""
                        then
                            Term.Term(credentials).getByNameAndChild_Name (typedSoFar,childTerm.Name,Helper.FullTextSearch.PerformanceComplete)
                        else
                            Term.Term(credentials).getByNameAndChild(typedSoFar,childTerm.TermAccession,Helper.FullTextSearch.PerformanceComplete)
                    |> Array.ofSeq
                    |> sorensenDiceSortTerms typedSoFar
                let res = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                return res
            }

        getAllTermsByChildTerm = fun (childTerm:TermMinimal) ->
            async {
                let searchRes = Term.Term(credentials).getAllByChild (childTerm) |> Array.ofSeq
                return searchRes  
            }

        getTermsForAdvancedSearch = fun advancedSearchOption ->
            async {
                let result = Term.Term(credentials).getByAdvancedTermSearch(advancedSearchOption) |> Array.ofSeq
                let filteredResult =
                    if advancedSearchOption.KeepObsolete then
                        result
                    else
                        result |> Array.filter (fun x -> x.IsObsolete |> not)
                return filteredResult
            }

        getUnitTermSuggestions = fun (max:int,typedSoFar:string, unit:UnitSearchRequest) ->
            async {
                let dbSearchRes =
                    match typedSoFar with
                    | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                        Term.Term(credentials).getByAccession foundAccession.Value
                    | notAnAccession ->
                        Term.Term(credentials).getByName(notAnAccession,sourceOntologyName="uo")
                    |> Array.ofSeq
                    |> sorensenDiceSortTerms typedSoFar
                let res = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                return (res, unit)
            }

        getTermsByNames = fun (queryArr) ->
            async {
                // check if search string is empty. This case should delete TAN- and TSR- values in table
                let filteredQueries = queryArr |> Array.filter (fun x -> x.Term.Name <> "" || x.Term.TermAccession <> "")
                let queries =
                    filteredQueries |> Array.map (fun searchTerm ->
                        // check if term accession was found. If so search also by this as it is unique
                        if searchTerm.Term.TermAccession <> "" then
                            Term.TermQuery.getByAccession searchTerm.Term.TermAccession
                        // if term is a unit it should be contained inside the unit ontology, if not it is most likely free text input.
                        elif searchTerm.IsUnit then
                            Term.TermQuery.getByName(searchTerm.Term.Name, searchType=Helper.FullTextSearch.Exact, sourceOntologyName="uo")
                        // if none of the above apply we do a standard term search
                        else
                            Term.TermQuery.getByName(searchTerm.Term.Name, searchType=Helper.FullTextSearch.Exact)
                    )
                let result =
                    Helper.Neo4j.runQueries(queries,credentials)
                    |> Array.map2 (fun termSearchable dbResults ->
                        // replicate if..elif..else conditions from 'queries'
                        if termSearchable.Term.TermAccession <> "" then
                            let result =
                                if Array.isEmpty dbResults then
                                    None
                                else
                                    // search by accession must be unique, and has unique restriction in database, so there can only be 0 or 1 result
                                    let r = dbResults |> Array.exactlyOne
                                    if r.Name <> termSearchable.Term.Name then 
                                        failwith $"""Found mismatch between Term Accession and Term Name. Term name "{termSearchable.Term.Name}" and term accession "{termSearchable.Term.TermAccession}",
                                        but accession belongs to name "{r.Name}" (ontology: {r.FK_Ontology})"""
                                    Some r
                            { termSearchable with SearchResultTerm = result }
                        // search is done by name and only in the unit ontology. Therefore unit term must be unique.
                        // This might need future work, as we might want to support types of unit outside of the unit ontology
                        elif termSearchable.IsUnit then
                            { termSearchable with SearchResultTerm = if dbResults |> Array.isEmpty then None else Some dbResults.[0] }
                        else
                            { termSearchable with SearchResultTerm = if dbResults |> Array.isEmpty then None else Some dbResults.[0] }
                    ) filteredQueries
                return result
            }

        // Tree related requests
        getTreeByAccession = fun accession -> async {
            let tree = Database.TreeSearch.Tree(credentials).getByAccession(accession)
            return tree
        }
    }

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
                User        = settings.["neo4j-username"]
                Pw          = settings.["neo4j-pw"]
                BoltUrl     = settings.["neo4j-uri"]
                DatabaseName= settings.["neo4j-db"]
            }
            credentials
        //let exmp = OntologyDB.Queries.Term(c).getByAdvancedTermSearch(termName="insturment~ -Shimadzu")
        return "Info", "nothing active here"
    }
    postTest = fun (termName) -> async {
        let c =
            let settings = ctx.GetService<IConfiguration>()
            let credentials : Helper.Neo4JCredentials= {
                User        = settings.["neo4j-username"]
                Pw          = settings.["neo4j-pw"]
                BoltUrl     = settings.["neo4j-uri"]
                DatabaseName= settings.["neo4j-db"]
            }
            credentials
        let exmp = Term.Term(c).getByName(termName,sourceOntologyName="ms")
        return "Info", sprintf "%A" (exmp |> Seq.length)
    }
}

let errorHandler (ex:exn) (routeInfo:RouteInfo<HttpContext>) =
    let msg = sprintf "%A %s @%s." ex.Message System.Environment.NewLine routeInfo.path
    Propagate msg

let createIProtocolApiv1 credentials =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (protocolApi credentials)
    //|> Remoting.withDocs Shared.URLs.DocsApiUrl DocsAnnotationAPIvs1.ontologyApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.buildHttpHandler

let createIOntologyApiv1 credentials =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (ontologyApi credentials)
    //|> Remoting.withDocs Shared.URLs.DocsApiUrl DocsAnnotationAPIvs1.ontologyApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.buildHttpHandler

let createIServiceAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue serviceApi
    //|> Remoting.withDocs Shared.URLs.DocsApiUrl2 DocsServiceAPIvs1.serviceApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.buildHttpHandler

let createISADotNetCommonAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue isaDotNetCommonAPIv1
    |> Remoting.withDocs "/api/IISADotNetCommonAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.buildHttpHandler

let createExpertAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue swateJsonAPIv1
    //|> Remoting.withDocs "/api/IExpertAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.buildHttpHandler

let createDagApiv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue dagApiv1
    //|> Remoting.withDocs "/api/IExpertAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.buildHttpHandler

let createTestApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext testApi
    //|> Remoting.withDocs "/api/IExpertAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler errorHandler
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

let topLevelRouter = router {
    get "/test/test1" (htmlString "<h1>Hi this is test response 1</h1>")
    get "/test/hello" (getMessage() |> json)

    forward @"" (fun next ctx ->
        let credentials =
            let settings = ctx.GetService<IConfiguration>()
            let (credentials : Helper.Neo4JCredentials) = {
                User        = settings.["neo4j-username"]
                Pw          = settings.["neo4j-pw"]
                BoltUrl     = settings.["neo4j-uri"]
                DatabaseName= settings.["neo4j-db"]
            }
            credentials
        createIOntologyApiv1 credentials next ctx
    )

    forward @"" (fun next ctx ->
        let credentials =
            let settings = ctx.GetService<IConfiguration>()
            let (credentials : Helper.Neo4JCredentials) = {
                User        = settings.["neo4j-username"]
                Pw          = settings.["neo4j-pw"]
                BoltUrl     = settings.["neo4j-uri"]
                DatabaseName= settings.["neo4j-db"]
            }
            credentials
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

let app = application {
    url "http://0.0.0.0:5000" //"http://localhost:5000/"
    use_router topLevelRouter
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

