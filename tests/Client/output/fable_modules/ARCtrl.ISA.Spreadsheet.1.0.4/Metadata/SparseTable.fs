namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open ARCtrl.ISA.Aux
open System.Collections.Generic
open FsSpreadsheet.DSL
open FsSpreadsheet


type SparseRow = (int * string) seq

module SparseRow = 

    let fromValues (v : string seq) : SparseRow = Seq.indexed v 
    
    let getValues (i : SparseRow) = i |> Seq.map snd
    
    let fromAllValues (v : string option seq) : SparseRow = 
        Seq.indexed v 
        |> Seq.choose (fun (i,o) -> Option.map (fun v -> i,v) o)
    
    let getAllValues (i : SparseRow) = 
        let m = i |> Map.ofSeq
        let max = i |> Seq.maxBy fst |> fst
        Seq.init (max + 1) (fun i -> Map.tryFind i m)

    let fromFsRow (r : FsRow) = 
        r.Cells
        |> Seq.choose (fun c -> 
            if c.Value = "" then Option.None 
            else Option.Some (c.ColumnNumber - 1, c.ValueAsString())          
        )

    let tryGetValueAt i (vs : SparseRow) =
        vs 
        |> Seq.tryPick (fun (index,v) -> if index = i then Option.Some v else None)

    let toDSLRow (vs : SparseRow) =

        row {
            for v in getAllValues vs do
                match v with
                | Option.Some v -> cell {v}
                | None -> cell {""}
        }
        
    let readFromSheet (sheet : FsWorksheet) =
        let rows = sheet.Rows |> Seq.map fromFsRow
        rows

    let writeToSheet (rowI : int) (row : SparseRow) (sheet : FsWorksheet) =
        let fsRow = sheet.Row(rowI)
        row
        |> Seq.iter (fun (colI,v) -> 
            if v.Trim() <> "" then fsRow.[colI + 1].SetValueAs v)

type SparseTable = 

    {
        Matrix : Dictionary<string*int,string>
        Keys : string list
        CommentKeys : string list
        ///Column Count
        ColumnCount : int
    }

    member this.TryGetValue(key) =
        Dictionary.tryGetValue key this.Matrix

    member this.TryGetValueDefault(defaultValue,key) =
        if this.Matrix.ContainsKey(key) then
            this.Matrix.Item(key)
        else
            defaultValue

    static member Create(?matrix,?keys,?commentKeys,?length) = 
        {
            Matrix= Option.defaultValue (Dictionary()) matrix
            Keys = Option.defaultValue [] keys
            CommentKeys = Option.defaultValue [] commentKeys
            ColumnCount = Option.defaultValue 0 length
        }

    static member AddRow key (values:seq<int*string>) (matrix : SparseTable) =

        values 
        |> Seq.iter (fun (i,v) -> 
            matrix.Matrix.Add((key,i),v)
        )

        let length = 
            if Seq.isEmpty values then 0
            else Seq.maxBy fst values |> fst |> (+) 1
        
        {matrix with 
            Keys = List.append matrix.Keys [key]
            ColumnCount = if length > matrix.ColumnCount then length else matrix.ColumnCount
        }

    static member AddEmptyComment key (matrix : SparseTable) =
        
        {matrix with 
            CommentKeys = List.append matrix.CommentKeys [key]
        }

    static member AddComment key (values:seq<int*string>) (matrix : SparseTable) =
       
        if Seq.length values = 0 then 
            SparseTable.AddEmptyComment key matrix
        else 
            values 
            |> Seq.iter (fun (i,v) -> 
                matrix.Matrix.Add((key,i),v)
            )

            let length = 
                if Seq.isEmpty values then 0
                else Seq.maxBy fst values |> fst |> (+) 1
        
            {matrix with 
                CommentKeys = List.append matrix.CommentKeys [key]
                ColumnCount = if length > matrix.ColumnCount then length else matrix.ColumnCount
            }

    static member FromRows(en:IEnumerator<SparseRow>,labels,lineNumber,?prefix) =
        try
            let prefix = match prefix with | Option.Some p -> p + " " | None -> ""
            let rec loop (matrix : SparseTable) remarks lineNumber = 

                if en.MoveNext() then  
                    let row = en.Current |> Seq.map (fun (i,v) -> int i - 1,v)
                    let key,vals = Seq.tryItem 0 row |> Option.map snd, Seq.trySkip 1 row
                    match key,vals with
                    | Comment.Comment k, Option.Some v -> 
                        loop (SparseTable.AddComment k v matrix) remarks (lineNumber + 1)

                    | Remark.Remark k, _  -> 
                        loop matrix (Remark.make lineNumber k :: remarks) (lineNumber + 1)

                    | Option.Some k, Option.Some v when List.exists (fun label -> k = prefix + label) labels -> 
                        let label = List.find (fun label -> k = prefix + label) labels
                        loop (SparseTable.AddRow label v matrix) remarks (lineNumber + 1)

                    | Option.Some k, _ -> Option.Some k,lineNumber,remarks, matrix
                    | _ -> None, lineNumber,remarks, matrix
                else
                    None,lineNumber,remarks, matrix
            loop (SparseTable.Create()) [] lineNumber
        with
        | err -> failwithf "Error parsing block in investigation file starting from line number %i: %s" lineNumber err.Message

    static member ToRows(matrix,?prefix) : SparseRow seq =
        let prefix = match prefix with | Option.Some p -> p + " " | None -> ""
        seq {
            for key in matrix.Keys do
                (SparseRow.fromValues (prefix + key :: List.init (matrix.ColumnCount - 1) (fun i -> matrix.TryGetValueDefault("",(key,i + 1)))))
            for key in matrix.CommentKeys do
                (SparseRow.fromValues (Comment.wrapCommentKey key :: List.init (matrix.ColumnCount - 1) (fun i -> matrix.TryGetValueDefault("",(key,i + 1)))))
        }

    static member GetEmptyComments(matrix) : Comment [] = 
        matrix.CommentKeys
        |> List.map (fun key -> Comment.create(Name = key))
        |> Array.ofList