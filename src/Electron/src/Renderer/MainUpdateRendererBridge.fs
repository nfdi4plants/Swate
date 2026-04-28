module Renderer.MainUpdateRendererBridge

open Renderer.IpcReceiver
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

let subscribePathChange handler =
    subscribeProxyReceiver<IPathChangeRendererApi> { pathChange = handler }

let subscribeRecentArcsUpdate handler =
    subscribeProxyReceiver<IRecentArcsRendererApi> { recentARCsUpdate = handler }

let subscribeAuthAccountsUpdate handler =
    subscribeProxyReceiver<IAuthAccountsRendererApi> { authAccountsUpdate = handler }

let subscribeFileTreeUpdate handler =
    subscribeProxyReceiver<IFileTreeRendererApi> { fileTreeUpdate = handler }

let subscribeGitProgressUpdate handler =
    subscribeProxyReceiver<IGitProgressRendererApi> { gitProgressUpdate = handler }
