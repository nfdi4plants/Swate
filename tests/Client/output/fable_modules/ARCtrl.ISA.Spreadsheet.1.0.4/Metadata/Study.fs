namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open Comment
open Remark
open System.Collections.Generic

module Studies = 

    let [<Literal>] identifierLabel = "Study Identifier"
    let [<Literal>] titleLabel = "Study Title"
    let [<Literal>] descriptionLabel = "Study Description"
    let [<Literal>] submissionDateLabel = "Study Submission Date"
    let [<Literal>] publicReleaseDateLabel = "Study Public Release Date"
    let [<Literal>] fileNameLabel = "Study File Name"

    let [<Literal>] designDescriptorsLabelPrefix = "Study Design"
    let [<Literal>] publicationsLabelPrefix = "Study Publication"
    let [<Literal>] factorsLabelPrefix = "Study Factor"
    let [<Literal>] assaysLabelPrefix = "Study Assay"
    let [<Literal>] protocolsLabelPrefix = "Study Protocol"
    let [<Literal>] contactsLabelPrefix = "Study Person"

    let [<Literal>] designDescriptorsLabel = "STUDY DESIGN DESCRIPTORS"
    let [<Literal>] publicationsLabel = "STUDY PUBLICATIONS"
    let [<Literal>] factorsLabel = "STUDY FACTORS"
    let [<Literal>] assaysLabel = "STUDY ASSAYS"
    let [<Literal>] protocolsLabel = "STUDY PROTOCOLS"
    let [<Literal>] contactsLabel = "STUDY CONTACTS"

    type StudyInfo =
        {
        Identifier : string
        Title : string
        Description : string
        SubmissionDate : string
        PublicReleaseDate : string
        FileName : string
        Comments : Comment list
        }

        static member create identifier title description submissionDate publicReleaseDate fileName comments =
            {
            Identifier = identifier
            Title = title
            Description = description
            SubmissionDate = submissionDate
            PublicReleaseDate = publicReleaseDate
            FileName = fileName
            Comments = comments
            }
  
        static member Labels = [identifierLabel;titleLabel;descriptionLabel;submissionDateLabel;publicReleaseDateLabel;fileNameLabel]
    
        static member FromSparseTable (matrix : SparseTable) =
        
            let i = 0

            let comments = 
                matrix.CommentKeys 
                |> List.map (fun k -> 
                    Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))

            StudyInfo.create
                (matrix.TryGetValueDefault(Identifier.createMissingIdentifier(),(identifierLabel,i)))  
                (matrix.TryGetValueDefault("",(titleLabel,i)))  
                (matrix.TryGetValueDefault("",(descriptionLabel,i)))  
                (matrix.TryGetValueDefault("",(submissionDateLabel,i)))  
                (matrix.TryGetValueDefault("",(publicReleaseDateLabel,i)))  
                (matrix.TryGetValueDefault("",(fileNameLabel,i)))                    
                comments


        static member ToSparseTable (study: ArcStudy) =
            let i = 1
            let matrix = SparseTable.Create (keys = StudyInfo.Labels,length = 2)
            let mutable commentKeys = []
            let processedIdentifier,processedFileName =
                if study.Identifier.StartsWith(Identifier.MISSING_IDENTIFIER) then "","" else 
                    study.Identifier, Identifier.Study.fileNameFromIdentifier study.Identifier

            do matrix.Matrix.Add ((identifierLabel,i),          processedIdentifier)
            do matrix.Matrix.Add ((titleLabel,i),               (Option.defaultValue "" study.Title))
            do matrix.Matrix.Add ((descriptionLabel,i),         (Option.defaultValue "" study.Description))
            do matrix.Matrix.Add ((submissionDateLabel,i),      (Option.defaultValue "" study.SubmissionDate))
            do matrix.Matrix.Add ((publicReleaseDateLabel,i),   (Option.defaultValue "" study.PublicReleaseDate))
            do matrix.Matrix.Add ((fileNameLabel,i),            processedFileName)

            if Array.isEmpty study.Comments |> not then
                study.Comments
                |> Array.iter (fun comment -> 
                    let n,v = comment |> Comment.toString
                    commentKeys <- n :: commentKeys
                    matrix.Matrix.Add((n,i),v)
                )    

            {matrix with CommentKeys = commentKeys |> List.distinct |> List.rev}

        static member fromRows lineNumber (rows : IEnumerator<SparseRow>) =
            SparseTable.FromRows(rows,StudyInfo.Labels,lineNumber)
            |> fun (s,ln,rs,sm) -> (s,ln,rs, StudyInfo.FromSparseTable sm)
    
        static member toRows (study : ArcStudy) =  
            study
            |> StudyInfo.ToSparseTable
            |> SparseTable.ToRows
    
    let fromParts (studyInfo:StudyInfo) (designDescriptors:OntologyAnnotation list) (publications: Publication list) (factors: Factor list) (assays: ArcAssay list) (protocols : Protocol list) (contacts: Person list) =
        let assayIdentifiers = assays |> List.map (fun assay -> assay.Identifier)
        ArcStudy.make 
            (studyInfo.Identifier)
            (Option.fromValueWithDefault "" studyInfo.Title)
            (Option.fromValueWithDefault "" studyInfo.Description) 
            (Option.fromValueWithDefault "" studyInfo.SubmissionDate)
            (Option.fromValueWithDefault "" studyInfo.PublicReleaseDate)
            (Array.ofList publications)
            (Array.ofList contacts)
            (Array.ofList designDescriptors)  
            (protocols |> List.map ArcTable.fromProtocol |> ResizeArray)
            (ResizeArray(assayIdentifiers))
            (Array.ofList factors) 
            (Array.ofList studyInfo.Comments)
        |> fun arcstudy -> 
            if arcstudy.isEmpty && arcstudy.Identifier = "" 
            then None else Some (arcstudy,assays)

    let fromRows lineNumber (en:IEnumerator<SparseRow>) = 

        let rec loop lastLine (studyInfo : StudyInfo) designDescriptors publications factors assays protocols contacts remarks lineNumber =
           
            match lastLine with

            | Some k when k = designDescriptorsLabel -> 
                let currentLine,lineNumber,newRemarks,designDescriptors = DesignDescriptors.fromRows (Some designDescriptorsLabelPrefix) (lineNumber + 1) en         
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = publicationsLabel -> 
                let currentLine,lineNumber,newRemarks,publications = Publications.fromRows (Some publicationsLabelPrefix) (lineNumber + 1) en       
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = factorsLabel -> 
                let currentLine,lineNumber,newRemarks,factors = Factors.fromRows (Some factorsLabelPrefix) (lineNumber + 1) en       
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = assaysLabel -> 
                let currentLine,lineNumber,newRemarks,assays = Assays.fromRows (Some assaysLabelPrefix) (lineNumber + 1) en       
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = protocolsLabel -> 
                let currentLine,lineNumber,newRemarks,protocols = Protocols.fromRows (Some protocolsLabelPrefix) (lineNumber + 1) en  
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = contactsLabel -> 
                let currentLine,lineNumber,newRemarks,contacts = Contacts.fromRows (Some contactsLabelPrefix) (lineNumber + 1) en  
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | k -> 
                k,lineNumber,remarks, fromParts studyInfo designDescriptors publications factors assays protocols contacts
    
        let currentLine,lineNumber,remarks,item = StudyInfo.fromRows lineNumber en  
        loop currentLine item [] [] [] [] [] [] remarks lineNumber

    
    let toRows (study : ArcStudy) (assays : ArcAssay list option) =
        let protocols = study.Tables |> Seq.collect (fun p -> p.GetProtocols()) |> List.ofSeq
        let assays = assays |> Option.defaultValue (study.GetRegisteredAssaysOrIdentifier() |> List.ofSeq)
        seq {          
            yield! StudyInfo.toRows study

            yield  SparseRow.fromValues [designDescriptorsLabel]
            yield! DesignDescriptors.toRows (Some designDescriptorsLabelPrefix) (List.ofArray study.StudyDesignDescriptors)

            yield  SparseRow.fromValues [publicationsLabel]
            yield! Publications.toRows (Some publicationsLabelPrefix) (List.ofArray study.Publications)

            yield  SparseRow.fromValues [factorsLabel]
            yield! Factors.toRows (Some factorsLabelPrefix) (List.ofArray study.Factors)

            yield  SparseRow.fromValues [assaysLabel]
            yield! Assays.toRows (Some assaysLabelPrefix) assays

            yield  SparseRow.fromValues [protocolsLabel]
            yield! Protocols.toRows (Some protocolsLabelPrefix) protocols

            yield  SparseRow.fromValues [contactsLabel]
            yield! Contacts.toRows (Some contactsLabelPrefix) (List.ofArray study.Contacts)
        }