module Spreadsheet.Controller

open System.Collections.Generic
open Shared.TermTypes

module Map =
    let maxKeyValue (m:Map<'Key,'Value>) =
        m.Keys |> Seq.max

let createAnnotationTable (state: Spreadsheet.Model) : Spreadsheet.Model =
    let newIndex = if Map.isEmpty state.Tables then 0 else state.Tables |> Map.maxKeyValue |> (+) 1
    let newTables = state.Tables.Add(newIndex, Table.init())
    { state with
        Tables = newTables
        ActiveTableIndex = newIndex
        ActiveTable = Map([
            for i in 0 .. 10 do
                for j in 0 .. 10 do
                    yield (i,j), TermMinimal.create (sprintf "%i - %i" i j) "user-specific"
        ])
    }
    