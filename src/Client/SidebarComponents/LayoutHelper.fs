namespace SidebarComponents

open Fable.React
open Fable.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Model
open Messages
open Messages.BuildingBlock
open Shared
open Feliz
open Elmish

module private LayoutHelperAux =

    let rnd = System.Random()

    let mutable order : bool =
        let v = rnd.Next(0,10)
        if v > 5 then false else true

type SidebarLayout =
    static member Container (children: ReactElement list) =
        Html.div [
            prop.className "flex flex-col gap-2 py-4"
            prop.children children
        ]

    static member LogicContainer (children: ReactElement list) =
        Html.div [
        //     prop.className "border-l-4 border-transparent px-4 py-2 shadow-md"
        //     prop.style [
        //         let rndVal = rnd.Next(30,70)
        //         let colorArr = [|NFDIColors.LightBlue.Lighter10; NFDIColors.Mint.Lighter10;|]
        //         style.custom("borderImageSlice", "1")
        //         style.custom("borderImageSource", $"linear-gradient({colorArr.[if order then 0 else 1]} {100-rndVal}%%, {colorArr.[if order then 1 else 0]})")
        //         order <- not order
        //     ]
            prop.className "relative flex p-4 animated-border shadow-md gap-4 flex-col" //experimental
            prop.children children
        ]

    static member Header(txt: string) =
        Html.h3 [
            prop.className "text-lg font-semibold"
            prop.text txt
        ]

    static member Description (content: ReactElement) =
        Html.div [
            prop.className "prose-sm prose-p:m-1 prose-ul:my-1 prose-ul:list-disc"
            prop.children content
        ]

    static member Description (content: string) =
        SidebarLayout.Description (Html.p content)