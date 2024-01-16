module ARCtrl.Template.Spreadsheet

open FsSpreadsheet
open ARCtrl.ISA.Aux
open ARCtrl.ISA.Spreadsheet
open ARCtrl.ISA
open System.Collections.Generic

exception TemplateReadError of string

module Metadata = 
    
    
    module ER = 

        let [<Literal>] erLabel = "ER"
        let [<Literal>] erTermAccessionNumberLabel = "ER Term Accession Number"
        let [<Literal>] erTermSourceREFLabel = "ER Term Source REF"

        let labels = [erLabel;erTermAccessionNumberLabel;erTermSourceREFLabel]

        let fromSparseTable (matrix : SparseTable) =
            OntologyAnnotationSection.fromSparseTable erLabel erTermSourceREFLabel erTermAccessionNumberLabel matrix

        let toSparseTable (designs: OntologyAnnotation list) =
            OntologyAnnotationSection.toSparseTable erLabel erTermSourceREFLabel erTermAccessionNumberLabel designs

        let fromRows (prefix : string option) (rows : IEnumerator<SparseRow>) =
            let nextHeader, _, _, ers = OntologyAnnotationSection.fromRows prefix erLabel erTermSourceREFLabel erTermAccessionNumberLabel 0 rows
            nextHeader,ers
    
        let toRows (prefix : string option) (designs : OntologyAnnotation list) =
            OntologyAnnotationSection.toRows prefix erLabel erTermSourceREFLabel erTermAccessionNumberLabel designs

    module Tags = 

        let [<Literal>] tagsLabel = "Tags"
        let [<Literal>] tagsTermAccessionNumberLabel = "Tags Term Accession Number"
        let [<Literal>] tagsTermSourceREFLabel = "Tags Term Source REF"

        let labels = [tagsLabel;tagsTermAccessionNumberLabel;tagsTermSourceREFLabel]

        let fromSparseTable (matrix : SparseTable) =
            OntologyAnnotationSection.fromSparseTable tagsLabel tagsTermSourceREFLabel tagsTermAccessionNumberLabel matrix

        let toSparseTable (designs: OntologyAnnotation list) =
            OntologyAnnotationSection.toSparseTable tagsLabel tagsTermSourceREFLabel tagsTermAccessionNumberLabel designs

        let fromRows (prefix : string option) (rows : IEnumerator<SparseRow>) =
            let nextHeader, _, _, tags = OntologyAnnotationSection.fromRows prefix tagsLabel tagsTermSourceREFLabel tagsTermAccessionNumberLabel 0 rows
            nextHeader, tags

        let toRows (prefix : string option) (designs : OntologyAnnotation list) =
            OntologyAnnotationSection.toRows prefix tagsLabel tagsTermSourceREFLabel tagsTermAccessionNumberLabel designs

    module Authors = 

        let [<Literal>] obsoleteORCIDLabel = "Authors ORCID"

    module Template = 
  
        let [<Literal>] identifierLabel = "Id"
        let [<Literal>] nameLabel = "Name"
        let [<Literal>] versionLabel = "Version"
        let [<Literal>] descriptionLabel = "Description"
        let [<Literal>] organisationLabel = "Organisation"
        let [<Literal>] tableLabel = "Table"

        let [<Literal>] authorsLabelPrefix = "Author"

        let [<Literal>] templateLabel = "TEMPLATE"

        let [<Literal>] authorsLabel = "AUTHORS"
        let [<Literal>] erLabel = "ERS"
        let [<Literal>] tagsLabel = "TAGS"

        let [<Literal>] obsoleteAuthorsLabel = "#AUTHORS list"
        let [<Literal>] obsoleteErLabel = "#ER list"
        let [<Literal>] obsoleteTagsLabel = "#TAGS list"
       
        type TemplateInfo =
            {
            Id : string
            Name : string
            Version : string
            Description : string
            Organisation : string
            Table : string
            Comments : Comment list
            }

            static member create id name version description organisation table comments =
                {Id = id;Name = name;Version = version;Description = description;Organisation = organisation;Table = table;Comments = comments}
  
            static member empty = 
                TemplateInfo.create "" "" "" "" "" "" []

            static member Labels = 
                [identifierLabel;nameLabel;versionLabel;descriptionLabel;organisationLabel;tableLabel]

            static member FromSparseTable (matrix : SparseTable) =
        
                let i = 0

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))

                TemplateInfo.create
                    (matrix.TryGetValueDefault(Identifier.createMissingIdentifier(),(identifierLabel,i)))  
                    (matrix.TryGetValueDefault("",(nameLabel,i)))  
                    (matrix.TryGetValueDefault("",(versionLabel,i)))  
                    (matrix.TryGetValueDefault("",(descriptionLabel,i)))  
                    (matrix.TryGetValueDefault("",(organisationLabel,i)))  
                    (matrix.TryGetValueDefault("",(tableLabel,i)))                    
                    comments


            static member ToSparseTable (template: Template) =
                let i = 1
                let matrix = SparseTable.Create (keys = TemplateInfo.Labels,length = 2)
                let mutable commentKeys = []
                let processedIdentifier =
                    if template.Id.ToString().StartsWith(Identifier.MISSING_IDENTIFIER) then "" else 
                        template.Id.ToString()

                do matrix.Matrix.Add ((identifierLabel,i),          processedIdentifier)
                do matrix.Matrix.Add ((nameLabel,i),               (template.Name))
                do matrix.Matrix.Add ((versionLabel,i),      (template.Version))
                do matrix.Matrix.Add ((descriptionLabel,i),         (template.Description))
                do matrix.Matrix.Add ((organisationLabel,i),   (template.Organisation.ToString()))
                do matrix.Matrix.Add ((tableLabel,i),            template.Table.Name)

                {matrix with CommentKeys = commentKeys |> List.distinct |> List.rev}

            static member fromRows (rows : IEnumerator<SparseRow>) =
                SparseTable.FromRows(rows,TemplateInfo.Labels,0)
                |> fun (s,ln,rs,sm) -> (s,TemplateInfo.FromSparseTable sm)
    
            static member toRows (template : Template) =  
                template
                |> TemplateInfo.ToSparseTable
                |> SparseTable.ToRows
    
        
        let mapDeprecatedKeys (rows : seq<SparseRow>)  = 
            rows
            |> Seq.map (fun r -> 
                r
                |> Seq.map (fun (k,v) ->
                    if k = 0 then 
                        match v with
                        | v when v = obsoleteAuthorsLabel -> k,authorsLabel
                        | v when v = obsoleteErLabel -> k,erLabel
                        | v when v = obsoleteTagsLabel -> k,tagsLabel

                        | v when v = Authors.obsoleteORCIDLabel -> k,$"Comment[{Person.orcidKey}]"

                        | v when v = "Authors Last Name"                    -> k,"Author Last Name"
                        | v when v = "Authors First Name"                   -> k,"Author First Name"
                        | v when v = "Authors Mid Initials"                 -> k,"Author Mid Initials"
                        | v when v = "Authors Email"                        -> k,"Author Email"
                        | v when v = "Authors Phone"                        -> k,"Author Phone"
                        | v when v = "Authors Fax"                          -> k,"Author Fax"
                        | v when v = "Authors Address"                      -> k,"Author Address"
                        | v when v = "Authors Affiliation"                  -> k,"Author Affiliation"
                        | v when v = "Authors Role"                         -> k,"Author Roles"
                        | v when v = "Authors Role Term Accession Number"   -> k,"Author Roles Term Accession Number"
                        | v when v = "Authors Role Term Source REF"         -> k,"Author Roles Term Source REF"
                        | v -> (k,v)
                    else (k,v)
                )
            )
            |> fun s -> 
                if Seq.head s |> SparseRow.tryGetValueAt 0 |> Option.exists (fun v -> v = templateLabel) then s else
                    Seq.append (SparseRow.fromValues [templateLabel] |> Seq.singleton) s

        let fromRows (rows : seq<SparseRow>) = 

            let rec loop en lastLine (templateInfo : TemplateInfo) ers tags authors  =
            
                match lastLine with

                | Some k when k = erLabel -> 
                    let currentLine,newERs = ER.fromRows None en
                    loop en currentLine templateInfo (List.append ers newERs) tags authors 

                | Some k when k = tagsLabel -> 
                    let currentLine,newTags = Tags.fromRows None en
                    loop en currentLine templateInfo ers (List.append tags newTags) authors 

                | Some k when k = authorsLabel -> 
                    let currentLine,_,_,newAuthors = Contacts.fromRows (Some authorsLabelPrefix) 0 en
                    loop en currentLine templateInfo ers tags (List.append authors newAuthors)
                | k -> 
                    templateInfo,ers,tags,authors
            let rows = mapDeprecatedKeys rows
            let en = rows.GetEnumerator()
            en.MoveNext() |> ignore
            let currentLine,item = TemplateInfo.fromRows en  
            loop en currentLine item [] [] []

    
        let toRows (template : Template) =
            seq {          
                yield  SparseRow.fromValues [templateLabel]
                yield! TemplateInfo.toRows template

                yield  SparseRow.fromValues [erLabel]
                yield! ER.toRows (None) (template.EndpointRepositories |> Array.toList)

                yield  SparseRow.fromValues [tagsLabel]
                yield! Tags.toRows (None) (template.Tags |> Array.toList)

                yield  SparseRow.fromValues [authorsLabel]
                yield! Contacts.toRows (Some authorsLabelPrefix) (List.ofArray template.Authors)
            }

    
module Template = 
    
    open Metadata
    open Template

    let [<Literal>] metaDataSheetName = "isa_template"
    let [<Literal>] obsoletemetaDataSheetName = "SwateTemplateMetadata"


    let fromParts (templateInfo:TemplateInfo) (ers:OntologyAnnotation list) (tags: OntologyAnnotation list) (authors : Person list) (table : ArcTable) (lastUpdated : System.DateTime)=
            Template.make 
                (System.Guid templateInfo.Id)
                table
                (templateInfo.Name)
                (templateInfo.Description)
                (Organisation.ofString templateInfo.Organisation) 
                (templateInfo.Version)
                (Array.ofList authors)
                (Array.ofList ers)
                (Array.ofList tags)  
                (lastUpdated)

    let toMetadataSheet (template : Template) : FsWorksheet =
        
        let sheet = FsWorksheet(metaDataSheetName)
        Template.toRows template
        |> Seq.iteri (fun rowI r -> SparseRow.writeToSheet (rowI + 1) r sheet)    
        sheet

    let fromMetadataSheet (sheet : FsWorksheet)  =
        sheet.Rows 
        |> Seq.map SparseRow.fromFsRow
        |> Template.fromRows

    /// Reads an assay from a spreadsheet
    let fromFsWorkbook (doc:FsWorkbook) = 
        // Reading the "Assay" metadata sheet. Here metadata 
        let templateInfo,ers,tags,authors = 
        
            match doc.TryGetWorksheetByName metaDataSheetName with 
            | Option.Some sheet ->
                fromMetadataSheet sheet
            | None ->  
                match doc.TryGetWorksheetByName obsoletemetaDataSheetName with 
                | Option.Some sheet ->
                    fromMetadataSheet sheet
                | None ->  
                    Metadata.Template.TemplateInfo.empty,[],[],[]
            
        let tryTableNameMatches (ws : FsWorksheet) = 
            if ws.Tables |> Seq.exists (fun t -> t.Name = templateInfo.Table) then Some ws else None

        let tryWSNameMatches (ws : FsWorksheet) = 
            if ws.Name = templateInfo.Table then Some ws else None

        let sheets = doc.GetWorksheets()
                
        let table = 
            // find worksheet by table = template.table.name
            match sheets |> Seq.tryPick tryTableNameMatches with
            | Some ws -> 
                // convert worksheet to ArcTable
                match ArcTable.tryFromFsWorksheet ws with
                | Some t -> t
                | None -> raise (TemplateReadError $"Ws with name `{ws.Name}` could not be converted to a table")
            | None ->
                // Fallback find worksheet by worksheet.name = template.table.name
                match sheets |> Seq.tryPick tryWSNameMatches with
                | Some ws -> 
                    match ArcTable.tryFromFsWorksheet ws with
                    | Some t -> t
                    | None -> raise (TemplateReadError <| $"Ws with name `{ws.Name}` could not be converted to a table")
                | None -> raise (TemplateReadError $"No worksheet or table with name `{templateInfo.Table}` found")
            
        fromParts templateInfo ers tags authors table (System.DateTime.Now)

    let toFsWorkbook (template : Template) =
        let doc = new FsWorkbook()
        let metaDataSheet = toMetadataSheet template
        doc.AddWorksheet metaDataSheet

        template.Table
        |> ArcTable.toFsWorksheet 
        |> doc.AddWorksheet

        doc


