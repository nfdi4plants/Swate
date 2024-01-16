namespace FsSpreadsheet.Exceljs

open FsSpreadsheet
open Fable.ExcelJs
open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS

/// This does currently not correctly work if you want to use this from js
/// https://github.com/fable-compiler/Fable/issues/3498
[<AttachMembers>]
type Xlsx =
    static member fromXlsxFile (path:string) : Promise<FsWorkbook> =
        promise {
            let wb = ExcelJs.Excel.Workbook()
            do! wb.xlsx.readFile(path)
            let fswb = JsWorkbook.readToFsWorkbook wb
            return fswb
        }

    static member fromXlsxStream (stream:System.IO.Stream) : Promise<FsWorkbook> =
        promise {
            let wb = ExcelJs.Excel.Workbook()
            do! wb.xlsx.read stream
            return JsWorkbook.readToFsWorkbook wb
        }

    static member fromBytes (bytes: byte []) : Promise<FsWorkbook> =
        promise {
            let wb = ExcelJs.Excel.Workbook()
            let uint8 = Fable.Core.JS.Constructors.Uint8Array.Create bytes
            do! wb.xlsx.load(uint8.buffer)
            return JsWorkbook.readToFsWorkbook wb
        }

    static member toFile (path: string) (wb:FsWorkbook) : Promise<unit> =
        let jswb = JsWorkbook.writeFromFsWorkbook wb
        jswb.xlsx.writeFile(path)

    static member toStream (stream: System.IO.Stream) (wb:FsWorkbook) : Promise<unit> =
        let jswb = JsWorkbook.writeFromFsWorkbook wb
        jswb.xlsx.write(stream)

    static member toBytes (wb:FsWorkbook) : Promise<byte []> =
        promise {
            let jswb = JsWorkbook.writeFromFsWorkbook wb
            let buffer = jswb.xlsx.writeBuffer()
            return !!buffer
        }
            
            