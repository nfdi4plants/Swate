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
    /// As for now, unit tags can contain a accession number if they are existing unit terms.
    let UnitTagStart = "#u"

module SwateInteropTypes =

    /// Maybe this can be replaced with AutoFillTypes/ColUnit
    type ColumnRepresentation = {
        Header          : string
        /// TODO: this is meant for future application and should be implemented together with separate unit columns
        Unit            : string option
        TagArray        : string []
        ParentOntology  : string option
    } with
        static member init (?header) = {
            Header          = if header.IsSome then header.Value else ""
            Unit            = None
            TagArray        = [||]
            ParentOntology  = None
        }

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
                    | _ -> Error "Could not process message. Swate was not able to identify the given annotation tables with a known case."

type ColHeader = {
    Header  : string
    CoreName: string option
    Ontology: string option
    TagArr: string [] option
    HasUnitAccession : string option
    IsUnitCol: bool
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