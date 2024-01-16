namespace rec FsSpreadsheet

open Fable.Core

//  Helper functions for working with "A1:A1"-style table areas.
/// <summary>
/// The areas marks the area in which the table lies. 
/// </summary>
module Range =

    /// <summary>
    /// Given A1-based top left start and bottom right end indices, returns a "A1:A1"-style area-
    /// </summary>
    let ofBoundaries fromCellReference toCellReference = 
        sprintf "%s:%s" fromCellReference toCellReference

    /// <summary>
    /// Given a "A1:A1"-style area, returns A1-based cell start and end cellReferences.
    /// </summary>
    let toBoundaries (area : string) = 
        area.Split ':'
        |> fun a -> a.[0], a.[1]

    /// <summary>
    /// Gets the right boundary of the area.
    /// </summary>
    let rightBoundary (area : string) = 
        toBoundaries area
        |> snd
        |> CellReference.toIndices
        |> fst

    /// <summary>
    /// Gets the left boundary of the area.
    /// </summary>
    let leftBoundary (area : string) = 
        toBoundaries area
        |> fst
        |> CellReference.toIndices
        |> fst

    /// <summary>
    /// Gets the Upper boundary of the area.
    /// </summary>
    let upperBoundary (area : string) = 
        toBoundaries area
        |> fst
        |> CellReference.toIndices
        |> snd

    /// <summary>
    /// Gets the lower boundary of the area.
    /// </summary>
    let lowerBoundary (area : string) = 
        toBoundaries area
        |> snd
        |> CellReference.toIndices
        |> snd

    /// <summary>
    /// Moves both start and end of the area by the given amount (positive amount moves area to right and vice versa).
    /// </summary>
    let moveHorizontal amount (area : string) =
        area
        |> toBoundaries
        |> fun (f,t) -> CellReference.moveHorizontal amount f, CellReference.moveHorizontal amount t
        ||> ofBoundaries

    /// <summary>
    /// Moves both start and end of the area by the given amount (positive amount moves area to right and vice versa).
    /// </summary>
    let moveVertical amount (area : string) =
        area
        |> toBoundaries
        |> fun (f,t) -> CellReference.moveHorizontal amount f, CellReference.moveHorizontal amount t
        ||> ofBoundaries

    /// <summary>
    /// Extends the right boundary of the area by the given amount (positive amount increases area to right and vice versa).
    /// </summary>
    let extendRight amount (area : string) =
        area
        |> toBoundaries
        |> fun (f,t) -> f, CellReference.moveHorizontal amount t
        ||> ofBoundaries

    /// <summary>
    /// Extends the left boundary of the area by the given amount (positive amount decreases the area to left and vice versa).
    /// </summary>
    let extendLeft amount (area : string) =
        area
        |> toBoundaries
        |> fun (f,t) -> CellReference.moveHorizontal amount f, t
        ||> ofBoundaries

    /// <summary>
    /// Returns true if the column index of the reference exceeds the right boundary of the area.
    /// </summary>
    let referenceExceedsAreaRight reference area = 
        (reference |> CellReference.toIndices |> fst) 
            > (area |> rightBoundary)
    
    /// <summary>
    /// Returns true if the column index of the reference exceeds the left boundary of the area.
    /// </summary>
    let referenceExceedsAreaLeft reference area = 
        (reference |> CellReference.toIndices |> fst) 
            < (area |> leftBoundary)  
 
    /// <summary>
    /// Returns true if the column index of the reference exceeds the upper boundary of the area.
    /// </summary>
    let referenceExceedsAreaAbove reference area = 
        (reference |> CellReference.toIndices |> snd) 
            > (area |> upperBoundary)
    
    /// <summary>
    /// Returns true if the column index of the reference exceeds the lower boundary of the area.
    /// </summary>
    let referenceExceedsAreaBelow reference area = 
        (reference |> CellReference.toIndices |> snd) 
            < (area |> lowerBoundary )  

    /// <summary>
    /// Returns true if the reference does not lie in the boundary of the area.
    /// </summary>
    let referenceExceedsArea reference area = 
        referenceExceedsAreaRight reference area
        ||
        referenceExceedsAreaLeft reference area
        ||
        referenceExceedsAreaAbove reference area
        ||
        referenceExceedsAreaBelow reference area
 
    /// <summary>
    /// Returns true if the A1:A1-style area is of correct format.
    /// </summary>
    let isCorrect area = 
        try
            let hor = leftBoundary  area <= rightBoundary area
            let ver = upperBoundary area <= lowerBoundary area 

            if not hor then printfn "Right area boundary must be higher or equal to left area boundary."
            if not ver then printfn "Lower area boundary must be higher or equal to upper area boundary."

            hor && ver

        with
        | err -> 
            printfn "Area \"%s\" could not be parsed: %s" area err.Message
            false

[<AllowNullLiteral>]
type FsRangeAddress(firstAddress : FsAddress, lastAddress : FsAddress) =

    let mutable _firstAddress = firstAddress
    let mutable _lastAddress = lastAddress


    new(rangeAddress) =
        let firstAdress,lastAddress = Range.toBoundaries rangeAddress
        FsRangeAddress(FsAddress(firstAdress),FsAddress(lastAddress))

    /// <summary>
    /// Creates a deep copy of this FsRangeAddress.
    /// </summary>
    member self.Copy() =
        FsRangeAddress(self.Range)

    /// <summary>
    /// Returns a deep copy of a given FsRangeAddress.
    /// </summary>
    static member copy (rangeAddress : FsRangeAddress) =
        rangeAddress.Copy()

    member self.Extend (address : FsAddress) =
        if address.RowNumber < _firstAddress.RowNumber then
            _firstAddress.RowNumber <- address.RowNumber

        if address.RowNumber > _lastAddress.RowNumber then
            _lastAddress.RowNumber <- address.RowNumber

        if address.ColumnNumber < _firstAddress.ColumnNumber then
            _firstAddress.ColumnNumber <- address.ColumnNumber

        if address.ColumnNumber > _lastAddress.ColumnNumber then
            _lastAddress.ColumnNumber <- address.ColumnNumber

    member self.Normalize() =

        let firstRow,lastRow = 
            if firstAddress.RowNumber < lastAddress.RowNumber then 
                firstAddress.RowNumber,lastAddress.RowNumber 
                else lastAddress.RowNumber,firstAddress.RowNumber

        let firstColumn,lastColumn = 
            if firstAddress.RowNumber < lastAddress.RowNumber then 
                firstAddress.RowNumber,lastAddress.RowNumber 
                else lastAddress.RowNumber,firstAddress.RowNumber

        _firstAddress <- FsAddress(firstRow,firstColumn)
        _lastAddress <- FsAddress(lastRow,lastColumn)

    member self.Range 
        with get() = Range.ofBoundaries _firstAddress.Address _lastAddress.Address
        and set(address) = 
            let firstAdress, lastAdress = Range.toBoundaries address            
            _firstAddress <- FsAddress (firstAdress)
            _lastAddress <- FsAddress (lastAdress)

    override self.ToString() =
        self.Range

    member self.FirstAddress : FsAddress = _firstAddress

    member self.LastAddress : FsAddress = _lastAddress

    member self.Union(rangeAddress : FsRangeAddress) =
        self.Extend(rangeAddress.FirstAddress)
        self.Extend(rangeAddress.LastAddress)
        self