namespace FsSpreadsheet

open Fable.Core
/// <summary>
/// Creates an empty FsWorkbook.
/// </summary>

[<AttachMembers>]
type FsWorkbook() =
 
    let _worksheets = ResizeArray()

    interface System.IDisposable with
        member self.Dispose() = ()

    // maybe better to leave that to methods...
    //member self.Worksheets 
    //    with get() = _worksheets
    //    and set(value) = _worksheets <- value


    // -------
    // METHODS
    // -------

    /// <summary>
    /// Creates a deep copy of this FsWorkbook.
    /// </summary>
    member self.Copy() =
        let shts = self.GetWorksheets().ToArray() |> Array.map (fun (s : FsWorksheet) -> s.Copy())
        let wb = new FsWorkbook()
        wb.AddWorksheets shts
        wb

    /// <summary>
    /// Returns a deep copy of a given FsWorkbook.
    /// </summary>
    static member copy (workbook : FsWorkbook) =
        workbook.Copy()
 
    /// <summary>
    /// Creates an empty FsWorksheet with given name and adds it to the FsWorkbook.
    /// </summary>
    member self.InitWorksheet(name : string) = 
        let sheet = FsWorksheet name
        _worksheets.Add(sheet)
        sheet

    /// <summary>
    /// Creates an empty FsWorksheet with given name and adds it to the FsWorkbook.
    /// </summary>
    static member initWorksheet (name : string) (workbook : FsWorkbook) = 
        workbook.InitWorksheet name
        
    
    /// <summary>
    /// Adds a given FsWorksheet to the FsWorkbook.
    /// </summary>
    member self.AddWorksheet(sheet : FsWorksheet) = 
        if _worksheets |> Seq.exists (fun ws -> ws.Name = sheet.Name) then
            failwithf "Could not add worksheet with name \"%s\" to workbook as it already contains a worksheet with the same name" sheet.Name
        else
            _worksheets.Add(sheet)

    /// <summary>
    /// Adds an FsWorksheet to an FsWorkbook.
    /// </summary>
    static member addWorksheet (sheet : FsWorksheet) (workbook : FsWorkbook) = 
        workbook.AddWorksheet sheet
        workbook

    /// <summary>
    /// Adds a collection of FsWorksheets to the FsWorkbook.
    /// </summary>
    member self.AddWorksheets(sheets : seq<FsWorksheet>) =
        sheets
        |> Seq.iter self.AddWorksheet

    /// <summary>
    /// Adds a collection of FsWorksheets to an FsWorkbook.
    /// </summary>
    static member addWorksheets sheets (workbook : FsWorkbook) =
        workbook.AddWorksheets sheets
        workbook

    /// <summary>
    /// Returns all FsWorksheets.
    /// </summary>
    member self.GetWorksheets() : ResizeArray<FsWorksheet> = 
        _worksheets

    /// <summary>
    /// Returns all FsWorksheets.
    /// </summary>
    static member getWorksheets (workbook : FsWorkbook) =
        workbook.GetWorksheets()

    /// <summary>
    /// Returns the FsWorksheet with the given 1 based index if it exists. Else returns None.
    /// </summary>
    member self.TryGetWorksheetAt(index : int) =
        _worksheets |> Seq.tryItem (index - 1)

    /// <summary>
    /// Returns the FsWorksheet with the given 1 based index if it exists in a given FsWorkbook. Else returns None.
    /// </summary>
    static member tryGetWorksheetAt (index : int) (workbook : FsWorkbook) =
        workbook.TryGetWorksheetAt index

    /// <summary>
    /// Returns the FsWorksheet with the given 1 based index.
    /// </summary>
    /// <exception cref="System.Exception">if FsWorksheet with at position is not present in the FsWorkkbook.</exception>
    member self.GetWorksheetAt(index : int) =
        match self.TryGetWorksheetAt index with
        | Some w -> w
        | None -> failwith $"FsWorksheet at position {index} is not present in the FsWorkbook."

    /// <summary>
    /// Returns the FsWorksheet with the given the given 1 based indexk.
    /// </summary>
    /// <exception cref="System.Exception">if FsWorksheet with at position is not present in the FsWorkkbook.</exception>
    static member getWorksheetAt (index : int) (workbook : FsWorkbook) =
        workbook.GetWorksheetAt index

    /// <summary>
    /// Returns the FsWorksheet with the given name if it exists in the FsWorkbook. Else returns None.
    /// </summary>
    member self.TryGetWorksheetByName(sheetName) =
        _worksheets |> Seq.tryFind (fun w -> w.Name = sheetName)

    /// <summary>
    /// Returns the FsWorksheet with the given name if it exists in a given FsWorkbook. Else returns None.
    /// </summary>
    static member tryGetWorksheetByName sheetName (workbook : FsWorkbook) =
        workbook.TryGetWorksheetByName sheetName

    /// <summary>
    /// Returns the FsWorksheet with the given name.
    /// </summary>
    /// <exception cref="System.Exception">if FsWorksheet with given name is not present in the FsWorkkbook.</exception>
    member self.GetWorksheetByName(sheetName) =
        try (self.TryGetWorksheetByName sheetName).Value
        with _ -> failwith $"FsWorksheet with name {sheetName} is not present in the FsWorkbook."

    /// <summary>
    /// Returns the FsWorksheet with the given name from an FsWorkbook.
    /// </summary>
    /// <exception cref="System.Exception">if FsWorksheet with given name is not present in the FsWorkkbook.</exception>
    static member getWorksheetByName sheetName (workbook : FsWorkbook) =
        workbook.GetWorksheetByName sheetName

    /// <summary>
    /// Removes an FsWorksheet with given name.
    /// </summary>
    /// <exception cref="System.Exception">if FsWorksheet with given name is not present in the FsWorkkbook.</exception>
    member self.RemoveWorksheet(name : string) =
        let ws = 
            try 
                _worksheets.Find(fun ws -> ws.Name = name)
            with
                | _ -> failwith $"FsWorksheet with name {name} was not found in FsWorkbook."
        _worksheets.Remove(ws) |> ignore

    /// <summary>
    /// Removes an FsWorksheet with given name from an FsWorkbook.
    /// </summary>
    static member removeWorksheet (name : string) (workbook : FsWorkbook) =
        workbook.RemoveWorksheet name
        workbook

    /// <summary>
    /// Returns all FsTables from the FsWorkbook.
    /// </summary>
    member self.GetTables() =
        self.GetWorksheets().ToArray()
        |> Array.collect (fun s -> s.Tables |> Array.ofSeq)

    /// <summary>
    /// Returns all FsTables from an FsWorkbook.
    /// </summary>
    static member getTables (workbook : FsWorkbook) =
        workbook.GetTables()