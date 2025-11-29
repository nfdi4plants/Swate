namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz

open Swate.Components.FileExplorer.FileTreeDataStructures

// ============================================================================
// FILE EXPLORER COMPONENT
// ============================================================================
[<Mangle(false); Erase>]
type FileExplorer =

    static member defaultIconPaths = {|
        file =
            "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"
        folder =
            "M2.25 12.75V12A2.25 2.25 0 014.5 9.75h15A2.25 2.25 0 0121.75 12v.75m-8.69-6.44l-2.12-2.12a1.5 1.5 0 00-1.061-.44H4.5A2.25 2.25 0 002.25 6v12a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9a2.25 2.25 0 00-2.25-2.25h-5.379a1.5 1.5 0 01-1.06-.44z"
        pdf = "M12 2a2 2 0 0 1 2 2v16a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h6zm0 0v6h6M12 2l6 6" // PDF
        psd =
            "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" // PSD
        txt =
            "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" // TXT
    |}

    static member initialData: FileItem list =
        let icons = FileExplorer.defaultIconPaths

        [
            {
                Id = "1"
                Name = "resume.pdf"
                IconPath = icons.pdf
                IsExpanded = false
                Children = None
            }
            {
                Id = "2"
                Name = "My Files"
                IconPath = icons.folder
                IsExpanded = true
                Children =
                    Some [
                        {
                            Id = "3"
                            Name = "Project-final.psd"
                            IconPath = icons.psd
                            IsExpanded = false
                            Children = None
                        }
                        {
                            Id = "4"
                            Name = "Project-final-2.psd"
                            IconPath = icons.psd
                            IsExpanded = true
                            Children =
                                Some [
                                    {
                                        Id = "5"
                                        Name = "nested-file-1.psd"
                                        IconPath = icons.psd
                                        IsExpanded = false
                                        Children = None
                                    }
                                    {
                                        Id = "6"
                                        Name = "nested-file-2.psd"
                                        IconPath = icons.psd
                                        IsExpanded = false
                                        Children = None
                                    }
                                ]
                        }
                    ]
            }
            {
                Id = "7"
                Name = "notes.txt"
                IconPath = icons.txt
                IsExpanded = false
                Children = None
            }
        ]


    [<ReactComponent>]
    static member FileExplorer(?initialItems: FileItem list) =
        let items, setItems =
            React.useState (defaultArg initialItems FileExplorer.initialData)

        let selectedId, setSelectedId = React.useState<string option> (None)
        let breadcrumbPath, setBreadcrumbPath = React.useState<FileItem list> ([])

        let handleToggle itemId =
            setItems (FileTree.toggleExpanded itemId items)

        let handleSelect itemId =
            setSelectedId (Some itemId)

            match FileTree.getPath itemId items [] with
            | Some path -> setBreadcrumbPath path
            | None -> setBreadcrumbPath []

        let handleNavigate =
            React.useCallback (
                (fun itemId ->
                    if itemId = "" then
                        setBreadcrumbPath []
                        setSelectedId None
                    else
                        handleSelect itemId
                ),
                [| box items |]
            )

        let icon (path: string) =
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
                        svg.className "h-4 w-4"
                        svg.children [ Svg.path [ svg.d path; svg.fill "currentColor" ] ]
                    ]
                ]
            ]


        let rec renderItem item =
            let isSelected = selectedId = Some item.Id
            let selectedClass = if isSelected then "swt:bg-base-300" else ""

            match item.Children with
            | Some children ->
                Html.li [
                    prop.key item.Id

                    prop.children [
                        Html.details [
                            prop.custom ("open", item.IsExpanded)
                            prop.children [
                                Html.summary [
                                    prop.className ("swt:px-2 swt:py-1 " + selectedClass)

                                    prop.onClick (fun ev ->
                                        ev.preventDefault ()
                                        ev.stopPropagation ()
                                        handleToggle item.Id
                                        handleSelect item.Id
                                    )

                                    prop.children [
                                        Html.div [
                                            prop.className "swt:flex swt:items-center swt:gap-2 swt:cursor-pointer"
                                            prop.children [
                                                icon item.IconPath
                                                Html.span [
                                                    prop.text item.Name
                                                    prop.onClick (fun _ -> handleSelect item.Id)
                                                ]
                                            ]
                                        ]
                                    ]
                                ]

                                if item.IsExpanded then
                                    Html.ul [ prop.children (children |> List.map renderItem) ]
                                else
                                    Html.none
                            ]
                        ]
                    ]
                ]
            | None ->
                Html.li [
                    prop.key item.Id
                    prop.children [
                        Html.a [
                            prop.className ("swt:px-2 swt:py-1 " + selectedClass)
                            prop.onClick (fun _ -> handleSelect item.Id)
                            prop.children [ icon item.IconPath; Html.span item.Name ]
                        ]
                    ]
                ]

        Html.div [
            prop.className "swt:w-full swt:bg-base-100 swt:rounded-box swt:shadow-md"
            prop.children [
                if not (List.isEmpty breadcrumbPath) then
                    Breadcrumbs.Breadcrumbs(breadcrumbPath, handleNavigate)

                Html.ul [
                    prop.className "swt:menu swt:w-full swt:bg-base-200 swt:rounded-box"
                    prop.children (items |> List.map renderItem)
                ]
            ]
        ]

// ============================================================================
// USAGE EXAMPLE
// ============================================================================
[<ReactComponent>]
module FileExplorerExample =

    [<ReactComponent>]
    let Example () =
        let items, setItems = React.useState (FileExplorer.initialData)

        let addNewFile () =
            let newFile = FileTree.createFile "new.txt" FileExplorer.defaultIconPaths.file
            setItems (items @ [ newFile ])

        let addToFolder pid =
            let newFile = FileTree.createFile "nested.txt" FileExplorer.defaultIconPaths.file
            setItems (FileTree.addChild pid newFile items)

        Html.div [
            prop.className "swt:p-4"
            prop.children [
                Html.div [
                    prop.className "swt:mb-4 swt:space-x-2"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.text "Add File to Root"
                            prop.onClick (fun _ -> addNewFile ())
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-secondary swt:btn-sm"
                            prop.text "Add to My Files"
                            prop.onClick (fun _ -> addToFolder "2")
                        ]
                    ]
                ]
                FileExplorer.FileExplorer(items)
            ]
        ]