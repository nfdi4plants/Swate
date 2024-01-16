namespace ARCtrl.ISA

type EMail = string

open Fable.Core

[<AttachMembers>]
type Comment = 
    {
        ID : URI option
        Name : string option
        Value : string option
    }

    static member make id name value : Comment =
        {
            ID      = id
            Name    = name
            Value   = value
        }

    static member create(?Id,?Name,?Value) : Comment =
        Comment.make Id Name Value

    static member fromString name value =
        Comment.create (Name=name,Value=value)
    
    static member toString (comment : Comment) =
        Option.defaultValue "" comment.Name, Option.defaultValue "" comment.Value

    member this.Copy() =
        Comment.make this.ID this.Name this.Value


[<AttachMembers>]
type Remark = 
    {
        Line : int 
        Value : string
    }
    
    static member make line value  : Remark = 
        {
            Line = line 
            Value = value      
        }

    static member create(line,value) : Remark = 
        Remark.make line value

    static member toTuple (remark : Remark ) =
        remark.Line, remark.Value

    member this.Copy() =
        Remark.make this.Line this.Value