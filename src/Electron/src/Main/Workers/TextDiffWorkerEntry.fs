/// Entry file of the text diff worker thread. Vite bundles it on its own into
/// text-diff-worker.cjs, so it must not reach Electron or any other main-process module.
module Main.Workers.TextDiffWorkerEntry

module WorkerThreads = VersionControlService.Runtime.Node.WorkerThreads
module TextDiffWorker = VersionControlService.Git.TextDiff.TextDiffWorker

// parentPort is null outside a worker thread, so loading this file elsewhere does nothing.
match WorkerThreads.parentPort with
| Some port -> TextDiffWorker.bootstrap port
| None -> ()
