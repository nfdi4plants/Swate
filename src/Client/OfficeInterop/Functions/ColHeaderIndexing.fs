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
module MainColumn =

    /// This function checks if the would be col names already exist. If they do, it ticks up the id tag to keep col names unique.
    /// This function returns the id for the main column.
    /// This will be not necessary for version 0.6.0, as we don't allow multiple instances of the same column in a table + separate indices between main column and reference columns.
    let findNewIdForColumn (allColHeaders:string []) (newBB:InsertBuildingBlock) =

        let rec loopingCheck int =
            let isExisting =
                allColHeaders
                // Should a column with the same name already exist, then count up the id tag.
                |> Array.exists (fun existingHeader ->
                    existingHeader = newBB.ColumnHeader.toAnnotationTableHeader()
                )
            if isExisting then
                loopingCheck (int+1)
            else
                int
        loopingCheck 1

[<RequireQualifiedAccess>]
module RefColumns =

    /// This is used to create the bracket information for reference (hidden) columns. This function has two modi, one with id tag and one without.
    /// This time no core name is needed as this will always be TSR or TAN.
    let createHiddenColAttributes (newBB:InsertBuildingBlock) (id:int) =
        /// The following cols are currently always singles (cannot have TSR, TAN, unit cols). For easier refactoring these names are saved in OfficeInterop.Types.
        let isSingleCol = newBB.ColumnHeader.Type.isSingleColumn
        if isSingleCol then
            failwith """The function "createHiddenColAttributes" should not get called if there is only a single column in the new building block."""
        /// Try to get existing term accession from InsertBuildingBlock. If none exists do not add any to the header
        let termAccession = if newBB.ColumnTerm.IsSome then newBB.ColumnTerm.Value.TermAccession else ""
        match id with
        | 1             -> $"({termAccession})" 
        | anyOtherId    -> $"({termAccession}#{anyOtherId})"

    let createTSRColName (newBB:InsertBuildingBlock) (id:int) =
        let bracketAttributes = createHiddenColAttributes newBB id
        $"{ColumnCoreNames.TermSourceRef.toString} {bracketAttributes}"  

    let createTANColName (newBB:InsertBuildingBlock) (id:int) =
        let bracketAttributes = createHiddenColAttributes newBB id
        $"{ColumnCoreNames.TermAccessionNumber.toString} {bracketAttributes}"  

    /// This function checks if the would be col names already exist. If they do, it ticks up the id tag to keep col names unique.
    /// This function returns the id for the reference columns.
    let findNewIdForReferenceColumns (allColHeaders:string []) (newBB:InsertBuildingBlock) =

        let rec loopingCheck int =
            let isExisting =
                allColHeaders
                // Should a column with the same name already exist, then count up the id tag.
                |> Array.exists (fun existingHeader ->
                    // i think it is necessary to also check for "TSR" and "TAN" because of the following possibilities
                    // Parameter [instrument model] | "Term Source REF (MS:0000sth) | ...
                    // Factor [instrument model] | "Term Source REF (MS:0000sth)  | ...
                    // in the example above the mainColumn name is different but "TSR" and "TAN" would be the same.
                    existingHeader = sprintf "%s %s" ColumnCoreNames.TermSourceRef.toString (createHiddenColAttributes newBB int)
                    || existingHeader = sprintf "%s %s" ColumnCoreNames.TermAccessionNumber.toString (createHiddenColAttributes newBB int)
                )
            if isExisting then
                loopingCheck (int+1)
            else
                int
        loopingCheck 1

