namespace FsSpreadsheet.DSL

open Microsoft.FSharp.Quotations
open FsSpreadsheet
open Expression

[<AutoOpen>]
module Operators = 
 
    #if FABLE_COMPILER
    #else
    let inline parseExpression (def : string -> SheetEntity<Value>) (s : Expr<'a>) : SheetEntity<Value> =
        try 
            let value = eval<'a> s |> DataType.InferCellValue
            SheetEntity.some value         
        with
        | err -> def err.Message
    #endif

    let inline parseOption (def : string -> SheetEntity<Value>) (s : Option<'a>) : SheetEntity<Value> =
        match s with
        | Option.Some value ->
            DataType.InferCellValue value
            |> SheetEntity.some  
        | None -> def "Value was missing"
    
    let inline parseResult (def : string -> SheetEntity<Value>) (s : Result<'a,exn>) : SheetEntity<Value> =
        match s with
        | Result.Ok value ->
            DataType.InferCellValue value
            |> SheetEntity.some  
        | Result.Error exn -> def exn.Message

    let inline parseAny (f : string -> SheetEntity<Value>) (v: 'T) : SheetEntity<Value> =
        match box v with
        #if FABLE_COMPILER
        #else
        | :? Expr<string> as e ->           parseExpression f e
        | :? Expr<int> as e ->              parseExpression f e
        | :? Expr<float> as e ->            parseExpression f e
        | :? Expr<single> as e ->           parseExpression f e
        | :? Expr<byte> as e ->             parseExpression f e
        | :? Expr<System.DateTime> as e ->  parseExpression f e
        #endif  

        | :? Option<string> as o ->             parseOption f o
        | :? Option<int> as o ->                parseOption f o
        | :? Option<float> as o ->              parseOption f o
        | :? Option<single> as o ->             parseOption f o
        | :? Option<byte> as o ->               parseOption f o
        | :? Option<System.DateTime> as o ->    parseOption f o

        | :? Result<string,exn> as r -> parseResult f r
        | :? Result<int,exn> as r -> parseResult f r
        | :? Result<float,exn> as r -> parseResult f r
        | :? Result<single,exn> as r -> parseResult f r
        | :? Result<byte,exn> as r -> parseResult f r
        | :? Result<System.DateTime,exn> as r -> parseResult f r

        | v -> failwith $"Could not parse value {v}. Only string,int,float,single,byte,System.DateTime allowed."

    /// Required value operator
    ///
    /// If expression does fail, returns a missing required value
    let inline (~+.) (v : 'T) : SheetEntity<Value> =
        let f = fun s -> NoneRequired([message s])
        parseAny f v

    /// Optional value operator
    ///
    /// If expression does fail, returns a missing optional value
    let inline (~-.) (v : 'T) : SheetEntity<Value> =
        let f = fun s -> NoneOptional([message s])
        parseAny f v 

    /// Required value operator
    ///
    /// If expression does fail, returns a missing required value
    let inline (+.) (f : 'T -> 'U) (v : 'T) : SheetEntity<Value> =
        try 
            f v 
            |> DataType.InferCellValue
            |> SheetEntity.some
        with 
        | err -> NoneRequired([Exception err])

    /// Optional value operator
    ///
    /// If expression does fail, returns a missing optional value
    let inline (-.) (f : 'T -> 'U) (v : 'T) : SheetEntity<Value> =
        try 
            f v 
            |> DataType.InferCellValue
            |> SheetEntity.some
        with 
        | err -> NoneOptional([Exception err])

    /// Optional operators for cell, row, column and sheet expressions
    let optional = OptionalSource()

    /// Required operators for cell, row, column and sheet expressions
    let required = RequiredSource()