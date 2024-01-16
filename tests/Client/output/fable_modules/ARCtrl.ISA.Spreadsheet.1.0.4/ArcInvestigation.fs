namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open FsSpreadsheet
open Comment
open Remark
open System.Collections.Generic

module ArcInvestigation = 

    let [<Literal>] identifierLabel = "Investigation Identifier"
    let [<Literal>] titleLabel = "Investigation Title"
    let [<Literal>] descriptionLabel = "Investigation Description"
    let [<Literal>] submissionDateLabel = "Investigation Submission Date"
    let [<Literal>] publicReleaseDateLabel = "Investigation Public Release Date"

    let [<Literal>] investigationLabel = "INVESTIGATION"
    let [<Literal>] ontologySourceReferenceLabel = "ONTOLOGY SOURCE REFERENCE"
    let [<Literal>] publicationsLabel = "INVESTIGATION PUBLICATIONS"
    let [<Literal>] contactsLabel = "INVESTIGATION CONTACTS"
    let [<Literal>] studyLabel = "STUDY"

    let [<Literal>] publicationsLabelPrefix = "Investigation Publication"
    let [<Literal>] contactsLabelPrefix = "Investigation Person"

    let [<Literal>] metaDataSheetName = "isa_investigation"
    let [<Literal>] metaDataSheetName_deprecated = "Investigation"


    type InvestigationInfo =
        {
        Identifier : string
        Title : string
        Description : string
        SubmissionDate : string
        PublicReleaseDate : string
        Comments : Comment list
        }

        static member create identifier title description submissionDate publicReleaseDate comments =
            {
            Identifier = identifier
            Title = title
            Description = description
            SubmissionDate = submissionDate
            PublicReleaseDate = publicReleaseDate
            Comments = comments
            }
  
        static member Labels = [identifierLabel;titleLabel;descriptionLabel;submissionDateLabel;publicReleaseDateLabel]
    
        static member FromSparseTable (matrix : SparseTable) =
        
            let i = 0

            let comments = 
                matrix.CommentKeys 
                |> List.map (fun k -> 
                    Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))

            InvestigationInfo.create
                (matrix.TryGetValueDefault("",(identifierLabel,i)))  
                (matrix.TryGetValueDefault("",(titleLabel,i)))  
                (matrix.TryGetValueDefault("",(descriptionLabel,i)))  
                (matrix.TryGetValueDefault("",(submissionDateLabel,i)))  
                (matrix.TryGetValueDefault("",(publicReleaseDateLabel,i)))  
                comments


        static member ToSparseTable (investigation: ArcInvestigation) =
            let i = 1
            let matrix = SparseTable.Create (keys = InvestigationInfo.Labels,length=2)
            let mutable commentKeys = []

            do matrix.Matrix.Add ((identifierLabel,i),          (investigation.Identifier))
            do matrix.Matrix.Add ((titleLabel,i),               (Option.defaultValue "" investigation.Title))
            do matrix.Matrix.Add ((descriptionLabel,i),         (Option.defaultValue "" investigation.Description))
            do matrix.Matrix.Add ((submissionDateLabel,i),      (Option.defaultValue "" investigation.SubmissionDate))
            do matrix.Matrix.Add ((publicReleaseDateLabel,i),   (Option.defaultValue "" investigation.PublicReleaseDate))

            if Array.isEmpty investigation.Comments |> not then
                investigation.Comments
                |> Array.iter (fun comment -> 
                    let n,v = comment |> Comment.toString
                    commentKeys <- n :: commentKeys
                    matrix.Matrix.Add((n,i),v)
                )   

            {matrix with CommentKeys = commentKeys |> List.distinct |> List.rev}

      
        static member fromRows lineNumber (rows : IEnumerator<SparseRow>) =
            SparseTable.FromRows(rows,InvestigationInfo.Labels,lineNumber)
            |> fun (s,ln,rs,sm) -> (s,ln,rs, InvestigationInfo.FromSparseTable sm)    
    
        static member toRows (investigation : ArcInvestigation) =  
            investigation
            |> InvestigationInfo.ToSparseTable
            |> SparseTable.ToRows
 
    let fromParts (investigationInfo:InvestigationInfo) (ontologySourceReference:OntologySourceReference list) (publications: Publication list) (contacts: Person list) (studies: ArcStudy list) (assays: ArcAssay list) (remarks: Remark list) =
        let studyIdentifiers = studies |> List.map (fun s -> s.Identifier)
        ArcInvestigation.make 
            (investigationInfo.Identifier)
            (Option.fromValueWithDefault "" investigationInfo.Title)
            (Option.fromValueWithDefault "" investigationInfo.Description) 
            (Option.fromValueWithDefault "" investigationInfo.SubmissionDate) 
            (Option.fromValueWithDefault "" investigationInfo.PublicReleaseDate)
            (Array.ofList ontologySourceReference) 
            (Array.ofList publications)  
            (Array.ofList contacts)  
            (ResizeArray(assays))
            (ResizeArray(studies))  
            (ResizeArray(studyIdentifiers))
            (Array.ofList investigationInfo.Comments)  
            (Array.ofList remarks)


    let fromRows (rows:seq<SparseRow>) =
        let en = rows.GetEnumerator()              
        
        let emptyInvestigationInfo = InvestigationInfo.create "" "" "" "" "" []

        let rec loop lastLine ontologySourceReferences investigationInfo publications contacts studies remarks lineNumber =
            match lastLine with

            | Some k when k = ontologySourceReferenceLabel -> 
                let currentLine,lineNumber,newRemarks,ontologySourceReferences = OntologySourceReference.fromRows (lineNumber + 1) en
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = investigationLabel -> 
                let currentLine,lineNumber,newRemarks,investigationInfo = InvestigationInfo.fromRows (lineNumber + 1) en       
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = publicationsLabel -> 
                let currentLine,lineNumber,newRemarks,publications = Publications.fromRows (Some publicationsLabelPrefix) (lineNumber + 1) en       
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = contactsLabel -> 
                let currentLine,lineNumber,newRemarks,contacts = Contacts.fromRows (Some contactsLabelPrefix) (lineNumber + 1) en       
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = studyLabel -> 
                let currentLine,lineNumber,newRemarks,study = Studies.fromRows (lineNumber + 1) en  
                if study.IsSome then
                    loop currentLine ontologySourceReferences investigationInfo publications contacts (study.Value::studies) (List.append remarks newRemarks) lineNumber
                else 
                    loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | k -> 
                let studies,assays = 
                    studies 
                    |> List.unzip 
                    |> fun (s,a) -> 
                        s |> List.rev, 
                        a |> List.concat |> List.distinctBy (fun a -> a.Identifier)
                fromParts investigationInfo ontologySourceReferences publications contacts studies assays remarks

        if en.MoveNext () then
            let currentLine = en.Current |> SparseRow.tryGetValueAt 0
            loop currentLine [] emptyInvestigationInfo [] [] [] [] 1
            
        else
            failwith "emptyInvestigationFile"
 
   
    let toRows (isLight: bool) (investigation:ArcInvestigation) : seq<SparseRow> =
        let insertRemarks (remarks:Remark list) (rows:seq<SparseRow>) = 
            try 
                let rm = remarks |> List.map Remark.toTuple |> Map.ofList            
                let rec loop i l nl =
                    match Map.tryFind i rm with
                    | Some remark ->
                         SparseRow.fromValues [wrapRemark remark] :: nl
                        |> loop (i+1) l
                    | None -> 
                        match l with
                        | [] -> nl
                        | h :: t -> 
                            loop (i+1) t (h::nl)
                loop 1 (rows |> List.ofSeq) []
                |> List.rev
            with | _ -> rows |> Seq.toList
        seq {
            yield  SparseRow.fromValues [ontologySourceReferenceLabel]
            yield! OntologySourceReference.toRows (List.ofArray investigation.OntologySourceReferences)

            yield  SparseRow.fromValues [investigationLabel]
            yield! InvestigationInfo.toRows investigation

            yield  SparseRow.fromValues [publicationsLabel]
            yield! Publications.toRows (Some publicationsLabelPrefix) (List.ofArray investigation.Publications)

            yield  SparseRow.fromValues [contactsLabel]
            yield! Contacts.toRows (Some contactsLabelPrefix) (List.ofArray investigation.Contacts)

            if not isLight then
                for studyIdentifier in investigation.RegisteredStudyIdentifiers do
                    let study = investigation.TryGetStudy(studyIdentifier) |> Option.defaultValue (ArcStudy(studyIdentifier))
                    yield  SparseRow.fromValues [studyLabel]
                    yield! Studies.toRows study None
        }
        |> insertRemarks (List.ofArray investigation.Remarks)
        |> seq

    let fromFsWorkbook (doc:FsWorkbook) =  
        try
            match doc.TryGetWorksheetByName metaDataSheetName with
            | Some sheet -> sheet
            | None -> 
                match doc.TryGetWorksheetByName metaDataSheetName_deprecated with
                | Some sheet -> sheet
                | None -> failwith "Could not find metadata sheet with sheetname \"isa_investigation\" or deprecated sheetname \"Investigation\""
            |> FsWorksheet.getRows
            |> Seq.map SparseRow.fromFsRow
            |> fromRows 
        with
        | err -> failwithf "Could not read investigation from spreadsheet: %s" err.Message

    let toFsWorkbook (investigation:ArcInvestigation) : FsWorkbook =           
        try
            let wb = new FsWorkbook()
            let sheet = FsWorksheet(metaDataSheetName)
            investigation
            |> toRows false
            |> Seq.iteri (fun rowI r -> SparseRow.writeToSheet (rowI + 1) r sheet)                     
            wb.AddWorksheet(sheet)
            wb
        with
        | err -> failwithf "Could not write investigation to spreadsheet: %s" err.Message

    let toLightFsWorkbook (investigation: ArcInvestigation) : FsWorkbook =
        try
            let wb = new FsWorkbook()
            let sheet = FsWorksheet(metaDataSheetName)
            investigation
            |> toRows true
            |> Seq.iteri (fun rowI r -> SparseRow.writeToSheet (rowI + 1) r sheet)                     
            wb.AddWorksheet(sheet)
            wb
        with
        | err -> failwithf "Could not write investigation to spreadsheet: %s" err.Message
