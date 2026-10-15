module Swate.Components.Shared.PathChildrenIndex

open System
open System.Collections.Generic

/// Maintains normalized parent-to-direct-child path relations.
type PathChildrenIndex() =
    let childrenByParent = Dictionary<string, HashSet<string>>()

    let normalizedParent path =
        PathHelpers.tryGetParentPath path
        |> Option.defaultValue ""
        |> PathHelpers.normalizePath

    let addPath path =
        let path = PathHelpers.normalizePath path

        if not (String.IsNullOrWhiteSpace path) then
            let parent = normalizedParent path

            match childrenByParent.TryGetValue parent with
            | true, children -> children.Add path |> ignore
            | false, _ -> childrenByParent.[parent] <- HashSet [ path ]

    member _.Rebuild(paths: seq<string>) =
        childrenByParent.Clear()

        paths |> Seq.iter addPath

    member _.Add(path: string) = addPath path

    member _.Remove(path: string) =
        let path = PathHelpers.normalizePath path

        if not (String.IsNullOrWhiteSpace path) then
            let parent = normalizedParent path

            match childrenByParent.TryGetValue parent with
            | true, children ->
                children.Remove path |> ignore

                if children.Count = 0 then
                    childrenByParent.Remove parent |> ignore
            | false, _ -> ()

        childrenByParent.Remove path |> ignore

    member _.GetDirectChildPaths(directoryPath: string) =
        match childrenByParent.TryGetValue(PathHelpers.normalizePath directoryPath) with
        | true, children -> children |> Seq.toArray
        | false, _ -> [||]

    member this.CollectSubtreePaths(rootPaths: seq<string>) =
        let pending, visited, paths =
            Stack<string>(), HashSet<string>(), ResizeArray<string>()

        rootPaths |> Seq.iter (PathHelpers.normalizePath >> pending.Push)

        while pending.Count > 0 do
            let path = pending.Pop()

            if visited.Add path then
                paths.Add path
                this.GetDirectChildPaths path |> Seq.iter pending.Push

        paths.ToArray()
