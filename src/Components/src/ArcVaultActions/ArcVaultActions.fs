namespace Swate.Components.ArcVaultActions

open Fable.Core
open Feliz
open Swate.Components


[<Erase; Mangle(false)>]
type ArcVaultActions =

    [<ReactMemoComponent>]
    static member private Trigger(arcName: string, pathValue: string, ?disabled: bool) =
        Popover.Trigger(
            Html.span [
                prop.className "swt:block swt:w-full swt:truncate swt:tracking-wide swt:uppercase swt:opacity-90"
                prop.text arcName
            ],
            className =
                "swt:mb-2 swt:w-full swt:min-h-0 swt:h-auto swt:justify-start swt:px-2 swt:py-1 swt:text-sm swt:font-semibold swt:normal-case swt:btn-ghost swt:btn-xl swt:max-w-md",
            props = [
                prop.testId "arc-vault-actions-btn"
                prop.title pathValue
                prop.disabled (defaultArg disabled false)
            ]
        )

    [<ReactMemoComponent>]
    static member private Content(pathValue: string, onCopyPath: string -> unit, onOpenArcFolder: unit -> unit) =
        Popover.Content(
            className = "swt:w-96 swt:max-w-[calc(100vw-3rem)]",
            children =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-3 swt:text-sm"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-1"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:text-sm swt:font-semibold"
                                    prop.text "ARC local path"
                                ]
                                Html.p [
                                    prop.testId "arc-vault-actions-path-value"
                                    prop.className "swt:break-all swt:text-xs swt:opacity-90"
                                    prop.text pathValue
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.testId "arc-vault-actions-path-copy"
                                    prop.className "swt:btn swt:btn-sm"
                                    prop.onClick (fun _ -> onCopyPath pathValue)
                                    prop.children [
                                        Html.i [ prop.className "swt:iconify swt:fluent--copy-24-regular" ]
                                        Html.span [ prop.text "Copy path" ]
                                    ]
                                ]
                                Html.button [
                                    prop.testId "arc-vault-actions-path-open-folder"
                                    prop.className "swt:btn swt:btn-sm"
                                    prop.onClick (fun _ -> onOpenArcFolder ())
                                    prop.children [
                                        Html.i [
                                            prop.className "swt:iconify swt:fluent--folder-open-24-regular"
                                        ]
                                        Html.span [ prop.text "Open folder" ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
        )

    /// This component displays the ARC vault name and provides actions related to the ARC vault, such as copying the local path or opening the folder in the file explorer.
    [<ReactComponent>]
    static member ArcVaultActions
        (arcName: string, arcRootPath: string option, onCopyPath: string -> unit, onOpenArcFolder: unit -> unit)
        =
        let pathValue = arcRootPath |> Option.defaultValue "Path unavailable."
        let disabled = arcRootPath.IsNone

        let onCopyPathCallback = React.useCallback (onCopyPath, [| onCopyPath |])

        let onOpenArcFolderCallback =
            React.useCallback (onOpenArcFolder, [| onOpenArcFolder |])

        Popover.Popover(
            debug = "ArcVaultActions",
            placement = FloatingUI.Placement.BottomStart,
            children =
                React.Fragment [
                    ArcVaultActions.Trigger(arcName, pathValue, disabled)
                    if not disabled then
                        ArcVaultActions.Content(pathValue, onCopyPath, onOpenArcFolder)
                ]
        )

    [<ReactComponent>]
    static member Entry(?emptyPath: bool, ?onCopyPath, ?onOpenArcFolder) =
        let onCopyPath =
            defaultArg onCopyPath (fun path -> Browser.Dom.window.alert ($"Copying path: {path}"))

        let onOpenArcFolder =
            defaultArg onOpenArcFolder (fun () -> Browser.Dom.window.alert ("Opening ARC folder"))

        let arcPath =
            match emptyPath with
            | Some true -> None
            | _ -> Some "C:\\Users\\User\\ArcVault"

        ArcVaultActions.ArcVaultActions(
            arcName = "Example ARC Vault",
            arcRootPath = arcPath,
            onCopyPath = onCopyPath,
            onOpenArcFolder = onOpenArcFolder
        )