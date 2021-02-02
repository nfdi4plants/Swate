module OfficeInterop.Types

open System.Collections.Generic
open System.Text.RegularExpressions

open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Browser

open OfficeJS
open Excel

[<Global>]
let Office : Office.IExports = jsNative

[<Global>]
//[<CompiledName("Office.Excel")>]
let Excel : Excel.IExports = jsNative

[<Global>]
let RangeLoadOptions : Interfaces.RangeLoadOptions = jsNative


// Testing Subscription
// https://elmish.github.io/elmish/subscriptions.html                                           // elmish subscriptions
// https://docs.microsoft.com/de-de/office/dev/add-ins/develop/dialog-api-in-office-add-ins     // office excel dialog
//module Subscription =
//    type Msg =
//    | TestSubscription of string
    
//    type Model =  {
//        TestString : string
//    } 
    
//    let init () = {
//        TestString = ""
//    }
    
//    let update msg currentModel =
//        match msg with
//        | TestSubscription str ->
//            let nextModel = {
//                currentModel with TestString = str
//            }
//            nextModel, Cmd.none

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

open System
open Fable.SimpleXml
open Fable.SimpleXml.Generator

//module SwateInteropTypes =

//    type ColumnRepresentation = {
//        Header          : string
//        /// TODO: this is meant for future application and should be implemented together with separate unit columns
//        Unit            : string option
//        TagArray        : string []
//        ParentOntology  : string option
//    } with
//        static member init (?header) = {
//            Header          = if header.IsSome then header.Value else ""
//            Unit            = None
//            TagArray        = [||]
//            ParentOntology  = None
//        }

module XmlValidationTypes =

    /// User can define what kind of input a column should have
    type ContentType =
        | OntologyTerm of string
        | UnitTerm     of string
        | Text
        | Url
        | Boolean
        | Number
        | Int
        | Decimal
    
        member this.toReadableString =
            match this with
            | OntologyTerm po ->
                sprintf "Ontology [%s]" po
            | UnitTerm ut ->
                sprintf "Unit [%s]" ut
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
            | "Text"        -> Text
            | "Url"         -> Url
            | "Boolean"     -> Boolean
            | "Number"      -> Number
            | "Int"         -> Int
            | "Decimal"     -> Decimal
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
        WorksheetName   : string
        TableName       : string
        DateTime        : DateTime
        // "FirstUser; SecondUser"
        Userlist        : string list
        ColumnValidations: ColumnValidation list
    } with
        static member create swateVersion worksheetName tableName dateTime userlist colValidations = {
            SwateVersion        = swateVersion
            WorksheetName       = worksheetName
            TableName           = tableName
            DateTime            = dateTime
            Userlist            = userlist
            ColumnValidations   = colValidations
        }
        static member init (?swateVersion, ?worksheetName,?tableName, (?dateTime:DateTime), ?userList) = {
            SwateVersion        = if swateVersion.IsSome then swateVersion.Value else ""
            WorksheetName       = if worksheetName.IsSome then worksheetName.Value else ""
            TableName           = if tableName.IsSome then tableName.Value else ""
            DateTime            = if dateTime.IsSome then dateTime.Value else DateTime.Now
            Userlist            = if userList.IsSome then userList.Value else []
            ColumnValidations   = []
        }

    /// This type is used to work on the CustomXml 'Validation' tag, which is used to store information on how to validate a specifc Swate table as correct.
    type SwateValidation = {
        /// Used to show the last used Swate version to edit SwateValidation CustomXml
        SwateVersion        : string
        TableValidations    : TableValidation list
    } with
        static member init v = {
            SwateVersion        = v
            TableValidations    = []
        }
    
        member this.toXml =
            node "Validation" [
                attr.value("SwateVersion", this.SwateVersion)
            ][
                for table in this.TableValidations do
                    yield
                        node "TableValidation" [
                            attr.value( "SwateVersion", table.SwateVersion )
                            attr.value( "WorksheetName", table.WorksheetName )
                            attr.value( "TableName", table.TableName )
                            attr.value( "DateTime", table.DateTime.ToString("yyyy-MM-dd HH:mm") )
                            attr.value( "Userlist", table.Userlist |> String.concat "; " )
                        ][
                            for column in table.ColumnValidations do
                                yield
                                    leaf "ColumnValidation" [
                                        attr.value("ColumnHeader"       , column.ColumnHeader)
                                        attr.value("ColumnAdress"       , if column.ColumnAdress.IsSome then string column.ColumnAdress.Value else "None")
                                        attr.value("Importance"         , if column.Importance.IsSome then string column.Importance.Value else "None")
                                        attr.value("ValidationFormat"   , if column.ValidationFormat.IsSome then string column.ValidationFormat.Value else "None")
                                        attr.value("Unit"               , if column.Unit.IsSome then column.Unit.Value else "None")
                                    ]
                        ]
            ] |> serializeXml
    
        static member ofXml (xmlString:string) =
            let xml = xmlString |> SimpleXml.parseElement
            let swateValidation =
                xml |> SimpleXml.tryFindElementByName "Validation"
            if swateValidation.IsNone then failwith "Could not find existing <Validation> tag."
            let tableValidations =
                xml |> SimpleXml.findElementsByName "TableValidation"
            let validationType = SwateValidation.init swateValidation.Value.Attributes.["SwateVersion"]
            let tableValidationTypes =
                tableValidations
                |> List.map (fun table ->
                    let swateVersion    = table.Attributes.["SwateVersion"]
                    let worksheetName   = table.Attributes.["WorksheetName"]
                    let tableName       = table.Attributes.["TableName"]
                    let dateTime        =
                        //let day, month, year =
                        //    let s = table.Attributes.["DateTime"].Split([|"/"|], StringSplitOptions.None)
                        //    int s.[0], int s.[1], int s.[2]
                        System.DateTime.Parse(table.Attributes.["DateTime"])
                    let userlist        = table.Attributes.["Userlist"].Split([|"; "|], StringSplitOptions.RemoveEmptyEntries) |> List.ofSeq
                    let columnValidationTypes =
                        table.Children
                        |> List.map (fun column ->
                            let columnHeader        = column.Attributes.["ColumnHeader"]
                            let columnAdress        = column.Attributes.["ColumnAdress"] |> fun x -> if x = "None" then None else Some (int x)
                            let importance          = column.Attributes.["Importance"] |> fun x -> if x = "None" then None else Some (int x)
                            let validationFormat    = column.Attributes.["ValidationFormat"] |> fun x -> if x = "None" then None else ContentType.ofString x |> Some
                            let unit                = column.Attributes.["Unit"] |> fun x -> if x = "None" then None else Some x
                            ColumnValidation.create columnHeader columnAdress importance validationFormat unit
                        )
                    TableValidation.create swateVersion worksheetName tableName dateTime userlist columnValidationTypes
                )
            { validationType with TableValidations = tableValidationTypes }


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

type ColHeader = {
    Header:     string
    CoreName:   string option
    Ontology:   string option
    TagArr:     string [] option
    IsUnitCol:  bool
}

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
        /// Term Source REF
        TSR         : Column option
        /// Term Accession Number
        TAN         : Column option
        Unit        : BuildingBlock option
    } with
        static member create mainCol tsr tan unit = {
            MainColumn  = mainCol
            TSR         = tsr
            TAN         = tan
            Unit        = unit
        }

        member this.toColumnValidation : (XmlValidationTypes.ColumnValidation) =

            {
                ColumnHeader        = this.MainColumn.Header.Value.Header
                ColumnAdress        = this.MainColumn.Index |> Some
                Importance          = None
                ValidationFormat    = None
                Unit                = if this.Unit.IsSome then this.Unit.Value.MainColumn.Header.Value.Ontology else None
            }


