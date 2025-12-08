namespace ElectronComponents

open Feliz

type Navbar =

    static member MaterialIcon(icon: string, ?className: string) =
        let className = defaultArg className ""

        Html.i [
            prop.className ("material-icons " + className)
            prop.text icon
        ]

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

    static member Main(?left: ReactElement, ?middle: ReactElement, ?right: ReactElement) =
        let left = defaultArg left (Html.div [])
        let middle = defaultArg middle (Html.div [])
        let right = defaultArg right (Html.div [])

        Html.div [
            prop.className "swt:navbar swt:bg-base-300 swt:text-base-content swt:gap-2 swt:flex swt:items-center"
            prop.role "navigation"
            prop.ariaLabel "arc navigation"
            prop.style [ style.minHeight (length.rem 3.25) ]
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
        let noteAddIcon = Navbar.MaterialIcon("note_add", "swt:size-6")
        let fileOpenIcon = Navbar.MaterialIcon("file_open", "swt:size-6")
        let cloudDownloadIcon = Navbar.MaterialIcon("cloud_download", "swt:size-6")

        let newARCButton = Navbar.Button(noteAddIcon, "Create a new ARC", fun _ -> ())

        let openARCButton = Navbar.Button(fileOpenIcon, "Open an existing ARC", fun _ -> ())

        let downLoadARCButton =
            Navbar.Button(cloudDownloadIcon, "Download an existing ARC", (fun _ -> ()), "swt:tooltip-left")

        Navbar.Main(newARCButton, openARCButton, downLoadARCButton)