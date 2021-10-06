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

let mainFunctionContainer children =
    div [
        Class "mainFunctionContainer"
        Style [
            let rndVal = rnd.Next(100)
            let shuffle (seq: 'a []) =
                for i in 0 .. seq.Length - 1 do
                    let j = rnd.Next(i, seq.Length)
                    let pom = seq.[i]
                    seq.[i] <- seq.[j]
                    seq.[j] <- pom
            let colorArr =
                let arr = [|NFDIColors.LightBlue.Lighter10; NFDIColors.Mint.Lighter10;|]
                //let expArr = [|NFDIColors.LightBlue.Lighter10; NFDIColors.Mint.Lighter10; NFDIColors.Red.Base; NFDIColors.Yellow.Base; NFDIColors.DarkBlue.Base; ExcelColors.Excel.Primary|]
                shuffle arr
                arr
            BorderImageSource $"linear-gradient({colorArr.[0]} {100-rndVal}%%, {colorArr.[1]})"
    ] ] children