module Swate.Components.Page.CwlEditor.UiHelpers

open ARCtrl.CWL

let cwlTypeSelectOptions: (string * string) list = [
    "", "Unset"
    "string", "string"
    "int", "int"
    "long", "long"
    "float", "float"
    "double", "double"
    "boolean", "boolean"
    "file", "File"
    "directory", "Directory"
    "stdout", "stdout"
    "null", "null"
]

let cwlTypeToKey (value: CWLType option) =
    match value with
    | None -> ""
    | Some cwlType ->
        match cwlType with
        | CWLType.String -> "string"
        | CWLType.Int -> "int"
        | CWLType.Long -> "long"
        | CWLType.Float -> "float"
        | CWLType.Double -> "double"
        | CWLType.Boolean -> "boolean"
        | CWLType.Stdout -> "stdout"
        | CWLType.Null -> "null"
        | CWLType.File _ -> "file"
        | CWLType.Directory _ -> "directory"
        | _ -> "custom"

let cwlTypeFromKey (value: string) =
    match value with
    | "" -> None
    | "string" -> Some CWLType.String
    | "int" -> Some CWLType.Int
    | "long" -> Some CWLType.Long
    | "float" -> Some CWLType.Float
    | "double" -> Some CWLType.Double
    | "boolean" -> Some CWLType.Boolean
    | "stdout" -> Some CWLType.Stdout
    | "null" -> Some CWLType.Null
    | "file" -> Some(CWLType.file ())
    | "directory" -> Some(CWLType.directory ())
    | _ -> None
