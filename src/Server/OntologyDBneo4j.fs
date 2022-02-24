module OntologyDBneo4j

open System
open System.IO
open Neo4j

module Types =
    type Ontology = {
        Name        : string
        Version     : string
        LastUpdated : string
        Author      : string
    }

    type Term = {
        TermAccession: string
        FK_Ontology: string
        Name: string
        Description: string
        IsObsolete: bool
    }

    type TermRelationship = {
        ChildTerm: string
        Relationship: string
        ParentTerm: string
    }

open Types
open Neo4j.Driver
open System.Threading.Tasks

type Neo4JCredentials = {
    User        : string
    Pw          : string
    BoltUrl     : string
    DatabaseName: string
}


let establishConnection(c: Neo4JCredentials) =
    let driver = Neo4j.Driver.GraphDatabase.Driver(c.BoltUrl, Neo4j.Driver.AuthTokens.Basic(c.User,c.Pw))
    driver.AsyncSession(SessionConfigBuilder.ForDatabase c.DatabaseName)
    
/// <summary>Standardized function to easily execute neo4j cypher query.</summary>
/// <param name="query">The cypher query string</param>
/// <param name="parameters">Map of key value pairs. Only use this if you used parameters, for example '$Name' in your query. In which case you need to provide `Map ["Name", value]`.</param>
/// <param name="resultAs">How to return query results. In the format of `(fun (record:IRecord) -> parsingFunction record)`.</param>
let runNeo4JQuery (query:string) (parameters:Map<string,'a> option) (resultAs:IRecord -> 'T) prdocutionCredentials=
    use session = establishConnection(prdocutionCredentials)
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
                session.RunAsync(Query(query,param))
            else
                session.RunAsync(query)
            |> Async.AwaitTask
        let! dbValues = 
            executeReadQuery.ToListAsync()
            |> Async.AwaitTask
        let parsedDbValues = dbValues |> Seq.map resultAs
        session.Dispose()
        return parsedDbValues
    } |> Async.RunSynchronously

module Queries =

    module Ontology =

        let private asOntology =
            fun (record:IRecord) ->
                {
                    Name        = record.["o.name"].As<string>()
                    Version     = 
                        let r = record.["o.version"].As<string>()
                        if isNull r then "" else r
                    LastUpdated = 
                        let r = record.["o.lastUpdated"].As<string>()
                        if isNull r then "" else r
                    Author      = 
                        let r = record.["o.author"].As<string>()
                        if isNull r then "" else r
                }
        let matchAll(credentials) = 
            let query = 
                @"MATCH (o:Ontology)
                RETURN o.name, o.lastUpdated, o.version, o.author" 
            runNeo4JQuery
                query
                None
                asOntology
                credentials

        let matchByName credentials (name: string) = 
            let query = 
                @"MATCH (o:Ontology {name: $Name})
                RETURN o.name, o.lastUpdated, o.version, o.author" 
            let param =
                Map ["Name",name]
            runNeo4JQuery
                query
                (Some param)
                asOntology
                credentials

    module Term =

        let private parseAccessionToOntologyName (accession:string) =
            let i = accession.IndexOfAny [|':'; '_'|]
            if i>0 then accession.[..i-1] else failwith $"Cannot parse accession: '{accession}' to ontology name."

        /// "termParamName": the query function tries to map properties of the "termParamName" to this function so depending on how the node was called in the query this needs to adapt.
        let private asTerm(termParamName) = fun (record:IRecord) ->
            let accession = record.[$"{termParamName}.accession"].As<string>()
            {
                TermAccession = accession
                FK_Ontology = parseAccessionToOntologyName accession
                Name = 
                    let r = record.[$"{termParamName}.name"].As<string>()
                    if isNull r then "" else r
                Description = 
                    let r = record.[$"{termParamName}.description"].As<string>()
                    if isNull r then "" else r
                IsObsolete = record.[$"{termParamName}.isObsolete"].As<bool>()
            }

        let matchByName credentials (name:string) =
            let query = 
                """CALL db.index.fulltext.queryNodes("TermName",$Name) YIELD node
                RETURN node.accession, node.name, node.description, node.isObsolete"""
            let autoComplete = name + "*"
            let param =
                Map ["Name",autoComplete]
            runNeo4JQuery
                query
                (Some param)
                (asTerm("node"))
                credentials

        let matchByNameFuzzy credentials (name:string) =
            let query = 
                """CALL db.index.fulltext.queryNodes("TermName",$Name) YIELD node
                RETURN node.accession, node.name, node.description, node.isObsolete"""
            let fuzzyName = name.Replace(" ","~ ") + "~"
            let param =
                Map ["Name",fuzzyName]
            runNeo4JQuery
                query
                (Some param)
                (asTerm("node"))
                credentials

        let matchAllChildrenByParentAccession credentials (parentAccession:string) =
            let query = 
                """MATCH (child)-[*1..]->(:Term {accession: $Accession})
                RETURN child.accession, child.name, child.description, child.isObsolete
                """
            let param =
                Map ["Accession",parentAccession]
            runNeo4JQuery
                query
                (Some param)
                (asTerm("child"))
                credentials

        let matchChildrenByParentAccession credentials (search:string,parentAccession:string) =
            let query =
                """CALL db.index.fulltext.queryNodes("TermName", $Search) 
                YIELD node
                WITH node
                MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {accession: $Accession}) 
                RETURN child.accession, child.name, child.description, child.isObsolete"""
            let search' = search + "*"
            let param =
                Map [
                    "Accession", parentAccession; 
                    "Search", search'
                ]
            runNeo4JQuery
                query
                (Some param)
                (asTerm("child"))
                credentials