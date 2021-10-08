[<AutoOpen>]
module FunctionContainer

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Fable.Core
open Fable.Core.JsInterop

open ExcelColors
open Model
open Messages
open Messages.BuildingBlock
open Shared
open TermTypes

open OfficeInteropTypes
open Elmish

let rnd = System.Random()

let mutable order : bool =
    let v = rnd.Next(0,10)
    if v > 5 then false else true

let mainFunctionContainer children =
    div [
        Class "mainFunctionContainer"
        Style [
            let rndVal = rnd.Next(30,70)
            let shuffle (seq: 'a []) =
                for i in 0 .. seq.Length - 1 do
                    let j = rnd.Next(i, seq.Length)
                    let pom = seq.[i]
                    seq.[i] <- seq.[j]
                    seq.[j] <- pom
            let colorArr = [|NFDIColors.LightBlue.Lighter10; NFDIColors.Mint.Lighter10;|]
            BorderImageSource $"linear-gradient({colorArr.[if order then 0 else 1]} {100-rndVal}%%, {colorArr.[if order then 1 else 0]})"
            order <- not order
    ] ] children