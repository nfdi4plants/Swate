module Renderer.Components.FileExplorerArcPath

open Feliz
open Swate.Components

[<ReactComponent>]
let ArcPathPopover
    (
        arcName: string,
        arcRootPath: string option,
        onCopyPath: string -> unit,
        onOpenArcFolder: unit -> unit
    )
    =
    let pathValue = arcRootPath |> Option.defaultValue "Path unavailable."
    let hasPath = arcRootPath.IsSome

    Popover.Popover(
        debug = "FileExplorerArcPath",
        placement = FloatingUI.Placement.BottomStart,
        children =
            React.Fragment [
                Popover.Trigger(
                    Html.span [
                        prop.className "swt:block swt:w-full swt:truncate"
                        prop.text arcName
                    ],
                    className =
                        "swt:mb-2 swt:w-full swt:min-h-0 swt:h-auto swt:justify-start swt:px-2 swt:py-1 swt:text-sm swt:font-semibold swt:normal-case swt:btn-ghost",
                    props = [
                        prop.testId "left-sidebar-file-explorer-arc-name"
                        prop.title pathValue
                    ]
                )
                Popover.Content(
                    className = "swt:w-96 swt:max-w-[calc(100vw-3rem)]",
                    children =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-3 swt:text-sm"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:flex-col swt:gap-1"
                                    prop.children [
                                        Html.h3 [
                                            prop.className "swt:text-sm swt:font-semibold"
                                            prop.text "ARC local path"
                                        ]
                                        Html.p [
                                            prop.testId "file-explorer-arc-path-value"
                                            prop.className "swt:break-all swt:text-xs swt:opacity-90"
                                            prop.text pathValue
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.testId "file-explorer-arc-path-copy"
                                            prop.className "swt:btn swt:btn-sm"
                                            prop.disabled (not hasPath)
                                            prop.onClick (fun _ ->
                                                arcRootPath
                                                |> Option.iter onCopyPath
                                            )
                                            prop.children [
                                                Html.i [
                                                    prop.className "swt:iconify swt:fluent--copy-24-regular"
                                                ]
                                                Html.span [
                                                    prop.text "Copy path"
                                                ]
                                            ]
                                        ]
                                        Html.button [
                                            prop.testId "file-explorer-arc-path-open-folder"
                                            prop.className "swt:btn swt:btn-sm"
                                            prop.disabled (not hasPath)
                                            prop.onClick (fun _ ->
                                                if hasPath then
                                                    onOpenArcFolder ()
                                            )
                                            prop.children [
                                                Html.i [
                                                    prop.className "swt:iconify swt:fluent--folder-open-24-regular"
                                                ]
                                                Html.span [
                                                    prop.text "Open folder"
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                )
            ]
    )
