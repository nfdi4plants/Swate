module Main.Bindings

open Fable.Core.JsInterop

type IFS =
    abstract member existsSync: path: string -> bool

let fs: IFS = importDefault "fs"