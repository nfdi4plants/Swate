module Routing

open Elmish.UrlParser

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

