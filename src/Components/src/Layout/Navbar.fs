namespace Swate.Components

open Feliz

type Navbar =

    static member MaterialIcon(icon: string, ?className: string) =
        Html.i [
            prop.className [
                //"swt:material-symbols-light--document-search-outline"
                "swt:iconify " + icon + " swt:size-6"
            ]
        ]

    static member Button(icon: ReactElement, tooltip: string, (onClick: unit -> unit), ?toolTipPosition: string) =
        let toolTipPosition = defaultArg toolTipPosition "swt:tooltip-right"

        Html.div [
            prop.className $"swt:tooltip {toolTipPosition}"
            prop.ariaLabel tooltip
            prop.children [
                Html.div [ prop.className "swt:tooltip-content"; prop.text tooltip ]
                Html.button [
                    prop.className "swt:btn swt:btn-square swt:btn-ghost"
                    prop.children [ icon ]
                    prop.onClick (fun _ -> onClick ())
                ]
            ]
        ]

    [<ReactComponent>]
    static member Selector((setState: string option -> unit), elements: string[], ?standardMethods: ReactElement[]) =

        let standardMethods = defaultArg standardMethods [||]

        let placeHolderOption =
            Html.option [
                prop.value "Select an action"
                prop.disabled true
                prop.hidden true
                prop.text "Select an action"
            ]

        Html.select [
            prop.className "swt:select swt:select-sm swt:join-item swt:border-none"
            prop.defaultValue "Select an action"
            prop.onChange (fun e -> setState (Some e))
            prop.children [
                placeHolderOption
                // -- Recent ARCs --
                Html.optgroup [
                    prop.label "Recent ARCs"
                    prop.children (
                        elements
                        |> Array.map (fun element -> Html.option [ prop.value element; prop.text element ])
                    )
                ]
                Html.optgroup [
                    prop.label "Standard Methods"
                    prop.children (standardMethods |> Array.map (fun element -> Html.option element))
                ]
            ]
        ]

    [<ReactComponent>]
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

        let states, setStates = React.useState ([| "1"; "2"; "3" |])

        let (state: string option), setState = React.useState (None)

        let noteAddIcon =
            Navbar.MaterialIcon("swt:material-symbols-light--create-new-folder-outline")

        let fileOpenIcon =
            Navbar.MaterialIcon("swt:material-symbols-light--document-search-outline")

        let cloudDownloadIcon = Navbar.MaterialIcon("swt:material-symbols-light--download")

        let newARCButton = Navbar.Button(noteAddIcon, "Create a new ARC", fun _ -> ())

        let openARCButton = Navbar.Button(fileOpenIcon, "Open an existing ARC", fun _ -> ())

        let downLoadARCButton =
            Navbar.Button(cloudDownloadIcon, "Download an existing ARC", (fun _ -> ()), "swt:tooltip-left")

        let standardButtons = [|
            newARCButton
            openARCButton
            Navbar.Button(cloudDownloadIcon, "Download an existing ARC", (fun _ -> ()))
        |]

        let selector = Navbar.Selector(setState, states, standardButtons)

        Navbar.Main(newARCButton, selector, downLoadARCButton)