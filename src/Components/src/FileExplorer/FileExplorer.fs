namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz

open Swate.Components.FileExplorerTypes

// ---------------------------------------------------------------------------
[<Mangle(false); Erase>]
type FileExplorer =

    static member defaultIconPaths = {|
        file =
            "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"
        folder =
            "M2.25 12.75V12A2.25 2.25 0 014.5 9.75h15A2.25 2.25 0 0121.75 12v.75m-8.69-6.44l-2.12-2.12a1.5 1.5 0 00-1.061-.44H4.5A2.25 2.25 0 002.25 6v12a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9a2.25 2.25 0 00-2.25-2.25h-5.379a1.5 1.5 0 01-1.06-.44z"
        pdf = "M12 2a2 2 0 0 1 2 2v16a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h6zm0 0v6h6M12 2l6 6"
        psd =
            "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"
        txt =
            "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"
        image =
            "M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z"
        markdown =
            "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12"
        datamap =
            "M3.375 19.5h17.25m-17.25 0a1.125 1.125 0 01-1.125-1.125M3.375 19.5h7.5c.621 0 1.125-.504 1.125-1.125m-9.75 0V5.625m0 12.75v-1.5c0-.621.504-1.125 1.125-1.125m18.375 2.625V5.625m0 12.75c0 .621-.504 1.125-1.125 1.125m1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125m0 3.75h-7.5A1.125 1.125 0 0112 18.375m9.75-12.75c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125m19.5 0v1.5c0 .621-.504 1.125-1.125 1.125M2.25 5.625v1.5c0 .621.504 1.125 1.125 1.125m0 0h17.25m-17.25 0h7.5c.621 0 1.125.504 1.125 1.125M3.375 8.25c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125m17.25-3.75h-7.5c-.621 0-1.125.504-1.125 1.125m8.625-1.125c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125M12 10.875v-1.5m0 1.5c0 .621-.504 1.125-1.125 1.125M12 10.875c0 .621.504 1.125 1.125 1.125m-2.25 0c.621 0 1.125.504 1.125 1.125M13.125 12h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125M20.625 12c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5"
    |}

    static member private icon(path: string) =
        Html.span [
            prop.className "swt:mr-2"
            prop.children [
                Svg.svg [
                    svg.xmlns "http://www.w3.org/2000/svg"
                    svg.fill "none"
                    svg.viewBox (0, 0, 24, 24)
                    svg.stroke "currentColor"
                    svg.strokeWidth 1.5
                    svg.custom ("strokeLinecap", "round")
                    svg.custom ("strokeLinejoin", "round")
                    svg.className "swt:h-4 swt:w-4"
                    svg.children [ Svg.path [ svg.d path; svg.strokeWidth 1.5 ] ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member FileExplorer
        (?initialItems: FileItem list, ?onItemClick: FileItem -> unit, ?onContextMenu: FileItem -> ContextMenuItem list)
        =
        let reducer model msg = FileExplorerLogic.update msg model

        let initialModel = FileExplorerLogic.init (defaultArg initialItems [])

        let model, dispatch = React.useReducer (reducer, initialModel)

        React.useEffect (
            (fun () -> dispatch (FileExplorerLogic.UpdateItems(defaultArg initialItems []))),
            [| box initialItems |]
        )

        let handleItemClick item =
            dispatch (FileExplorerLogic.SelectItem item.Id)
            onItemClick |> Option.iter (fun fn -> fn item)

        let handleContextMenu (e: Browser.Types.MouseEvent) item =
            e.preventDefault ()

            let menuItems =
                match onContextMenu with
                | Some fn -> fn item
                | None -> []

            if not (List.isEmpty menuItems) then
                dispatch (FileExplorerLogic.ShowContextMenu(e.clientX, e.clientY, menuItems))

        let rec renderItem item =
            let isSelected = model.SelectedId = Some item.Id
            let selectedClass = if isSelected then "swt:bg-base-300" else ""
            let isExpanded = model.ExpandedIds.Contains item.Id

            match item.Children with
            | Some children ->
                Html.li [
                    prop.key item.Id
                    prop.children [
                        Html.details [
                            if isExpanded then
                                prop.custom ("open", true)
                            prop.children [
                                Html.summary [
                                    prop.className ("swt:px-2 swt:py-1 swt:cursor-pointer " + selectedClass)
                                    prop.onContextMenu (fun e -> handleContextMenu e item)
                                    prop.onClick (fun ev ->
                                        ev.preventDefault ()
                                        ev.stopPropagation ()
                                        dispatch (FileExplorerLogic.ToggleExpanded item.Id)
                                        handleItemClick item
                                    )
                                    prop.children [
                                        Html.div [
                                            prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                                            prop.children [
                                                Html.div [
                                                    prop.className "swt:flex swt:items-center swt:gap-2"
                                                    prop.children [
                                                        Html.i [ prop.className [ "swt:iconify " + item.IconPath ] ]
                                                        Html.span item.Name
                                                    ]
                                                ]

                                                // LFS badge and size if applicable
                                                if item.IsLFS = Some true then
                                                    Html.div [
                                                        prop.className "swt:flex swt:gap-2 swt:items-center"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "swt:btn swt:btn-xs"
                                                                prop.disabled (item.Downloaded = Some true)
                                                                prop.text "LFS"
                                                                prop.onClick (fun e ->
                                                                    e.stopPropagation ()

                                                                    dispatch (
                                                                        FileExplorerLogic.ToggleLFSDownload item.Id
                                                                    )
                                                                )
                                                            ]
                                                            match item.SizeFormatted with
                                                            | Some size ->
                                                                Html.span [
                                                                    prop.className "swt:badge swt:badge-sm"
                                                                    prop.text size
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]

                                if isExpanded then
                                    Html.ul [
                                        prop.className "swt:ml-4"
                                        prop.children (children |> List.map renderItem)
                                    ]
                            ]
                        ]
                    ]
                ]
            | None ->
                Html.li [
                    prop.key item.Id
                    prop.children [
                        Html.a [
                            prop.className (
                                "swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-between "
                                + selectedClass
                            )
                            prop.onContextMenu (fun e -> handleContextMenu e item)
                            prop.onClick (fun _ -> handleItemClick item)
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:gap-2"
                                    prop.children [
                                        Html.i [ prop.className [ "swt:iconify " + item.IconPath ] ]
                                        Html.span item.Name
                                    ]
                                ]

                                // LFS badge for files
                                if item.IsLFS = Some true then
                                    Html.div [
                                        prop.className "swt:flex swt:gap-2 swt:items-center"
                                        prop.children [
                                            Html.button [
                                                prop.className "swt:btn swt:btn-xs"
                                                prop.disabled (item.Downloaded = Some true)
                                                prop.text "LFS"
                                                prop.onClick (fun e ->
                                                    e.stopPropagation ()
                                                    dispatch (FileExplorerLogic.ToggleLFSDownload item.Id)
                                                )
                                            ]
                                            match item.SizeFormatted with
                                            | Some size ->
                                                Html.span [ prop.className "swt:badge swt:badge-sm"; prop.text size ]
                                            | None -> Html.none
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]

        Html.div [
            prop.className "swt:w-full swt:bg-base-100 swt:rounded-box swt:shadow-md"
            prop.children [
                if not (List.isEmpty model.BreadcrumbPath) then
                    Breadcrumbs.Breadcrumbs(model.BreadcrumbPath, fun id -> dispatch (FileExplorerLogic.NavigateTo id))
                Html.ul [
                    prop.testId "file-explorer-container"
                    prop.className "swt:menu swt:w-full swt:bg-base-200 swt:rounded-box"
                    prop.children (model.Items |> List.map renderItem)
                ]
            ]
        ]



module FileExplorerExample =
    [<ReactComponent>]
    let Example () =
        let icons = FileExplorer.defaultIconPaths

        let initialItems: FileItem list = [
            FileTree.createFile "resume.pdf" None icons.pdf
            {
                FileTree.createFolder "My Files" None icons.folder with
                    IsExpanded = true
                    Children =
                        Some [
                            FileTree.createFile "Project-final.psd" None icons.psd
                            {
                                FileTree.createFolder "Subfolder" None icons.folder with
                                    IsExpanded = true
                                    Children =
                                        Some [
                                            FileTree.createFile "nested-file-1.txt" None icons.txt
                                            FileTree.createFile "nested-file-2.md" None icons.markdown
                                            {
                                                FileTree.createFolder "NestedFolder" None icons.folder with
                                                    IsExpanded = true
                                                    Children =
                                                        Some [
                                                            FileTree.createFile "Project-2-final.psd" None icons.psd
                                                            FileTree.createFile "Project-3-final.psd" None icons.psd
                                                        ]
                                            }
                                        ]
                            }
                        ]
            }
            FileTree.createFile "notes.txt" None icons.txt
        ]

        let handleItemClick (item: FileItem) =
            Browser.Dom.console.log ("Clicked:", item.Name)

        let handleContextMenu (item: FileItem) = [
            {
                Label = "Rename"
                Icon = "edit"
                OnClick = fun () -> Browser.Dom.console.log ("Rename", item.Name)
                Disabled = None
            }
            {
                Label = "Delete"
                Icon = "delete"
                OnClick = fun () -> Browser.Dom.console.log ("Delete", item.Name)
                Disabled = None
            }
        ]

        Html.div [
            prop.className "swt:p-4"
            prop.children [
                Html.h2 [
                    prop.className "swt:text-2xl swt:font-bold swt:mb-4"
                    prop.text "File Explorer Demo"
                ]
                FileExplorer.FileExplorer(
                    initialItems = initialItems,
                    onItemClick = handleItemClick,
                    onContextMenu = handleContextMenu
                )
            ]
        ]