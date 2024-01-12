module Spreadsheet.IO

open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open FsSpreadsheet.Exceljs
open Shared
open FsSpreadsheet

let private tryToConvertAssay (fswb: FsWorkbook) =
    try
        ArcAssay.fromFsWorkbook fswb |> Assay |> Some
    with
        | _ -> None

let private tryToConvertStudy (fswb: FsWorkbook) =
    try
        ArcStudy.fromFsWorkbook fswb |> Study |> Some
    with
        | _ -> None

let private tryToConvertInvestigation (fswb: FsWorkbook) =
    try
        ArcInvestigation.fromFsWorkbook fswb |> Investigation |> Some
    with
        | _ -> None

let private tryToConvertTemplate (fswb: FsWorkbook) =
    try
        ARCtrl.Template.Spreadsheet.Template.fromFsWorkbook fswb |> Template |> Some
    with
        | _ -> None

// List of conversion functions
let private converters = [tryToConvertAssay; tryToConvertStudy; tryToConvertInvestigation; tryToConvertTemplate]

// TODO: Can this be done better? If we want to allow upload of any isa.xlsx file?
let readFromBytes (bytes: byte []) =
    // Try each conversion function and return the first successful result
    let rec tryConvert (converters: ('a -> 'b option) list) (json: 'a) =
        match converters with
        | [] -> None
        | convert :: rest ->
            match convert json with
            | Some result -> Some result
            | None -> tryConvert rest json
    promise {
        let! fswb = FsSpreadsheet.Exceljs.Xlsx.fromBytes bytes
        let arcFile = tryConvert converters fswb
        return arcFile
    }