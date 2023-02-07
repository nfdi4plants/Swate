module Context

open Feliz
open Feliz.ReactApi

let mutable SpreadsheetData_collector : Map<int*int,string> = Map.empty

type SpreadsheetData = {
    State: Map<int*int,string>
    SetState: Map<int*int,string> -> unit
} with
    static member TestMap =
        [
            for i in 0 .. 20 do
                for j in 0 .. 20 do
                    yield (i,j), sprintf "%i - %i" i j
        ] |> Map.ofList
    static member init = {
        State = SpreadsheetData.TestMap
        SetState = fun _ -> ()
    }
    static member create state setState = {
        State = state
        SetState = setState
    }



let SpreadsheetDataCtx = React.createContext(nameof(SpreadsheetData), SpreadsheetData.init)