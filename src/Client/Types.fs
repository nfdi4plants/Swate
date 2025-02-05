[<AutoOpen>]
module Types

open ARCtrl

module JsonImport =

    [<RequireQualifiedAccess>]
    type ImportTable = {
        Index: int
        /// If FullImport is true, the table will be imported in full, otherwise it will be appended to active table.
        FullImport: bool
    }

    type SelectiveImportModalState = {
        ImportType: ARCtrl.TableJoinOptions
        ImportMetadata: bool
        ImportTables: ImportTable list
        DeSelectedColumns: Set<int*int>
        TemplateName: string option
    } with
        static member init() =
            {
                ImportType = ARCtrl.TableJoinOptions.Headers
                ImportMetadata = false
                ImportTables = []
                DeSelectedColumns = Set.empty
                TemplateName = None
            }

        static member updateDeSelectedColumns(deSelectedColumns: Set<int*int>, tableIndex: int, columnIndex: int) =
            if Set.contains (columnIndex, tableIndex) deSelectedColumns then
                Set.remove (columnIndex, tableIndex) deSelectedColumns
            else
                Set.add (columnIndex, tableIndex) deSelectedColumns

        static member getDeSelectedTableColumns (deSelectedColumns: Set<int*int>, tableIndex: int) =
            deSelectedColumns
            |> List.ofSeq
            |> List.map (fun item -> if (fst item) = tableIndex then Some (snd item) else None)
            |> List.choose (fun item -> item)

open Fable.Core

type Style =
    {
        classes: U2<string, string []>
        subClasses: Map<string, Style> option
    } with
        static member init(classes: string, ?subClasses: Map<string, Style>) =
            {
                classes = U2.Case1 classes
                subClasses = subClasses
            }
        static member init(classes: string [], ?subClasses: Map<string, Style>) =
            {
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
