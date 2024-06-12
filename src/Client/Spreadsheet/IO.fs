module Spreadsheet.IO

open ARCtrl
open ARCtrl.Spreadsheet
open FsSpreadsheet.Js
open Shared
open FsSpreadsheet

// TODO: Can this be done better? If we want to allow upload of any isa.xlsx file?
let readFromBytes (bytes: byte []) =
    // Try each conversion function and return the first successful result
    promise {
        let! fswb = FsSpreadsheet.Js.Xlsx.fromXlsxBytes bytes
        let ws = fswb.GetWorksheets()
        let arcfile =
            match ws with
            | _ when ws.Exists (fun ws -> ARCtrl.Spreadsheet.ArcAssay.metaDataSheetName = ws.Name ) -> 
                ArcAssay.fromFsWorkbook fswb |> Assay 
            | _ when ws.Exists (fun ws -> ARCtrl.Spreadsheet.ArcStudy.metaDataSheetName = ws.Name ) -> 
                ArcStudy.fromFsWorkbook fswb |> Study
            | _ when ws.Exists (fun ws -> ARCtrl.Spreadsheet.ArcInvestigation.metaDataSheetName = ws.Name ) -> 
                ArcInvestigation.fromFsWorkbook fswb |> Investigation
            | _ when ws.Exists (fun ws -> ARCtrl.Spreadsheet.Template.metaDataSheetName = ws.Name ) -> 
                ARCtrl.Spreadsheet.Template.fromFsWorkbook fswb |> Template
            | _ -> failwith "Unable to identify given file. Missing metadata sheet with correct name."
        return arcfile
    }