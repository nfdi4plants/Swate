namespace Main

open ARCtrl.Contract

module ArcScaffold =

    let defaultGitignoreContent =
        ("""# ARC local/generated artifacts
/arc-validate-results.xml
/.cwl/

# OS-generated files
.DS_Store
.AppleDouble
.LSOverride
Icon
._*
.Spotlight-V100
.TemporaryItems
.Trashes
Thumbs.db
Thumbs.db:encryptable
ehthumbs.db
ehthumbs_vista.db
[Dd]esktop.ini
$RECYCLE.BIN/
*~
.fuse_hidden*
.directory
.Trash-*
.nfs*

# Editor and spreadsheet temporary files
~$*
*.tmp
*.temp
*.bak
*.swp
*.swo""")
            .Replace("\r\n", "\n")

    let defaultGitignoreContract =
        Contract.createCreate (".gitignore", DTOType.PlainText, DTO.Text defaultGitignoreContent)

    let tryWriteDefaultGitignoreAsync (arcPath: string) =
        [| defaultGitignoreContract |] |> fullFillContractBatchAsync arcPath
