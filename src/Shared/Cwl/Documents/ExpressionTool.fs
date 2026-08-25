module Swate.Components.Shared.Cwl.Documents.ExpressionTool

open Swate.Components.Shared.Cwl.Documents.Types

let setExpression (value: string) (document: EditorDocument) =
    match document with
    | ExpressionToolDoc model -> ExpressionToolDoc { model with Expression = value }
    | _ -> document
