module CustomComponents.ResponsiveFA
open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fulma.Extensions.Wikiki
open Fable.Core.JsInterop

open ExcelColors
open Model
open Messages

let responsiveFaElement toggle fa faToggled = 
    div [Style [
        Position PositionOptions.Relative
    ]] [
        Fa.i [
            Fa.Props [Style [
                Position PositionOptions.Absolute
                Top "0"
                Left "0"
                Display DisplayOptions.Block
                Transition "opacity 0.25s, transform 0.25s"
                if toggle then Opacity "0" else Opacity "1"
            ]]
            fa
        ][]
        Fa.i [
            Fa.Props [Style [
                Position PositionOptions.Absolute
                Top "0"
                Left "0"
                Display DisplayOptions.Block
                Transition "opacity 0.25s, transform 0.25s"
                if toggle then Opacity "1" else Opacity "0"
                if toggle then Transform "rotate(-180deg)" else Transform "rotate(0deg)"
            ]]
            faToggled
        ][]
        // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
        Fa.i [
            Fa.Props [Style [
                Display DisplayOptions.Block
                Opacity "0" 
            ]]
            fa
        ][]
    ]

let private createTriggeredId id =
    sprintf "%s_triggered" id

let private createNonTriggeredId id =
    sprintf "%s_triggered_not" id

let triggerResponsiveReturnEle id =
    let notTriggeredId = createNonTriggeredId id
    let triggeredId = createTriggeredId id
    let ele = Browser.Dom.document.getElementById notTriggeredId
    let triggeredEle = Browser.Dom.document.getElementById triggeredId
    ele?style?opacity <- "0"
    triggeredEle?style?opacity      <- "1"
    triggeredEle?style?transform    <- "rotate(-360deg)" 

let responsiveReturnEle id fa faToggled =
    let notTriggeredId = createNonTriggeredId id
    let triggeredId = createTriggeredId id
    div [Style [
        Position PositionOptions.Relative
    ]] [
        Fa.i [
            Fa.Props [
                Style [
                    Position PositionOptions.Absolute
                    Top "0"
                    Left "0"
                    Display DisplayOptions.Block
                    Transition "opacity 0.25s, transform 0.25s"
                    Opacity "1"
                ]
                Id notTriggeredId

                OnTransitionEnd (fun e ->
                    Fable.Core.JS.setTimeout (fun () ->
                        let ele = Browser.Dom.document.getElementById notTriggeredId
                        ele?style?opacity <- "1"
                    ) 1500 |> ignore
                    ()
                )
            ]
            fa
        ][]
        Fa.i [
            Fa.Props [
                Style [
                    Position PositionOptions.Absolute
                    Top "0"
                    Left "0"
                    Display DisplayOptions.Block
                    Transition "opacity 0.25s, transform 0.25s"
                    Opacity "0"
                    Transform "rotate(0deg)"
                ]
                Id triggeredId

                OnTransitionEnd (fun e ->
                    Fable.Core.JS.setTimeout (fun () ->
                        let triggeredEle = Browser.Dom.document.getElementById triggeredId
                        triggeredEle?style?opacity      <- "0"
                        triggeredEle?style?transform    <- "rotate(0deg)"
                    ) 1500 |> ignore
                    ()
                )
            ]
            faToggled
        ][]
        // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
        Fa.i [
            Fa.Props [Style [
                Display DisplayOptions.Block
                Opacity "0" 
            ]]
            fa
        ][]
    ]