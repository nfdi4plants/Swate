module OntologyDB

open MySql.Data
open MySql.Data.MySqlClient
open System.Data
open System

open Shared

let establishConnection cString = 
    new MySqlConnection(cString)

let insertOntology cString (name:string) (currentVersion:string) (definition:string) (dateCreated:System.DateTime) (userID:string)=

    use connection = establishConnection cString
    connection.Open()
    use insertOntologyCmd = connection.CreateCommand()

    insertOntologyCmd
        .CommandText <-"""
INSERT INTO Ontology (Name,CurrentVersion,Definition,DateCreated,UserID)
VALUES (@name,@cv,@def,@dc,@uid);
SELECT max(ID) FROM Ontology"""

    let nameParam           = insertOntologyCmd.Parameters.Add("name",MySqlDbType.VarChar)
    let currentVersionParam = insertOntologyCmd.Parameters.Add("cv",MySqlDbType.VarChar)
    let definitionParam     = insertOntologyCmd.Parameters.Add("def",MySqlDbType.VarChar)
    let dateCreatedParam    = insertOntologyCmd.Parameters.Add("dc",MySqlDbType.DateTime)
    let userIDParam         = insertOntologyCmd.Parameters.Add("uid",MySqlDbType.VarChar)

    nameParam           .Value <- name
    currentVersionParam .Value <- currentVersion
    definitionParam     .Value <- definition
    dateCreatedParam    .Value <- System.DateTimeOffset.UtcNow
    userIDParam         .Value <- userID

    use reader = insertOntologyCmd.ExecuteReader()
    match reader.Read() with
    | true -> 
        let identity = reader.GetInt64(0)
        DbDomain.createOntology 
            identity
            name
            currentVersion
            definition
            dateCreated
            userID
    | false -> failwith "Inserting ontology failed."


let insertTerm cString (accession:string) (ontologyID:int64) (name:string) (definition:string) (xrefvaluetype: string option) (isObsolete:bool) =
    
    use connection = establishConnection cString
    connection.Open()
    use insertTermCmd = connection.CreateCommand()

    insertTermCmd
        .CommandText <-"""
INSERT INTO Term (Accession,OntologyID,Name,Definition,XRefValueType,IsObsolete)
VALUES (@acc,@ontId,@name,@def,@xrv,@iO);
SELECT max(ID) FROM Term"""

    let accessionParam      = insertTermCmd.Parameters.Add("acc",MySqlDbType.VarChar)
    let ontologyIDParam     = insertTermCmd.Parameters.Add("ontId",MySqlDbType.Int64)
    let nameParam           = insertTermCmd.Parameters.Add("name",MySqlDbType.VarChar)
    let definitionParam     = insertTermCmd.Parameters.Add("def",MySqlDbType.VarChar)
    let xRefValueTypeParam  = insertTermCmd.Parameters.Add("xrv",MySqlDbType.VarChar)
    let isObsoleteParam     = insertTermCmd.Parameters.Add("iO",MySqlDbType.Bit)

    accessionParam      .Value <- accession
    ontologyIDParam     .Value <- ontologyID
    nameParam           .Value <- name
    definitionParam     .Value <- definition
    isObsoleteParam     .Value <- isObsolete
    match xrefvaluetype with
        |Some v -> xRefValueTypeParam  .Value <- v
        |None -> xRefValueTypeParam.Value <- DBNull.Value

    use reader = insertTermCmd.ExecuteReader()
    match reader.Read() with
    | true -> 
        let identity = reader.GetInt64(0)
        DbDomain.createTerm 
            identity
            accession
            ontologyID
            name
            definition
            xrefvaluetype
            isObsolete
    | false -> failwith "Inserting term failed."

let getTermSuggestions cString (query:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getTermSuggestions",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam      = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)

    queryParam      .Value <- query

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetInt64(0))
                    (reader.GetString(1))
                    (reader.GetInt64(2))
                    (reader.GetString(3))
                    (reader.GetString(4))
                    (if (reader.IsDBNull(5)) then
                        None
                    else
                        Some (reader.GetString(5)))
                    (reader.GetBoolean(6))
    |]

let getTermSuggestionsByParentOntology cString (query:string, parentOntology:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getTermSuggestionsByParentOntology",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam              = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)
    let parentOntologyParam     = getTermSuggestionsCmd.Parameters.Add("parentOntology",MySqlDbType.VarChar)

    queryParam              .Value <- query
    parentOntologyParam     .Value <- parentOntology

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetInt64(0))
                    (reader.GetString(1))
                    (reader.GetInt64(2))
                    (reader.GetString(3))
                    (reader.GetString(4))
                    (if (reader.IsDBNull(5)) then
                        None
                    else
                        Some (reader.GetString(5)))
                    (reader.GetBoolean(6))
    |]

let getUnitTermSuggestions cString (query:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getUnitTermSuggestions",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam      = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)

    queryParam .Value <- query

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetInt64(0))
                    (reader.GetString(1))
                    (reader.GetInt64(2))
                    (reader.GetString(3))
                    (reader.GetString(4))
                    (if (reader.IsDBNull(5)) then
                        None
                    else
                        Some (reader.GetString(5)))
                    (reader.GetBoolean(6))
    |]


let getAllOntologies cString () =
    
    use connection = establishConnection cString
    connection.Open()
    use getAllOntologiesCmd = new MySqlCommand("getAllOntologies",connection)
    getAllOntologiesCmd.CommandType <- CommandType.StoredProcedure

    use reader = getAllOntologiesCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createOntology
                    (reader.GetInt64(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (reader.GetDateTime(4)) // TODO:
                    (reader.GetString(5))
    |]

let getAdvancedTermSearchResults cString (ont : DbDomain.Ontology option) (startsWith : string) (mustContain:string) (endsWith:string) (keepObsolete:bool) (definitionMustContain:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use advancedTermSearchCmd = new MySqlCommand("advancedTermSearch",connection)
    advancedTermSearchCmd.CommandType <- CommandType.StoredProcedure

    let ontIdParam                  = advancedTermSearchCmd.Parameters.Add("ontologyId",MySqlDbType.Int64)
    let startsWithParam             = advancedTermSearchCmd.Parameters.Add("startsWith",MySqlDbType.VarChar)
    let endsWithParam               = advancedTermSearchCmd.Parameters.Add("endsWith",MySqlDbType.VarChar)
    let mustContainParam            = advancedTermSearchCmd.Parameters.Add("mustContain",MySqlDbType.VarChar)
    let definitionMustContainParam  = advancedTermSearchCmd.Parameters.Add("definitionMustContain",MySqlDbType.VarChar)

    if ont.IsSome then
        ontIdParam              .Value <- ont.Value.ID
    else
        ontIdParam              .Value <- DBNull.Value
 
    startsWithParam             .Value <- startsWith
    endsWithParam               .Value <- endsWith
    mustContainParam            .Value <- mustContain
    definitionMustContainParam  .Value <- definitionMustContain

    use reader = advancedTermSearchCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetInt64(0))
                    (reader.GetString(1))
                    (reader.GetInt64(2))
                    (reader.GetString(3))
                    (reader.GetString(4))
                    (if (reader.IsDBNull(5)) then
                        None
                    else
                        Some (reader.GetString(5)))
                    (reader.GetBoolean(6))
    |]
    |> fun res ->
        if keepObsolete then
            res
        else
            res
            |> Array.filter (fun r -> not r.IsObsolete)