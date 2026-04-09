namespace Renderer.Components


open Feliz

open Swate.Components
open Swate.Components.Shared
open Fable.Electron.Remoting.Renderer

module NavbarHelper =

    module Selector =

        /// Unified open: main process decides current window / new window / focus existing.
        let openARC =
            fun _ ->
                promise {
                    let! r = Api.ipcArcVaultApi.openARC (unbox null)

                    match r with
                    | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
                    | Ok _ -> ()
                }
                |> Promise.start

        /// Click on a recent ARC: main process decides open-or-focus.
        let openArcByPath (clickedARC: ARCPointer) =
            promise {
                match! Api.ipcArcVaultApi.openARCByPath (unbox null) clickedARC.path with
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

    [<ReactComponent>]
    static member private Actionbar(setNewArcModalIsOpen: bool -> unit, toggleSelector: unit -> unit) =

        let onCreateARC =
            fun _ ->
                setNewArcModalIsOpen true
                toggleSelector ()

        let onOpenARC =
            fun _ ->
                NavbarHelper.Selector.openARC ()
                toggleSelector ()

        // let downloadARC =
        //     Actionbar.ButtonInfo.create (
        //         "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
        //         "Download an existing ARC",
        //         onClick
        //     )
        Actionbar.Main(
            [|
                Selector.CreateArcActionBtn(onCreateARC)
                Selector.OpenArcActionBtn(onOpenARC)
            |],
            4
        )

    [<ReactComponent>]
    static member Main() =
        let recentArc, setRecentArc = React.useState ([||]: ARCPointer[])
        let isLoading, setIsLoading = React.useState true

        let currentlyOpenArcPath, setCurrentlyOpenArcPath =
            React.useState (None: string option)

        let newArcModalIsOpen, setNewArcModalIsOpen = React.useState false

        let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
            pathChange = setCurrentlyOpenArcPath
            recentARCsUpdate = fun arcs -> setRecentArc arcs
            authAccountsUpdate = ignore
            fileTreeUpdate = ignore
            gitProgressUpdate = ignore
        }

        // Get remote recent ARCs on first load before rendering the selector
        React.useLayoutEffectOnce (fun _ ->
            promise {
                let! arcs = Api.ipcArcVaultApi.getRecentARCs ()
                let! currentlyOpenArcPath = Api.ipcArcVaultApi.getOpenPath (unbox null)
                setCurrentlyOpenArcPath currentlyOpenArcPath
                setRecentArc arcs
                setIsLoading false
            }
            |> Promise.start

            Remoting.init |> Remoting.buildHandler ipcHandler
        )

        let selectorControlRef =
            React.useRef ({ toggle = ignore }: SelectorRef)

        let onOpen =
            fun (b: bool) ->
                if b then
                    Api.ipcArcVaultApi.getRecentARCs () |> Promise.map setRecentArc |> Promise.start

        React.Fragment [
            BaseModal.BaseModal(
                newArcModalIsOpen,
                setNewArcModalIsOpen,
                Renderer.Components.InitState.CreateNewArcModalContent(fun () -> setNewArcModalIsOpen false)
            )
            Swate.Components.Selector.Main(
                recentArc,
                NavbarHelper.Selector.openArcByPath,
                rmvRecentArc = NavbarHelper.Selector.rmvRecentArc,
                onOpenChange = onOpen,
                actionbar = Selector.Actionbar(setNewArcModalIsOpen, selectorControlRef.current.toggle),
                isLoading = isLoading,
                controlRef = selectorControlRef,
                ?currentlyOpenArcPath = currentlyOpenArcPath
            )
        ]

module private Authentication =

    open AuthenticationTypes
    open Swate.Electron.Shared.AuthTypes

    let private toUserInfo (u: AuthUserDto) : UserInformation = {
        Name = u.Name
        Email = u.Email
        AvatarUrl = u.AvatarUrl
        TargetDataHub = u.TargetDataHub
    }

    let private toAccountSummary (a: AuthAccountSummary) : AccountSummary = {
        AccountId = a.AccountId
        Name = a.Name
        Email = a.Email
        AvatarUrl = a.AvatarUrl
        TargetDataHub = a.TargetDataHub
        IsActive = a.IsActive
    }

    let private refreshState (setUser: UserInformation option -> unit) (setAccounts: AuthAccountSummary array -> unit) = promise {
        let! stateResult = Api.ipcAuthApi.getAuthState ()

        match stateResult with
        | Ok state ->
            setAccounts state.Accounts

            match state.User with
            | Some u when state.IsAuthenticated -> setUser (Some(toUserInfo u))
            | _ -> setUser None
        | Error _ ->
            setUser None
            setAccounts [||]
    }

    [<ReactComponent>]
    let UserAvatar () =
        let user, setUser = React.useState (None: UserInformation option)
        let accounts, setAccounts = React.useState ([||]: AuthAccountSummary array)
        let isLoading, setIsLoading = React.useState false

        let refresh () =
            refreshState setUser setAccounts |> Promise.start

        let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
            pathChange = ignore
            recentARCsUpdate = ignore
            authAccountsUpdate =
                fun accounts ->
                    setAccounts accounts
                    let activeAccount = accounts |> Array.tryFind (fun a -> a.IsActive)

                    match activeAccount with
                    | Some a ->
                        {
                            AccountId = a.AccountId
                            Name = a.Name
                            Email = a.Email
                            AvatarUrl = a.AvatarUrl
                            TargetDataHub = a.TargetDataHub
                        }
                        |> toUserInfo
                        |> Some
                        |> setUser

                    | None -> setUser None
            fileTreeUpdate = ignore
            gitProgressUpdate = ignore
        }

        // On mount: load persisted auth state from Main
        React.useEffectOnce (fun _ ->

            Remoting.init |> Remoting.buildHandler ipcHandler

            promise {
                do! refreshState setUser setAccounts
                setIsLoading false
            }
            |> Promise.start
        )

        let onSignIn (signInInfo: SignInInformation) =
            promise {
                setIsLoading true

                let request: AuthSignInRequest = {
                    GitLabBaseUrl = signInInfo.GitLabBaseUrl
                    PersonalAccessToken = signInInfo.PersonalAccessToken
                }

                let! result = Api.ipcAuthApi.signIn request

                match result with
                | Ok authResult when authResult.Success -> do! refreshState setUser setAccounts
                | Ok authResult ->
                    let msg = authResult.Message |> Option.defaultValue "Authentication failed."
                    signInInfo.OnErrorCallback(exn msg)
                | Error ex -> signInInfo.OnErrorCallback ex

                setIsLoading false
            }
            |> Promise.start

        let onLogout () =
            promise {
                let! _ = Api.ipcAuthApi.signOut ()
                do! refreshState setUser setAccounts
            }
            |> Promise.start

        let onSwitchAccount (accountId: string) =
            promise {
                let! _ = Api.ipcAuthApi.setActiveAccount accountId
                refresh ()
            }
            |> Promise.start

        let onRemoveAccount (accountId: string) =
            promise {
                let! _ = Api.ipcAuthApi.removeAccount accountId
                refresh ()
            }
            |> Promise.start

        let mappedAccounts = accounts |> Array.map toAccountSummary

        Authentication.UserAvatar(
            user,
            onSignIn,
            onLogout,
            isLoading = isLoading,
            dropdownClassName = "swt:dropdown-bottom swt:dropdown-end",
            accounts = mappedAccounts,
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
                        Layout.LeftSidebarToggleBtn(activeBorderStyle = false)
                    Layout.RightSidebarToggleBtn()
                ]
            ]

        Swate.Components.Navbar.Main(left = left, right = right)
