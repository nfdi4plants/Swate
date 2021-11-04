module ProtocolDB

open MySql.Data
open MySql.Data.MySqlClient
open System.Data
open System

open Shared.TermTypes
open Shared.ProtocolTemplateTypes
open OntologyDB


/// Returns Protocols with empty protocol blocks
let getAllProtocols cString =
    
    use connection = establishConnection cString
    connection.Open()

    use cmd = connection.CreateCommand()
    cmd
        .CommandText <- """
            SELECT * FROM Protocol
        """

    use reader = cmd.ExecuteReader()
    [|
        while reader.Read() do
            let tags = reader.GetString(7).Split([|","|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())
            yield
                ProtocolTemplate.create
                    (reader.GetString(1))       // name
                    (reader.GetString(2))       // version
                    (reader.GetDateTime(3))     // created
                    (reader.GetString(4))       // author
                    (reader.GetString(5))       // description
                    (reader.GetString(6))       // docs link
                    tags                        // tag array
                    []                          // tableJson
                    (reader.GetInt32(8))        // used
                    (reader.GetInt32(9))        // rating
    |]

open ISADotNet

// Returns Protocols by name with empty protocol blocks
let getProtocolByName cString (queryStr:string) =
    
    use connection = establishConnection cString
    connection.Open()

    use cmd = connection.CreateCommand()
    cmd
        .CommandText <- """
            SELECT * FROM Protocol WHERE Name = @name
        """

    let queryParam = cmd.Parameters.Add("name",MySqlDbType.VarChar)

    queryParam.Value    <- queryStr

    use reader = cmd.ExecuteReader()
    reader.Read() |> ignore
    let tags = reader.GetString(7).Split([|","|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())
    /// Parse assay.json in database to insertbuildingblocks.
    let insertBuildingBlockList =
        let dbJson = reader.GetString(10)
        (dbJson |> rowMajorOfTemplateJson).headerToInsertBuildingBlockList
    ProtocolTemplate.create
        (reader.GetString(1))       // name
        (reader.GetString(2))       // version
        (reader.GetDateTime(3))     // created
        (reader.GetString(4))       // author
        (reader.GetString(5))       // description
        (reader.GetString(6))       // docs link
        tags
        insertBuildingBlockList     
        (reader.GetInt32(8))        // used
        (reader.GetInt32(9))        // rating

let increaseTimesUsed cString (templateName) =
    use connection = establishConnection cString
    connection.Open()

    use cmd = connection.CreateCommand()
    cmd
        .CommandText <- """
            UPDATE Protocol
            SET Used = Used + 1
            WHERE Name = @name
        """

    let nameParam = cmd.Parameters.Add("name",MySqlDbType.VarChar)

    nameParam.Value <- templateName

    let nOfRowsAffected = cmd.ExecuteNonQuery()

    if nOfRowsAffected <> 1 then
        failwith (sprintf "Executed command returned error: ER-ITU:%i" nOfRowsAffected)


