namespace Shared

module Regex =

    module Pattern =

        // Checks

        //Source Name
        //Sample Name
        //Characteristics [Sample type]
        //Characteristics [biological replicate]
        //Factor [Sample type#2]
        //Parameter [biological replicate#2]
        //Data File Name
        //Term Source REF (NFDI4PSO:0000064)
        //Term Source REF (NFDI4PSO:0000064#2)
        //Term Accession Number (MS:1001809)
        //Term Accession Number (MS:1001809#2)
        //Unit
        //Unit (#3)
        //"http://purl.obolibrary.org/obo/NFDI4PSO_0000064"

        [<LiteralAttribute>]
        let IdPattern = "#\d+" //  @"(?<=#)\d+(?=[\)\]])" <- Cannot be used in IE11

        [<LiteralAttribute>]
        // <summary> This pattern captures characters between squared brackets, without id: Parameter [biological replicate#2] -> biological replicate</summary>
        let SquaredBracketsTermNamePattern = "\[.*\]" //  @"(?<= \[)[^#\]]*(?=[\]#])" <- Cannot be used in IE11

        [<LiteralAttribute>]
        // Used to get unit name from Excel numberFormat: 0.00 "degree Celsius"
        let DoubleQuotesPattern = "\"(.*?)\""

        [<LiteralAttribute>]
        // This pattern captures all input coming before an opening square bracket or normal bracket (with whitespace).
        let CoreNamePattern = "^[^[(]*"

        //// Hits term accession, without id, NEEDS brackets before and after: ENVO:01001831
        //[<LiteralAttribute>]
        //let TermAccessionPattern = @"(?<=\()\S+[:_][^;)#]*(?=[\)\#])" //"[a-zA-Z0-9]+?[:_][a-zA-Z0-9]+"

        // Hits term accession, without id: ENVO:01001831
        [<LiteralAttribute>]
        let TermAccessionPattern = @"[\w]+?:[\d]+"

        // https://obofoundry.org/id-policy.html#mapping-of-owl-ids-to-obo-format-ids
        // <summary>Regex pattern is designed to hit only Foundry-compliant URIs.</summary>
        [<LiteralAttribute>]
        let TermAccessionPatternURI = @"http://purl.obolibrary.org/obo/(?<idspace>[\w]+?)_(?<localid>[\d]+)"

    module Aux =
    
        open System.Text.RegularExpressions
    
        /// (|Regex|_|) pattern input
        let (|Regex|_|) pattern input =
            let m = Regex.Match(input, pattern)
            if m.Success then Some(m)
            else None

    open Pattern
    open Aux
    open System
    open System.Text.RegularExpressions

    let parseSquaredTermNameBrackets (headerStr:string) =
        match headerStr with
        | Regex SquaredBracketsTermNamePattern value ->
            // trim whitespace AND remove brackets
            value.Value.Trim().[1..value.Length-2]
            // remove #id pattern
            |> fun str -> Regex.Replace(str, IdPattern, "")
            |> Some 
        | _ ->
            None

    let parseCoreName (headerStr:string) =
        match headerStr with
        | Regex CoreNamePattern value ->
            value.Value.Trim()
            |> Some
        | _ ->
            None

    //let parseTermAccession (headerStr:string) =
    //    printfn "parse.."
    //    match headerStr with
    //    | Regex TermAccessionPattern value ->
    //        value.Trim().Replace('_',':')
    //        |> Some
    //    | _ ->
    //        None

    /// <summary>This function can be used to extract `IDSPACE:LOCALID` (or: `Term Accession` from Swate header strings or obofoundry conform URI strings.</summary>
    let parseTermAccession (headerStr:string) =
        match headerStr.Trim() with
        | Regex TermAccessionPattern value ->
            value.Value.Trim()
            |> Some
        | Regex TermAccessionPatternURI value ->
            let idspace = value.Groups.["idspace"].Value
            let localid = value.Groups.["localid"].Value
            idspace + ":" + localid
            |> Some
        | _ ->
            None

    let parseDoubleQuotes (headerStr:string) =
        match headerStr with
        | Regex DoubleQuotesPattern value ->
            // remove quotes at beginning and end of matched string
            value.Value.[1..value.Length-2].Trim()
            |> Some
        | _ ->
            None

    let getId (headerStr:string) =
        match headerStr with
        | Regex IdPattern value -> value.Value.Trim().[1..] |> Some
        | _ ->
            None