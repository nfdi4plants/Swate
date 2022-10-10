module Database.Helper

open System
open Neo4j.Driver

let defaultOutputWith<'a> (def:'a) (neo4jReturnVal:obj) =
    if isNull neo4jReturnVal then def else neo4jReturnVal.As<'a>()

type FullTextSearch =
| Exact
| Complete
| PerformanceComplete
| Fuzzy
with
    member this.ofQueryString(queryString:string) =
        match this with
        | Exact         -> queryString
        | Complete      -> queryString + "*"
        | PerformanceComplete ->
            let s = queryString.Split(" ", StringSplitOptions.RemoveEmptyEntries)
            s
            // add "+" to every word so the fulltext search must include the previous word, this highly improves search performance
            // Searchquality is further improved by $"({str}^4 OR {str}*)". So it will value perfect hits higher then autocomplete hits.
            |> Array.mapi (fun i str -> if i <> s.Length-1 then "+" + str else $"({str}^4 OR {str}*)")
            |> String.concat " "
        | Fuzzy         -> queryString.Replace(" ","~ ") + "~"
            
type Neo4JCredentials = {
    User        : string
    Pw          : string
    BoltUrl     : string
    DatabaseName: string
} with
    static member UserVarString = "DB_USER"
    static member PwVarString = "DB_PASSWORD"
    static member UriVarString = "DB_URL"
    static member DBNameVarString = "DB_NAME"

type Neo4j =
    
    static member establishConnection(c: Neo4JCredentials) =
        let driver = Neo4j.Driver.GraphDatabase.Driver(c.BoltUrl, Neo4j.Driver.AuthTokens.Basic(c.User,c.Pw))
        driver.AsyncSession(SessionConfigBuilder.ForDatabase c.DatabaseName)

    /// <summary>Standardized function to easily execute neo4j cypher query.</summary>
    /// <param name="query">The cypher query string</param>
    /// <param name="parameters">Map of key value pairs. Only use this if you used parameters, for example '$Name' in your query. In which case you need to provide `Map ["Name", value]`.</param>
    /// <param name="resultAs">How to return query results. In the format of `(fun (record:IRecord) -> parsingFunction record)`.</param>
    /// <param name="credentials">Username, password, bolt-url and database name to create session with database.</param>
    /// <param name="session">Optional parameter to insert query into running session.</param>
    static member runQuery(query:string,parameters:Map<string,'a> option,resultAs:IRecord -> 'T, ?credentials:Neo4JCredentials, ?session:IAsyncSession) =
        if credentials.IsNone && session.IsNone then failwith "Cannot execute query without credentials or session parameter!"
        let currentSession = if session.IsSome then session.Value else Neo4j.establishConnection(credentials.Value)
        async {
            let! executeReadQuery =
                if parameters.IsSome then
                    // Cast a whole lot of types to expected types by neo4j driver
                    let param =
                        parameters.Value 
                        |> Map.fold (fun s k v ->  
                            let kvp = Collections.Generic.KeyValuePair.Create(k, box v)
                            kvp::s
                        ) []
                        |> fun x -> Collections.Generic.Dictionary<string,obj>(x :> Collections.Generic.IEnumerable<_>)
                    currentSession.RunAsync(Query(query,param))
                else
                    currentSession.RunAsync(query)
                |> Async.AwaitTask
            let! dbValues = 
                executeReadQuery.ToListAsync()
                |> Async.AwaitTask
            let parsedDbValues = dbValues |> Seq.map resultAs
            if session.IsNone then currentSession.Dispose()
            return parsedDbValues
        } |> Async.RunSynchronously

    /// <summary>Standardized function to easily execute neo4j cypher queries in parallel.</summary>
    /// <param name="queryArr">Array of query information. See 'runQuery' for description of parameters.</param>
    /// <param name="credentials">Username, password, bolt-url and database name to create session with database.</param>
    static member runQueries(queryArr: (string*(Map<string,'a> option)*(IRecord -> 'T)) [], credentials:Neo4JCredentials) =
        async { 
            // let! transaction = currentSession.BeginTransactionAsync() |> Async.AwaitTask
            let queries =
                queryArr
                |> Array.map (fun (q,p,resultAs) ->
                    // Cast a whole lot of types to expected types by neo4j driver
                    if p.IsSome then
                        let param =
                            p.Value 
                            |> Map.fold (fun s k v ->  
                                let kvp = Collections.Generic.KeyValuePair.Create(k, box v)
                                kvp::s
                            ) []
                            |> fun x -> Collections.Generic.Dictionary<string,obj>(x :> Collections.Generic.IEnumerable<_>)
                        Query(q,param)
                    else
                        Query(q)
                )
            let transactions = 
                queries 
                |> Array.map (fun query ->
                    let currentSession = Neo4j.establishConnection(credentials)
                    let transaction = currentSession.ReadTransactionAsync(fun tx ->
                        async {
                            let! result = tx.RunAsync query |> Async.AwaitTask
                            return! result.ToListAsync() |> Async.AwaitTask
                        }
                        |> Async.StartAsTask
                    )
                    transaction
                ) 
            let parsedToResult =
                transactions
                |> Array.mapi (fun i x -> 
                    let _,_,resultAs = queryArr.[i]
                    Seq.map resultAs x.Result
                    |> Array.ofSeq
                )
            return parsedToResult

        }
        |> Async.RunSynchronously