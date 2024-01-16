[<AutoOpenAttribute>]
module FsSpreadsheet.Exceljs.FsSpreadsheet

open FsSpreadsheet
open FsSpreadsheet.Exceljs
open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop

// This is mainly used for fsharp based access in a fable environment. 
// If you want to use these bindings from js, you should use the ones in `Xlsx.fs`
type FsWorkbook with

    static member fromXlsxFile(path:string) : Promise<FsWorkbook> =
        Xlsx.fromXlsxFile(path)

    static member fromXlsxStream(stream:System.IO.Stream) : Promise<FsWorkbook> =
        Xlsx.fromXlsxStream stream

    static member fromBytes(bytes: byte []) : Promise<FsWorkbook> =
        Xlsx.fromBytes bytes

    static member toFile(path: string) (wb:FsWorkbook) : Promise<unit> =
        Xlsx.toFile path wb

    static member toStream(stream: System.IO.Stream) (wb:FsWorkbook) : Promise<unit> =
        Xlsx.toStream stream wb

    static member toBytes(wb:FsWorkbook) : Promise<byte []> =
        Xlsx.toBytes wb

    member this.ToFile(path: string) : Promise<unit> =
        FsWorkbook.toFile path this

    member this.ToStream(stream: System.IO.Stream) : Promise<unit> =
        FsWorkbook.toStream stream this

    member this.ToBytes() : Promise<byte []> =
        FsWorkbook.toBytes this