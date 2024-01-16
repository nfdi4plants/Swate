namespace ARCtrl.FileSystem

open Fable.Core

[<AttachMembers>]
type FileSystem = 
    {
        Tree : FileSystemTree
        History : Commit array
    }
    
    [<NamedParams>]
    static member create(tree : FileSystemTree, ?history : Commit array) = 
        let history = defaultArg history [||]
        {
            Tree = tree
            History = history
        }

    member this.AddFile (path: string) : FileSystem =
        {
            this with
                Tree = this.Tree.AddFile(path)
        }

    static member addFile (path: string) =
        fun (fs: FileSystem) -> fs.AddFile(path)

    static member fromFilePaths (paths : string []) : FileSystem =
        let tree = FileSystemTree.fromFilePaths paths
        FileSystem.create(tree)

    member this.Union(other : FileSystem) : FileSystem =
        let tree = this.Tree.Union(other.Tree)
        let history = Array.append this.History other.History
        FileSystem.create(tree, history)

    member this.Copy() =
        let fstCopy = this.Tree.Copy()
        let historyCopy = this.History |> Array.map id
        FileSystem.create(fstCopy,historyCopy)