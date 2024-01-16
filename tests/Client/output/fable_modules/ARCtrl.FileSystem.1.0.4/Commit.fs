namespace ARCtrl.FileSystem

open Fable.Core

[<AttachMembers>]
type Commit = 
    {
        Hash : string
        UserName : string
        UserEmail : string
        Date : System.DateTime
        Message : string
    }