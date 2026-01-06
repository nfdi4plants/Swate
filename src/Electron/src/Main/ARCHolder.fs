[<AutoOpen>]
module Main.ARCHolder

open Swate.Components

let mutable recentARCs: ARCPointer[] = [||]

let setRecentARCs(newARCs: ARCPointer[]) = recentARCs <- newARCs

let ARCPointerExists (path: string) =
    recentARCs
    |> Array.exists (fun arcPointer -> arcPointer.path = path)

let reorderARCPointers (path: string) =
    let filteredRecentARCs =
        recentARCs
        |> Array.filter (fun arc -> arc.path <> path)
        |> Array.map (fun arc -> ARCPointer.create(arc.name, arc.path, false))
    Array.append filteredRecentARCs [| ARCPointer.create(path, path, true) |]

let createNewARCPointers (currentARC: ARCPointer) (recentARCs: ARCPointer []) maxNumberRecentARCs =
    if recentARCs.Length = maxNumberRecentARCs then
        let tmp = recentARCs.[1..]
        Array.append tmp [| currentARC |]
    else
        Array.append recentARCs [| currentARC |]

let updateRecentARCs path maxNumberRecentARCs =
    if ARCPointerExists path then
        let reorderedARCPointers = reorderARCPointers path
        setRecentARCs reorderedARCPointers
        reorderedARCPointers
    else
        let newARCPointers =
            let newARCPointer = ARCPointer.create(path, path, true)
            let tmp =
                recentARCs
                |> Array.map (fun arc -> ARCPointer.create(arc.name, arc.path, false))
            createNewARCPointers newARCPointer tmp maxNumberRecentARCs
        setRecentARCs newARCPointers
        newARCPointers
