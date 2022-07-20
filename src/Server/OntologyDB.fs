module OntologyDB

open System
open System.IO
open Neo4j

open Neo4j.Driver
open System.Threading.Tasks
open Shared.TermTypes

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
            |> Array.mapi (fun i str -> if i <> s.Length-1 then "+" + str else str + "*")
            |> String.concat " "
        | Fuzzy         -> queryString.Replace(" ","~ ") + "~"
            
type Neo4JCredentials = {
    User        : string
    Pw          : string
    BoltUrl     : string
    DatabaseName: string
}

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

type Ontology(credentials:Neo4JCredentials) =

    static member private asOntology : IRecord -> Shared.TermTypes.Ontology =
        fun (record:IRecord) ->
            let (ontology:Shared.TermTypes.Ontology) = {
                Name        = record.["o.name"].As<string>()
                Version     = 
                    let r = record.["o.version"].As<string>()
                    if isNull r then "" else r
                LastUpdated = record.["o.lastUpdated"].As<string>() |> System.DateTime.Parse
                Author      = 
                    let r = record.["o.author"].As<string>()
                    if isNull r then "" else r
            }
            ontology

    member this.getAll() = 
        let query = 
            @"MATCH (o:Ontology)
            RETURN o.name, o.lastUpdated, o.version, o.author" 
        Neo4j.runQuery(
            query,
            None,
            Ontology.asOntology,
            credentials
        )

    member this.getByName(name: string) = 
        let query = 
            @"MATCH (o:Ontology {name: $Name})
            RETURN o.name, o.lastUpdated, o.version, o.author" 
        let param =
            Map ["Name",name] |> Some
        Neo4j.runQuery(
            query,
            param,
            Ontology.asOntology,
            credentials
        )

type Term(?credentials:Neo4JCredentials, ?session:IAsyncSession) =

    static member private parseAccessionToOntologyName (accession:string) =
        let i = accession.IndexOfAny [|':'; '_'|]
        if i>0 then accession.[..i-1] else failwith $"Cannot parse accession: '{accession}' to ontology name."

    /// "termParamName": the query function tries to map properties of the "termParamName" to this function so depending on how the node was called in the query this needs to adapt.
    static member asTerm(termParamName) = fun (record:IRecord) ->
        let accession = record.[$"{termParamName}.accession"].As<string>()
        let (term:Shared.TermTypes.Term) = {
            Accession = accession
            FK_Ontology = Term.parseAccessionToOntologyName accession
            Name = 
                let r = record.[$"{termParamName}.name"].As<string>()
                if isNull r then "" else r
            Description = 
                let r = record.[$"{termParamName}.description"].As<string>()
                if isNull r then "" else r
            IsObsolete = record.[$"{termParamName}.isObsolete"].As<bool>()
        }
        term

    /// Searchtype defaults to "get term suggestions with auto complete".
    member this.getByName(termName:string, ?searchType:FullTextSearch, ?sourceOntologyName:string) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString termName
            else
                FullTextSearch.Complete.ofQueryString termName
        let query =
            let sourceOntologyText = """ WHERE EXISTS((:Term {accession: node.accession})-[:CONTAINED_IN]->(:Ontology {name: $OntologyName})) """
            if searchType.IsSome && searchType.Value = FullTextSearch.Exact then
                sprintf
                    """MATCH (node:Term {name: $Name})%s
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
                    (if sourceOntologyName.IsSome then sourceOntologyText else "")
            else
                sprintf
                    """CALL db.index.fulltext.queryNodes("TermName",$Name)
                    YIELD node%s
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
                    (if sourceOntologyName.IsSome then sourceOntologyText else "")
        let param =
            Map [
                "Name",fulltextSearchStr
                if sourceOntologyName.IsSome then "OntologyName", sourceOntologyName.Value
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("node")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("node")),
                credentials.Value
            )
                

    /// This function will allow for raw apache lucene input. It is possible to search either term name or description or both.
    /// The function will error if both term name and term description are None.
    member this.getByAdvancedTermSearch(advancedSearchOptions:Shared.AdvancedSearchTypes.AdvancedSearchOptions) =
        let termName = if advancedSearchOptions.TermName = "" then None else Some advancedSearchOptions.TermName
        let termDescription = if advancedSearchOptions.TermDescription = "" then None else Some advancedSearchOptions.TermDescription
        let indexName, queryInsert =
            match termName,termDescription with
            | None, None -> failwith "Cannot execute term search without any term name or term description."
            | Some _, None -> "TermName", termName.Value
            | Some name, Some desc -> "TermNameAndDescription", sprintf """name: "%s", description: "%s" """ name desc
            | None, Some _ -> "TermDescription", termDescription.Value
        let query =
            if advancedSearchOptions.OntologyName.IsSome then
                sprintf
                    """CALL db.index.fulltext.queryNodes("%s", $Query) 
                    YIELD node
                    WHERE EXISTS((:Term {accession: node.accession})-[:CONTAINED_IN]->(:Ontology {name: $OntologyName}))
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
                    indexName
            else
                sprintf
                    """CALL db.index.fulltext.queryNodes("%s", $Query) YIELD node
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
                    indexName
        let param =
            Map [
                if advancedSearchOptions.OntologyName.IsSome then "OntologyName", advancedSearchOptions.OntologyName.Value
                "Query", queryInsert
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("node")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("node")),
                credentials.Value
            )


    /// Exact match for unique identifier term accession.
    member this.getByAccession(termAccession:string) =
        let query = 
            """MATCH (term:Term {accession: $Accession})
            RETURN term.accession, term.name, term.description, term.isObsolete"""
        let param =
            Map ["Accession",termAccession] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("term")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("term")),
                credentials.Value
            )

    member this.getAllByParent(parentAccession:string, ?limit:int) =
        let query =
            sprintf
                """MATCH (child)-[*1..]->(:Term {accession: $Accession})
                RETURN child.accession, child.name, child.description, child.isObsolete
                %s"""
                (if limit.IsSome then "LIMIT $Limit" else "")
        let param =
            Map [
                /// need to box values, because limit.Value will error if parsed as string
                "Accession", box parentAccession
                if limit.IsSome then "Limit", box limit.Value
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                credentials.Value
            )

    /// This function uses only the parent term accession
    member this.getAllByParent(parentAccession:TermMinimal, ?limit:int) =
        let query =
            sprintf
                """MATCH (child)-[*1..]->(:Term {accession: $Accession})
                RETURN child.accession, child.name, child.description, child.isObsolete
                %s"""
                (if limit.IsSome then "LIMIT $Limit" else "")
        let param =
            Map [
                /// need to box values, because limit.Value will error if parsed as string
                "Accession", box parentAccession.TermAccession
                if limit.IsSome then "Limit", box limit.Value
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                credentials.Value
            )

    /// Searchtype defaults to "get child term suggestions with auto complete"
    member this.getByNameAndParent(termName:string,parentAccession:string,?searchType:FullTextSearch) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString termName
            else
                FullTextSearch.Complete.ofQueryString termName
        let query =
            """CALL db.index.fulltext.queryNodes("TermName", $Search) 
            YIELD node
            WITH node
            MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {accession: $Accession}) 
            RETURN child.accession, child.name, child.description, child.isObsolete"""
        let param =
            Map [
                "Accession", parentAccession; 
                "Search", fulltextSearchStr
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                credentials.Value
            )

    /// Searchtype defaults to "get child term suggestions with auto complete"
    member this.getByNameAndParent(term:TermMinimal,parent:TermMinimal,?searchType:FullTextSearch) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString term.Name
            else
                FullTextSearch.Complete.ofQueryString term.Name
        let query =
            """CALL db.index.fulltext.queryNodes("TermName", $Search) 
            YIELD node
            WITH node
            MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {accession: $Accession}) 
            RETURN child.accession, child.name, child.description, child.isObsolete"""
        let param =
            Map [
                "Accession", parent.TermAccession; 
                "Search", fulltextSearchStr
            ] |> Some 
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                credentials.Value
            )

    // Searchtype defaults to "get child term suggestions with auto complete"
    member this.getByNameAndParent(termName:string,parentAccession:TermMinimal,?searchType:FullTextSearch) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString termName
            else
                FullTextSearch.Complete.ofQueryString termName
        let query =
            """CALL db.index.fulltext.queryNodes("TermName", $Search) 
            YIELD node
            WITH node
            MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {accession: $Accession}) 
            RETURN child.accession, child.name, child.description, child.isObsolete"""
        let param =
            Map [
                "Accession", parentAccession.TermAccession; 
                "Search", fulltextSearchStr
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                credentials.Value
            )

    member this.getByNameAndParent_Name(termName:string,parentName:string,?searchType:FullTextSearch) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString termName
            else
                FullTextSearch.Complete.ofQueryString termName
        let query =
            """CALL db.index.fulltext.queryNodes("TermName", $Search) 
            YIELD node
            WITH node
            MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {name: $Name}) 
            RETURN child.accession, child.name, child.description, child.isObsolete"""
        let param =
            Map [
                "Name", parentName; 
                "Search", fulltextSearchStr
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("child")),
                credentials.Value
            )

    /// This function uses only the parent term accession
    member this.getAllByChild(childAccession:TermMinimal) =
        let query = 
            """MATCH (:Term {accession: $Accession})-[*1..]->(parent:Term)
            RETURN parent.accession, parent.name, parent.description, parent.isObsolete
            """
        let param =
            Map ["Accession",childAccession.TermAccession] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("parent")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("parent")),
                credentials.Value
            )

    /// Searchtype defaults to "get child term suggestions with auto complete"
    member this.getByNameAndChild(termName:string,childAccession:string,?searchType:FullTextSearch) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString termName
            else
                FullTextSearch.Complete.ofQueryString termName
        let query =
            """CALL db.index.fulltext.queryNodes("TermName", $Search) 
            YIELD node
            WITH node
            MATCH (:Term {accession: $Accession})-[*1..]->(parent:Term {accession: node.accession})
            RETURN parent.accession, parent.name, parent.description, parent.isObsolete"""
        let param =
            Map [
                "Accession", childAccession; 
                "Search", fulltextSearchStr
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("parent")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("parent")),
                credentials.Value
            )

    /// Searchtype defaults to "get child term suggestions with auto complete"
    member this.getByNameAndChild_Name(termName:string,childName:string,?searchType:FullTextSearch) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString termName
            else
                FullTextSearch.Complete.ofQueryString termName
        let query =
            """CALL db.index.fulltext.queryNodes("TermName", $Search) 
            YIELD node
            WITH node
            MATCH (:Term {name: $Name})-[*1..]->(parent:Term {accession: node.accession})
            RETURN parent.accession, parent.name, parent.description, parent.isObsolete"""
        let param =
            Map [
                "Name", childName; 
                "Search", fulltextSearchStr
            ] |> Some
        if session.IsSome then
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("parent")),
                session = session.Value
            )
        else
            Neo4j.runQuery(
                query,
                param,
                (Term.asTerm("parent")),
                credentials.Value
            )

type TermQuery = 

    static member getByName(termName:string, ?searchType:FullTextSearch, ?sourceOntologyName:string) =
        let fulltextSearchStr =
            if searchType.IsSome then
                searchType.Value.ofQueryString termName
            else
                FullTextSearch.Complete.ofQueryString termName
        let query =
            let sourceOntologyText = """ WHERE EXISTS((:Term {accession: node.accession})-[:CONTAINED_IN]->(:Ontology {name: $OntologyName})) """
            if searchType.IsSome && searchType.Value = FullTextSearch.Exact then
                sprintf
                    """MATCH (node:Term {name: $Name})%s
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
                    (if sourceOntologyName.IsSome then sourceOntologyText else "")
            else
                sprintf
                    """CALL db.index.fulltext.queryNodes("TermName",$Name)
                    YIELD node%s
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
                    (if sourceOntologyName.IsSome then sourceOntologyText else "")
        let param =
            Map [
                "Name",fulltextSearchStr
                if sourceOntologyName.IsSome then "OntologyName", sourceOntologyName.Value
            ] |> Some
        query, param, Term.asTerm("node")

    /// Exact match for unique identifier term accession.
    static member getByAccession(termAccession:string) =
        let query = 
            """MATCH (term:Term {accession: $Accession})
            RETURN term.accession, term.name, term.description, term.isObsolete"""
        let param =
            Map ["Accession",termAccession] |> Some
        query, param, Term.asTerm("term")