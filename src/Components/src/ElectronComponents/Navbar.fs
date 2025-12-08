namespace ElectronComponents

open Feliz

type Navbar =

    static member MaterialIcon(icon: string, ?className: string) =
        Html.i [ prop.className ("swt:iconify " + icon + " swt:size-6") ]

    static member Button(icon: ReactElement, tooltip: string, (onClick: unit -> unit), ?toolTipPosition: string) =
        let toolTipPosition = defaultArg toolTipPosition "swt:tooltip-right"

        Html.div [
            prop.className $"swt:tooltip {toolTipPosition}"
            prop.ariaLabel tooltip
            prop.children [
                Html.div [ prop.className "swt:tooltip-content"; prop.text tooltip ]
                Html.button [
                    prop.className "swt:btn swt:btn-square swt:btn-ghost swt:btn-sm"
                    prop.children [ icon ]
                    prop.onClick (fun _ -> onClick ())
                ]
            ]
        ]

    static member Main(?left: ReactElement, ?middle: ReactElement, ?right: ReactElement, ?navbarHeight: int) =
        let left = defaultArg left (Html.div [])
        let middle = defaultArg middle (Html.div [])
        let right = defaultArg right (Html.div [])

        let navbarHeight = defaultArg navbarHeight 40

        Html.div [
            prop.className "swt:navbar swt:bg-base-300 swt:text-base-content swt:gap-2 swt:flex swt:items-center"
            prop.role "navigation"
            prop.ariaLabel "arc navigation"
            prop.style [
                style.minHeight length.auto
                style.height navbarHeight
                style.paddingTop 0
                style.paddingBottom 0
            ]
            prop.children [
                Html.div [ prop.className "swt:flex-none"; prop.children [ left ] ]
                Html.div [
                    prop.className "swt:flex-1 swt:text-center"
                    prop.children [ middle ]
                ]
                Html.div [ prop.className "swt:flex-none"; prop.children [ right ] ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        let noteAddIcon =
            Navbar.MaterialIcon("swt:material-symbols-light--left-panel-close")

        let fileOpenIcon =
            Navbar.MaterialIcon("swt:material-symbols-light--left-panel-close")

        let cloudDownloadIcon =
            Navbar.MaterialIcon("swt:material-symbols-light--left-panel-close")

        let newARCButton = Navbar.Button(noteAddIcon, "Create a new ARC", fun _ -> ())

        let openARCButton = Navbar.Button(fileOpenIcon, "Open an existing ARC", fun _ -> ())

        let downLoadARCButton =
            Navbar.Button(cloudDownloadIcon, "Download an existing ARC", (fun _ -> ()), "swt:tooltip-left")

        Navbar.Main(newARCButton, openARCButton, downLoadARCButton)