namespace Main

open ARCtrl.Contract

module ArcScaffold =

    let tryWriteDefaultGitignoreAsync (arcPath: string) =
        [| ARCtrl.Contract.Git.gitignoreContract |]
        |> fullFillContractBatchAsync arcPath
