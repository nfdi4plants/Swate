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

let testApi = {
    //Development
    getTestNumber = fun () -> async { return 42 }
}

let annotatorApi cString = {

    //Development
    getTestNumber = fun () -> async { return 42 }
    getTestString = fun () -> async { return "test string" }

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

let testWebApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (testApi)
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler


let annotatorDocs = Docs.createFor<IAnnotatorAPI>()

let annotatorApiDocs =
    Remoting.documentation "Annotation API" [
        annotatorDocs.route <@ fun api -> api.getTestString @>
        |> annotatorDocs.alias "Get Test String"
        |> annotatorDocs.description "This is used during development to check connection between client and server."

        annotatorDocs.route <@ fun api -> api.getTermSuggestionsByParentTerm @>
        |> annotatorDocs.alias "Get Terms By Parent Ontology"
        |> annotatorDocs.description "This is used to reduce the number of possible hits searching only data that is in a \"is_a\" relation to the parent ontology (written at the top of the column)."
        |> annotatorDocs.example <@ fun api -> api.getTermSuggestionsByParentTerm (5,"micrOTOF-Q","instrument model") @>
    ]

let webApp cString =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (annotatorApi cString)
    |> Remoting.withDocs "/api/IAnnotatorAPI/docs" annotatorApiDocs
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
    forward @"/api/IAnnotatorAPI" (fun next ctx ->
        // user secret part for production
        let cString = 
            let settings = ctx.GetService<IConfiguration>()
            settings.["Swate:ConnectionString"]
        webApp cString next ctx
    )
    forward @"/api/ITestAPI" (fun next ctx ->
        testWebApp next ctx
    )


}

let app = application {
    url "https://localhost:443/"
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
            config.AddJsonFile("dev.json",true,true)  |> ignore
        )
)
|> run

