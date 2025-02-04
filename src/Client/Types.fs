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
        SelectedColumns: int Set []
        TemplateName: string option
    } with
        static member init(tablesCount: int) =
            {
                ImportType = ARCtrl.TableJoinOptions.Headers
                ImportMetadata = false
                ImportTables = []
                SelectedColumns = Array.init tablesCount (fun _ -> Set.empty<int>)
                TemplateName = None
            }

        static member updateSelectedColumns(selectedColumns: int Set [], tableIndex: int, columnIndex: int) =
            let selectedData = selectedColumns
            if Set.contains columnIndex selectedColumns.[tableIndex] then
                selectedColumns.[tableIndex] <- Set.remove columnIndex selectedColumns.[tableIndex]
            else
                selectedColumns.[tableIndex] <- Set.add columnIndex selectedColumns.[tableIndex]
            selectedData

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
