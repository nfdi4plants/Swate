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

        let newARCButton =
            ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", fun _ -> ())

        let openARCButton =
            ButtonInfo.create ("swt:fluent--folder-arrow-up-24-regular swt:size-5", "Open an existing ARC", fun _ -> ())

        let downLoadARCButton =
            ButtonInfo.create (
                "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
                "Download an existing ARC",
                fun _ -> ()
            )

        let standardButtons = [|
            newARCButton
            openARCButton
            downLoadARCButton
            newARCButton
            openARCButton
        |]

        let testRecentARCs = [|
            ARCPointer.create ("Test 1", "/Here", false)
            ARCPointer.create ("Test 2", "/Here/Here", false)
            ARCPointer.create ("Test 3", "/Here/Here/Here", false)
            ARCPointer.create (
                "Test jfcesjföisjyfnwjtiewhroiajlkfnnalkfjwarkoiewfanflkndslkfjwiajofkcmscnskjfafdölmsalknoisjfamlkcnkj<ycwaklfnewjföosajö",
                "/Here/Here/Here/Here",
                false
            )
        |]

        let recentARCs, setRecentARCs = React.useState (testRecentARCs)

        let selector =
            Selector.Main(recentARCs, setRecentARCs, standardButtons, 5, 3, potMaxWidth = 48, ?debug = debug)

        Navbar.Main(selector, ?debug = debug)