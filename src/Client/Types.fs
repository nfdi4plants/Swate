[<AutoOpen>]
module Types

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
    } with
        static member init() =
            {
                ImportType = ARCtrl.TableJoinOptions.Headers
                ImportMetadata = false
                ImportTables = []
            }