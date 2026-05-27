namespace Renderer.Components.LeftSidebar

open Fable.Core
open Feliz
open Swate.Components.Primitive.Blankslate
open Swate.Components.Primitive.Blankslate.Types

type EmptyStateAction = {
    Label: string
    IconClassName: string
    Disabled: bool
    OnClick: unit -> unit
}

[<Erase; Mangle(false)>]
type GitSidebarEmptyState =

    [<ReactComponent>]
    static member Main
        (
            title: string,
            description: string,
            primaryAction: EmptyStateAction,
            ?secondaryAction: EmptyStateAction,
            ?iconClassName: string,
            ?infoText: string,
            ?extraContent: ReactElement
        ) =
        let iconClassName = defaultArg iconClassName "swt:fluent--info-24-regular"

        let secondaryActions =
            match secondaryAction with
            | Some action -> [
                BlankslateAction.create (
                    action.Label,
                    action.OnClick,
                    iconClassName = action.IconClassName,
                    disabled = action.Disabled,
                    kind = BlankslateActionKind.Secondary
                )
              ]
            | None -> []

        let actions =
            BlankslateAction.create (
                primaryAction.Label,
                primaryAction.OnClick,
                iconClassName = primaryAction.IconClassName,
                disabled = primaryAction.Disabled,
                kind = BlankslateActionKind.Primary
            )
            :: secondaryActions

        Blankslate.Blankslate(
            title = title,
            description = description,
            iconClassName = iconClassName,
            actions = actions,
            ?infoText = infoText,
            ?extraContent = extraContent,
            fullHeight = true,
            testId = "GitSidebarEmptyState"
        )