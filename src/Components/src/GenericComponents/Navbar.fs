namespace Swate.Components

open Feliz
open Fable.Core

[<Erase; Mangle(false)>]
type Navbar =

    [<ReactComponent>]
    static member Main
        (?left: ReactElement, ?middle: ReactElement, ?right: ReactElement, ?navbarHeight: int, ?debug: bool)
        =
        let debug = defaultArg debug false
        let left = defaultArg left (Html.div [])
        let middle = defaultArg middle (Html.div [])
        let right = defaultArg right (Html.div [])

        Html.div [
            prop.className
                "swt:bg-base-300 swt:text-base-content swt:gap-2 swt:flex swt:items-center swt:w-full swt:h-full swt:p-2"
            prop.role "navigation"
            prop.ariaLabel "arc navigation"
            if debug then
                prop.testId "navbar-test"
            prop.children [
                Html.div [
                    prop.className "swt:grow-0 swt:flex swt:flex-row"
                    prop.children left
                ]
                Html.div [
                    prop.className "swt:grow swt:flex swt:flex-row swt:text-center"
                    prop.children middle
                ]
                Html.div [
                    prop.className "swt:grow-0 swt:flex swt:flex-row"
                    prop.children right
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry(?debug: bool) =

        Navbar.Main(Html.div [], ?debug = debug)