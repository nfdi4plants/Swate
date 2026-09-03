module Git

open System

/// Get all git tags and strip leading 'v' if present
let getTags () =

    let x = runReadAsync "git" [ "tag" ] ""
    let tags, _ = x

    tags.Trim().Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun t -> if t.StartsWith("v") then t.Substring 1 else t)

let createTagAndPush (tag: string) =
    run "git" [ "tag"; tag ] ""

    run "git" [ "push"; "origin"; tag ] ""

    printGreenfn "Tag %s created and pushed" tag
