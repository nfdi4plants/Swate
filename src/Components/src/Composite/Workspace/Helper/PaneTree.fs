module Swate.Components.Composite.Workspace.Helper.PaneTree

open System
open Swate.Components.Composite.Workspace.Types

let private newPaneId () = Guid.NewGuid().ToString()

type Pane with

    /// Returns all leaf pane ids in the tree (1-4 values).
    static member leafIds(pane: Pane) : string list =
        match pane with
        | Pane.Leaf id -> [ id ]
        | Pane.Split(_, first, second, _) ->
            Pane.leafIds first @ Pane.leafIds second

    /// Returns the max depth of the tree. Root leaf = 0, each split adds 1. Max = 2.
    static member depth(pane: Pane) : int =
        match pane with
        | Pane.Leaf _ -> 0
        | Pane.Split(_, first, second, _) ->
            1 + max (Pane.depth first) (Pane.depth second)

    /// Checks if a leaf can be split in the given direction.
    /// Orthogonal constraint: splits must alternate direction.
    /// Cannot split if already at max depth AND trying to split in a direction
    /// that would exceed it.
    ///
    /// Examples:
    ///   Leaf("a") can split Horizontal → true
    ///   Leaf("a") can split Vertical → true
    ///   Split(Horizontal, Leaf("a"), Leaf("b")) — "a" can split Vertical → true
    ///   Split(Horizontal, Leaf("a"), Leaf("b")) — "a" can split Horizontal → false (already Horizontal at this depth)
    ///   Split(Horizontal, Split(Vertical, Leaf("a"), Leaf("c")), Leaf("b")) — "a" can split anything → false (depth 2 reached)
    static member canSplitLeaf(pane: Pane) (targetLeafId: string) (direction: SplitDirection) : bool =
        let rec findParentDirection (pane: Pane) (leafId: string) : SplitDirection option =
            match pane with
            | Pane.Leaf _ -> None
            | Pane.Split(dir, Pane.Leaf id, _, _) when id = leafId -> Some dir
            | Pane.Split(dir, _, Pane.Leaf id, _) when id = leafId -> Some dir
            | Pane.Split(_, first, second, _) ->
                match findParentDirection first leafId with
                | Some d -> Some d
                | None -> findParentDirection second leafId

        if not (Pane.leafIds pane |> List.contains targetLeafId) then
            false
        else
            match findParentDirection pane targetLeafId with
            | Some parentDir ->
                parentDir <> direction && Pane.depth pane < 2
            | None ->
                Pane.depth pane < 2

    /// Splits the leaf pane with the given id, replacing it with a Split node
    /// containing the original leaf and a new empty leaf.
    ///
    /// Examples:
    ///   splitLeaf Leaf("p1") Horizontal → Split(Horizontal, Leaf("p1"), Leaf("new-guid"), 0.5)
    ///   splitLeaf Split(Horizontal, Leaf("p1"), Leaf("p2")) by "p1" Vertical
    ///     → Split(Horizontal, Split(Vertical, Leaf("p1"), Leaf("new-guid"), 0.5), Leaf("p2"))
    ///   Returns None if leaf id not found or split exceeds depth constraints.
    static member splitLeaf
        (pane: Pane)
        (targetLeafId: string)
        (direction: SplitDirection)
        (newLeafId: string)
        : Pane option
        =
        if not (Pane.canSplitLeaf pane targetLeafId direction) then
            None
        else
            let rec split (pane: Pane) : Pane option =
                match pane with
                | Pane.Leaf id when id = targetLeafId ->
                    Pane.Split(direction, Pane.Leaf id, Pane.Leaf newLeafId, 0.5)
                    |> Some
                | Pane.Leaf _ -> None
                | Pane.Split(dir, first, second, ratio) ->
                    match split first with
                    | Some newFirst -> Pane.Split(dir, newFirst, second, ratio) |> Some
                    | None ->
                        match split second with
                        | Some newSecond -> Pane.Split(dir, first, newSecond, ratio) |> Some
                        | None -> None

            split pane

    /// Removes a leaf pane from the tree. If the leaf's parent is a Split,
    /// the sibling leaf replaces the Split. Returns None if this is the
    /// only pane (root leaf).
    ///
    /// Examples:
    ///   removeLeaf Split(Horizontal, Leaf("a"), Leaf("b")) "a" → Some Leaf("b")
    ///   removeLeaf Leaf("root") "root" → None
    ///   removeLeaf Split(Horizontal, Split(Vertical, Leaf("a"), Leaf("c")), Leaf("b")) "a"
    ///     → Some Split(Horizontal, Leaf("c"), Leaf("b"))
    static member removeLeaf(pane: Pane) (leafId: string) : Pane option =
        match pane with
        | Pane.Leaf id when id = leafId -> None
        | Pane.Leaf _ -> Some pane
        | Pane.Split(dir, first, second, _) ->
            match first with
            | Pane.Leaf id when id = leafId -> Some second
            | _ ->
                match second with
                | Pane.Leaf id when id = leafId -> Some first
                | _ ->
                    match Pane.removeLeaf first leafId with
                    | Some newFirst -> Pane.Split(dir, newFirst, second, 0.5) |> Some
                    | None ->
                        match Pane.removeLeaf second leafId with
                        | Some newSecond -> Pane.Split(dir, first, newSecond, 0.5) |> Some
                        | None -> None
