/// Entry file of the text diff worker thread. Vite bundles it on its own into
/// text-diff-worker.cjs, so it must not reach Electron or any other main-process module.
module Main.Workers.TextDiffWorkerEntry

module WorkerThreads = VersionControlService.Runtime.Node.WorkerThreads
module TextDiffWorker = VersionControlService.Git.TextDiff.TextDiffWorker
module Dispatcher = VersionControlService.Git.TextDiff.TextDiffWorkerDispatcher

// parentPort is null outside a worker thread, so loading this file elsewhere does nothing.
match WorkerThreads.parentPort with
| Some port ->
    VersionControlService.TextDiff.AsyncTrampoline.switchToSetImmediate ()
    let adapter = CommittedDiffAdapter.Adapter()
    let mutable receive: obj -> unit = ignore

    let post message =
        adapter.Post(
            message,
            (fun value -> port.postMessage value),
            (fun answer -> WorkerThreads.setImmediate (fun () -> receive answer))
        )

    receive <- Dispatcher.attachDispatcher (post, (fun () -> port.close ()), adapter.Handler)
    port.onMessage receive
| None -> ()
