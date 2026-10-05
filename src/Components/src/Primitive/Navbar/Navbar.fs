namespace Swate.Components.Primitive.Navbar

open Feliz
open Fable.Core

[<Erase; Mangle(false)>]
type Navbar =

    [<ReactComponent>]
    static member Main(?left: ReactElement, ?middle: ReactElement, ?right: ReactElement, ?debug: bool) =
        let debug = defaultArg debug false

        Html.div [
            prop.className
                "swt:text-base-content swt:gap-2 swt:flex swt:items-center swt:w-full swt:h-full swt:p-2 swt:shadow-xl swt:bg-base-200"
            prop.role "navigation"
            prop.ariaLabel "arc navigation"
            if debug then
                prop.testId "navbar-test"
            prop.children [
                if left.IsSome then
                    Html.div [
                        prop.className "swt:grow-0 swt:flex swt:flex-row swt:gap-2"
                        prop.children left.Value
                    ]
                if middle.IsSome then
                    Html.div [
                        prop.className "swt:grow swt:flex swt:flex-row swt:text-center swt:gap-2"
                        prop.children middle.Value
                    ]
                if right.IsSome then
                    Html.div [
                        prop.className "swt:grow-0 swt:flex swt:flex-row swt:gap-2"
                        prop.children right.Value
                    ]
            ]
        ]

    [<ReactComponent(true)>]
    static member Entry(?debug: bool) =

        Navbar.Main(Html.div [], ?debug = debug)
