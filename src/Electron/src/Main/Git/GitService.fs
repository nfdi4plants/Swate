module Main.Git.GitService

///Temporary facade for GitServiceCore and GitServiceValidation to simplify imports in other modules. This will be removed once the GitService is fully split into multiple files.
type GitFailure = GitServiceCore.GitFailure
type GitResult<'T> = GitServiceCore.GitResult<'T>
type GitPushTarget = GitServiceCore.GitPushTarget
type GitPullResult = GitServiceCore.GitPullResult
type GitProgressCallback = GitServiceCore.GitProgressCallback

let classifyFailureKind = GitServiceFailureClassification.classifyFailureKind
let ensureValidBranchLikeName = GitServiceValidation.ensureValidBranchLikeName
let ensureValidPathspec = GitServiceValidation.ensureValidPathspec
let validatePathspecs = GitServiceValidation.validatePathspecs
let validateRemoteName = GitServiceValidation.validateRemoteName
let ensureAllowedRemoteUrl = GitServiceValidation.ensureAllowedRemoteUrl

let tryGetRepositoryWebUrlFromRemoteUrl =
    GitServiceValidation.tryGetRepositoryWebUrlFromRemoteUrl

let tryGetUnsupportedGitContent = GitServiceCore.tryGetUnsupportedGitContent
let resolvePushTarget = GitServiceCore.resolvePushTarget
let getStatus = GitServiceCore.getStatus
let getBranches = GitServiceCore.getBranches
let getLfsSettings = GitServiceCore.getLfsSettings
let getDiffSummary = GitServiceCore.getDiffSummary
let getDiff = GitServiceCore.getDiff
let getWordDiff = GitServiceCore.getWordDiff
let getDiffViewData = GitServiceCore.getDiffViewData
let getMergeConflictViewData = GitServiceCore.getMergeConflictViewData
let fetch = GitServiceCore.fetch
let previewPull = GitServiceCore.previewPull
let pull = GitServiceCore.pull
let executePushWorkflow = GitServiceCore.executePushWorkflow
let push = GitServiceCore.push
let setLfsSettings = GitServiceCore.setLfsSettings
let pruneLfsCache = GitServiceCore.pruneLfsCache
let dedupLfsStorage = GitServiceCore.dedupLfsStorage
let stagePaths = GitServiceCore.stagePaths
let unstagePaths = GitServiceCore.unstagePaths
let freeLocalLfsCopy = GitServiceCore.freeLocalLfsCopy
let discardPaths = GitServiceCore.discardPaths
let commit = GitServiceCore.commit
let confirmMergeResolution = GitServiceCore.confirmMergeResolution
let createBranch = GitServiceCore.createBranch
let addRemote = GitServiceCore.addRemote
let getOriginRepositoryWebUrl = GitServiceCore.getOriginRepositoryWebUrl
let checkoutBranch = GitServiceCore.checkoutBranch
let downloadLfsFile = GitServiceCore.downloadLfsFile
