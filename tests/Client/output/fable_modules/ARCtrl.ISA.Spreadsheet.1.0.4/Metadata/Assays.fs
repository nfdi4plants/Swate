namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open Comment
open Remark
open System.Collections.Generic

module Assays = 

    let [<Literal>] measurementTypeLabel =                    "Measurement Type"
    let [<Literal>] measurementTypeTermAccessionNumberLabel = "Measurement Type Term Accession Number"
    let [<Literal>] measurementTypeTermSourceREFLabel =       "Measurement Type Term Source REF"
    let [<Literal>] technologyTypeLabel =                     "Technology Type"
    let [<Literal>] technologyTypeTermAccessionNumberLabel =  "Technology Type Term Accession Number"
    let [<Literal>] technologyTypeTermSourceREFLabel =        "Technology Type Term Source REF"
    let [<Literal>] technologyPlatformLabel =                 "Technology Platform"
    let [<Literal>] fileNameLabel =                           "File Name"

    let labels = 
        [
            measurementTypeLabel;measurementTypeTermAccessionNumberLabel;measurementTypeTermSourceREFLabel;
            technologyTypeLabel;technologyTypeTermAccessionNumberLabel;technologyTypeTermSourceREFLabel;technologyPlatformLabel;fileNameLabel
        ]

    
    let fromString measurementType measurementTypeTermSourceREF measurementTypeTermAccessionNumber technologyType technologyTypeTermSourceREF technologyTypeTermAccessionNumber technologyPlatform fileName comments : ArcAssay = 
        let measurementType = OntologyAnnotation.fromString(?termName = measurementType,?tan = measurementTypeTermAccessionNumber,?tsr = measurementTypeTermSourceREF)
        let technologyType = OntologyAnnotation.fromString(?termName = technologyType,?tan = technologyTypeTermAccessionNumber,?tsr = technologyTypeTermSourceREF)
        ArcAssay.make 
            (fileName)
            (Option.fromValueWithDefault OntologyAnnotation.empty measurementType)
            (Option.fromValueWithDefault OntologyAnnotation.empty technologyType) 
            (technologyPlatform |> Option.map ArcAssay.decomposeTechnologyPlatform)
            (ResizeArray())             
            [||] 
            (comments)
        
    let fromSparseTable (matrix : SparseTable) : ArcAssay list=
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            ArcAssay.create(Identifier.createMissingIdentifier(),comments = comments)
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> Array.ofList

                fromString
                    (matrix.TryGetValue(measurementTypeLabel,i))            
                    (matrix.TryGetValue((measurementTypeTermSourceREFLabel,i)))
                    (matrix.TryGetValue((measurementTypeTermAccessionNumberLabel,i)))
                    (matrix.TryGetValue(technologyTypeLabel,i))             
                    (matrix.TryGetValue((technologyTypeTermSourceREFLabel,i)))   
                    (matrix.TryGetValue((technologyTypeTermAccessionNumberLabel,i))) 
                    (matrix.TryGetValue(technologyPlatformLabel,i))     
                    (matrix.TryGetValueDefault(Identifier.createMissingIdentifier(),(fileNameLabel,i)) |> Identifier.Assay.identifierFromFileName)                    
                    comments
            )

    let toSparseTable (assays: ArcAssay list) =
        let matrix = SparseTable.Create (keys = labels,length=assays.Length + 1)
        let mutable commentKeys = []
        assays
        |> List.iteri (fun i a ->
            let processedFileName =
                if a.Identifier.StartsWith(Identifier.MISSING_IDENTIFIER) then Identifier.removeMissingIdentifier(a.Identifier) else Identifier.Assay.fileNameFromIdentifier(a.Identifier)
            let i = i + 1
            let mt = Option.defaultValue OntologyAnnotation.empty a.MeasurementType |> fun mt -> OntologyAnnotation.toString(mt,true)
            let tt = Option.defaultValue OntologyAnnotation.empty a.TechnologyType |> fun tt -> OntologyAnnotation.toString(tt,true)
            do matrix.Matrix.Add ((measurementTypeLabel,i),                       mt.TermName)
            do matrix.Matrix.Add ((measurementTypeTermAccessionNumberLabel,i),    mt.TermAccessionNumber)
            do matrix.Matrix.Add ((measurementTypeTermSourceREFLabel,i),          mt.TermSourceREF)
            do matrix.Matrix.Add ((technologyTypeLabel,i),                        tt.TermName)
            do matrix.Matrix.Add ((technologyTypeTermAccessionNumberLabel,i),     tt.TermAccessionNumber)
            do matrix.Matrix.Add ((technologyTypeTermSourceREFLabel,i),           tt.TermSourceREF)
            do matrix.Matrix.Add ((technologyPlatformLabel,i),                    (Option.defaultValue "" (a.TechnologyPlatform |> Option.map ArcAssay.composeTechnologyPlatform)))
            do matrix.Matrix.Add ((fileNameLabel,i),                              processedFileName)

            if Array.isEmpty a.Comments |> not then
                a.Comments
                |> Array.iter (fun comment -> 
                    let n,v = comment |> Comment.toString
                    commentKeys <- n :: commentKeys
                    matrix.Matrix.Add((n,i),v)
                )   
        )
        {matrix with CommentKeys = commentKeys |> List.distinct |> List.rev}

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber,?prefix = prefix)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)
 
    let toRows prefix (assays : ArcAssay list) =
        assays
        |> toSparseTable
        |> fun m -> SparseTable.ToRows(m,?prefix = prefix)
