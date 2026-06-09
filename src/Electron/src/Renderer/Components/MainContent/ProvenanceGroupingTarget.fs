module Renderer.Components.MainContent.ProvenanceGroupingTarget

open Feliz

[<ReactComponent>]
let ProvenanceGroupingTarget () =
    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-auto swt:p-4"
        prop.testId "provenance-grouping-page"
        prop.text "Loading provenance grouping..."
    ]
