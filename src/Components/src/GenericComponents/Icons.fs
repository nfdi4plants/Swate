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