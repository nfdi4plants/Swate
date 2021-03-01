module ProtocolDB

open MySql.Data
open MySql.Data.MySqlClient
open System.Data
open System

open Shared
open OntologyDB

type XmlType =
| CustomXml
| TableXml
    static member ofString str =
        match str with
        | "CustomXml" -> CustomXml
        | "TableXml"  -> TableXml
        | anyOther -> failwith (sprintf "Cannot parse string (%s) to ProtocolDB.XmlType" anyOther)

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
            let tags = reader.GetString(6).Split([|";"|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())
            yield
                ProtocolTemplate.create
                    (reader.GetString(0))       // name
                    (reader.GetString(1))       // version
                    (reader.GetDateTime(2))     // created
                    (reader.GetString(3))       // author
                    (reader.GetString(4))       // description
                    (reader.GetString(5))       // docs link
                    tags
                    ""                          // customXml
                    ""                          // tableXml
                    (reader.GetInt32(7))        // used
                    (reader.GetInt32(8))        // rating
    |]

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
    [|
        while reader.Read() do
            let tags = reader.GetString(6).Split([|";"|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())
            yield
                ProtocolTemplate.create
                    (reader.GetString(0))       // name
                    (reader.GetString(1))       // version
                    (reader.GetDateTime(2))     // created
                    (reader.GetString(3))       // author
                    (reader.GetString(4))       // description
                    (reader.GetString(5))       // docs link
                    tags
                    ""                          // customXml
                    ""                          // tableXml
                    (reader.GetInt32(7))        // used
                    (reader.GetInt32(8))        // rating
    |]

let getXmlByProtocol cString (protocol:ProtocolTemplate) =
    use connection = establishConnection cString
    connection.Open()

    use cmd = connection.CreateCommand()
    cmd
        .CommandText <- """
            SELECT * FROM ProtocolXml
            WHERE FK_Name = @name
            AND FK_Version = @version
        """

    let nameParam = cmd.Parameters.Add("name",MySqlDbType.VarChar)
    let versionParam = cmd.Parameters.Add("version",MySqlDbType.VarChar)

    nameParam.Value     <- protocol.Name
    versionParam.Value  <- protocol.Version

    use reader = cmd.ExecuteReader()
    let res = [|
        while reader.Read() do
            yield
                reader.GetString(2), reader.GetString(3)
    |]

    let customXml = res |> Array.tryFind (fun (xmlType,xml) ->
        xmlType = string CustomXml
    )

    let tableXml = res |> Array.tryFind (fun (xmlType,xml) ->
        xmlType = string TableXml
    )

    { protocol with
        CustomXml = if customXml.IsSome then customXml.Value |> snd else ""
        TableXml = if tableXml.IsSome then tableXml.Value |> snd else ""
    }

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


