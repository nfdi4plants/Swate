open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
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

//let connectionString = System.Environment.GetEnvironmentVariable("AnnotatorTestDbCS")
//[<Literal>]
//let DevLocalConnectionString = "server=127.0.0.1;user id=root;password=example; port=42333;database=SwateDB;allowuservariables=True;persistsecurityinfo=True"


/// Showcase of how versioning could work
let testApi = {
        //Development
        getTestNumber = fun () -> async { return 42 }
    }

let serviceApi = {
    getAppVersion = fun () -> async {return System.AssemblyVersionInformation.AssemblyVersion}
}

let annotatorApi cString = {

    //Development
    getTestNumber = fun () -> async { return 42 }
    getTestString = fun strOpt -> async { return sprintf "Test string: %A" strOpt }

    //Ontology related requests
    testOntologyInsert = fun (name,version,definition,created,user) ->
        async {
            /// Don't allow users to access this part!! At least for now
            //let createdEntry = OntologyDB.insertOntology cString name version definition created user
            let onto =
                DbDomain.createOntology 
                    0L
                    name
                    version
                    definition
                    created
                    user
            printfn "created ontology entry: \t%A" onto
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
            let like = OntologyDB.getTermSuggestions cString typedSoFar
            let searchSet = typedSoFar |> Suggestion.createBigrams

            return
                like
                |> Array.sortByDescending (fun sugg ->
                        Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                )
                
                |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)
        }

    getTermSuggestionsByParentTerm = fun (max:int,typedSoFar:string,parentTerm:string) ->
        async {
            let like = OntologyDB.getTermSuggestionsByParentTerm cString (typedSoFar,parentTerm)
            let searchSet = typedSoFar |> Suggestion.createBigrams

            return
                like
                |> Array.sortByDescending (fun sugg ->
                        Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                )
                
                |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)
        }

    getTermsForAdvancedSearch = fun (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete) ->
        async {
            let result =
                OntologyDB.getAdvancedTermSearchResults cString ontOpt searchName mustContainName searchDefinition mustContainDefinition keepObsolete
            return result
        }

    getUnitTermSuggestions = fun (max:int,typedSoFar:string) ->
        async {
            let like = OntologyDB.getUnitTermSuggestions cString typedSoFar
            let searchSet = typedSoFar |> Suggestion.createBigrams

            return
                like
                |> Array.sortByDescending (fun sugg ->
                        Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                )
                
                |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)
        }
}

let testWebApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue testApi
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler

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

let mainApiController = router {

    //
    forward @"/IAnnotatorAPIv1" (fun next ctx ->
        let cString = 
            let settings = ctx.GetService<IConfiguration>()
            settings.["Swate:ConnectionString"]
        createIAnnotatorApiv1 cString next ctx
    )

    //
    forward @"/ITestAPI" (fun next ctx ->
        testWebApp next ctx
    )

    //
    forward @"/IServiceAPIv1" (fun next ctx ->
        createIServiceAPIv1 next ctx
    )
}

let topLevelRouter = router {
    get "/test/test1" (htmlString "<h1>Hi this is test response 1</h1>")
    forward "/api" mainApiController
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

