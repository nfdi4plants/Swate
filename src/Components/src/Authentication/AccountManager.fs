namespace Swate.Components

open Fable.Core
open Feliz

open AuthenticationTypes

[<Erase; Mangle(false)>]
type AccountManager =

    [<ReactComponent>]
    static member private AccountRow(account: AccountSummary, ?onSwitch: string -> unit, ?onRemove: string -> unit) =
        Html.div [
            prop.key account.AccountId
            prop.testId $"AccountRow-{account.AccountId}"
            prop.className [
                "swt:flex swt:items-center swt:justify-between swt:gap-2 swt:p-1.5 swt:rounded swt:text-sm"
                if account.IsActive then
                    "swt:bg-primary/10"
            ]
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2 swt:overflow-hidden"
                    prop.children [
                        Html.img [
                            prop.className "swt:w-6 swt:h-6 swt:rounded-full swt:shrink-0"
                            prop.src account.AvatarUrl
                            prop.alt account.Name
                        ]
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:overflow-hidden"
                            prop.children [
                                Html.span [
                                    prop.className "swt:truncate swt:text-sm"
                                    prop.text account.Name
                                ]
                                Html.span [
                                    prop.className "swt:truncate swt:text-xs swt:text-base-content/60"
                                    prop.text account.TargetDataHub
                                ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-0.5 swt:shrink-0"
                    prop.children [
                        match onSwitch with
                        | Some switchFn when not account.IsActive ->
                            Html.button [
                                prop.testId $"UseAccountButton-{account.AccountId}"
                                prop.className "swt:btn swt:btn-xs swt:btn-ghost"
                                prop.title "Switch to this account"
                                prop.text "Use"
                                prop.onClick (fun _ -> switchFn account.AccountId)
                            ]
                        | _ -> ()
                        match onRemove with
                        | Some removeFn ->
                            Html.button [
                                prop.testId $"RemoveAccountButton-{account.AccountId}"
                                prop.className "swt:btn swt:btn-xs swt:btn-ghost swt:text-error"
                                prop.title "Remove this account"
                                prop.text "✕"
                                prop.onClick (fun _ -> removeFn account.AccountId)
                            ]
                        | None -> ()
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main
        (accounts: AccountSummary array, ?onSwitchAccount: string -> unit, ?onRemoveAccount: string -> unit)
        =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-1 swt:mt-1 swt:pt-1 swt:border-t swt:border-base-content/10"
            prop.children [
                if accounts.Length > 1 then
                    Html.div [
                        prop.className "swt:text-xs swt:text-base-content/60 swt:px-1"
                        prop.textf "Accounts (%d)" accounts.Length
                    ]

                for acct in accounts do
                    AccountManager.AccountRow(acct, ?onSwitch = onSwitchAccount, ?onRemove = onRemoveAccount)
            ]
        ]