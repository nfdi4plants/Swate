namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open Comment
open Remark
open System.Collections.Generic

module Publications = 

    let pubMedIDLabel =                     "PubMed ID"
    let doiLabel =                          "DOI"
    let authorListLabel =                   "Author List"
    let titleLabel =                        "Title"
    let statusLabel =                       "Status"
    let statusTermAccessionNumberLabel =    "Status Term Accession Number"
    let statusTermSourceREFLabel =          "Status Term Source REF"

    let labels = [pubMedIDLabel;doiLabel;authorListLabel;titleLabel;statusLabel;statusTermAccessionNumberLabel;statusTermSourceREFLabel]

    let fromString pubMedID doi author title status statusTermSourceREF statusTermAccessionNumber comments =
        let status = OntologyAnnotation.fromString(?termName = status,?tan = statusTermAccessionNumber,?tsr = statusTermSourceREF)
        Publication.make 
            (pubMedID |> Option.map URI.fromString)
            (doi)
            (author)
            (title) 
            (Option.fromValueWithDefault OntologyAnnotation.empty status) 
            (Option.fromValueWithDefault [||] comments)

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            Publication.create(Comments = comments)
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> Array.ofList

                fromString
                    (matrix.TryGetValue(pubMedIDLabel,i))            
                    (matrix.TryGetValue(doiLabel,i))             
                    (matrix.TryGetValue(authorListLabel,i))         
                    (matrix.TryGetValue(titleLabel,i))                 
                    (matrix.TryGetValue(statusLabel,i))                
                    (matrix.TryGetValue((statusTermSourceREFLabel,i)))    
                    (matrix.TryGetValue((statusTermAccessionNumberLabel,i)))
                    comments
            )

    let toSparseTable (publications: Publication list) =
        let matrix = SparseTable.Create (keys = labels,length=publications.Length + 1)
        let mutable commentKeys = []
        publications
        |> List.iteri (fun i p ->
            let i = i + 1
            let s = Option.defaultValue OntologyAnnotation.empty p.Status |> fun s -> OntologyAnnotation.toString (s,true)
            do matrix.Matrix.Add ((pubMedIDLabel,i),                    (Option.defaultValue "" p.PubMedID))
            do matrix.Matrix.Add ((doiLabel,i),                         (Option.defaultValue "" p.DOI))
            do matrix.Matrix.Add ((authorListLabel,i),                  (Option.defaultValue "" p.Authors))
            do matrix.Matrix.Add ((titleLabel,i),                       (Option.defaultValue "" p.Title))
            do matrix.Matrix.Add ((statusLabel,i),                      s.TermName)
            do matrix.Matrix.Add ((statusTermAccessionNumberLabel,i),   s.TermAccessionNumber)
            do matrix.Matrix.Add ((statusTermSourceREFLabel,i),         s.TermSourceREF)

            match p.Comments with 
            | None -> ()
            | Some c ->
                c
                |> Array.iter (fun comment -> 
                    let n,v = comment |> Comment.toString
                    commentKeys <- n :: commentKeys
                    matrix.Matrix.Add((n,i),v)
                )
        )
        {matrix with CommentKeys = commentKeys |> List.distinct |> List.rev} 

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        match prefix with
        | Some p -> SparseTable.FromRows(rows,labels,lineNumber,p)
        | None -> SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toRows prefix (publications : Publication list) =
        publications
        |> toSparseTable
        |> fun m -> 
            match prefix with 
            | Some prefix -> SparseTable.ToRows(m,prefix)
            | None -> SparseTable.ToRows(m)