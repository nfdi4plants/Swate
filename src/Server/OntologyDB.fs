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