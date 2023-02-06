module Context

open Feliz
open Feliz.ReactApi

type SpreadsheetData = {
    State: Map<int*int,string>
    SetState: Map<int*int,string> -> unit
} with
    static member private m =
        [
            for i in 0 .. 20 do
                for j in 0 .. 20 do
                    yield (i,j), sprintf "%i - %i" i j
        ] |> Map.ofList
    static member init = {
        State = SpreadsheetData.m
        SetState = fun _ -> ()
    }
    static member create state setState = {
        State = state
        SetState = setState
    }



let SpreadsheetDataCtx = React.createContext(nameof(SpreadsheetData), SpreadsheetData.init)