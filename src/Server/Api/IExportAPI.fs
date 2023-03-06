module API.IExportAPI

open Shared
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open ISADotNet
open FsSpreadsheet.DSL
open ISADotNet.XLSX.AssayFile
open FsSpreadsheet.ExcelIO.FsExtensions

module V1 =

    let exportApi = {
        toAssayXlsx = fun tables -> async {
            let assay =  Export.parseBuildingBlockSeqsToAssay tables
            /// https://github.com/nfdi4plants/ISADotNet/blob/a06af930e4d3f9d3c49a7b07bb0496f927c4e6cc/src/ISADotNet.XLSX/AssayFile/Assay.fs#L188
            let a = QueryModel.QAssay.fromAssay assay
            let wb = 
                workbook {
                    for (i,s) in List.indexed a.Sheets do QSheet.toSheet i s
                    sheet "Assay" {
                        for r in MetaData.toDSLSheet assay [] do r
                    }
                }
            /// Parsing unit is not done correctly.
            /// https://github.com/nfdi4plants/ISADotNet/issues/81
            let fsSpreadsheet = wb.Value.Parse().ToBytes()
            return fsSpreadsheet
        }
    }

    let createExportApi() =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue exportApi
        |> Remoting.withDiagnosticsLogger(printfn "%A")
        |> Remoting.withErrorHandler Helper.errorHandler
        |> Remoting.buildHttpHandler