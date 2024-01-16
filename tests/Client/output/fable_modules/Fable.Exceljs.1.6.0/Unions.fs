[<AutoOpen>]
module Fable.ExcelJs.Unions

open Fable.Core
open Fable.Core.JsInterop

type ErrorValue =
| NotApplicable
| Ref
| Name
| DivZero
| Null
| Value
| Num
    with
        member this.toString =
            match this with
            | NotApplicable    -> "#N/A"
            | Ref              -> "#REF!"
            | Name             -> "#NAME?"
            | DivZero          -> "#DIV/0!"
            | Null             -> "#NULL!"
            | Value            -> "#VALUE!"
            | Num              -> "#NUM!"
        static member ofString (str:string) =
            match str with
            | "#N/A"    -> NotApplicable
            | "#REF!"   -> Ref          
            | "#NAME?"  -> Name         
            | "#DIV/0!" -> DivZero      
            | "#NULL!"  -> Null         
            | "#VALUE!" -> Value        
            | "#NUM!"   -> Num
            | anyElse -> failwith "Cannot match input string to ErrorValue"
            

type ValueType =
| Null = 0
| Merge = 1
| Number = 2
| String = 3
| Date = 4
| Hyperlink = 5
| Formula = 6
| RichText = 8
| Boolean = 9
| Error = 10

// TODO: implement this with create function to easily use
//[<Erase>]
//type ValueType =
//| Null
//| Merge
//| Number of float
//| String of string
//| Date of System.DateOnly
//| Hyperlink of string
//| Formula of obj
//| RichText of obj []
//| Boolean of bool
//| Error of obj

[<RequireQualifiedAccess>]
type StyleOption =
/// inherit from row above
| [<CompiledName("'i'")>] Inherit
/// inherit from row above and include empty cells
| [<CompiledName("'i+'")>] InheritWithNulls
/// original style
| [<CompiledName("'o'")>] Original
/// original style and include empty cells
| [<CompiledName("'o+'")>] OriginalWithNulls
/// Default; no style options
| [<CompiledName("'n'")>] None

type CellValue = obj option
/// e.g. "B5", "A1", "AZ3",..
type CellAdress = string
/// e.g. ""A1:C5""
type CellRange = string

/// Example1: {|key1 = "Test"; key2 = 2; key3 = 4; key4 = None; key5 = None; key7 = "Test me too"|}.
/// Example2: createObj ["key1" ==> "Test"; "key2" ==> 2; "key3" ==> 4; "key4" ==> None; "key5" ==> None; "key7" ==> "Test me too" ]
/// Example3: Row.createValues (["key1","Test"; "key2",2])
/// Example4: [Some "Test"; Some 2; Some 4; None; None; None; Some "Test me too"]
type RowValues = obj

