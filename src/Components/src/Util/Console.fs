[<AutoOpen>]
module ConsoleHelpers

module console =
    let log (a) = Browser.Dom.console.log a
    let warn (a) = Browser.Dom.console.warn a
    let error (a) = Browser.Dom.console.error a