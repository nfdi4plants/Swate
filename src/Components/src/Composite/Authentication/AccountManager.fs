namespace Swate.Components.Composite.Authentication

open Fable.Core
open Feliz

open Types

[<Erase; Mangle(false)>]
type AccountManager =

    [<ReactComponent>]
    static member private AccountRow
        (account: AccountSummary, isActive: bool, ?onSwitch: string -> unit, ?onRemove: string -> unit)
        =

        Html.div [
            prop.key account.User.LocalSwateAccountId
            prop.testId $"AccountRow-{account.User.LocalSwateAccountId}"
            prop.className [
                "swt:flex swt:items-center swt:justify-between swt:gap-2 swt:p-1.5 swt:rounded swt:text-sm"
                if isActive then
                    "swt:bg-primary/10"
                if account.TokenInvalid then
                    "swt:ring-1 swt:ring-error/40"
            ]
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2 swt:overflow-hidden"
                    prop.children [
                        Html.a [
                            prop.testId $"AccountProfileLink-{account.User.LocalSwateAccountId}"
                            prop.className "swt:shrink-0 swt:btn swt:btn-square swt:btn-ghost"
                            prop.href (Helper.GitLabUrls.profileUrl account.User)
                            prop.target.blank
                            prop.rel "noopener noreferrer"
                            prop.title "Open user profile"
                            prop.children [
                                Html.img [
                                    prop.className "swt:w-6 swt:h-6 swt:rounded-full swt:shrink-0"
                                    prop.src account.User.AvatarUrl
                                    prop.alt account.User.Name
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:overflow-hidden"
                            prop.children [
                                Html.span [
                                    prop.className "swt:truncate swt:text-sm"
                                    prop.text account.User.Name
                                ]
                                Html.span [
                                    prop.className "swt:truncate swt:text-xs swt:text-base-content/60"
                                    prop.text account.User.TargetDataHub
                                ]
                                if account.TokenInvalid then
                                    Html.div [
                                        prop.className "swt:flex swt:items-center swt:gap-1 swt:text-xs swt:text-error"
                                        prop.children [
                                            Html.span [
                                                prop.className "swt:iconify swt:fluent--warning-24-regular swt:size-4"
                                            ]
                                            Html.span [ prop.text "Token invalid" ]
                                            Html.a [
                                                prop.testId $"RegenerateTokenLink-{account.User.LocalSwateAccountId}"
                                                prop.className "swt:link swt:link-error"
                                                prop.href (
                                                    Helper.GitLabUrls.regenerateTokenUrl account.User.TargetDataHub
                                                )
                                                prop.target.blank
                                                prop.rel "noopener noreferrer"
                                                prop.text "Regenerate token"
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-0.5 swt:shrink-0"
                    prop.children [
                        match onSwitch with
                        | Some switchFn when not isActive ->
                            Html.button [
                                prop.testId $"UseAccountButton-{account.User.LocalSwateAccountId}"
                                prop.className "swt:btn swt:btn-xs swt:btn-ghost"
                                prop.title "Switch to this account"
                                prop.text "Use"
                                prop.onClick (fun _ -> switchFn account.User.LocalSwateAccountId)
                            ]
                        | _ -> ()
                        match onRemove with
                        | Some removeFn ->
                            Html.button [
                                prop.testId $"RemoveAccountButton-{account.User.LocalSwateAccountId}"
                                prop.className "swt:btn swt:btn-xs swt:btn-ghost swt:text-error"
                                prop.title "Remove this account"
                                prop.text "✕"
                                prop.onClick (fun _ -> removeFn account.User.LocalSwateAccountId)
                            ]
                        | None -> ()
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(accounts: AuthStateDto, ?onSwitchAccount: string -> unit, ?onRemoveAccount: string -> unit) =
        let activeLocalSwateAccountId =
            accounts.ActiveAccount
            |> Option.map (fun account -> account.User.LocalSwateAccountId)

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-1 swt:mt-1 swt:pt-1 swt:border-t swt:border-base-content/10"
            prop.children [
                if accounts.StoredAccounts.Length > 1 then
                    Html.div [
                        prop.className "swt:text-xs swt:text-base-content/60 swt:px-1"
                        prop.textf "Accounts (%d)" accounts.StoredAccounts.Length
                    ]

                for acct in accounts.StoredAccounts do
                    let isActive = activeLocalSwateAccountId = Some acct.User.LocalSwateAccountId

                    AccountManager.AccountRow(acct, isActive, ?onSwitch = onSwitchAccount, ?onRemove = onRemoveAccount)
            ]
        ]