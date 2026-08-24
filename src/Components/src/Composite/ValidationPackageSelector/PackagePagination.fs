namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Feliz
open Swate.Components
open Types

[<Erase; Mangle(false)>]
type PackagePagination =

    [<ReactComponent(true)>]
    static member PackagePagination(items: ValidationPackageDTO[], renderPage: ValidationPackageDTO[] -> ReactElement) =
        let page, setPage = React.useState 0
        let prevItems, setPrevItems = React.useState items

        // Render-time state adjustment: reset page whenever the filtered
        // items array changes identity (i.e. any filter changed). No useEffect.
        if not (obj.ReferenceEquals(prevItems, items)) then
            setPrevItems items
            setPage 0

        let pageItems =
            React.useMemo ((fun () -> Helper.slicePage items page), [| box items; box page |])

        let totalPages = Helper.pageCount items

        React.Fragment [
            Html.tbody [ renderPage pageItems ]
            Html.tfoot [
                Html.tr [
                    Html.td [
                        prop.colSpan 8
                        prop.className "swt:bg-base-100"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:items-center swt:justify-center swt:gap-6 swt:py-2"
                                prop.children [
                                    Html.button [
                                        prop.type' "button"
                                        prop.testId "validation-package-selector-prev"
                                        prop.className "swt:btn swt:btn-xs"
                                        prop.text "Prev"
                                        prop.disabled (page <= 0)
                                        prop.onClick (fun _ -> setPage (max 0 (page - 1)))
                                    ]
                                    Html.span [
                                        prop.testId "validation-package-selector-page-indicator"
                                        prop.className "swt:text-xs swt:text-base-content/70"
                                        prop.text $"Page {page + 1} of {totalPages}"
                                    ]
                                    Html.button [
                                        prop.type' "button"
                                        prop.testId "validation-package-selector-next"
                                        prop.className "swt:btn swt:btn-xs"
                                        prop.text "Next"
                                        prop.disabled (page + 1 >= totalPages)
                                        prop.onClick (fun _ -> setPage (page + 1))
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
