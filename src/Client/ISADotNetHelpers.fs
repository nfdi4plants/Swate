module ISADotNetHelpers

open ISADotNet
open Shared

let annotationValueToString (annoVal:ISADotNet.AnnotationValue) =
    match annoVal with
    | ISADotNet.AnnotationValue.Text v  -> v
    | ISADotNet.AnnotationValue.Float v -> string v  
    | ISADotNet.AnnotationValue.Int v   -> string v

let termAccessionReduce (uri:ISADotNet.URI) =
    let li = uri.LastIndexOf @"/"
    uri.Remove(0,li+1).Replace("_",":")

let valueIsOntology (v:ISADotNet.Value) =
    match v with
    | ISADotNet.Value.Ontology o    ->
        let name =  o.Name.Value |> annotationValueToString
        let tsr =   o.TermSourceREF.Value
        let tan =   o.TermAccessionNumber.Value |> termAccessionReduce
        Some <| OntologyInfo.create name tan
    | _ ->
        None

let valueToString (v:ISADotNet.Value) =
    match v with
    | ISADotNet.Value.Float f       -> string f
    | ISADotNet.Value.Int i         -> string i
    | ISADotNet.Value.Name s        -> s
    | ISADotNet.Value.Ontology o    -> failwith "This Function (valueToString) should not be used to parse ISADotNet.Value.Ontology"