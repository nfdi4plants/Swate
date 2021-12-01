module HumanReadableIds

open Fable.Core.JsInterop

type HRI =
    abstract member random : unit -> string

// https://www.npmjs.com/package/human-readable-ids
let hri : HRI = (importDefault "human-readable-ids")?hri

let tableName() =
    let hriRnd = hri.random()
    hriRnd.Split([|"-"|], System.StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun word ->
        let fl = word.Substring(0,1).ToUpper()
        fl + word.Remove(0,1)
    )
    |> String.concat ""