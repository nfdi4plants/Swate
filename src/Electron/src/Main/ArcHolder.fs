[<AutoOpen>]
module Main.ARCHolder

open Swate.Components

let mutable recentARCs: SelectorTypes.ARCPointer[] = [||]

let setRecentARCs (newARCs: SelectorTypes.ARCPointer[]) = recentARCs <- newARCs

let mutable maxNumberRecentARCs = 5

let setMaxNumberRecentARCs (newMaxNumberRecentARCs: int) =
    maxNumberRecentARCs <- newMaxNumberRecentARCs

let getNameFromPath (path: string) =
    path |> (fun p -> p.Replace("\\", "/")) |> (fun p -> p.Split("/")) |> Array.last

let ARCPointerExists (path: string) =
    recentARCs |> Array.exists (fun arcPointer -> arcPointer.path = path)

let reorderARCPointers (path: string) =
    let filteredRecentARCs =
        recentARCs
        |> Array.filter (fun arc -> arc.path <> path)
        |> Array.map (fun arc -> SelectorTypes.ARCPointer.create (arc.name, arc.path, false))

    Array.append
        [|
            SelectorTypes.ARCPointer.create (getNameFromPath path, path, true)
        |]
        filteredRecentARCs

let createNewARCPointers
    (currentARC: SelectorTypes.ARCPointer)
    (recentARCs: SelectorTypes.ARCPointer[])
    maxNumberRecentARCs
    =
    if recentARCs.Length = maxNumberRecentARCs then
        let tmp = Array.take (maxNumberRecentARCs - 1) recentARCs
        Array.append [| currentARC |] tmp
    else
        Array.append [| currentARC |] recentARCs

let updateRecentARCs path maxNumberRecentARCs =
    if ARCPointerExists path then
        let reorderedARCPointers = reorderARCPointers path
        setRecentARCs reorderedARCPointers
        reorderedARCPointers
    else
        let newARCPointers =
            let newARCPointer =
                SelectorTypes.ARCPointer.create (getNameFromPath path, path, true)

            let tmp =
                recentARCs
                |> Array.map (fun arc -> SelectorTypes.ARCPointer.create (arc.name, arc.path, false))

            createNewARCPointers newARCPointer tmp maxNumberRecentARCs

        setRecentARCs newARCPointers
        newARCPointers