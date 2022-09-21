module JsonImport

open ISADotNet
open ISADotNet.Json

// The following functions are used to unify all jsonInput to the table type.

let assayJsonToTable jsonString =
    let assay = Assay.fromString jsonString
    let tables = QueryModel.QAssay.fromAssay assay
    tables

//let tableJsonToTable jsonString =
//    let tables = Json.AssayCommonAPI.RowWiseAssay.fromString jsonString
//    tables

let processSeqJsonToTable jsonString =
    let processSeq = ProcessSequence.fromString jsonString
    let assay = Assay.create(ProcessSequence = List.ofSeq processSeq)
    let tables = QueryModel.QAssay.fromAssay assay
    tables


/// This function tries to parse any possible json input to building blocks.
let tryToTable jsonString =
    try
        let assay = Assay.fromString jsonString
        let tables = QueryModel.QAssay.fromAssay assay
        tables
    with
        | _ ->
            try 
                let processSeq = ProcessSequence.fromString jsonString
                let assay = Assay.create(ProcessSequence = List.ofSeq processSeq)
                let tables = QueryModel.QAssay.fromAssay assay
                tables
            with
                | _ ->
                    failwith "Could not match given json to allowed input."