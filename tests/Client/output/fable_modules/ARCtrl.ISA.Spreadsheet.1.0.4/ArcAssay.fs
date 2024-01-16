module ARCtrl.ISA.Spreadsheet.ArcAssay

open ARCtrl.ISA
open FsSpreadsheet

let [<Literal>] obsoleteAssaysLabel = "ASSAY METADATA"
let [<Literal>] assaysLabel = "ASSAY"
let [<Literal>] contactsLabel = "ASSAY PERFORMERS"

let [<Literal>] assaysPrefix = "Assay"
let [<Literal>] contactsPrefix = "Assay Person"

let [<Literal>] obsoleteMetaDataSheetName = "Assay"
let [<Literal>] metaDataSheetName = "isa_assay"

let toMetadataSheet (assay : ArcAssay) : FsWorksheet =
    let toRows (assay:ArcAssay) =
        seq {          
            yield  SparseRow.fromValues [assaysLabel]
            yield! Assays.toRows (Some assaysPrefix) [assay]

            yield SparseRow.fromValues [contactsLabel]
            yield! Contacts.toRows (Some contactsPrefix) (List.ofArray assay.Performers)
        }
    let sheet = FsWorksheet(metaDataSheetName)
    assay
    |> toRows
    |> Seq.iteri (fun rowI r -> SparseRow.writeToSheet (rowI + 1) r sheet)    
    sheet

let fromMetadataSheet (sheet : FsWorksheet) : ArcAssay =
    let fromRows (usePrefixes : bool) (rows: seq<SparseRow>) =
        let aPrefix,cPrefix = 
            if usePrefixes then 
                Some assaysPrefix,Some contactsPrefix
            else None,None
        let en = rows.GetEnumerator()
        let rec loop lastLine assays contacts lineNumber =
               
            match lastLine with

            | Some k when k = assaysLabel || k = obsoleteAssaysLabel -> 
                let currentLine,lineNumber,_,assays = Assays.fromRows aPrefix (lineNumber + 1) en       
                loop currentLine assays contacts lineNumber

            | Some k when k = contactsLabel -> 
                let currentLine,lineNumber,_,contacts = Contacts.fromRows cPrefix (lineNumber + 1) en  
                loop currentLine assays contacts lineNumber
            | k -> 
                match assays, contacts with
                | [], [] -> ArcAssay.create(Identifier.createMissingIdentifier())
                | assays, contacts ->
                    assays
                    |> Seq.tryHead 
                    |> Option.defaultValue (ArcAssay.create(Identifier.createMissingIdentifier()))
                    |> ArcAssay.setPerformers (Array.ofList contacts)
        
        if en.MoveNext () then
            let currentLine = en.Current |> SparseRow.tryGetValueAt 0
            loop currentLine [] [] 1
            
        else
            failwith "empty assay metadata sheet"
    let rows =        
        sheet.Rows 
        |> Seq.map SparseRow.fromFsRow
    let hasPrefix = 
        rows
        |> Seq.exists (fun row -> row |> Seq.head |> snd |> fun s -> s.StartsWith(assaysPrefix))
    rows
    |> fromRows hasPrefix

/// Reads an assay from a spreadsheet
let fromFsWorkbook (doc:FsWorkbook) = 
    // Reading the "Assay" metadata sheet. Here metadata 
    let assayMetaData = 
        
        match doc.TryGetWorksheetByName metaDataSheetName with 
        | Option.Some sheet ->
            fromMetadataSheet sheet
        | None -> 
            match doc.TryGetWorksheetByName obsoleteMetaDataSheetName with 
            | Option.Some sheet ->
                fromMetadataSheet sheet
            | None -> 
                printfn "Cannot retrieve metadata: Assay file does not contain \"%s\" or \"%s\" sheet." metaDataSheetName obsoleteMetaDataSheetName
                ArcAssay.create(Identifier.createMissingIdentifier())
    let annotationTables = 
        doc.GetWorksheets()
        |> Seq.choose ArcTable.tryFromFsWorksheet
    if annotationTables |> Seq.isEmpty |> not then
        assayMetaData.Tables <- ResizeArray annotationTables
    assayMetaData

let toFsWorkbook (assay : ArcAssay) =
    let doc = new FsWorkbook()
    let metaDataSheet = toMetadataSheet (assay)
    doc.AddWorksheet metaDataSheet

    assay.Tables
    |> Seq.iter (ArcTable.toFsWorksheet >> doc.AddWorksheet)

    doc