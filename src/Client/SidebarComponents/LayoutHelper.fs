[<AutoOpen>]
module LayoutHelper

open Fable.React
open Fable.React.Props
open Fable.Core
open Fable.Core.JsInterop

open ExcelColors
open Model
open Messages
open Messages.BuildingBlock
open Shared
open Feliz
open Elmish

let rnd = System.Random()

let mutable order : bool =
    let v = rnd.Next(0,10)
    if v > 5 then false else true

let mainFunctionContainer (children: ReactElement list) =
    Html.div [
        prop.className "mainFunctionContainer"
        prop.style [
            let rndVal = rnd.Next(30,70)
            let colorArr = [|NFDIColors.LightBlue.Lighter10; NFDIColors.Mint.Lighter10;|]
            style.custom("borderImageSource", $"linear-gradient({colorArr.[if order then 0 else 1]} {100-rndVal}%%, {colorArr.[if order then 1 else 0]})")
            order <- not order
        ]
        prop.children children
    ]

open Feliz
open Feliz.Bulma

let pageHeader (header: string) = Bulma.title [Bulma.title.is5; prop.text header]
