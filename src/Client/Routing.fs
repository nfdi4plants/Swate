module Routing

open Elmish.UrlParser
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Route =
| Home
| AddBuildingBlock
| TermSearch
| FilePicker
| ActivityLog
| NotFound

    static member toRouteUrl (route:Route) =
        match route with
        | Route.Home                 -> "/"
        | Route.AddBuildingBlock     -> "/#AddBuildingBlock"
        | Route.TermSearch           -> "/#TermSearch"
        | Route.FilePicker           -> "/#FilePicker"
        | Route.ActivityLog          -> "/#ActivityLog"
        | Route.NotFound             -> "/#NotFound"

    static member toString (route:Route) =
        match route with
        | Route.Home                 -> ""
        | Route.AddBuildingBlock     -> "AddBuildingBlock"
        | Route.TermSearch           -> "TermSearch"
        | Route.FilePicker           -> "FilePicker"
        | Route.ActivityLog          -> "ActivityLog"
        | Route.NotFound             -> "NotFound"

    static member toIcon (p: Route)=
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

        match p with
        | Route.Home             -> createElem [Fa.Solid.Home    ] (p |> Route.toString)
        | Route.TermSearch       -> createElem [Fa.Solid.SearchPlus   ] (p |> Route.toString)
        | Route.AddBuildingBlock -> createElem [Fa.Solid.Columns; Fa.Solid.PlusCircle ] (p |> Route.toString)
        | Route.FilePicker       -> createElem [Fa.Solid.FileUpload; ] (p |> Route.toString)
        | Route.ActivityLog      -> createElem [Fa.Solid.History   ] (p |> Route.toString)
        | _  -> Fa.i [Fa.Solid.QuestionCircle]   []

///explained here: https://elmish.github.io/browser/routing.html
//let curry f x y = f (x,y)

module Routing =

    open Elmish.UrlParser
    open Elmish.Navigation

    /// The URL is turned into a Result.
    let route : Parser<Route -> Route,_> =
        oneOf [
            map Route.Home               (s "")
            map Route.TermSearch         (s "TermSearch")
            map Route.AddBuildingBlock   (s "AddBuildingBlock")
            map Route.FilePicker         (s "FilePicker")
            map Route.ActivityLog        (s "ActivityLog")
            map Route.NotFound           (s "NotFound")
        ]

    //this would be the way to got if we would use push based routing, but i decided to use hash based routing. Ill leave this here for now as a note.
    //let urlParser location = parsePath pageParser location


