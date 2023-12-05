module Spreadsheet.IO

open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open FsSpreadsheet.Exceljs
open Shared

// TODO: Can this be done better? If we want to allow upload of any isa.xlsx file?
let readFromBytes (bytes: byte []) =
    promise {
        let! fswb = FsSpreadsheet.Exceljs.Xlsx.fromBytes bytes
        let r =
            try
                ArcAssay.fromFsWorkbook fswb |> Assay
            with
                | _ ->
                    try 
                        ArcStudy.fromFsWorkbook fswb |> Study
                    with
                        | _ ->
                            ArcInvestigation.fromFsWorkbook fswb |> Investigation
        return r
    }