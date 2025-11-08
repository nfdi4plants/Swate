module Components.Tests.Table.SafariTableView

open Fable.Mocha
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Fable.React
open Swate.Components


let globalThis: obj = JS.eval ("globalThis")

let ensureWindowExists () =
    // For if window is missing
    if isNull (globalThis?window) then
        globalThis?window <- createObj [ "navigator" ==> createObj [] ]

    // For if document is missing
    if isNull (globalThis?document) then
        globalThis?document <-
            createObj [
                "body" ==> createObj [ "appendChild" ==> (fun (_: obj) -> ()) ]
                "createElement"
                ==> (fun (_: string) ->
                    // Minimal element with style + querySelector returning self
                    let style = createObj []

                    createObj [
                        "style" ==> style
                        "querySelector" ==> (fun (_: string) -> createObj [ "style" ==> style ])
                    ]
                )
            ]

ensureWindowExists ()

let mockSafariUserAgent () =
    let nav = unbox<obj> window?navigator
    nav?userAgent <- "Mozilla/5.0 (Macintosh; Intel Mac OS X) AppleWebKit/605.1.15 Safari/605.1.15"

let mockChromeUserAgent () =
    let nav = unbox<obj> window?navigator
    nav?userAgent <- "Mozilla/5.0 Chrome/118 Safari/605.1.15"

let dummyCell _ = Html.div []
let dummyActive _ = Html.div []

let renderTable (container: Browser.Types.HTMLElement) (table: ReactElement) =
    let root = ReactDOM.createRoot (container)
    root.render (table)

let getTableContainerStyle (container: Browser.Types.HTMLElement) =
    let el = unbox<obj> (container.querySelector ("[key='table-container']"))
    el?style

let tests =
    testList "Safari Table View" [

        testCase "Table uses Safari styles when userAgent is Safari"
        <| fun _ ->
            mockSafariUserAgent ()

            let table =
                Table.Table(
                    rowCount = 3,
                    columnCount = 3,
                    renderCell = dummyCell,
                    renderActiveCell = dummyActive,
                    height = 200,
                    width = 200,
                    debug = true
                )

            let container = document.createElement "div" :?> Browser.Types.HTMLElement
            document.body.appendChild (container) |> ignore

            renderTable container table

            let style = getTableContainerStyle container

            Expect.equal style?willChange "transform" "Expected willChange transform for Safari"
            Expect.isTrue (style?minHeight.ToString().EndsWith("px")) "minHeight should be px"
            Expect.isTrue (style?minWidth.ToString().EndsWith("px")) "minWidth should be px"
            Expect.equal style?contain "size layout paint" "Expected contain property for Safari"

        testCase "Table uses width/height styles when not Safari"
        <| fun _ ->
            mockChromeUserAgent ()

            let table =
                Table.Table(
                    rowCount = 3,
                    columnCount = 3,
                    renderCell = dummyCell,
                    renderActiveCell = dummyActive,
                    height = 200,
                    width = 200,
                    debug = true
                )

            let container = document.createElement "div" :?> Browser.Types.HTMLElement
            document.body.appendChild (container) |> ignore

            renderTable container table

            let style = getTableContainerStyle container

            let height = unbox<string> style?height
            let width = unbox<string> style?width

            Expect.notEqual (unbox<string> style?willChange) "transform" "Should not use Safari willChange style"
            Expect.isTrue (height.Length > 0) "Height should be set"
            Expect.isTrue (width.Length > 0) "Width should be set"
    ]

let Main = testList "SafariTableView" [ tests ]