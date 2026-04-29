namespace Renderer.Components

open Feliz
open Swate.Components
open Swate.Components.Layout
open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

module NavbarHelper =

    module Selector =

        /// Unified open: main process decides current window / new window / focus existing.
        let openARC =
            fun _ ->
                promise {
                    let! r = Api.ipcArcVaultApi.openARC ()

                    match r with
                    | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
                    | Ok _ -> ()
                }
                |> Promise.start

        /// Click on a recent ARC: main process decides open-or-focus.
        let openArcByPath (clickedARC: ARCPointer) =
            promise {
                match! Api.ipcArcVaultApi.openARCByPath clickedARC.path with
                | Ok _ -> ()
                | Error exn -> console.error (Fable.Core.JS.JSON.stringify exn.Message)
            }
            |> Promise.start

        let rmvRecentArc (pointer: ARCPointer) =
            promise {
                match! Api.ipcArcVaultApi.removeRecentARC pointer with
                | Ok _ -> ()
                | Error exn -> console.error (Fable.Core.JS.JSON.stringify exn.Message)
            }
            |> Promise.start

type private Selector =

    static member CreateArcActionBtn(onClick: Browser.Types.MouseEvent -> unit) =
        Actionbar.ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", onClick)

    static member OpenArcActionBtn(onClick: Browser.Types.MouseEvent -> unit) =
        Actionbar.ButtonInfo.create ("swt:fluent--folder-open-24-regular swt:size-5", "Open an existing ARC", onClick)

    static member DownloadArcActionBtn(onClick: Browser.Types.MouseEvent -> unit) =
        Actionbar.ButtonInfo.create (
            "swt:fluent--cloud-beaker-24-regular swt:size-5",
            "Download ARC from DataHub",
            onClick
        )

    [<ReactComponent>]
    static member private Actionbar(setNewArcModalIsOpen: bool -> unit, toggleSelector: unit -> unit) =
        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()

        let onCreateARC =
            fun _ ->
                setNewArcModalIsOpen true
                toggleSelector ()

        let onOpenARC =
            fun _ ->
                NavbarHelper.Selector.openARC ()
                toggleSelector ()

        let openDataHubBrowser =
            fun _ ->
                pageStateCtx.setState (Some Renderer.Types.PageState.DataHubBrowser)
                toggleSelector ()

        Actionbar.Main(
            [|
                Selector.CreateArcActionBtn(onCreateARC)
                Selector.OpenArcActionBtn(onOpenARC)
                Selector.DownloadArcActionBtn(openDataHubBrowser)
            |],
            4
        )

    [<ReactComponent>]
    static member Main() =
        let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
        let newArcModalIsOpen, setNewArcModalIsOpen = React.useState false

        let recentArcs =
            Renderer.MainSyncedState.useMainSyncedState {
                initial = [||]
                load = fun () -> Api.ipcArcVaultApi.getRecentARCs ()
                subscribe =
                    fun setRecentArcs ->
                        Renderer.IpcReceiver.subscribeProxyReceiver<IRecentArcsRendererApi> {
                            recentARCsUpdate = setRecentArcs
                        }
                onError = fun ex -> console.error ("Failed to load recent ARCs.", ex.Message)
                dependencies = [||]
            }

        let selectorControlRef =
            React.useRef ({ toggle = ignore }: SelectorRef)

        let onOpen =
            fun (isOpen: bool) ->
                if isOpen then
                    recentArcs.refresh ()

        React.Fragment [
            BaseModal.BaseModal(
                newArcModalIsOpen,
                setNewArcModalIsOpen,
                Renderer.Components.InitState.CreateNewArcModalContent(fun () -> setNewArcModalIsOpen false)
            )
            Swate.Components.Selector.Main(
                recentArcs.state,
                NavbarHelper.Selector.openArcByPath,
                rmvRecentArc = NavbarHelper.Selector.rmvRecentArc,
                onOpenChange = onOpen,
                actionbar = Selector.Actionbar(setNewArcModalIsOpen, selectorControlRef.current.toggle),
                isLoading = recentArcs.isLoading,
                controlRef = selectorControlRef,
                ?currentlyOpenArcPath = appStateCtx
            )
        ]

module private Authentication =

    open Authentication.Types
    open Swate.Electron.Shared.AuthTypes

    [<ReactComponent>]
    let UserAvatar () =
        let isLoading, setIsLoading = React.useState false
        let authStateCtx = Renderer.Context.AuthStateContext.useAuthStateCtx ()

        let onSignIn (signInInfo: SignInInformation) =
            promise {
                setIsLoading true

                let request: AuthSignInRequest = {
                    GitLabBaseUrl = signInInfo.GitLabBaseUrl
                    PersonalAccessToken = signInInfo.PersonalAccessToken
                }

                let! result = Api.ipcAuthApi.signIn request

                match result with
                | Ok authResult when authResult.Success -> ()
                | Ok authResult ->
                    let msg = authResult.Message |> Option.defaultValue "Authentication failed."
                    signInInfo.OnErrorCallback(exn msg)
                | Error ex -> signInInfo.OnErrorCallback ex

                setIsLoading false
            }
            |> Promise.start

        let onLogout () =
            promise {
                match! Api.ipcAuthApi.signOut () with
                | Ok _ -> ()
                | Error _ -> ()
            }
            |> Promise.start

        let onSwitchAccount (accountId: string) =
            promise {
                match! Api.ipcAuthApi.setActiveAccount accountId with
                | Ok _ ->
                    match! Api.ipcAuthApi.revalidate () with
                    | Ok _ -> ()
                    | Error _ -> ()
                | Error _ -> ()
            }
            |> Promise.start

        let onRemoveAccount (accountId: string) =
            promise {
                match! Api.ipcAuthApi.removeAccount accountId with
                | Ok _ -> ()
                | Error _ -> ()
            }
            |> Promise.start

        Authentication.Authentication.UserAvatar(
            authStateCtx,
            onSignIn,
            onLogout,
            isLoading = isLoading,
            dropdownClassName = "swt:dropdown-bottom swt:dropdown-end",
            onSwitchAccount = onSwitchAccount,
            onRemoveAccount = onRemoveAccount
        )

type Navbar =

    [<ReactComponent>]
    static member Main(?showDetailsSidebarToggle: bool) =
        let showDetailsSidebarToggle = defaultArg showDetailsSidebarToggle false

        let left = Selector.Main()

        let right =
            Html.div [
                prop.className "swt:flex swt:items-center"
                prop.children [
                    Authentication.UserAvatar()
                    Html.div [ prop.className "swt:divider swt:divider-horizontal" ]
                    if showDetailsSidebarToggle then
                        Layout.RightSidebarToggleBtn()
                    Layout.LeftSidebarToggleBtn(activeBorderStyle = false)
                ]
            ]

        Swate.Components.Navbar.Main(left = left, right = right)
