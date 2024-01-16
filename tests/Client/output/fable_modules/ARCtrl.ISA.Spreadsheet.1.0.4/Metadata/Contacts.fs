namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open Comment
open Remark
open System.Collections.Generic

module Contacts = 

    let lastNameLabel = "Last Name"
    let firstNameLabel = "First Name"
    let midInitialsLabel = "Mid Initials"
    let emailLabel = "Email"
    let phoneLabel = "Phone"
    let faxLabel = "Fax"
    let addressLabel = "Address"
    let affiliationLabel = "Affiliation"
    let rolesLabel = "Roles"
    let rolesTermAccessionNumberLabel = "Roles Term Accession Number"
    let rolesTermSourceREFLabel = "Roles Term Source REF"

    let labels = [lastNameLabel;firstNameLabel;midInitialsLabel;emailLabel;phoneLabel;faxLabel;addressLabel;affiliationLabel;rolesLabel;rolesTermAccessionNumberLabel;rolesTermSourceREFLabel]

    let fromString lastName firstName midInitials email phone fax address affiliation role rolesTermAccessionNumber rolesTermSourceREF comments =
        let roles = OntologyAnnotation.fromAggregatedStrings ';' role rolesTermSourceREF rolesTermAccessionNumber
        Person.make 
            None 
            None
            (lastName   ) 
            (firstName  )
            (midInitials) 
            (email      )
            (phone      )
            (fax        )
            (address    )
            (affiliation) 
            (Option.fromValueWithDefault [||] roles    )
            (Option.fromValueWithDefault [||] comments )
        |> Person.setOrcidFromComments

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            Person.create(Comments = comments)
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 
                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> Array.ofList
                fromString
                    (matrix.TryGetValue(lastNameLabel,i))
                    (matrix.TryGetValue(firstNameLabel,i))
                    (matrix.TryGetValue(midInitialsLabel,i))
                    (matrix.TryGetValue(emailLabel,i))
                    (matrix.TryGetValue(phoneLabel,i))
                    (matrix.TryGetValue(faxLabel,i))
                    (matrix.TryGetValue(addressLabel,i))
                    (matrix.TryGetValue(affiliationLabel,i))
                    (matrix.TryGetValueDefault("",(rolesLabel,i)))
                    (matrix.TryGetValueDefault("",(rolesTermAccessionNumberLabel,i)))
                    (matrix.TryGetValueDefault("",(rolesTermSourceREFLabel,i)))
                    comments
            )

    let toSparseTable (persons:Person list) =
        let matrix = SparseTable.Create (keys = labels,length=persons.Length + 1)
        let mutable commentKeys = []
        persons
        |> List.map Person.setCommentFromORCID
        |> List.iteri (fun i p ->
            let i = i + 1
            let rAgg = Option.defaultValue [||] p.Roles |> OntologyAnnotation.toAggregatedStrings ';'
            do matrix.Matrix.Add ((lastNameLabel,i),                    (Option.defaultValue ""  p.LastName     ))
            do matrix.Matrix.Add ((firstNameLabel,i),                   (Option.defaultValue ""  p.FirstName    ))
            do matrix.Matrix.Add ((midInitialsLabel,i),                 (Option.defaultValue ""  p.MidInitials  ))
            do matrix.Matrix.Add ((emailLabel,i),                       (Option.defaultValue ""  p.EMail        ))
            do matrix.Matrix.Add ((phoneLabel,i),                       (Option.defaultValue ""  p.Phone        ))
            do matrix.Matrix.Add ((faxLabel,i),                         (Option.defaultValue ""  p.Fax          ))
            do matrix.Matrix.Add ((addressLabel,i),                     (Option.defaultValue ""  p.Address      ))
            do matrix.Matrix.Add ((affiliationLabel,i),                 (Option.defaultValue ""  p.Affiliation  ))
            do matrix.Matrix.Add ((rolesLabel,i),                       rAgg.TermNameAgg)  
            do matrix.Matrix.Add ((rolesTermAccessionNumberLabel,i),    rAgg.TermAccessionNumberAgg)
            do matrix.Matrix.Add ((rolesTermSourceREFLabel,i),          rAgg.TermSourceREFAgg)

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
        SparseTable.FromRows(rows,labels,lineNumber,?prefix = prefix)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toRows (prefix : string option) (persons : Person list) =
        persons
        |> toSparseTable
        |> fun m -> 
            match prefix with 
            | Some prefix -> SparseTable.ToRows(m,prefix)
            | None -> SparseTable.ToRows(m)