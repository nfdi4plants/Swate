module Main.Bindings.Filesystem

open Fable.Core

[<JS.PojoAttribute>]
type MkdirOptions(?recursive: bool) =
    member val recursive: bool option = recursive with get, set

[<StringEnum(CaseRules.LowerFirst)>]
type TextEncoding = | Utf8

[<Import("mkdirSync", "fs")>]
let mkdirSync (path: string) (options: obj) : unit = jsNative

[<Import("existsSync", "fs")>]
let existsSync (path: string) : bool = jsNative

[<Import("readFileSync", "fs")>]
let readFileSync (path: string) (encoding: TextEncoding) : string = jsNative

[<Import("writeFileSync", "fs")>]
let writeFileSync (path: string) (content: string) (encoding: TextEncoding) : unit = jsNative

[<Import("renameSync", "fs")>]
let renameSync (oldPath: string) (newPath: string) : unit = jsNative

[<Import("unlinkSync", "fs")>]
let unlinkSync (path: string) : unit = jsNative

[<Import("readdirSync", "fs")>]
let readdirSync (path: string) : string array = jsNative