namespace Components

open Fable
open Feliz
open Feliz.DaisyUI
open System

/// https://www.bekk.christmas/post/2021/02/how-to-lazy-render-large-data-tables-to-up-performance
type LazyLoadTable =

    /// <summary>
    ///
    /// </summary>
    /// <param name="tableName">Used to generate unique react keys</param>
    /// <param name="data"></param>
    /// <param name="headerRow"></param>
    /// <param name="rowHeight"></param>
    [<ReactComponent>]
    static member Main
        (
            tableName: string,
            data: 'a list[],
            createCell: 'a -> ReactElement list,
            ?headerRow:
                {|
                    data: 'b list
                    createCell: 'b -> ReactElement list
                |},
            ?rowHeight: float,
            ?maxTableHeight: int,
            ?tableClasses: string[],
            ?containerClasses: string[],
            ?rowLabel:
                {|
                    styling: (int -> ReactElement) option
                |}
        ) =
        let displayStart, setDisplayStart = React.useState (0)
        let displayEnd, setDisplayEnd = React.useState (0)
        let scrollPosition, setScrollPosition = React.useState (0.)
        let ref = React.useElementRef ()
        let RowHeight = defaultArg rowHeight 57.

        let ScreenHeight =
            Math.Max(Browser.Dom.document.documentElement.clientHeight, Browser.Dom.window.innerHeight)

        let Offset = ScreenHeight // We want to render more than we see, or else we will see nothing when scrolling fast
        let rowsToRender = Math.Floor((ScreenHeight + Offset) / RowHeight)

        let setDisplayPositions =
            React.useCallback (
                (fun (scroll: float) ->
                    // we want to start rendering a bit above the visible screen
                    let scrollWithOffset = Math.Ceiling(scroll - rowsToRender - Offset / 2.)
                    // start position should never be less than 0
                    let displayStartPosition =
                        Math.Round(Math.Max(0., Math.Ceiling(scrollWithOffset / RowHeight)))
                    // end position should never be larger than our data array
                    let displayEndPosition =
                        Math.Round(Math.Min(displayStartPosition + rowsToRender, data.Length - 1))
                    //
                    setDisplayStart (int displayStartPosition)
                    setDisplayEnd (int displayEndPosition)),
                [| box data.Length |]
            )
        //Attach a listener to the scroll event on the window. This function will run every time the scroll changes.
        React.useEffect (
            (fun () ->
                if ref.current.IsSome then
                    let onScroll =
                        throttleAndDebounce (
                            (fun _ ->
                                let scrollTop = ref.current.Value.scrollTop

                                if data.Length <> 0 then
                                    setScrollPosition scrollTop
                                    setDisplayPositions scrollTop),
                            100
                        )

                    ref.current.Value.addEventListener ("scroll", onScroll)

                    Some
                        { new IDisposable with
                            member this.Dispose() =
                                if ref.current.IsSome then
                                    ref.current.Value.removeEventListener ("scroll", onScroll)
                        }
                else
                    None),
            [| box setDisplayPositions; box data.Length |]
        )
        //We also need to make sure our calculations are run when we first render our page, even before we have started to scroll. So let's add this
        React.useEffect (
            (fun () -> setDisplayPositions scrollPosition),
            [| box scrollPosition; box setDisplayPositions |]
        )
        // add a filler row at the top. The further down we scroll the taller this will be
        let startRowFiller =
            let h = displayStart * int RowHeight

            Html.tr [
                prop.key $"lazy_load_table_{tableName}_Row_StartFiller"
                prop.style [ style.height h ]
            ]
        // add a filler row at the end. The further up we scroll the taller this will be
        let endRowfiller =
            let h = (data.Length - displayEnd) * int RowHeight

            Html.tr [
                prop.key $"lazy_load_table_{tableName}_Row_Endfiller"
                prop.style [ style.height h ]
            ]

        let createContentRow (index: int) (contentRow: 'a list) =
            Html.tr [
                prop.key $"lazy_load_table_{tableName}_Row_{index}"
                prop.children [
                    if rowLabel.IsSome && rowLabel.Value.styling.IsSome then
                        rowLabel.Value.styling.Value index
                    elif rowLabel.IsSome then
                        Html.th []
                    for content in contentRow do
                        yield! createCell content
                ]
            ]

        Html.div [
            prop.ref ref
            prop.key $"lazy_load_table_{tableName}"
            if containerClasses.IsSome then
                prop.className containerClasses.Value
            prop.style [
                if maxTableHeight.IsSome then
                    style.maxHeight maxTableHeight.Value
                else
                    style.maxHeight (length.perc 100)
                style.overflowY.auto
                style.overflowX.auto
                style.flexGrow 1
            ]
            prop.children [
                Html.table [
                    prop.className [
                        if tableClasses.IsSome then
                            tableClasses.Value |> String.concat " "
                        else
                            "swt:table"
                    ]
                    prop.children [
                        if headerRow.IsSome then
                            Html.thead [
                                Html.tr [
                                    prop.key $"lazy_load_table_{tableName}_Row_Header"
                                    prop.children [
                                        if rowLabel.IsSome && rowLabel.Value.styling.IsSome then
                                            rowLabel.Value.styling.Value -1
                                        elif rowLabel.IsSome then
                                            Html.th []
                                        for header in headerRow.Value.data do
                                            yield! headerRow.Value.createCell header
                                    ]
                                ]
                            ]
                        Html.tbody [
                            prop.children [
                                startRowFiller
                                match Array.tryItem displayStart data, Array.tryItem displayEnd data with
                                | Some _, Some _ ->
                                    for i in displayStart..displayEnd do
                                        createContentRow i data.[i]
                                | _ -> Html.none
                                endRowfiller
                            ]
                        ]
                    ]
                ]
            ]
        ]