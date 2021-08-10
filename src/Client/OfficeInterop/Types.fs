module OfficeInterop.Types

open System.Collections.Generic
open System.Text.RegularExpressions

open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Browser

open ExcelJS.Fable
open Excel

//[<Global>]
//let Office : Office.IExports = jsNative

//[<Global>]
////[<CompiledName("Office.Excel")>]
//let Excel : Excel.IExports = jsNative

//[<Global>]
//let RangeLoadOptions : Interfaces.RangeLoadOptions = jsNative

module ColumnCoreNames =

    module Shown =

        [<Literal>]
        let Parameter       = "Parameter"

        [<Literal>]
        let Factor          = "Factor"

        [<Literal>]
        let Characteristics = "Characteristics"

        [<Literal>]
        let Sample          = "Sample Name"

        [<Literal>]
        let Data            = "Data File Name"

        [<Literal>]
        let Source          = "Source Name"

    module Hidden =

        [<Literal>]
        let TermSourceREF = "Term Source REF"

        [<Literal>]
        let TermAccessionNumber = "Term Accession Number"

        [<Literal>]
        let Unit = "Unit"

module ColumnTags =

    [<Literal>]
    let HiddenTag = "#h"

    [<Literal>]
    let UnitTag = "#u"

    /// This has additional information afterwards so it needs to be parsed as 'StartsWith'
    /// Not used
    [<Literal>]
    let GroupTag = "#g"

    /// This has additional information afterwards so it needs to be parsed as 'StartsWith'
    [<Literal>]
    let TermAccessionTag = "#t"

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



type TryFindAnnoTableResult =
| Success of string
| Error of string 

    with
        static member
            /// This function is used on an array of table names (string []). If the length of the array is <> 1 it will trough the correct error.
            /// Only returns success if annoTables.Length = 1. Does not check if the existing table names are correct/okay.
            exactlyOneAnnotationTable (annoTables:string [])=
                match annoTables.Length with
                | x when x < 1 ->
                    Error "Could not find annotationTable in active worksheet. Please create one before trying to execute this function."
                | x when x > 1 ->
                    Error "The active worksheet contains more than one annotationTable. Please move one of them to another worksheet."
                | 1 ->
                    annoTables |> Array.exactlyOne |> Success
                | _ ->
                    Error "Could not process message. Swate was not able to identify the given annotation tables with a known case."

open Shared

type ColHeader = {
    Header:     string
    CoreName:   string option
    Ontology:   OntologyInfo option
    TagArr:     string [] option
} with
    member this.IsUnitCol =
        this.TagArr.IsSome && this.TagArr.Value |> Array.exists (fun x -> x.StartsWith ColumnTags.UnitTag)

/// This module contains types to handle value search for TSR and TAN columns.
/// The types help to summarize and collect needed information about the column partitions (~ building block, e.g. 1 col for `Source Name`,
/// 3 cols for standard `Parameter`, 6 cols for `Parameter` with unit). As excel allows to drag 'n drop values down for a column we need these types
/// to find such occurrences and fill in the missing TSR, TAN and unit cols.
module BuildingBlockTypes =

    type Cell = {
        Index: int
        Value: string option
    } with
        static member create ind value = {
            Index = ind
            Value = value
        }

    type Column = {
        Index: int
        Header: ColHeader option
        Cells: Cell []
    } with
        static member create ind headerOpt cellsArr = {
            Index   =  ind
            Header  = headerOpt
            Cells   = cellsArr
        } 

    type BuildingBlock = {
        MainColumn  : Column
        Unit        : Column option
        /// Term Source REF
        TSR         : Column option
        /// Term Accession Number
        TAN         : Column option
    } with
        static member create mainCol tsr tan unit = {
            MainColumn  = mainCol
            TSR         = tsr
            TAN         = tan
            Unit        = unit
        }

        member this.toColumnValidation : (Xml.ValidationTypes.ColumnValidation) =

            {
                ColumnHeader        = this.MainColumn.Header.Value.Header
                ColumnAdress        = this.MainColumn.Index |> Some
                Importance          = None
                ValidationFormat    = None
                Unit                = if this.Unit.IsSome then this.Unit.Value.Header.Value.Ontology.Value.Name |> Some else None
            }

        member this.hasCompleteTSRTAN =
            match this.TAN, this.TSR with
            | Some tan, Some tsr ->
                true
            | None, None ->
                false
            | _, _ ->
                failwith (sprintf "Swate found unknown building block pattern in building block %s. Found only TSR or TAN." this.MainColumn.Header.Value.Header)

        member this.hasUnit = this.Unit.IsSome

    open ISADotNetHelpers

    type MinimalBuildingBlock = {
        /// If 'IsAlreadyExisting' = false then this is just a core name + ont (e.g. Parameter [instrument model], so no id).
        /// If 'IsAlreadyExisting' = true this is the real value from the table.
        ColumnName          : string
        UnitName            : string option
        ColumnTermAccession : string option
        Values              : OntologyInfo option
        /// When this type is given to 'AddBuildingBlocks' this parameter differentiates between term that were already found in the table and term that
        /// need to be added. This is important to correctly update existing protocols by their newest version from the DB
        IsAlreadyExisting       : bool
    } with
        static member create mainColName colTermAccession unitName values isExisting = {
            ColumnName          = mainColName
            ColumnTermAccession = colTermAccession
            UnitName                = unitName
            Values                  = values
            IsAlreadyExisting       = isExisting
        }

        // This function assumes that Process.ExecutesProtocol.Parameters.IsSome and Process.ParameterValues.IsSome.
        // For ExecutesProtocol.Parameters 'parameterName' ('annotationValue','termSource','termAccession') are required.
        // For Process.ParameterValues 'category' ('annotationValue','termSource','termAccession') and 'value' (IF ONTOLOGY 'annotationValue','termSource','termAccession') are required.
        // IF Process.ParameterValue has Unit then ('annotationValue','termSource','termAccession') are required.
        //static member ofISADotNetProcess (isaProcess:ISADotNet.Process) = 
        //    let paramValuesPairs = isaProcess.ParameterValues.Value
        //    paramValuesPairs
        //    |> List.map (fun paramValuePair ->
        //        let hasUnit             = paramValuePair.Unit.IsSome
        //        let hasOntologyValue    = paramValuePair.Value.Value |> ISADotNetHelpers.valueIsOntology
        //        let mainColName         =
        //            let n = paramValuePair.Category.Value.ParameterName.Value.Name.Value |> ISADotNetHelpers.annotationValueToString
        //            sprintf "Parameter [%s]" n
        //        let colTermAccession    = paramValuePair.Category.Value.ParameterName.Value.TermAccessionNumber.Value |> ISADotNetHelpers.termAccessionReduce
        //        let unitName            = if hasUnit then paramValuePair.Unit.Value.Name.Value |> ISADotNetHelpers.annotationValueToString |> Some else None
        //        let unitTermAccession   = if hasUnit then paramValuePair.Unit.Value.TermAccessionNumber.Value |> ISADotNetHelpers.termAccessionReduce |> Some else None
        //        let values              = if hasOntologyValue.IsSome then hasOntologyValue else OntologyInfo.create (ISADotNetHelpers.valueToString paramValuePair.Value.Value) "" |> Some
        //        MinimalBuildingBlock.create mainColName (Some colTermAccession) unitName unitTermAccession values false
        //    )

        static member ofBuildingBlockWithoutValues isExisting (buildingBlock:BuildingBlock) =
            let bbHeader = buildingBlock.MainColumn.Header.Value
            let colName =
                let ont = if bbHeader.Ontology.IsSome then sprintf " [%s]" bbHeader.Ontology.Value.Name else ""
                sprintf "%s%s" bbHeader.CoreName.Value ont
            let colAccession =
                if bbHeader.Ontology.IsSome then bbHeader.Ontology.Value.TermAccession |> Some else None
            let unitName =
                if buildingBlock.hasUnit then
                    let unitHeader = buildingBlock.Unit.Value.Header.Value
                    let unitColName =
                        if unitHeader.Ontology.IsSome then unitHeader.Ontology.Value.Name |> Some else None
                    unitColName
                else
                    None
            MinimalBuildingBlock.create colName colAccession unitName None isExisting



            

