module CustomComponents.ResponsiveFA
open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop

open ExcelColors
open Model
open Messages

open Feliz
open Feliz.Bulma

let responsiveFaElement toggle fa faToggled = 
    div [Style [
        Position PositionOptions.Relative
    ]] [
        Bulma.icon [ Html.i [
            prop.style [
                style.position.absolute
                style.top 0
                style.left 0
                style.display.block
                style.transitionProperty "opacity 0.25s, transform 0.25s"
                if toggle then style.opacity 0 else style.opacity 1
            ]
            fa
        ]]
        Bulma.icon [ Html.i [
            prop.style [
                style.position.absolute
                style.top 0
                style.left 0
                style.display.block
                style.transitionProperty "opacity 0.25s, transform 0.25s"
                if toggle then style.opacity 1 else style.opacity 0
                if toggle then style.transform [transform.rotate -180] else style.transform [transform.rotate 0]
            ]
            faToggled
        ]]
        // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
        Bulma.icon [ Html.i [
            prop.style [
                style.display.block
                style.opacity 0 
            ]
            fa
        ]]
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
        ] []
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
        ] []
        // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
        Fa.i [
            Fa.Props [Style [
                Display DisplayOptions.Block
                Opacity "0" 
            ]]
            fa
        ] []
    ]