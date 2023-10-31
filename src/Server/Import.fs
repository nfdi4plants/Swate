module Import

//open ISADotNet
//open ISADotNet.Json


/////<summary>Input is json string.</summary>
//module Json =

//    // The following functions are used to unify all jsonInput to the table type.

//    let fromAssay jsonString =
//        let assay = Assay.fromString jsonString
//        let tables = QueryModel.QAssay.fromAssay assay
//        tables

//    let fromProcessSeq jsonString =
//        let processSeq = ProcessSequence.fromString jsonString
//        let assay = Assay.create(ProcessSequence = List.ofSeq processSeq)
//        let tables = QueryModel.QAssay.fromAssay assay
//        tables

/////<summary>Input is byte [].</summary>
//module Xlsx =

//    let fromAssay (byteArray: byte []) =
//        let ms = new System.IO.MemoryStream(byteArray)
//        let _,assay = ISADotNet.XLSX.AssayFile.Assay.fromStream ms
//        let tables = QueryModel.QAssay.fromAssay assay
//        tables

///// This function tries to parse any possible json input to building blocks.
//let tryToTable (bytes: byte []) =
//    let jsonString = System.Text.Encoding.ASCII.GetString(bytes)
//    try
//        Json.fromAssay jsonString
//    with
//        | _ ->
//            try 
//                Json.fromProcessSeq jsonString
//            with
//                | _ ->
//                    try
//                        Xlsx.fromAssay bytes
//                    with
//                        | _ -> failwith "Could not match given file to supported file type."