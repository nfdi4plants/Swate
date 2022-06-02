module Button

open Fulma
open Fable.React
open Fable.React.Props

let buttonComponent (colorMode: ExcelColors.ColorMode) (isDarkMode:bool) txt onClick =

    Button.button [
        if isDarkMode then
            Button.Color IsWarning
            Button.IsOutlined
        else
            Button.Props [Style [BackgroundColor colorMode.BodyForeground; Color colorMode.Text]]
        Button.IsFullWidth
        Button.OnClick onClick
        ] [
        str txt
    ]