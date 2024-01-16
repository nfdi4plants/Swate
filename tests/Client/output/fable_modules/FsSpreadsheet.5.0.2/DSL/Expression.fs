namespace FsSpreadsheet.DSL


module Expression =

    [<NoComparison; NoEquality; Sealed>]
    type OptionalSource<'T>(s : 'T) =
        member this.Source = s

    [<NoComparison; NoEquality; Sealed>]
    type RequiredSource<'T>(s : 'T) =
        member this.Source = s

    [<NoComparison; NoEquality; Sealed>]
    type ExpressionSource<'T>(s : 'T) =

        member this.Source = s

    #if FABLE_COMPILER
    #else
    open Microsoft.FSharp.Linq.RuntimeHelpers

    let eval<'T> q = LeafExpressionConverter.EvaluateQuotation q :?> 'T
    #endif
