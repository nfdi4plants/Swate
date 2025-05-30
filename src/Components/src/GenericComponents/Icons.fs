namespace Swate.Components

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type Icons =

    static member BuildingBlock() =
        Html.span [
            Html.i [ prop.className "fa-solid fa-circle-plus" ]
            Html.i [ prop.className "fa-solid fa-table-columns" ]
        ]

    static member FilePicker() =
        Html.i [ prop.className "fa-solid fa-file-signature" ]

    static member DataAnnotator() =
        Html.i [ prop.className "fa-solid fa-object-group" ]

    static member FileImport() =
        Html.i [ prop.className "fa-solid fa-file-import" ]

    static member FileExport() =
        Html.i [ prop.className "fa-solid fa-file-export" ]

    static member Terms() =
        Html.i [ prop.className "fa-solid fa-magnifying-glass-plus" ]

    static member Templates() =
        Html.span [
            Html.i [ prop.className "fa-solid fa-circle-plus" ]
            Html.i [ prop.className "fa-solid fa-table" ]
        ]

    static member Settings() =
        Html.i [ prop.className "fa-solid fa-cog" ]

    static member About() =
        Html.i [ prop.className "fa-solid fa-question-circle" ]

    static member PrivacyPolicy() =
        Html.i [ prop.className "fa-solid fa-fingerprint" ]

    static member Docs() =
        Html.i [ prop.className "fa-solid fa-book" ]

    static member Contact() =
        Html.i [ prop.className "fa-solid fa-comments" ]

    static member Save() =
        Html.i [ prop.className "fa-solid fa-floppy-disk" ]

    static member Delete() =
        Html.i [ prop.className "fa-solid fa-trash-can" ]

    static member Forward() =
        Html.i [ prop.className "fa-solid fa-rotate-right" ]

    static member Back() =
        Html.i [ prop.className "fa-solid fa-rotate-left" ]

    static member BuildingBlockInformation() =
        Html.span [
            Html.i [ prop.className "fa-solid fa-question pr-1" ]
            Html.i [ prop.className "fa-solid fa-table-columns" ]
        ]

    static member RemoveBuildingBlock() =
        Html.span [
            Html.i [ prop.className "fa-solid fa-minus pr-1" ]
            Html.i [ prop.className "fa-solid fa-table-columns" ]
        ]

    static member RectifyOntologyTerms(reactElement: ReactElement) =
        Html.span [
            Html.i [ prop.className "fa-solid fa-spell-check" ]
            reactElement
            Html.i [ prop.className "fa-solid fa-pen" ]
        ]

    static member AutoformatTable() =
        Html.i [ prop.className "fa-solid fa-rotate" ]

    static member CreateAnnotationTable() =
        Html.span [
            Html.i [ prop.className "fa-solid fa-plus" ]
            Html.i [ prop.className "fa-solid fa-table" ]
        ]

    static member CreateMetadata() =
        Html.span [
            Html.i [ prop.className "fa-solid fa-plus" ]
            Html.i [ prop.className "fa-solid fa-info" ]
        ]