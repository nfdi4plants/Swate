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

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let port = 8080us

module Suggestion =
    
    let inline sorensenDice (x : Set<'T>) (y : Set<'T>) =
        match  (x.Count, y.Count) with
        | (0,0) -> 1.
        | (xCount,yCount) -> (2. * (Set.intersect x y |> Set.count |> float)) / ((xCount + yCount) |> float)
    
    
    let createBigrams (s:string) =
        s
            .ToUpperInvariant()
            .ToCharArray()
        |> Array.windowed 2
        |> Array.map (fun inner -> sprintf "%c%c" inner.[0] inner.[1])
        |> set

let annotatorApi = {
    testOntologyInsert =
        fun (name,version,definition,created,user) ->
            async {
                let createdEntry = OntologyDB.insertOntology name version definition created user
                printfn "created ontology entry: \t%A" createdEntry
                return createdEntry
            }

    getTermSuggestions =
        fun (max:int,typedSoFar:string) -> async {
            let like = OntologyDB.getTermSuggestions typedSoFar
            let searchSet = typedSoFar |> Suggestion.createBigrams

            return
                like
                |> Array.sortByDescending (fun sugg ->
                        Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                )
                
                |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)
        }
}

let docs = Docs.createFor<IAnnotatorAPI>()

let apiDocumentation =
    Remoting.documentation "CSBAnnotatorAPI" [
        docs.route <@ fun api (name,version,definition,created,user) -> api.testOntologyInsert (name,version,definition,created,user) @>
        |> docs.alias "maketestinsert"
        |> docs.description "I dont know i just want to test xd"
        |> docs.example<@ fun api -> api.testOntologyInsert ("Name","SooSOSO","FIIIF",System.DateTime.UtcNow,"MEEM") @>
    ]


let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue annotatorApi
    |> Remoting.withDocs "/api/docs" apiDocumentation
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler

let topLevelRouter = router {
    get "/test/test1" (htmlString "<h1>Hi this is test response 1</h1>")
    forward "/api" webApp
}

let app = application {
    url ("https://0.0.0.0:" + port.ToString() + "/")
    force_ssl
    use_router topLevelRouter
    memory_cache
    use_static publicPath
    use_gzip
    logging (fun (builder: ILoggingBuilder) -> builder.SetMinimumLevel(LogLevel.Warning) |> ignore)
}

run app
