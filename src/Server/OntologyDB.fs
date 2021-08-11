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
        DbDomain.createOntology 
            name
            currentVersion
            definition
            dateCreated
            userID
    | false -> failwith "Inserting ontology failed."


let insertTerm cString (accession:string) (ontologyName:string) (name:string) (definition:string) (xrefvaluetype: string option) (isObsolete:bool) =
    
    use connection = establishConnection cString
    connection.Open()
    use insertTermCmd = connection.CreateCommand()

    insertTermCmd
        .CommandText <-"""
INSERT INTO Term (Accession,FK_OntologyName,Name,Definition,XRefValueType,IsObsolete)
VALUES (@acc,@ontId,@name,@def,@xrv,@iO);
SELECT max(ID) FROM Term"""

    let accessionParam      = insertTermCmd.Parameters.Add("acc",MySqlDbType.VarChar)
    let ontologyNameParam     = insertTermCmd.Parameters.Add("ontId",MySqlDbType.VarChar)
    let nameParam           = insertTermCmd.Parameters.Add("name",MySqlDbType.VarChar)
    let definitionParam     = insertTermCmd.Parameters.Add("def",MySqlDbType.VarChar)
    let xRefValueTypeParam  = insertTermCmd.Parameters.Add("xrv",MySqlDbType.VarChar)
    let isObsoleteParam     = insertTermCmd.Parameters.Add("iO",MySqlDbType.Bit)

    accessionParam      .Value <- accession
    ontologyNameParam   .Value <- ontologyName
    nameParam           .Value <- name
    definitionParam     .Value <- definition
    isObsoleteParam     .Value <- isObsolete
    match xrefvaluetype with
        |Some v -> xRefValueTypeParam  .Value <- v
        |None -> xRefValueTypeParam.Value <- DBNull.Value

    use reader = insertTermCmd.ExecuteReader()
    match reader.Read() with
    | true -> 
        DbDomain.createTerm 
            accession
            ontologyName
            name
            definition
            xrefvaluetype
            isObsolete
    | false -> failwith "Inserting term failed."

open System

let getTermSuggestions cString (query:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getTermSuggestions",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam      = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)

    let query' =
        if query.Contains " " then
            query.Split([|" "|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun x -> x.Length >= 3 )
            |> Array.map (fun x -> sprintf "+%s" x)
            |> String.concat " "
        else
            query
        |> fun x -> x + " *"

    queryParam      .Value <- query'

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getTermSuggestionsByParentTerm cString (query:string, parentTerm:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getTermSuggestionsByParentTerm",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam              = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)
    let parentOntologyParam     = getTermSuggestionsCmd.Parameters.Add("parentOntology",MySqlDbType.VarChar)

    queryParam              .Value <- query
    parentOntologyParam     .Value <- parentTerm

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getTermSuggestionsByChildTerm cString (query:string, childTerm:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getTermSuggestionsByChildTerm",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam              = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)
    let childOntologyParam     = getTermSuggestionsCmd.Parameters.Add("childOntology",MySqlDbType.VarChar)

    queryParam              .Value <- query
    childOntologyParam      .Value <- childTerm

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getTermByParentTermOntologyInfo cString (query:string, parentTerm:TermMinimal) =

    let hasAccession = parentTerm.TermAccession <> ""

    use connection = establishConnection cString
    connection.Open()
    use cmd =
        if hasAccession then
            new MySqlCommand("getTermByParentTermAndAccession",connection)
        else
            new MySqlCommand("getTermByParentTerm",connection)

    cmd.CommandType <- CommandType.StoredProcedure

    let queryParam              = cmd.Parameters.Add("query",MySqlDbType.VarChar)
    let parentOntologyParam     = cmd.Parameters.Add("parentOntology",MySqlDbType.VarChar)

    queryParam              .Value <- query
    parentOntologyParam     .Value <- parentTerm.Name

    if hasAccession then
        let accessionParam = cmd.Parameters.Add("parentTermAccession", MySqlDbType.VarChar)
        accessionParam      .Value <- parentTerm.TermAccession

    use reader = cmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getAllTermsByParentTermOntologyInfo cString (parentTerm:TermMinimal) =

    let hasAccession = parentTerm.TermAccession <> ""

    use connection = establishConnection cString
    connection.Open()
    use cmd =
        if hasAccession then
            new MySqlCommand("getAllTermsByParentTermAndAccession",connection)
        else
            new MySqlCommand("getAllTermsByParentTerm",connection)

    cmd.CommandType <- CommandType.StoredProcedure

    let parentOntologyParam     = cmd.Parameters.Add("parentOntology",MySqlDbType.VarChar)

    parentOntologyParam     .Value <- parentTerm.Name

    if hasAccession then
        let accessionParam = cmd.Parameters.Add("parentTermAccession", MySqlDbType.VarChar)
        accessionParam      .Value <- parentTerm.TermAccession

    use reader = cmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getAllTermsByChildTermOntologyInfo cString (childTerm:TermMinimal) =

    let hasAccession = childTerm.TermAccession <> ""

    use connection = establishConnection cString
    connection.Open()
    use cmd =
        if hasAccession then
            new MySqlCommand("getAllTermsByChildTermAndAccession",connection)
        else
            new MySqlCommand("getAllTermsByChildTerm",connection)

    cmd.CommandType <- CommandType.StoredProcedure

    let parentOntologyParam     = cmd.Parameters.Add("childOntology",MySqlDbType.VarChar)

    parentOntologyParam     .Value <- childTerm.Name

    if hasAccession then
        let accessionParam = cmd.Parameters.Add("childTermAccession", MySqlDbType.VarChar)
        accessionParam      .Value <- childTerm.TermAccession

    use reader = cmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getTermSuggestionsByParentTermAndAccession cString (query:string, parentTerm:string, parentTermAccession:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getTermSuggestionsByParentTermAndAccession",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam                  = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)
    let parentOntologyParam         = getTermSuggestionsCmd.Parameters.Add("parentOntology",MySqlDbType.VarChar)
    let parentTermAccessionParam    = getTermSuggestionsCmd.Parameters.Add("parentTermAccession",MySqlDbType.VarChar)

    queryParam              .Value <- query
    parentOntologyParam     .Value <- parentTerm
    parentTermAccessionParam.Value <- parentTermAccession

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getTermSuggestionsByChildTermAndAccession cString (query:string, childTerm:string, childTermAccession:string) =
    
    use connection = establishConnection cString
    connection.Open()
    use getTermSuggestionsCmd = new MySqlCommand("getTermSuggestionsByChildTermAndAccession",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam                  = getTermSuggestionsCmd.Parameters.Add("query",MySqlDbType.VarChar)
    let childOntologyParam         = getTermSuggestionsCmd.Parameters.Add("childOntology",MySqlDbType.VarChar)
    let childTermAccessionParam    = getTermSuggestionsCmd.Parameters.Add("childTermAccession",MySqlDbType.VarChar)

    queryParam              .Value <- query
    childOntologyParam     .Value <- childTerm
    childTermAccessionParam.Value <- childTermAccession

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getUnitTermSuggestions cString (query:string) =
    
    use connection = establishConnection cString
    connection.Open()

    use getTermSuggestionsCmd = new MySqlCommand("getUnitTermSuggestions",connection)
    getTermSuggestionsCmd.CommandType <- CommandType.StoredProcedure

    let queryParam      = getTermSuggestionsCmd.Parameters.Add("queryParam",MySqlDbType.VarChar)

    queryParam .Value <- query

    use reader = getTermSuggestionsCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
    |]

let getTermByName cString (queryStr:string) =
    
    use connection = establishConnection cString
    connection.Open()

    use getTermByNameCmd = connection.CreateCommand()
    getTermByNameCmd
        .CommandText <- """
            SELECT * FROM Term WHERE Term.Name = @name
        """

    let queryParam = getTermByNameCmd.Parameters.Add("name",MySqlDbType.VarChar)

    queryParam.Value    <- queryStr

    use reader = getTermByNameCmd.ExecuteReader()
    [|
        while reader.Read() do
            DbDomain.createTerm
               (reader.GetString(0))
               (reader.GetString(1))
               (reader.GetString(2))
               (reader.GetString(3))
               (if (reader.IsDBNull(4)) then
                   None
               else
                   Some (reader.GetString(4)))
               (reader.GetBoolean(5))
    |]

let getTermByNameAndAccession cString (queryStr:string,accessionString:string) =
    
    use connection = establishConnection cString
    connection.Open()

    use cmd = connection.CreateCommand()
    cmd
        .CommandText <- """
            SELECT * FROM Term
            WHERE Term.Name = @name
            AND Term.Accession = @accession
        """

    let queryParam      = cmd.Parameters.Add("name",MySqlDbType.VarChar)
    let accessionParam  = cmd.Parameters.Add("accession",MySqlDbType.VarChar)

    queryParam.Value        <- queryStr
    accessionParam.Value    <- accessionString

    use reader = cmd.ExecuteReader()
    [|
        while reader.Read() do
            DbDomain.createTerm
               (reader.GetString(0))
               (reader.GetString(1))
               (reader.GetString(2))
               (reader.GetString(3))
               (if (reader.IsDBNull(4)) then
                   None
               else
                   Some (reader.GetString(4)))
               (reader.GetBoolean(5))
    |]

let getTermByAccession cString (queryStr:string) =
    
    use connection = establishConnection cString
    connection.Open()

    use cmd = connection.CreateCommand()
    cmd
        .CommandText <- """
            SELECT * FROM Term WHERE Term.Accession = @accession
        """

    let queryParam = cmd.Parameters.Add("accession",MySqlDbType.VarChar)

    queryParam.Value    <- queryStr

    use reader = cmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
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
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetDateTime(3)) // TODO:
                    (reader.GetString(4))
    |]

let getAdvancedTermSearchResults cString (ont : DbDomain.Ontology option) (searchName : string) (mustContainName:string) (searchDef:string) (mustContainDef:string) (keepObsolete:bool) =
    
    use connection = establishConnection cString
    connection.Open()
    use advancedTermSearchCmd = new MySqlCommand("advancedTermSearch",connection)
    advancedTermSearchCmd.CommandType <- CommandType.StoredProcedure

    let ontNameParam                = advancedTermSearchCmd.Parameters.Add("ontologyName",MySqlDbType.VarChar)
    let searchNameParam             = advancedTermSearchCmd.Parameters.Add("searchTermName",MySqlDbType.VarChar)
    let mustContainNameParam        = advancedTermSearchCmd.Parameters.Add("mustContainName",MySqlDbType.VarChar)
    let searchDefParam              = advancedTermSearchCmd.Parameters.Add("searchTermDefinition",MySqlDbType.VarChar)
    let mustContainDefParam         = advancedTermSearchCmd.Parameters.Add("mustContainDefinition",MySqlDbType.VarChar)

    if ont.IsSome then
        ontNameParam            .Value <- ont.Value.Name
    else
        ontNameParam            .Value <- DBNull.Value
 
    searchNameParam             .Value <- searchName
    mustContainNameParam        .Value <- mustContainName
    searchDefParam              .Value <- searchDef
    mustContainDefParam         .Value <- mustContainDef

    use reader = advancedTermSearchCmd.ExecuteReader()
    [|
        while reader.Read() do
            yield
                DbDomain.createTerm
                    (reader.GetString(0))
                    (reader.GetString(1))
                    (reader.GetString(2))
                    (reader.GetString(3))
                    (if (reader.IsDBNull(4)) then
                        None
                    else
                        Some (reader.GetString(4)))
                    (reader.GetBoolean(5))
        |]
    |> fun res ->
        if keepObsolete then
            res
        else
            res
            |> Array.filter (fun r -> not r.IsObsolete)