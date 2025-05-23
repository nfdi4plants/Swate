namespace Components

open Feliz
open Feliz.DaisyUI

type BaseNavbar =
    static member Main(children: ReactElement) =
        //Daisy.navbar [
        Html.div [
            prop.className "swt:navbar swt:bg-secondary"
            prop.role "navigation"
            prop.ariaLabel "main navigation"
            prop.style [ style.minHeight (length.rem 3.25) ]
            prop.children children
        ]

    static member Main(children: ReactElement seq) =
        BaseNavbar.Main(React.fragment children)

    static member Glow(children: ReactElement) =
        //Daisy.navbar [
        Html.div [
            prop.className "swt:navbar swt:relative swt:bg-secondary"
            prop.role "navigation"
            prop.ariaLabel "main navigation"
            prop.style [ style.minHeight (length.rem 3.25) ]
            prop.children [
                Html.div [
                    prop.className "swt:absolute swt:inset-0 swt:bg-gradient-to-r swt:from-primary swt:to-info swt:blur-2xl swt:opacity-75"
                ]
                Html.div [
                    prop.className "swt:z-10 swt:flex swt:flex-row swt:gap-2 swt:w-full swt:items-center"
                    prop.children children
                ]
            ]
        ]

    static member Glow(children: ReactElement seq) =
        BaseNavbar.Glow(React.fragment children)