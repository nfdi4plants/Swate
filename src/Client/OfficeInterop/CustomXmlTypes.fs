[<RequireQualifiedAccess>]
module OfficeInterop.CustomXmlTypes

open System
open Fable.SimpleXml
open Fable.SimpleXml.Generator
                    
open Shared.OfficeInteropTypes

module Validation =

    [<Literal>]
    let ValidationXmlRoot = "TableValidation"

    /// User can define what kind of input a column should have
    type ContentType =
        | OntologyTerm  of string
        | UnitTerm      of string
        | Text
        | Url
        | Boolean
        | Number
        | Int
    
        member this.toReadableString =
            match this with
            | OntologyTerm ot ->
                sprintf "Ontology [%s]" ot
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
            | _ -> 
                failwith ( sprintf "Tried parsing '%s' to ContenType. No match found." str ) 

    type ColumnValidation = {
        ColumnHeader        : SwateColumnHeader
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

        static member init (colHeader:string, ?colAdress) = {
            ColumnHeader        = SwateColumnHeader.create colHeader
            ColumnAdress        = if colAdress.IsSome then colAdress.Value else None
            Importance          = None
            ValidationFormat    = None
            Unit                = None
        }

        static member ofBuildingBlock (buildingBlock: BuildingBlock) =
            let colHeader = buildingBlock.MainColumn.Header.SwateColumnHeader
            let adress = buildingBlock.MainColumn.Index
            ColumnValidation.init(colHeader,colAdress = Some adress)
            
    type TableValidation = {
        DateTime            : DateTime
        SwateVersion        : string
        AnnotationTable     : string
        // "FirstUser; SecondUser"
        Userlist            : string list
        ColumnValidations   : ColumnValidation list
    } with

        static member create swateVersion tableName dateTime userlist colValidations = {
            SwateVersion        = swateVersion
            AnnotationTable     = tableName
            DateTime            = dateTime
            Userlist            = userlist
            ColumnValidations   = colValidations
        }

        static member init (?swateVersion, ?worksheetName,?tableName, ?dateTime:DateTime, ?userList) = {
            SwateVersion        = if swateVersion.IsSome then swateVersion.Value else ""
            AnnotationTable     = if tableName.IsSome then tableName.Value else ""
            DateTime            = if dateTime.IsSome then dateTime.Value else DateTime.Now.ToUniversalTime()
            Userlist            = if userList.IsSome then userList.Value else []
            ColumnValidations   = []
        }

        member this.toXml =
            node "TableValidation" [
                    attr.value( "SwateVersion", this.SwateVersion )
                    attr.value( "TableName", this.AnnotationTable)
                    attr.value( "DateTime", this.DateTime.ToString("yyyy-MM-dd HH:mm") )
                    attr.value( "Userlist", this.Userlist |> String.concat "; " )
            ][
                for column in this.ColumnValidations do
                    yield
                        leaf "ColumnValidation" [
                            attr.value("ColumnHeader"       , column.ColumnHeader.SwateColumnHeader)
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
            match tableValidationOpt with
            | None -> failwith (sprintf "Could not find existing <%s> tag." ValidationXmlRoot)
            | Some tableValidation ->
                // failsafe end
                let nextTableValidation =
                    let swateVersion    = tableValidation.Attributes.["SwateVersion"]
                    let tableName       = tableValidation.Attributes.["TableName"]
                    let dateTime        = System.DateTime.Parse(tableValidation.Attributes.["DateTime"])
                    let userlist        = tableValidation.Attributes.["Userlist"].Split([|"; "|], StringSplitOptions.RemoveEmptyEntries) |> List.ofSeq
                    let nextColumnValidations =
                        tableValidation.Children
                        |> List.map (fun column ->
                            let columnHeader        = column.Attributes.["ColumnHeader"] |> SwateColumnHeader.create
                            let columnAdress        = column.Attributes.["ColumnAdress"] |> fun x -> if x = "None" then None else Some (int x)
                            let importance          = column.Attributes.["Importance"] |> fun x -> if x = "None" then None else Some (int x)
                            let validationFormat    = column.Attributes.["ValidationFormat"] |> fun x -> if x = "None" then None else ContentType.ofString x |> Some
                            let unit                = column.Attributes.["Unit"] |> fun x -> if x = "None" then None else Some x
                            ColumnValidation.create columnHeader columnAdress importance validationFormat unit
                        )
                    TableValidation.create swateVersion tableName dateTime userlist nextColumnValidations
                nextTableValidation

