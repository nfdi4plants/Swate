module OntologyDB


open System.Data.SqlClient
open System.Data
open System
open Shared

let establishConnection () = 
    let connectionString = @"Data Source=localhost;Initial Catalog=AnnotatorTest;Integrated Security=True"
        
        //let cStringPath =
        //    ServerPath.resolve ["."; "connectionstring.txt"]
        //seq {
        //    use sr = new System.IO.StreamReader (cStringPath)
        //    while not sr.EndOfStream do
        //        yield sr.ReadLine ()
        //}
        //|> String.concat ""
    //printfn "establishing connection?"
    new SqlConnection(connectionString)

let insertOntology (name:string) (currentVersion:string) (definition:string) (dateCreated:System.DateTime) (userID:string)=

    use connection = establishConnection()
    connection.Open()
    use insertOntologyCmd = connection.CreateCommand()

    insertOntologyCmd
        .CommandText <-"""
INSERT INTO Ontology (Name,CurrentVersion,Definition,DateCreated,UserID)
VALUES (@name,@cv,@def,@dc,@uid);
SELECT max(ID) FROM Ontology"""

    let nameParam           = insertOntologyCmd.Parameters.Add("name",SqlDbType.NVarChar)
    let currentVersionParam = insertOntologyCmd.Parameters.Add("cv",SqlDbType.NVarChar)
    let definitionParam     = insertOntologyCmd.Parameters.Add("def",SqlDbType.NVarChar)
    let dateCreatedParam    = insertOntologyCmd.Parameters.Add("dc",SqlDbType.DateTimeOffset)
    let userIDParam         = insertOntologyCmd.Parameters.Add("uid",SqlDbType.NVarChar)

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


let insertTerm (accession:string) (ontologyID:int64) (name:string) (definition:string) (xrefvaluetype: string option) (isObsolete:bool) =
    
    use connection = establishConnection()
    connection.Open()
    use insertTermCmd = connection.CreateCommand()

    insertTermCmd
        .CommandText <-"""
INSERT INTO Term (Accession,OntologyID,Name,Definition,XRefValueType,IsObsolete)
VALUES (@acc,@ontId,@name,@def,@xrv,@iO);
SELECT max(ID) FROM Term"""

    let accessionParam      = insertTermCmd.Parameters.Add("acc",SqlDbType.NVarChar)
    let ontologyIDParam     = insertTermCmd.Parameters.Add("ontId",SqlDbType.BigInt)
    let nameParam           = insertTermCmd.Parameters.Add("name",SqlDbType.NVarChar)
    let definitionParam     = insertTermCmd.Parameters.Add("def",SqlDbType.NVarChar)
    let xRefValueTypeParam  = insertTermCmd.Parameters.Add("xrv",SqlDbType.NVarChar)
    let isObsoleteParam     = insertTermCmd.Parameters.Add("iO",SqlDbType.Bit)

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

let getTermSuggestions (query:string) =
    
    use connection = establishConnection()
    connection.Open()
    use getTermSuggestionsCmd = new SqlCommand("getTermSuggestions",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam      = getTermSuggestionsCmd.Parameters.Add("query",SqlDbType.NVarChar)

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

let getUnitTermSuggestions (query:string) =
    
    use connection = establishConnection()
    connection.Open()
    use getTermSuggestionsCmd = new SqlCommand("getUnitTermSuggestions",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam      = getTermSuggestionsCmd.Parameters.Add("query",SqlDbType.NVarChar)

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


let getAllOntologies () =
    
    use connection = establishConnection()
    connection.Open()
    use getAllOntologiesCmd = new SqlCommand("getAllOntologies",connection)
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
                    (reader.GetDateTimeOffset(4)).UtcDateTime
                    (reader.GetString(5))
    |]

let getAdvancedTermSearchResults (ont : DbDomain.Ontology option) (startsWith : string) (mustContain:string) (endsWith:string) (keepObsolete:bool) (definitionMustContain:string) =
    
    use connection = establishConnection()
    connection.Open()
    use advancedTermSearchCmd = new SqlCommand("advancedTermSearch",connection)
    advancedTermSearchCmd.CommandType <- CommandType.StoredProcedure

    let ontIdParam                  = advancedTermSearchCmd.Parameters.Add("ontologyId",SqlDbType.BigInt)
    let startsWithParam             = advancedTermSearchCmd.Parameters.Add("startsWith",SqlDbType.NVarChar)
    let endsWithParam               = advancedTermSearchCmd.Parameters.Add("endsWith",SqlDbType.NVarChar)
    let mustContainParam            = advancedTermSearchCmd.Parameters.Add("mustContain",SqlDbType.NVarChar)
    let definitionMustContainParam  = advancedTermSearchCmd.Parameters.Add("definitionMustContain",SqlDbType.NVarChar)

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