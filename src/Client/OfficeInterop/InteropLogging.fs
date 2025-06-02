module InteropLogging

open System

/// This module is used to store log identifiers for the GenericLog Message
type LogIdentifier =
    | Debug
    | Info
    | Warning
    | Error

    static member ofString str =
        match str with
        | "Debug" -> Debug
        | "Info" -> Info
        | "Error" -> Error
        | "Warning" -> Warning
        | anythingElse -> failwith $"Unable to parse {anythingElse} to LogIdentifier."

[<RequireQualifiedAccess>]
type Msg = {
    LogIdentifier: LogIdentifier
    MessageTxt: string
} with

    static member create logIdent msgTxt = {
        LogIdentifier = logIdent
        MessageTxt = msgTxt
    }

let NoActiveTableMsg =
    Msg.create Error "Error! No annotation table found in active worksheet!"