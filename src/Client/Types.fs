[<AutoOpen>]
module Types

open ARCtrl
open Fable.Core

module Feliz =

    [<RequireQualifiedAccess>]
    type GenericApiState<'s> =
        | Idle
        | Loading
        | Ok of 's
        | Error of exn

module FileImport =

    [<RequireQualifiedAccess>]
    type ImportTable = {
        Index: int
        /// If FullImport is true, the table will be imported in full, otherwise it will be appended to active table.
        FullImport: bool
    }

    type SelectiveImportConfig = {
        ImportType: ARCtrl.TableJoinOptions
        ImportMetadata: bool
        ImportTables: ImportTable list
        DeselectedColumns: Set<int * int>
        TemplateName: string option
    } with

        static member init() = {
            ImportType = ARCtrl.TableJoinOptions.Headers
            ImportMetadata = false
            ImportTables = []
            DeselectedColumns = Set.empty
            TemplateName = None
        }

        member this.toggleDeselectColumn(tableIndex: int, columnIndex: int) : SelectiveImportConfig = {
            this with
                DeselectedColumns =
                    if Set.contains (tableIndex, columnIndex) this.DeselectedColumns then
                        Set.remove (tableIndex, columnIndex) this.DeselectedColumns
                    else
                        Set.add (tableIndex, columnIndex) this.DeselectedColumns
        }

open Fable.Core

type Style = {
    classes: U2<string, string[]>
    subClasses: Map<string, Style> option
} with

    static member init(classes: string, ?subClasses: Map<string, Style>) = {
        classes = U2.Case1 classes
        subClasses = subClasses
    }

    static member init(classes: string[], ?subClasses: Map<string, Style>) = {
        classes = U2.Case2 classes
        subClasses = subClasses
    }

    member this.StyleString =
        match this.classes with
        | U2.Case1 style -> style
        | U2.Case2 styleArr -> styleArr |> String.concat " "

    member this.TryGetSubclass(name: string) =
        match this.subClasses with
        | Some subClasses -> subClasses.TryFind name
        | None -> None

    member this.GetSubclassStyle(name: string) =
        match this.subClasses with
        | Some subClasses -> subClasses.TryFind name
        | None -> None
        |> Option.map _.StyleString
        |> Option.defaultValue ""