module ARCtrl.ISA.CommentArray

/// If a comment with the given key exists in the [], return its value, else return None
let tryItem (key: string) (comments : Comment []) =
    comments
    |> Array.tryPick (fun c -> 
        match c.Name with
        | Some n when n = key -> c.Value
        | _ -> None        
    )

/// Returns true, if the key exists in the []
let containsKey (key: string) (comments : Comment []) =
    comments
    |> Array.exists (fun c -> 
        match c.Name with
        | Some n when n = key -> true
        | _ -> false        
    )

/// If a comment with the given key exists in the [], return its value
let item (key: string) (comments : Comment []) =
    (tryItem key comments).Value

/// Create a map of comment keys to comment values
let toMap (comments : Comment []) =
    comments
    |> Array.choose (fun c -> 
        match c.Name with
        | Some n -> Some (n,c.Value)
        | _ -> None
    )
    |> Map.ofArray
  
/// Adds the given comment to the comment []  
let add (comment : Comment) (comments : Comment []) =
    Array.append comments [|comment|]

/// Add the given comment to the comment [] if it doesnt exist, else replace it 
let set (comment : Comment) (comments : Comment []) =
    if containsKey comment.Name.Value comments then
        comments
        |> Array.map (fun c -> if c.Name = comment.Name then comment else c)
    else
        Array.append comments [|comment|]

/// Returns a new comment [] where comments with the given key are filtered out
let dropByKey (key: string) (comments : Comment []) =
    comments
    |> Array.filter (fun c -> 
        match c.Name with
        | Some n when n = key -> false
        | _ -> true        
    )