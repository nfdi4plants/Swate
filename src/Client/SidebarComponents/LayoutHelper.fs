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