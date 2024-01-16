namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open System.Text.RegularExpressions

module Comment = 
    let commentRegex = Regex(@"(?<=Comment\[<).*(?=>\])")

    let commentRegexNoAngleBrackets = Regex(@"(?<=Comment\[).*(?=\])")

    let (|Comment|_|) (key : Option<string>) =
        key
        |> Option.bind (fun k ->
            let r = commentRegex.Match(k)
            if r.Success then Some r.Value
            else 
                let r = commentRegexNoAngleBrackets.Match(k)
                if r.Success then Some r.Value
                else None
        )
   
    let wrapCommentKey k = 
        sprintf "Comment[%s]" k

    let fromString k v =
        Comment.make 
            None 
            (Option.fromValueWithDefault "" k) 
            (Option.fromValueWithDefault "" v)

    let toString (c:Comment) =
        Option.defaultValue "" c.Name,    
        Option.defaultValue "" c.Value

module Remark = 

    let remarkRegex = Regex(@"(?<=#).*")


    let (|Remark|_|) (key : Option<string>) =
        key
        |> Option.bind (fun k ->
            let r = remarkRegex.Match(k)
            if r.Success then Some r.Value
            else None
        )


    let wrapRemark r = 
        sprintf "#%s" r