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

let annotatorApi cString = {

    //Development
    getTestNumber = fun () -> async { return 42 }

    //Ontology related requests
    testOntologyInsert = fun (name,version,definition,created,user) ->
        async {
            let createdEntry = OntologyDB.insertOntology cString name version definition created user
            printfn "created ontology entry: \t%A" createdEntry
            return createdEntry
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

    getTermsForAdvancedSearch = fun (ont,startsWith,mustContain,endsWith,keepObsolete,definitionMustContain) ->
        async {
            let result =
                OntologyDB.getAdvancedTermSearchResults cString ont startsWith mustContain endsWith keepObsolete definitionMustContain
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

let webApp cString =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (annotatorApi cString)
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler

let topLevelRouter = router {
    get "/test/test1" (htmlString "<h1>Hi this is test response 1</h1>")
    // Never ever use this in production lol
    //get "/test/test2" (fun next ctx ->
        
    //    let settings = ctx.GetService<IConfiguration>()
    //    let cString = settings.["Swate:ConnectionString"]

    //    htmlString (sprintf "<h1>Here is a secret: %s</h1>" cString) next ctx
    //)
    forward "/api" (fun next ctx ->
        let cString = 
            let settings = ctx.GetService<IConfiguration>()
            settings.["Swate:LocalConnectionString"]
        webApp cString next ctx

    )

}

let app = application {
    url "https://0.0.0.0:8080"
    force_ssl
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
        )
)
|> run

