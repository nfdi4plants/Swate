namespace Components

open Feliz
open Feliz.DaisyUI

type BaseNavbar =
    static member Main (children: ReactElement) =
        Daisy.navbar [
            prop.className "bg-secondary"
            prop.role "navigation"
            prop.ariaLabel "main navigation"
            prop.style [
                style.minHeight(length.rem 3.25)
            ]
            prop.children children
        ]

    static member Main (children: ReactElement seq) =
        BaseNavbar.Main (React.fragment children)