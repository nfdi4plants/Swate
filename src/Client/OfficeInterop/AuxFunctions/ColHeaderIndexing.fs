module OfficeInterop.Indexing

open Shared.OfficeInteropTypes

[<RequireQualifiedAccess>]
module Unit =

    /// This will create the column header attributes for a unit block.
    /// as unit always has to be a term and cannot be for example "Source" or "Sample", both of which have a differen format than for exmaple "Parameter [TermName]",
    /// we only need one function to generate id and bring the unit term in the right format.
    let createUnitColHeader (id:int) =
        match id with
        | 1             -> $"Unit"
        | anyOtherId    -> $"Unit (#{anyOtherId})" 

    /// This function will iterate through all existing headers to find the next available index
    let findNewIdForUnit (allColHeaders:string []) =
        let rec loopingCheck int =
            let isExisting =
                allColHeaders
                // Should a column with the same name already exist, then count up the id tag.
                |> Array.exists (fun existingHeader -> existingHeader = createUnitColHeader int)
            if isExisting then
                loopingCheck (int+1)
            else
                int
        loopingCheck 1

[<RequireQualifiedAccess>]
module Column =

    /// This is used to create the bracket information for reference (hidden) columns. This function has two modi, one with id tag and one without.
    /// This time no core name is needed as this will always be TSR or TAN.
    let createHiddenColAttributes (newBB:BuildingBlockTypes.InsertBuildingBlock) (id:int) =
        /// The following cols are currently always singles (cannot have TSR, TAN, unit cols). For easier refactoring these names are saved in OfficeInterop.Types.
        let isSingleCol = newBB.Column.Type.isSingleColumn
        if isSingleCol then
            failwith """The function "createHiddenColAttributes" should not get called if there is only a single column in the new building block."""
        /// Try to get existing term accession from InsertBuildingBlock. If none exists do not add any to the header
        let termAccession = if newBB.ColumnTerm.IsSome then newBB.ColumnTerm.Value.TermAccession else ""
        match id with
        | 1             -> $"({termAccession})" 
        | anyOtherId    -> $"({termAccession}#{anyOtherId})"

    /// This function will create the mainColumn name from the base name (e.g. 'Parameter [instrument model]' -> Parameter [instrument model] (#1)).
    /// The possible addition of an id tag is needed, because column headers need to be unique in excel.
    let createMainColName (newBB:BuildingBlockTypes.InsertBuildingBlock) (id:int) =
        match id with
        | 1             -> newBB.Column.toAnnotationTableHeader()
        | anyOtherId    -> $"{newBB.Column.toAnnotationTableHeader(anyOtherId)}" 

    let createTSRColName (newBB:BuildingBlockTypes.InsertBuildingBlock) (id:int) =
        let bracketAttributes = createHiddenColAttributes newBB id
        $"{ColumnCoreNames.TermSourceRef.toString} {bracketAttributes}"  

    let createTANColName (newBB:BuildingBlockTypes.InsertBuildingBlock) (id:int) =
        let bracketAttributes = createHiddenColAttributes newBB id
        $"{ColumnCoreNames.TermAccessionNumber.toString} {bracketAttributes}"  

    /// This function checks if the would be col names already exist. If they do, it ticks up the id tag to keep col names unique.
    /// This function returns the id for the main column and related reference columns.
    let findNewIdForColumn (allColHeaders:string []) (newBB:BuildingBlockTypes.InsertBuildingBlock) =

        /// The following cols are currently always singles (cannot have TSR, TAN, unit cols). For easier refactoring these names are saved in OfficeInterop.Types.
        let isSingleCol = newBB.Column.Type.isSingleColumn
        let rec loopingCheck int =
            let isExisting =
                allColHeaders
                // Should a column with the same name already exist, then count up the id tag.
                |> Array.exists (fun existingHeader ->
                    if isSingleCol then
                        existingHeader = createMainColName newBB int
                    else
                        existingHeader = createMainColName newBB int
                        // i think it is necessary to also check for "TSR" and "TAN" because of the following possibilities
                        // Parameter [instrument model] | "Term Source REF (MS:0000sth) | ...
                        // Factor [instrument model] | "Term Source REF (MS:0000sth)  | ...
                        // in the example above the mainColumn name is different but "TSR" and "TAN" would be the same.
                        || existingHeader = sprintf "%s %s" ColumnCoreNames.TermSourceRef.toString (createHiddenColAttributes newBB int)
                        || existingHeader = sprintf "%s %s" ColumnCoreNames.TermAccessionNumber.toString (createHiddenColAttributes newBB int)
                )
            if isExisting then
                loopingCheck (int+1)
            else
                int
        loopingCheck 1

