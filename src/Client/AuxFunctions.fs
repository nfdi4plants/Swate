module AuxFunctions

open System.Text.RegularExpressions

/// (|Regex|_|) pattern input
let (|Regex|_|) pattern input =
    let m = Regex.Match(input, pattern)
    if m.Success then Some(m.Value)
    else None
