module ExcelColors

open Fable.React
open Fable.React.Props
open Fulma

//https://developer.microsoft.com/en-us/fluentui#/styles/web/colors/products
module Excel =

        let Shade20    = "#004b1c"
        let Shade10    = "#0e5c2f"
        let Primary    = "#217346"
        let Tint10     = "#3f8159"
        let Tint20     = "#4e9668"
        let Tint30     = "#6eb38a"
        let Tint40     = "#9fcdb3"
        let Tint50     = "#e9f5ee"

module Colorfull =

        let gray180 = "#252423"
        let gray140 = "#484644"
        let gray130 = "#605e5c"
        let gray80  = "#b3b0ad"
        let gray60  = "#c8c6c4"
        let gray50  = "#d2d0ce"
        let gray40  = "#e1dfdd"
        let gray30  = "#edebe9"
        let gray20  = "#f3f2f1"
        let white   = "#ffffff"


module Black =

        let Primary = "#000000"
        let gray190 = "#201f1e"
        let gray180 = "#252423"
        let gray170 = "#292827"
        let gray160 = "#323130"
        let gray150 = "#3b3a39"
        let gray140 = "#484644"
        let gray130 = "#605e5c"
        let gray100 = "#979593"
        let gray90  = "#a19f9d"
        let gray70  = "#bebbb8"
        let gray40  = "#e1dfdd"
        let white   = "#ffffff"

type ColorMode = {
    Name                    : string
    BodyBackground          : string
    BodyForeground          : string
    ControlBackground       : string
    ControlForeground       : string
    ElementBackground       : string
    ElementForeground       : string
    Text                    : string
    Accent                  : string
    Fade                    : string

}

let darkMode = {
    Name                    = "Dark"
    BodyBackground          = Black.gray180
    BodyForeground          = Black.gray160
    ControlBackground       = Black.gray140
    ControlForeground       = Black.gray100
    ElementBackground       = Black.Primary
    ElementForeground       = Black.gray140
    Text                    = Black.white
    Accent                  = Black.white
    Fade                    = Black.gray70
}

let colorfullMode = {
    Name                    = "Colorfull"
    BodyBackground          = Colorfull.gray20
    BodyForeground          = Colorfull.gray20
    ControlBackground       = Colorfull.white
    ControlForeground       = Colorfull.gray40
    ElementBackground       = Excel.Tint10
    ElementForeground       = Colorfull.white
    Text                    = Colorfull.gray180
    Accent                  = Excel.Primary
    Fade                    = Excel.Tint30
}

let colorElement (mode:ColorMode) =
    Style [
        BackgroundColor mode.ElementBackground
        BorderColor     mode.ElementForeground
        Color           mode.Text
    ]

let colorControl (mode:ColorMode) =
    Style [
        BackgroundColor mode.ControlBackground
        BorderColor     mode.ControlForeground
        Color           mode.Text
    ]

let colorBackground (mode:ColorMode) =
    Style [
        BackgroundColor mode.BodyBackground
        BorderColor     mode.BodyForeground
        Color           mode.Text
    ]