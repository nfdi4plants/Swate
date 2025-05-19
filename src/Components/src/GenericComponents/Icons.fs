namespace Swate.Components

open Feliz

type Icons =

    static member AddBuildingBlock =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-circle-plus" ]
            Html.i [ prop.className "fa-solid fa-table-columns" ]
        ]

    static member AddTemplate =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-circle-plus" ]
            Html.i [ prop.className "fa-solid fa-table" ]
        ]

    static member FilePicker =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-file-signature" ]
        ]

    static member DataAnnotator =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-object-group" ]
        ]

    static member FileExport =
        React.fragment[
            Html.i [ prop.className "fa-solid fa-file-export" ]
        ]

    static member Terms =
        React.fragment[
            Html.i [ prop.className "fa-solid fa-magnifying-glass-plus" ]
        ]

    static member Templates =
        React.fragment[
            Html.i [ prop.className "fa-solid fa-circle-plus" ]
            Html.i [ prop.className "fa-solid fa-table" ]
        ]

    static member Settings =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-cog" ]
        ]

    static member About =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-question-circle" ]
        ]

    static member PrivacyPolicy =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-fingerprint" ]
        ]

    static member Docs =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-book" ]
        ]

    static member Contact =
        React.fragment [
            Html.i [ prop.className "fa-solid fa-comments" ]
        ]
