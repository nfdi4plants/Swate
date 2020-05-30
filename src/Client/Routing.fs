module Routing

open Elmish.UrlParser
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Page =
    | Home
    | TermSearch
    | AddBuildingBlock
    | FilePicker
    | ActivityLog
    | NotFound

    static member toPath = function
        | Page.Home                 -> "/"
        | Page.TermSearch           -> "/#TermSearch"
        | Page.AddBuildingBlock     -> "/#AddBuildingBlock"
        | Page.FilePicker           -> "/#FilePicker"
        | Page.ActivityLog          -> "/#ActivityLog"
        | Page.NotFound             -> "/#NotFound"

    static member toString = function
        | Page.Home                 -> ""
        | Page.TermSearch           -> "TermSearch"
        | Page.AddBuildingBlock     -> "AddBuildingBlock"
        | Page.FilePicker           -> "FilePicker"
        | Page.ActivityLog          -> "ActivityLog"
        | Page.NotFound             -> "NotFound"

    static member toIcon =
        let createElem icons name =
            Fable.React.Standard.span [
                Fable.React.Props.Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
                Tooltip.dataTooltip (name)
            ] (
                icons
                |> List.map (
                    fun icon ->
                        Fa.span [icon] []
                )
            )

        fun (p: Page) ->
            match p with
            | p when p = Page.Home             -> createElem [Fa.Solid.Home    ] (p |> Page.toString)
            | p when p = Page.TermSearch       -> createElem [Fa.Solid.SearchPlus   ] (p |> Page.toString)
            | p when p = Page.AddBuildingBlock -> createElem [Fa.Solid.Columns; Fa.Solid.PlusCircle ] (p |> Page.toString)
            | p when p = Page.FilePicker       -> createElem [Fa.Solid.FileUpload; ] (p |> Page.toString)
            | p when p = Page.ActivityLog      -> createElem [Fa.Solid.History   ] (p |> Page.toString)
            | _  -> Fa.i [Fa.Solid.QuestionCircle]   []

/// The URL is turned into a Result.
let pageParser : Parser<Page -> Page,_> =
    oneOf [
        map Page.Home               (s "")
        map Page.TermSearch         (s "TermSearch")
        map Page.AddBuildingBlock   (s "AddBuildingBlock")
        map Page.FilePicker         (s "FilePicker")
        map Page.ActivityLog        (s "ActivityLog")
        map Page.NotFound           (s "NotFound")
    ]

//this would be the way to got if we would use push based routing, but i decided to use hash based routing. Ill leave this here for now as a note.
//let urlParser location = parsePath pageParser location

