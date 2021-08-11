open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
//open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration.Json
open Microsoft.Extensions.Configuration.UserSecrets
open Microsoft.AspNetCore.Hosting

/// Was transferred into dev.json
//[<Literal>]
//let DevLocalConnectionString = "server=127.0.0.1;user id=root;password=example; port=42333;database=SwateDB;allowuservariables=True;persistsecurityinfo=True"

let serviceApi = {
    getAppVersion = fun () -> async { return System.AssemblyVersionInformation.AssemblyVersion }
}

//open ISADotNet

//let isaDotNetApi = {
//    parseJsonToProcess = fun jsonString -> async {
//        let parsedJson = ISADotNet.Json.Process.fromString jsonString
//        return parsedJson
//    }
//}

let annotatorApi cString = {

    //Development
    getTestNumber = fun () -> async { return 42 }
    getTestString = fun strOpt -> async { return None }

    //Ontology related requests
    testOntologyInsert = fun (name,version,definition,created,user) ->
        async {
            /// Don't allow users to access this part!! At least for now
            //let createdEntry = OntologyDB.insertOntology cString name version definition created user
            let onto =
                DbDomain.createOntology 
                    name
                    version
                    definition
                    created
                    user
            printfn "created pseudo ontology entry: \t%A. No actual db insert has happened." onto
            return onto
        }

    getAllOntologies = fun () ->
        async {
            let results = OntologyDB.getAllOntologies cString ()
            return results
        }

    // Term related requests
    getTermSuggestions = fun (max:int,typedSoFar:string) ->
        async {
            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like = OntologyDB.getTermSuggestions cString (typedSoFar)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)
            return searchRes
        }

    getTermSuggestionsByParentTerm = fun (max:int,typedSoFar:string,parentTerm:TermMinimal) ->
        async {

            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like =
                        if parentTerm.TermAccession = ""
                        then
                            OntologyDB.getTermSuggestionsByParentTerm cString (typedSoFar,parentTerm.Name)
                        else
                            OntologyDB.getTermSuggestionsByParentTermAndAccession cString (typedSoFar,parentTerm.Name,parentTerm.TermAccession)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                    
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)

            return searchRes
        }

    getAllTermsByParentTerm = fun (parentTerm:TermMinimal) ->
        async {
            let searchRes =
                OntologyDB.getAllTermsByParentTermOntologyInfo cString parentTerm

            return searchRes  
        }

    getTermSuggestionsByChildTerm = fun (max:int,typedSoFar:string,childTerm:TermMinimal) ->
        async {

            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like =
                        if childTerm.TermAccession = ""
                        then
                            OntologyDB.getTermSuggestionsByChildTerm cString (typedSoFar,childTerm.Name)
                        else
                            OntologyDB.getTermSuggestionsByChildTermAndAccession cString (typedSoFar,childTerm.Name,childTerm.TermAccession)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                    
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)

            return searchRes
        }

    getAllTermsByChildTerm = fun (childTerm:TermMinimal) ->
        async {
            let searchRes =
                OntologyDB.getAllTermsByChildTermOntologyInfo cString childTerm

            return searchRes  
        }

    getTermsForAdvancedSearch = fun (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete) ->
        async {
            let result =
                let searchSet = searchName + mustContainName + searchDefinition + mustContainDefinition|> Suggestion.createBigrams
                OntologyDB.getAdvancedTermSearchResults cString ontOpt searchName mustContainName searchDefinition mustContainDefinition keepObsolete
                |> Array.sortByDescending (fun sugg ->
                    Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
            return result
        }

    getUnitTermSuggestions = fun (max:int,typedSoFar:string, unit:UnitSearchRequest) ->
        async {
            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like = OntologyDB.getUnitTermSuggestions cString (typedSoFar)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)

            return (searchRes, unit)
        }

    getTermsByNames = fun (queryArr) ->
        async {
            let result =
                queryArr |> Array.map (fun searchTerm ->
                    {searchTerm with
                        TermOpt =
                            // check if search string is empty. This case should delete TAN- and TSR- values in table
                            if searchTerm.SearchQuery.Name = "" then None
                            // check if term accession was found. If so search also by this as it is unique
                            elif searchTerm.SearchQuery.TermAccession <> "" then
                                let searchRes = OntologyDB.getTermByNameAndAccession cString (searchTerm.SearchQuery.Name,searchTerm.SearchQuery.TermAccession)
                                if Array.isEmpty searchRes then
                                    None
                                else
                                    searchRes |> Array.head |> Some
                            elif searchTerm.IsA.IsSome then
                                let searchRes = OntologyDB.getTermByParentTermOntologyInfo cString (searchTerm.SearchQuery.Name,searchTerm.IsA.Value)
                                if Array.isEmpty searchRes then
                                    let searchRes' = OntologyDB.getTermByName cString searchTerm.SearchQuery.Name
                                    if Array.isEmpty searchRes' then None else searchRes' |> Array.head |> Some
                                else
                                    searchRes |> Array.head |> Some
                            else
                                let searchRes = OntologyDB.getTermByName cString searchTerm.SearchQuery.Name
                                if Array.isEmpty searchRes then None else searchRes |> Array.head |> Some
                    }
                )
            return result
        }

    getAllProtocolsWithoutXml = fun () -> async {
        let protocols = ProtocolDB.getAllProtocols cString
        return protocols
    }

    getProtocolXmlForProtocol = fun prot -> async { return ProtocolDB.getXmlByProtocol cString prot }

    getProtocolsByName = fun (names) -> async {
        let protsWithoutXml = names |> Array.map (fun x -> ProtocolDB.getProtocolByName cString x)
        let protsWithXml = protsWithoutXml |> Array.map (ProtocolDB.getXmlByProtocol cString)
        return protsWithXml
    }

    increaseTimesUsed = fun templateName -> async {
        ProtocolDB.increaseTimesUsed cString templateName
        return ()
    }
}

let createIServiceAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue serviceApi
    |> Remoting.withDocs "/api/IServiceAPIv1/docs" DocsServiceAPIvs1.serviceApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler

//let createISADotNetAPIv1 =
//    Remoting.createApi()
//    |> Remoting.withRouteBuilder Route.builder
//    |> Remoting.fromValue isaDotNetApi
//    |> Remoting.withDocs "/api/IISADotNetAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetApiDocsv1
//    |> Remoting.withDiagnosticsLogger(printfn "%A")
//    |> Remoting.withErrorHandler(
//        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
//    )
//    |> Remoting.buildHttpHandler

let createIAnnotatorApiv1 cString =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (annotatorApi cString)
    |> Remoting.withDocs "/api/IAnnotatorAPIv1/docs" DocsAnnotationAPIvs1.annotatorApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler


/// due to a bug in Fable.Remoting this does currently not work as inteded and is ignored. (https://github.com/Zaid-Ajaj/Fable.Remoting/issues/198)
let mainApiController = router {

    //
    forward @"/IAnnotatorAPIv1" (fun next ctx ->
        let cString = 
            let settings = ctx.GetService<IConfiguration>()
            settings.["Swate:ConnectionString"]
        createIAnnotatorApiv1 cString next ctx
    )

    //
    forward @"/IServiceAPIv1" (fun next ctx ->
        createIServiceAPIv1 next ctx
    )
}

let topLevelRouter = router {
    get "/test/test1" (htmlString "<h1>Hi this is test response 1</h1>")
    //forward "/api" mainApiController
    forward @"" (fun next ctx ->
        let cString = 
            let settings = ctx.GetService<IConfiguration>()
            settings.["Swate:ConnectionString"]
        createIAnnotatorApiv1 cString next ctx
    )

    //
    forward @"" (fun next ctx ->
        createIServiceAPIv1 next ctx
    )

    //
    //forward @"" (fun next ctx ->
    //    createISADotNetAPIv1 next ctx
    //)
}

let app = application {
    url "http://localhost:5000/"
    //force_ssl
    use_router topLevelRouter
    memory_cache
    use_static "public"
    use_gzip
    logging (fun (builder: ILoggingBuilder) -> builder.SetMinimumLevel(LogLevel.Warning) |> ignore)
}

app
    .ConfigureAppConfiguration(
        System.Action<Microsoft.Extensions.Hosting.HostBuilderContext,IConfigurationBuilder> ( fun ctx config ->
            config.AddUserSecrets("6de80bdf-2a05-4cf7-a1a8-d08581dfa887") |> ignore
            config.AddJsonFile("production.json",true,true)  |> ignore
            config.AddJsonFile("dev.json",true,true)  |> ignore
        )
)
|> run

