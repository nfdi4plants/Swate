[<AutoOpen>]
module Main.RecentArcStore

open System
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

[<Literal>]
let maxNumberRecentArcs = 5

module private Helpers =

    let toPointer (name: string) (path: string) (isActive: bool) =
        ARCPointer.create (name, path, isActive)

    let sanitize (arcs: ARCPointer[]) =
        arcs
        |> Array.filter (fun arc -> not (String.IsNullOrWhiteSpace arc.path))
        |> Array.distinctBy (fun arc -> PathHelpers.normalizePath arc.path)
        |> fun xs ->
            if xs.Length > maxNumberRecentArcs then
                Array.take maxNumberRecentArcs xs
            else
                xs

    let tryDeserializeArcPointer (entry: obj) =
        try
            let path = entry?path |> unbox<string>

            if String.IsNullOrWhiteSpace path then
                None
            else
                let nameValue =
                    if isNull entry?name then
                        ""
                    else
                        entry?name |> unbox<string>

                let isActive =
                    // if isNull entry?isActive then
                    //     false
                    // else
                    //     entry?isActive |> unbox<bool>
                    false // For safety, we do not trust the persisted value and will always initialize as inactive. The active ARC will be determined at runtime based on the currently open ARC.

                let name =
                    if String.IsNullOrWhiteSpace nameValue then
                        PathHelpers.getFileName path
                    else
                        nameValue

                Some(toPointer name path isActive)
        with _ ->
            None

    let persist (arcs: ARCPointer[]) =
        let serializable =
            arcs
            |> Array.map (fun arc ->
                createObj [
                    "name" ==> arc.name
                    "path" ==> arc.path
                    "isActive" ==> false
                ]
            )

        let payload = JS.JSON.stringify serializable
        writeSettingsFileAtomic recentArcsSettingsFileName payload

    let load () =
        try
            match tryReadSettingsFile recentArcsSettingsFileName with
            | None -> [||]
            | Some content when String.IsNullOrWhiteSpace content -> [||]
            | Some content -> JS.JSON.parse content |> unbox<obj[]> |> Array.choose tryDeserializeArcPointer
        with _ -> [||]

type RecentARCStore() =

    member val private RecentArcsState: ARCPointer[] = Helpers.load () |> Helpers.sanitize with get, set

    member private this.SetState(newArcs: ARCPointer[]) =
        this.RecentArcsState <- Helpers.sanitize newArcs
        Helpers.persist this.RecentArcsState

    member this.Get() = this.RecentArcsState

    /// This function will add a new ARC to the recent ARCs list or update an existing one to be active and be moved to the front of the list.
    member this.Add(path: string) =
        if String.IsNullOrWhiteSpace path then
            this.RecentArcsState
        else
            let arc =
                Helpers.toPointer (PathHelpers.getFileName path) path true

            let remainingArcs =
                this.RecentArcsState |> Array.filter (fun arc -> not (pathsEqual arc.path path))

            let next = Array.append [| arc |] remainingArcs
            this.SetState next
            this.RecentArcsState

    member this.Remove(path: string) =
        if String.IsNullOrWhiteSpace path then
            this.RecentArcsState
        else
            this.RecentArcsState
            |> Array.filter (fun arc -> not (pathsEqual arc.path path))
            |> this.SetState

            this.RecentArcsState

    member this.Inactivate(path: string) =
        if String.IsNullOrWhiteSpace path then
            this.RecentArcsState
        else
            this.RecentArcsState
            |> Array.map (fun arc ->
                if pathsEqual arc.path path then
                    Helpers.toPointer arc.name arc.path false
                else
                    arc
            )
            |> this.SetState

            this.RecentArcsState

let RECENT_ARCS = RecentARCStore()