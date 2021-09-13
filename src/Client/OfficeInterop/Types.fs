module OfficeInterop.Types

open System
open Fable.SimpleXml
open Fable.SimpleXml.Generator

module Xml =

    module GroupTypes =

        [<Literal>]
        let ProtocolGroupXmlRoot = "ProtocolGroup"

        type SpannedBuildingBlock = {
            ColumnName      : string
            TermAccession   : string
        } with
            static member create name termAccession = {
                ColumnName      = name
                TermAccession   = termAccession
            }
            static member init = SpannedBuildingBlock.create "" ""

        type Protocol = {
            Id                      : string
            ProtocolVersion         : string
            SwateVersion            : string
            AnnotationTable         : Shared.AnnotationTable
            SpannedBuildingBlocks   : SpannedBuildingBlock list
        } with
            static member create id version swateVersion spannedBuildingBlocks tableName worksheetName = {
                Id                      = id
                ProtocolVersion         = version
                SwateVersion            = swateVersion
                AnnotationTable         = Shared.AnnotationTable.create tableName worksheetName
                SpannedBuildingBlocks   = spannedBuildingBlocks
            }
            static member init = Protocol.create "" "" "" []

        type ProtocolGroup = {
            SwateVersion    : string
            AnnotationTable : Shared.AnnotationTable
            Protocols       : Protocol list
        } with
            static member create swateVersion tableName worksheetName protocols = {
                SwateVersion    = swateVersion
                AnnotationTable = Shared.AnnotationTable.create tableName worksheetName
                Protocols       = protocols
            }
            static member init = ProtocolGroup.create "" "" "" []

            member this.toXml =
                node "ProtocolGroup" [
                    attr.value( "SwateVersion"  , this.SwateVersion                 )
                    attr.value( "TableName"     , this.AnnotationTable.Name         )
                    attr.value( "WorksheetName" , this.AnnotationTable.Worksheet    )
                ] [
                    for protocol in this.Protocols do
                        yield
                            node "Protocol" [
                                attr.value( "Id",               protocol.Id                         )
                                attr.value( "SwateVersion",     protocol.SwateVersion               )
                                attr.value( "ProtocolVersion",  protocol.ProtocolVersion            )
                                attr.value( "TableName",        protocol.AnnotationTable.Name       )
                                attr.value( "WorksheetName",    protocol.AnnotationTable.Worksheet  )

                            ][
                                for spannedBuildingBlock in protocol.SpannedBuildingBlocks do
                                    yield
                                        leaf "SpannedBuildingBlock" [
                                            attr.value( "Name",             spannedBuildingBlock.ColumnName     )
                                            attr.value( "TermAccession",    spannedBuildingBlock.TermAccession  )
                                        ]
                            ]
                ]  |> serializeXml

            static member ofXml (xmlString:string) =
                let xml = xmlString |> SimpleXml.parseElement

                // failsafe
                let protocolGroup = xml |> SimpleXml.tryFindElementByName ProtocolGroupXmlRoot
                if protocolGroup.IsNone then failwith (sprintf "Could not find existing <%s> tag." ProtocolGroupXmlRoot)
                // failsafe end
                let protocols       = xml |> SimpleXml.findElementsByName "Protocol"
                let swateVersion    = protocolGroup.Value.Attributes.["SwateVersion"]
                let tableName       = protocolGroup.Value.Attributes.["TableName"]
                let worksheetName   = protocolGroup.Value.Attributes.["WorksheetName"]
                let nextProtocols   =
                    protocols
                    |> List.map (fun protocol ->
                        let id              = protocol.Attributes.["Id"]
                        let swateVersion    = protocol.Attributes.["SwateVersion"]
                        let protocolVersion = protocol.Attributes.["ProtocolVersion"]
                        let tableName       = protocol.Attributes.["TableName"]
                        let worksheetName   = protocol.Attributes.["WorksheetName"]
                        let nextSpannedBBs   =
                            protocol.Children
                            |> List.map (fun spannedBB ->
                                let name            = spannedBB.Attributes.["Name"]
                                let termAccession   = spannedBB.Attributes.["TermAccession"]
                                SpannedBuildingBlock.create name termAccession
                            )
                        Protocol.create id protocolVersion swateVersion nextSpannedBBs tableName worksheetName
                    )
                ProtocolGroup.create swateVersion tableName worksheetName nextProtocols

    module ValidationTypes =

        [<Literal>]
        let ValidationXmlRoot = "TableValidation"

        //type Checksum =
        //    | MD5
        //    | Sha256
        //    | NoChecksum

        //    static member tryOfString str =
        //        match str with
        //        | "MD5"     -> Some MD5
        //        | "Sha256"  -> Some Sha256
        //        | "None"    -> Some NoChecksum
        //        | anyElse   -> None 

        /// User can define what kind of input a column should have
        type ContentType =
            | OntologyTerm  of string
            | UnitTerm      of string
            | Checksum      of string * string
            | Text
            | Url
            | Boolean
            | Number
            | Int
    
            member this.toReadableString =
                match this with
                | OntologyTerm po ->
                    sprintf "Ontology [%s]" po
                | UnitTerm ut ->
                    sprintf "Unit [%s]" ut
                | Checksum (checksum,col) ->
                    sprintf "Checksum [%A%s]" checksum (if col <> "" then "," + col else "")
                | _ ->
                    string this
    
            static member ofString (str:string) =
                match str with
                | ontology when str.StartsWith "OntologyTerm " ->
                    let s = ontology.Replace("OntologyTerm ","").Replace("\"","")
                    OntologyTerm s
                | unit when str.StartsWith "UnitTerm " ->
                    let s = unit.Replace("UnitTerm ", "").Replace("\"","")
                    UnitTerm s
                | checksum when str.StartsWith "Checksum " ->
                    let split = checksum.Replace("Checksum ","").Replace("\"","")
                    let s = split.[1..split.Length-2]
                    let hasColumn =
                        let split = s.Split([|","|], 1, StringSplitOptions.RemoveEmptyEntries)
                        if split.Length = 2 then Some split.[1] else None
                    Checksum (s,if hasColumn.IsNone then "" else hasColumn.Value)
                | "Text"        -> Text
                | "Url"         -> Url
                | "Boolean"     -> Boolean
                | "Number"      -> Number
                | "Int"         -> Int
                | _ -> 
                    failwith ( sprintf "Tried parsing '%s' to ContenType. No match found." str ) 

        type ColumnValidation = {
            ColumnHeader        : string
            ColumnAdress        : int option
            Importance          : int option
            ValidationFormat    : ContentType option
            Unit                : string option
        } with
            static member create colHeader colAdress importance validationFormat unit = {
                ColumnHeader        = colHeader
                ColumnAdress        = colAdress
                Importance          = importance
                ValidationFormat    = validationFormat
                Unit                = unit
            }

            static member init (?colHeader, ?colAdress) = {
                ColumnHeader        = if colHeader.IsSome then colHeader.Value else ""
                ColumnAdress        = if colAdress.IsSome then colAdress.Value else None
                Importance          = None
                ValidationFormat    = None
                Unit                = None
            }
            
        type TableValidation = {
            SwateVersion    : string
            AnnotationTable : Shared.AnnotationTable
            DateTime        : DateTime
            // "FirstUser; SecondUser"
            Userlist        : string list
            ColumnValidations: ColumnValidation list
        } with

            static member create swateVersion worksheetName tableName dateTime userlist colValidations = {
                SwateVersion        = swateVersion
                AnnotationTable     = Shared.AnnotationTable.create tableName worksheetName
                DateTime            = dateTime
                Userlist            = userlist
                ColumnValidations   = colValidations
            }

            static member init (?swateVersion, ?worksheetName,?tableName, (?dateTime:DateTime), ?userList) = {
                SwateVersion        = if swateVersion.IsSome then swateVersion.Value else ""
                AnnotationTable     = Shared.AnnotationTable.create (if tableName.IsSome then tableName.Value else "") (if worksheetName.IsSome then worksheetName.Value else "")
                DateTime            = if dateTime.IsSome then dateTime.Value else DateTime.Now.ToUniversalTime()
                Userlist            = if userList.IsSome then userList.Value else []
                ColumnValidations   = []
            }

            member this.toXml =
                node "TableValidation" [
                        attr.value( "SwateVersion", this.SwateVersion )
                        attr.value( "WorksheetName", this.AnnotationTable.Worksheet)
                        attr.value( "TableName", this.AnnotationTable.Name)
                        attr.value( "DateTime", this.DateTime.ToString("yyyy-MM-dd HH:mm") )
                        attr.value( "Userlist", this.Userlist |> String.concat "; " )
                ][
                    for column in this.ColumnValidations do
                        yield
                            leaf "ColumnValidation" [
                                attr.value("ColumnHeader"       , column.ColumnHeader)
                                attr.value("ColumnAdress"       , if column.ColumnAdress.IsSome then string column.ColumnAdress.Value else "None")
                                attr.value("Importance"         , if column.Importance.IsSome then string column.Importance.Value else "None")
                                attr.value("ValidationFormat"   , if column.ValidationFormat.IsSome then string column.ValidationFormat.Value else "None")
                                attr.value("Unit"               , if column.Unit.IsSome then column.Unit.Value else "None")
                            ]
                ] |> serializeXml

            static member ofXml (xmlString:string) =
                let xml = xmlString |> SimpleXml.parseElement
                // failsafe
                let tableValidationOpt = xml |> SimpleXml.tryFindElementByName ValidationXmlRoot
                if tableValidationOpt.IsNone then failwith (sprintf "Could not find existing <%s> tag." ValidationXmlRoot)
                // failsafe end
                let tableValidation = xml |> SimpleXml.findElementByName ValidationXmlRoot
                let nextTableValidation =
                    let swateVersion    = tableValidation.Attributes.["SwateVersion"]
                    let worksheetName   = tableValidation.Attributes.["WorksheetName"]
                    let tableName       = tableValidation.Attributes.["TableName"]
                    let dateTime        =
                        System.DateTime.Parse(tableValidation.Attributes.["DateTime"])
                    let userlist        = tableValidation.Attributes.["Userlist"].Split([|"; "|], StringSplitOptions.RemoveEmptyEntries) |> List.ofSeq
                    let nextColumnValidations =
                        tableValidation.Children
                        |> List.map (fun column ->
                            let columnHeader        = column.Attributes.["ColumnHeader"]
                            let columnAdress        = column.Attributes.["ColumnAdress"] |> fun x -> if x = "None" then None else Some (int x)
                            let importance          = column.Attributes.["Importance"] |> fun x -> if x = "None" then None else Some (int x)
                            let validationFormat    = column.Attributes.["ValidationFormat"] |> fun x -> if x = "None" then None else ContentType.ofString x |> Some
                            let unit                = column.Attributes.["Unit"] |> fun x -> if x = "None" then None else Some x
                            ColumnValidation.create columnHeader columnAdress importance validationFormat unit
                        )
                    TableValidation.create swateVersion worksheetName tableName dateTime userlist nextColumnValidations
                nextTableValidation

    type XmlTypes =
    | ProtocolType      of GroupTypes.Protocol
    | GroupType         of GroupTypes.ProtocolGroup
    | ValidationType    of ValidationTypes.TableValidation
        with
            member this.toStringRdb =
                match this with
                | ProtocolType v    -> sprintf "Protocol %A (%s, %s)" v.Id v.AnnotationTable.Name v.AnnotationTable.Worksheet
                | GroupType v       -> sprintf "Protocol group (%s, %s)" v.AnnotationTable.Name v.AnnotationTable.Worksheet
                | ValidationType v  -> sprintf "Table checklist (%s, %s)" v.AnnotationTable.Name v.AnnotationTable.Worksheet



            

