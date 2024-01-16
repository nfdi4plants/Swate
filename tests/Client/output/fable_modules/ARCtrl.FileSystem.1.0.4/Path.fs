module ARCtrl.Path 
    
open System

let [<Literal>] PathSeperator = '/'
let [<Literal>] PathSeperatorWindows = '\\'
let seperators = [|PathSeperator; PathSeperatorWindows|]

let split(path: string) = 
    path.Split(seperators, enum<StringSplitOptions>(3))

let combine (path1 : string) (path2 : string) : string = 
    let path1_trimmed = path1.TrimEnd(seperators)
    let path2_trimmed = path2.TrimStart(seperators)
    let combined = path1_trimmed + string PathSeperator + path2_trimmed
    combined // should we trim any excessive path seperators?

let combineMany (paths : string []) : string = 
    paths 
    |> Array.mapi (fun i p -> 
        if i = 0 then p.TrimEnd(seperators)
        elif i = (paths.Length-1) then p.TrimStart(seperators)
        else
            p.Trim(seperators)
    )
    |> String.concat(string PathSeperator)

let getFileName (path: string) : string =
    split path |> Array.last

/// <summary>
/// Checks if `path` points to a file with the name `fileName`
/// </summary>
/// <param name="fileName">The name of the file the path should point to.</param>
/// <param name="path">The path to a file.</param>
let isFile (fileName: string) (path: string) = (getFileName path) = fileName