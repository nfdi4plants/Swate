namespace Renderer.Components

open Fable.Core
open Feliz
open Renderer.Components.Helper
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components
open Swate.Components.Composite.ArcOpening
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

type private Selector =

    [<ReactComponent>]
    static member Actionbar(setNewArcModalIsOpen: bool -> unit, openArc: unit -> unit) =
        Actionbar.Main(
            [|
                ButtonInfo.create (
                    "swt:fluent--folder-add-24-regular swt:size-5",
                    "Create a new ARC",
                    fun _ -> setNewArcModalIsOpen true
                )
                ButtonInfo.create (
                    "swt:fluent--folder-open-24-regular swt:size-5",
                    "Open an existing ARC",
                    fun _ -> openArc ()
                )
            |],
            2
        )

    [<ReactComponent>]
    static member Main
        (
            onArcError: string -> unit,
            setNewArcModalIsOpen: bool -> unit,
            openArc: unit -> unit,
            openArcByPath: string -> unit
        ) =

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

        let removeRecentArc pointer =
            promise {
                match! Api.ipcArcVaultApi.removeRecentARC pointer with
                | Ok _ -> ()
                | Error exn -> onArcError exn.Message
            }
            |> Promise.start

        Swate.Components.Composite.ArcSelector.ArcSelector.Main(
            recentArcs.state,
            (fun clickedARC -> openArcByPath clickedARC.path),
            rmvRecentArc = removeRecentArc,
            actionbar = Selector.Actionbar(setNewArcModalIsOpen, openArc),
            onOpenChange = onOpen,
            isLoading = recentArcs.isLoading,
            controlRef = selectorControlRef,
            ?currentlyOpenArcPath = Renderer.Context.AppStateContext.useAppStateCtx ()
        )

module private Authentication =

    open Swate.Components.Composite.Authentication.Types
    open Swate.Electron.Shared.AuthTypes

    [<ReactComponent>]
    let UserAvatar () =
        let isLoading, setIsLoading = React.useState false
        let authStateCtx = Renderer.Context.AuthStateContext.useAuthStateCtx ()
        let errorModalCtx = useErrorModalCtx ()

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
                | Error ex -> errorModalCtx.enqueue (ErrorModalRequest.create (ex.Message, title = "Sign Out Error"))
            }
            |> Promise.start

        let onSwitchAccount (localSwateAccountId: string) =
            promise {
                match! Api.ipcAuthApi.setActiveAccount localSwateAccountId with
                | Ok _ ->
                    match! Api.ipcAuthApi.revalidate () with
                    | Ok _ -> ()
                    | Error ex ->
                        errorModalCtx.enqueue (
                            ErrorModalRequest.create (ex.Message, title = "Error revalidating account")
                        )
                | Error ex ->
                    errorModalCtx.enqueue (ErrorModalRequest.create (ex.Message, title = "Error switching account"))
            }
            |> Promise.start

        let onRemoveAccount (localSwateAccountId: string) =
            promise {
                match! Api.ipcAuthApi.removeAccount localSwateAccountId with
                | Ok _ -> ()
                | Error ex ->
                    errorModalCtx.enqueue (ErrorModalRequest.create (ex.Message, title = "Error removing account"))
            }
            |> Promise.start

        let onRotateToken (localSwateAccountId: string) =
            promise {
                match! Api.ipcAuthApi.rotatePersonalAccessToken localSwateAccountId with
                | Ok _ -> Browser.Dom.console.log $"Token rotation successful for account {localSwateAccountId}"
                | Error ex ->
                    errorModalCtx.enqueue (ErrorModalRequest.create (ex.Message, title = "Error rotating token"))
            }
            |> Promise.start

        Swate.Components.Composite.Authentication.Authentication.UserAvatar(
            authStateCtx,
            onSignIn,
            onLogout,
            isLoading = isLoading,
            dropdownClassName = "swt:dropdown-bottom swt:dropdown-end",
            onRotateToken = onRotateToken,
            onSwitchAccount = onSwitchAccount,
            onRemoveAccount = onRemoveAccount
        )

[<Erase; Mangle(false)>]
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
        let isSaving, setIsSaving = React.useState false

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

        let onSaveArc _ =
            if hasUnsavedChanges.state && not isSaving then
                setIsSaving true

                promise {
                    try
                        match! Api.ipcArcVaultApi.saveArcFile () with
                        | Ok _ -> ()
                        | Error ex ->
                            errorCtx.enqueue (ErrorModalRequest.create (ex.Message, title = "Error saving ARC"))
                    finally
                        setIsSaving false
                }
                |> Promise.catch (fun ex ->
                    errorCtx.enqueue (ErrorModalRequest.create (ex.Message, title = "Error saving ARC"))
                )
                |> Promise.start

        Html.button [
            prop.type'.button
            prop.disabled (isSaving || not hasUnsavedChanges.state)
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
        let newArcModalIsOpen, setNewArcModalIsOpen = React.useState false
        let isOpeningArc, setIsOpeningArc = React.useState false
        let errorCtx = useErrorModalCtx ()

        let onArcError =
            createErrorModalCallback errorCtx.enqueue "ARC action failed" appStateCtx

        let handleOpenArc () =
            openArcWithProgress
                isOpeningArc
                Api.ipcArcVaultApi.pickDirectory
                (openArcByPath onArcError)
                onArcError
                setIsOpeningArc
            |> Promise.start

        let handleOpenArcByPath arcPath =
            openArcByPathWithProgress isOpeningArc arcPath (openArcByPath onArcError) setIsOpeningArc
            |> Promise.start

        let left =
            Html.div [
                prop.className "swt:flex swt:items-center swt:gap-2"
                prop.children [
                    Navbar.SettingsButton()
                    Selector.Main(onArcError, setNewArcModalIsOpen, handleOpenArc, handleOpenArcByPath)
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

        React.Fragment [
            BaseModal.BaseModal(
                newArcModalIsOpen,
                setNewArcModalIsOpen,
                Renderer.Components.InitState.CreateNewArcModalContent(fun () -> setNewArcModalIsOpen false)
            )
            Modals.OpeningArc(isOpeningArc)
            Swate.Components.Primitive.Navbar.Navbar.Main(left = left, right = right)
        ]
