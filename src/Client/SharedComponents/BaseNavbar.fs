namespace Components

open Feliz
open Feliz.DaisyUI

type BaseNavbar =
    static member Main(children: ReactElement) =
        Daisy.navbar [
            prop.className "bg-secondary"
            prop.role "navigation"
            prop.ariaLabel "main navigation"
            prop.style [ style.minHeight (length.rem 3.25) ]
            prop.children children
        ]

    static member Main(children: ReactElement seq) =
        BaseNavbar.Main(React.fragment children)

    static member Glow(children: ReactElement) =
        Daisy.navbar [
            prop.className "relative bg-secondary"
            prop.role "navigation"
            prop.ariaLabel "main navigation"
            prop.style [ style.minHeight (length.rem 3.25) ]
            prop.children [
                Html.div [
                    prop.className "absolute inset-0 bg-gradient-to-r from-primary to-info blur-2xl opacity-75"
                ]
                Html.div [
                    prop.className "z-10 flex flex-row gap-2 w-full items-center"
                    prop.children children
                ]
            ]
        ]

    static member Glow(children: ReactElement seq) =
        BaseNavbar.Glow(React.fragment children)