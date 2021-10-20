module JSONImport

open ISADotNet
open ISADotNet.Json

// The following functions are used to unify all jsonInput to the table type.

let assayJsonToTable jsonString =
    let assay = Assay.fromString jsonString
    let tables = AssayCommonAPI.RowWiseAssay.fromAssay assay
    tables

let tableJsonToTable jsonString =
    let tables = Json.AssayCommonAPI.RowWiseAssay.fromString jsonString
    tables

let processSeqJsonToTable jsonString =
    let processSeq = ProcessSequence.fromString jsonString
    let assay = Assay.create(ProcessSequence = List.ofSeq processSeq)
    let tables = AssayCommonAPI.RowWiseAssay.fromAssay assay
    tables
