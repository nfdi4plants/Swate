namespace ARCtrl.ISA.Spreadsheet

open FsSpreadsheet
open ARCtrl.ISA

open System.Collections.Generic

module OntologySourceReference = 

    let nameLabel = "Term Source Name"
    let fileLabel = "Term Source File"
    let versionLabel = "Term Source Version"
    let descriptionLabel = "Term Source Description"

    
    let labels = [nameLabel;fileLabel;versionLabel;descriptionLabel]

    let fromString description file name version comments =
        OntologySourceReference.make
            (description)
            (file)
            (name)
            (version)
            (Option.fromValueWithDefault [||] comments)

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            OntologySourceReference.create(Comments = comments)
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> Array.ofList

                fromString
                    (matrix.TryGetValue(descriptionLabel,i))
                    (matrix.TryGetValue(fileLabel,i))
                    (matrix.TryGetValue(nameLabel,i))
                    (matrix.TryGetValue(versionLabel,i))
                    comments
            )

    let toSparseTable (ontologySources: OntologySourceReference list) =
        let matrix = SparseTable.Create (keys = labels,length=ontologySources.Length + 1)
        let mutable commentKeys = []
        ontologySources
        |> List.iteri (fun i o ->
            let i = i + 1
            do matrix.Matrix.Add ((nameLabel,i),        (Option.defaultValue "" o.Name))
            do matrix.Matrix.Add ((fileLabel,i),        (Option.defaultValue "" o.File))
            do matrix.Matrix.Add ((versionLabel,i),     (Option.defaultValue "" o.Version))
            do matrix.Matrix.Add ((descriptionLabel,i), (Option.defaultValue "" o.Description))

            match o.Comments with 
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

    let fromRows lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)
    
    let toRows (termSources : OntologySourceReference list) =
        termSources
        |> toSparseTable
        |> fun m -> SparseTable.ToRows(m)