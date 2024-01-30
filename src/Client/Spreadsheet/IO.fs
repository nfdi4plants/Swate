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
    promise {
        let! fswb = FsSpreadsheet.Exceljs.Xlsx.fromBytes bytes
        let ws = fswb.GetWorksheets()
        let arcfile =
            match ws with
            | _ when ws.Exists (fun ws -> ARCtrl.ISA.Spreadsheet.ArcAssay.metaDataSheetName = ws.Name ) -> 
                ArcAssay.fromFsWorkbook fswb |> Assay |> Some
            | _ when ws.Exists (fun ws -> ARCtrl.ISA.Spreadsheet.ArcStudy.metaDataSheetName = ws.Name ) -> 
                ArcStudy.fromFsWorkbook fswb |> Study |> Some
            | _ when ws.Exists (fun ws -> ARCtrl.ISA.Spreadsheet.ArcInvestigation.metaDataSheetName = ws.Name ) -> 
                ArcInvestigation.fromFsWorkbook fswb |> Investigation |> Some
            | _ when ws.Exists (fun ws -> ARCtrl.Template.Spreadsheet.Template.metaDataSheetName = ws.Name ) -> 
                ARCtrl.Template.Spreadsheet.Template.fromFsWorkbook fswb |> Template |> Some
            | _ -> None
        return arcfile
    }