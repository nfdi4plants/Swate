[<AutoOpen>]
module Main.RecentArcStore

open System
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components

[<Literal>]
let maxNumberRecentArcs = 5

module private Helpers =

    let getNameFromPath (path: string) =
        path
        |> (fun p -> p.Replace("\\", "/"))
        |> (fun p -> p.TrimEnd('/'))
        |> (fun p -> p.Split("/"))
        |> Array.last

    let normalizePath (path: string) =
        path.Replace("\\", "/").Trim().TrimEnd('/').ToLowerInvariant()

    let pathsEqual (left: string) (right: string) =
        normalizePath left = normalizePath right

    let toPointer (name: string) (path: string) (isActive: bool) =
        SelectorTypes.ARCPointer.create (name, path, isActive)

    let toInactivePointer (arc: SelectorTypes.ARCPointer) = toPointer arc.name arc.path false

    let sanitize (arcs: SelectorTypes.ARCPointer[]) =
        arcs
        |> Array.filter (fun arc -> not (String.IsNullOrWhiteSpace arc.path))
        |> Array.distinctBy (fun arc -> normalizePath arc.path)
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
                    if isNull entry?isActive then
                        false
                    else
                        entry?isActive |> unbox<bool>

                let name =
                    if String.IsNullOrWhiteSpace nameValue then
                        getNameFromPath path
                    else
                        nameValue

                Some(toPointer name path isActive)
        with _ ->
            None

    let persist (arcs: SelectorTypes.ARCPointer[]) =
        let serializable =
            arcs
            |> Array.map (fun arc ->
                createObj [
                    "name" ==> arc.name
                    "path" ==> arc.path
                    "isActive" ==> arc.isActive
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

    member val private RecentArcsState: SelectorTypes.ARCPointer[] = Helpers.load () |> Helpers.sanitize with get, set

    member private this.SetState(newArcs: SelectorTypes.ARCPointer[]) =
        this.RecentArcsState <- Helpers.sanitize newArcs
        Helpers.persist this.RecentArcsState

    member this.Get() = this.RecentArcsState

    member this.Add(path: string) =
        if String.IsNullOrWhiteSpace path then
            this.RecentArcsState
        else
            let maybeExisting =
                this.RecentArcsState
                |> Array.tryFind (fun arc -> Helpers.pathsEqual arc.path path)

            let activeName =
                maybeExisting
                |> Option.map _.name
                |> Option.defaultValue (Helpers.getNameFromPath path)

            let activeArc = Helpers.toPointer activeName path true

            let remainingInactiveArcs =
                this.RecentArcsState
                |> Array.filter (fun arc -> not (Helpers.pathsEqual arc.path path))
                |> Array.map Helpers.toInactivePointer

            let next = Array.append [| activeArc |] remainingInactiveArcs
            this.SetState next
            this.RecentArcsState

    member this.Remove(path: string) =
        if String.IsNullOrWhiteSpace path then
            this.RecentArcsState
        else
            this.RecentArcsState
            |> Array.filter (fun arc -> not (Helpers.pathsEqual arc.path path))
            |> this.SetState

            this.RecentArcsState

    member this.Inactivate(path: string) =
        if String.IsNullOrWhiteSpace path then
            this.RecentArcsState
        else
            this.RecentArcsState
            |> Array.map (fun arc ->
                if Helpers.pathsEqual arc.path path then
                    Helpers.toPointer arc.name arc.path false
                else
                    arc
            )
            |> this.SetState

            this.RecentArcsState

let RECENT_ARCS = RecentARCStore()