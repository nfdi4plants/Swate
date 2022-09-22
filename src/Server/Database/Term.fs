module Database.Term

open Neo4j.Driver
open Shared.TermTypes
open Helper

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
            Name = record.[$"{termParamName}.name"] |> defaultOutputWith<string> ""
            Description = record.[$"{termParamName}.definition"] |> defaultOutputWith<string> ""
            IsObsolete = record.[$"{termParamName}.is_obsolete"] |> defaultOutputWith<bool> false
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
                    RETURN node.accession, node.name, node.definition, node.is_obsolete"""
                    (if sourceOntologyName.IsSome then sourceOntologyText else "")
            else
                sprintf
                    """CALL db.index.fulltext.queryNodes("TermName",$Name)
                    YIELD node%s
                    RETURN node.accession, node.name, node.definition, node.is_obsolete"""
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
            | Some name, Some desc -> "TermNameAndDefinition", sprintf """name: "%s", definition: "%s" """ name desc
            | None, Some _ -> "TermDefinition", termDescription.Value
        let query =
            if advancedSearchOptions.OntologyName.IsSome then
                sprintf
                    """CALL db.index.fulltext.queryNodes("%s", $Query) 
                    YIELD node
                    WHERE EXISTS((:Term {accession: node.accession})-[:CONTAINED_IN]->(:Ontology {name: $OntologyName}))
                    RETURN node.accession, node.name, node.definition, node.is_obsolete"""
                    indexName
            else
                sprintf
                    """CALL db.index.fulltext.queryNodes("%s", $Query) YIELD node
                    RETURN node.accession, node.name, node.definition, node.is_obsolete"""
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
            RETURN term.accession, term.name, term.definition, term.is_obsolete"""
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
                RETURN child.accession, child.name, child.definition, child.is_obsolete
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
                RETURN child.accession, child.name, child.definition, child.is_obsolete
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
            RETURN child.accession, child.name, child.definition, child.is_obsolete"""
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
            RETURN child.accession, child.name, child.definition, child.is_obsolete"""
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
            RETURN child.accession, child.name, child.definition, child.is_obsolete"""
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
            RETURN child.accession, child.name, child.definition, child.is_obsolete"""
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
            RETURN parent.accession, parent.name, parent.definition, parent.is_obsolete
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
            RETURN parent.accession, parent.name, parent.definition, parent.is_obsolete"""
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
            RETURN parent.accession, parent.name, parent.definition, parent.is_obsolete"""
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
                    RETURN node.accession, node.name, node.definition, node.is_obsolete"""
                    (if sourceOntologyName.IsSome then sourceOntologyText else "")
            else
                sprintf
                    """CALL db.index.fulltext.queryNodes("TermName",$Name)
                    YIELD node%s
                    RETURN node.accession, node.name, node.definition, node.is_obsolete"""
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
            RETURN term.accession, term.name, term.definition, term.is_obsolete"""
        let param =
            Map ["Accession",termAccession] |> Some
        query, param, Term.asTerm("term")