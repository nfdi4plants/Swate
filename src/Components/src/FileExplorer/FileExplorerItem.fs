namespace Swate.Components.FileExplorer

open Fable.Core
open Feliz
open Swate.Components.FileExplorer.Types

[<Mangle(false); Erase>]
type FileExplorerItem =

    [<ReactComponent>]
    static member private CreateItemButton
        (item: FileItem, onCreateItem: (FileItem -> unit) option, canCreateItem: FileItem -> bool)
        =
        let handleCreateItem (ev: Browser.Types.MouseEvent) =
            ev.preventDefault ()
            ev.stopPropagation ()
            onCreateItem |> Option.iter (fun fn -> fn item)

        if item.IsDirectory && canCreateItem item && onCreateItem.IsSome then
            Html.button [
                prop.type'.button
                prop.className
                    "swt:btn swt:btn-ghost swt:btn-square swt:btn-xs swt:opacity-0 swt:transition-opacity swt:group-hover:opacity-100 swt:focus:opacity-100"
                prop.ariaLabel $"Create new item in {item.Name}"
                prop.title $"Create new item in {item.Name}"
                prop.onClick handleCreateItem
                prop.children [
                    Html.i [
                        prop.className "swt:iconify swt:fluent--add-24-regular swt:size-4"
                    ]
                ]
            ]
        else
            Html.none

    [<ReactComponent>]
    static member private LFSStatusPill (item: FileItem) =
        let isDownloaded = item.Downloaded = Some true

        let statusAccessibilityText =
            match item.SizeFormatted, isDownloaded with
            | Some size, true -> $"LFS Downloaded - {size}"
            | Some size, false -> $"LFS Not Downloaded - {size}"
            | None, true -> "LFS Downloaded"
            | None, false -> "LFS Not Downloaded"

        let showSizeSegment = item.SizeFormatted.IsSome

        let statusClassName =
            if isDownloaded then
                "swt:badge-success"
            else
                "swt:bg-info swt:text-info-content"

        let statusIconClassName =
            if isDownloaded then
                "swt:fluent--checkmark-circle-24-regular"
            else
                "swt:fluent--cloud-arrow-down-24-regular"

        let statusShapeClass =
            if showSizeSegment then
                "swt:rounded-none swt:border-0"
            else
                "swt:rounded-full"

        let statusBadge =
            Html.span [
                prop.className
                    $"swt:badge swt:badge-sm swt:cursor-default swt:gap-0.5 {statusClassName} {statusShapeClass}"
                prop.custom (
                    "data-lfs-download-status",
                    if isDownloaded then
                        "downloaded"
                    else
                        "not-downloaded"
                )
                prop.children [
                    Html.i [
                        prop.className $"swt:iconify {statusIconClassName} swt:size-3"
                    ]
                    Html.span [
                        prop.text "LFS"
                    ]
                ]
            ]

        let badgeSegments =
            [
                statusBadge

                match item.SizeFormatted with
                | Some size ->
                    Html.span [
                        prop.className
                            "swt:badge swt:badge-sm swt:rounded-none swt:border-0 swt:cursor-default swt:bg-base-200 swt:text-base-content"
                        prop.text size
                    ]
                | None -> ()
            ]

        Html.span [
            prop.className "swt:inline-flex swt:overflow-hidden swt:rounded-full swt:border swt:border-base-300"
            prop.ariaLabel statusAccessibilityText
            prop.title statusAccessibilityText
            prop.onClick (fun e ->
                e.preventDefault ()
                e.stopPropagation ()
            )
            prop.children badgeSegments
        ]

    [<ReactComponent>]
    static member DirectoryRow
        (
            item: FileItem,
            rowHighlightClass: string,
            selectedNameClass: string,
            isExpanded: bool,
            useDirectoryChevronToggle: bool,
            canExpandDirectory: bool,
            getItemIconClass: FileItem -> string option,
            onDirectorySelect: Browser.Types.MouseEvent -> unit,
            onDirectoryArrowToggle: Browser.Types.MouseEvent -> unit,
            ?onCreateItem: FileItem -> unit,
            ?canCreateItem: FileItem -> bool,
            ?children: ReactElement
        ) =
        let canCreateItem = defaultArg canCreateItem (fun (_: FileItem) -> false)
        let canCreateFromDirectory = canCreateItem item && onCreateItem.IsSome

        let directoryToggleIconClass =
            if isExpanded then
                "swt:iconify swt:fluent--caret-down-24-filled swt:size-4 swt:shrink-0"
            else
                "swt:iconify swt:fluent--caret-right-24-filled swt:size-4 swt:shrink-0"

        let rowChildren =
            [
                Html.div [
                    prop.custom ("data-file-item-id", item.Id)
                    prop.className [
                        "swt:group swt:w-full swt:px-2 swt:py-1 swt:cursor-default"
                        rowHighlightClass
                    ]
                    prop.style [
                        style.display.flex
                        style.width (length.percent 100)
                    ]

                    if not useDirectoryChevronToggle then
                        prop.onClick onDirectorySelect

                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:w-full swt:items-center swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.type'.button
                                    prop.className
                                        "swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2 swt:bg-transparent swt:border-0 swt:p-0 swt:text-left swt:cursor-default"
                                    prop.onClick onDirectorySelect
                                    prop.children [
                                        Html.i [
                                            prop.className (
                                                FileExplorerItemHelper.iconClassName (
                                                    [ "swt:iconify"; "swt:shrink-0" ],
                                                    item,
                                                    getItemIconClass
                                                )
                                            )
                                        ]
                                        Html.span [
                                            prop.className [
                                                "swt:truncate"
                                                selectedNameClass
                                            ]
                                            prop.text item.Name
                                        ]
                                    ]
                                ]

                                if item.IsLFS = Some true || canExpandDirectory || canCreateFromDirectory then
                                    Html.div [
                                        prop.className "swt:ml-auto swt:shrink-0 swt:flex swt:items-center swt:gap-2"
                                        prop.children [
                                            FileExplorerItem.CreateItemButton (
                                                item,
                                                onCreateItem,
                                                canCreateItem
                                            )

                                            if item.IsLFS = Some true then
                                                Html.div [
                                                    prop.className "swt:flex swt:items-center"
                                                    prop.children [ FileExplorerItem.LFSStatusPill item ]
                                                ]

                                            if canExpandDirectory then
                                                Html.button [
                                                    prop.type'.button
                                                    prop.className [
                                                        "swt:flex swt:min-h-0 swt:h-5 swt:w-5 swt:shrink-0 swt:items-center swt:justify-center swt:rounded swt:border-0 swt:bg-transparent swt:p-0 swt:cursor-pointer"
                                                        rowHighlightClass
                                                    ]
                                                    prop.ariaLabel (
                                                        if isExpanded then
                                                            $"Collapse {item.Name}"
                                                        else
                                                            $"Expand {item.Name}"
                                                    )
                                                    prop.onClick onDirectoryArrowToggle
                                                    prop.children [
                                                        Html.i [ prop.className directoryToggleIconClass ]
                                                    ]
                                                ]
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]

                match children with
                | Some childTree -> childTree
                | None -> Html.none
            ]

        Html.li [
            prop.key item.Id
            prop.custom ("data-file-item-id", item.Id)
            prop.className "swt:w-full"
            prop.children rowChildren
        ]

    [<ReactComponent>]
    static member FileRow
        (
            item: FileItem,
            rowHighlightClass: string,
            selectedNameClass: string,
            getItemIconClass: FileItem -> string option,
            onSelect: unit -> unit
        ) =
        Html.li [
            prop.key item.Id
            prop.custom ("data-file-item-id", item.Id)
            prop.children [
                Html.a [
                    prop.custom ("data-file-item-id", item.Id)
                    prop.className [
                        "swt:group swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-between swt:cursor-default"
                        rowHighlightClass
                    ]
                    prop.onClick (fun _ -> onSelect ())
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:gap-2"
                            prop.children [
                                Html.i [
                                    prop.className (
                                        FileExplorerItemHelper.iconClassName (
                                            [ "swt:iconify" ],
                                            item,
                                            getItemIconClass
                                        )
                                    )
                                ]
                                Html.span [
                                    prop.className selectedNameClass
                                    prop.text item.Name
                                ]
                            ]
                        ]

                        if item.IsLFS = Some true then
                            Html.div [
                                prop.className "swt:flex swt:items-center"
                                prop.children [ FileExplorerItem.LFSStatusPill item ]
                            ]
                    ]
                ]
            ]
        ]

