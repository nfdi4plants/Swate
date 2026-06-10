namespace Renderer.Components

open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Composite.Layout
open Swate.Components.Composite.Authentication.Types
open Swate.Components.Primitive.Actionbar
open Swate.Components.Primitive.Actionbar.Types
open Swate.Components.Primitive.BaseModal
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Renderer.Types

module NavbarHelper =

    module Selector =

        /// Unified open: main process decides current window / new window / focus existing.
        let openARC (onError: string -> unit) () =
            Renderer.Components.Helper.ArcVaultHelper.openArc onError |> Promise.start

        /// Click on a recent ARC: main process decides open-or-focus.
        let openArcByPath (onError: string -> unit) (clickedARC: ARCPointer) =
            Renderer.Components.Helper.ArcVaultHelper.openArcByPath onError clickedARC.path
            |> Promise.start

        let rmvRecentArc (onError: string -> unit) (pointer: ARCPointer) =
            promise {
                match! Api.ipcArcVaultApi.removeRecentARC pointer with
                | Ok _ -> ()
                | Error exn -> onError exn.Message
            }
            |> Promise.start

type private Selector =

    static member CreateArcActionBtn(onClick: Browser.Types.MouseEvent -> unit) =
        ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", onClick)

    static member OpenArcActionBtn(onClick: Browser.Types.MouseEvent -> unit) =
        ButtonInfo.create ("swt:fluent--folder-open-24-regular swt:size-5", "Open an existing ARC", onClick)

    static member DownloadArcActionBtn(onClick: Browser.Types.MouseEvent -> unit) =
        ButtonInfo.create ("swt:fluent--cloud-beaker-24-regular swt:size-5", "Download ARC from DataHub", onClick)

    [<ReactComponent>]
    static member private Actionbar
        (setNewArcModalIsOpen: bool -> unit, toggleSelector: unit -> unit, onArcError: string -> unit)
        =
        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()

        let onCreateARC =
            fun _ ->
                setNewArcModalIsOpen true
                toggleSelector ()

        let onOpenARC =
            fun _ ->
                NavbarHelper.Selector.openARC onArcError ()
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
        let errorCtx = useErrorModalCtx ()

        let onArcError =
            createErrorModalCallback errorCtx.enqueue "ARC action failed" appStateCtx

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

        let selectorControlRef = React.useRef ({ toggle = ignore }: SelectorRef)

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
            Swate.Components.Composite.ArcSelector.ArcSelector.Main(
                recentArcs.state,
                NavbarHelper.Selector.openArcByPath onArcError,
                rmvRecentArc = NavbarHelper.Selector.rmvRecentArc onArcError,
                onOpenChange = onOpen,
                actionbar = Selector.Actionbar(setNewArcModalIsOpen, selectorControlRef.current.toggle, onArcError),
                isLoading = recentArcs.isLoading,
                controlRef = selectorControlRef,
                ?currentlyOpenArcPath = appStateCtx
            )
        ]

module private Authentication =

    open Swate.Components.Composite.Authentication.Types
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

        let onSwitchAccount (localSwateAccountId: string) =
            promise {
                match! Api.ipcAuthApi.setActiveAccount localSwateAccountId with
                | Ok _ ->
                    match! Api.ipcAuthApi.revalidate () with
                    | Ok _ -> ()
                    | Error _ -> ()
                | Error _ -> ()
            }
            |> Promise.start

        let onRemoveAccount (localSwateAccountId: string) =
            promise {
                match! Api.ipcAuthApi.removeAccount localSwateAccountId with
                | Ok _ -> ()
                | Error _ -> ()
            }
            |> Promise.start

        Swate.Components.Composite.Authentication.Authentication.UserAvatar(
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
    static member private Separator() =
        Html.div [
            prop.className "swt:divider swt:divider-horizontal swt:mx-0"
        ]

    [<ReactComponent>]
    static member private SettingsButton() =
        let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()

        let isActive =
            match pageStateCtx.state with
            | Some PageState.SettingsPage -> true
            | _ -> false

        let onToggleSettings _ =
            if isActive then
                pageStateCtx.setState None
            else
                pageStateCtx.setState (Some PageState.SettingsPage)

        Html.button [
            prop.type'.button
            prop.className [
                "swt:btn swt:btn-outline swt:btn-square swt:btn-sm"
                if isActive then
                    "swt:btn-active"
            ]
            prop.onClick onToggleSettings
            prop.title "Settings"
            prop.ariaLabel "Settings"
            prop.testId "navbar-settings-button"
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--settings-24-regular swt:size-5"
                ]
            ]
        ]

    [<ReactComponent>]
    static member private SaveArcButton() =

        let errorCtx = useErrorModalCtx ()

        let hasUnsavedChanges =
            Renderer.MainSyncedState.useMainSyncedState {
                initial = false
                load =
                    fun () -> promise {
                        match! Api.ipcArcVaultApi.getHasUnsavedArcChanges () with
                        | Ok hasUnsavedChanges -> return hasUnsavedChanges
                        | Error _ -> return false
                    }
                subscribe =
                    fun setHasUnsavedChanges ->
                        Renderer.IpcReceiver.subscribeProxyReceiver<IHasUnsavedArcChangesRendererApi> {
                            arcUnsavedChangesUpdate = setHasUnsavedChanges
                        }
                onError =
                    fun ex ->
                        errorCtx.enqueue (
                            ErrorModalRequest.create (ex.Message, title = "Error checking for unsaved changes")
                        )
                dependencies = [||]
            }

        let onSaveArc =
            fun _ ->
                promise {
                    if not hasUnsavedChanges.state then
                        return ()

                    match! Api.ipcArcVaultApi.saveArcFile () with
                    | Ok _ -> ()
                    | Error ex -> errorCtx.enqueue (ErrorModalRequest.create (ex.Message, title = "Error saving ARC"))
                }
                |> Promise.start

        Html.button [
            prop.type'.button
            prop.disabled (not hasUnsavedChanges.state)
            prop.className "swt:btn swt:btn-square swt:btn-info swt:btn-sm"
            prop.onClick onSaveArc
            prop.title "Save ARC"
            prop.ariaLabel "Save ARC"
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--save-16-filled swt:size-5"
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main() =

        let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

        let left =
            Html.div [
                prop.className "swt:flex swt:items-center swt:gap-2"
                prop.children [
                    Navbar.SettingsButton()
                    Selector.Main()
                    Navbar.SaveArcButton()
                ]
            ]

        let right =
            Html.div [
                prop.className "swt:flex swt:items-center"
                prop.children [
                    Authentication.UserAvatar()
                    if appStateCtx.IsSome then
                        Navbar.Separator()
                        Layout.LeftSidebarToggleBtn()
                ]
            ]

        Swate.Components.Primitive.Navbar.Navbar.Main(left = left, right = right)
