module Spreadsheet.IO

open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open FsSpreadsheet.Exceljs
open Shared
open FsSpreadsheet

// TODO: Can this be done better? If we want to allow upload of any isa.xlsx file?
let readFromBytes (bytes: byte []) =
    // Try each conversion function and return the first successful result
    promise {
        let! fswb = FsSpreadsheet.Exceljs.Xlsx.fromBytes bytes
        let ws = fswb.GetWorksheets()
        let arcfile =
            match ws with
            | _ when ws.Exists (fun ws -> ARCtrl.ISA.Spreadsheet.ArcAssay.metaDataSheetName = ws.Name ) -> 
                ArcAssay.fromFsWorkbook fswb |> Assay 
            | _ when ws.Exists (fun ws -> ARCtrl.ISA.Spreadsheet.ArcStudy.metaDataSheetName = ws.Name ) -> 
                ArcStudy.fromFsWorkbook fswb |> Study
            | _ when ws.Exists (fun ws -> ARCtrl.ISA.Spreadsheet.ArcInvestigation.metaDataSheetName = ws.Name ) -> 
                ArcInvestigation.fromFsWorkbook fswb |> Investigation
            | _ when ws.Exists (fun ws -> ARCtrl.Template.Spreadsheet.Template.metaDataSheetName = ws.Name ) -> 
                ARCtrl.Template.Spreadsheet.Template.fromFsWorkbook fswb |> Template
            | _ -> failwith "Unable to identify given file. Missing metadata sheet with correct name."
        return arcfile
    }