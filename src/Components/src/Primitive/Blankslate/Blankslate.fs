namespace Swate.Components.Primitive.Blankslate

open Fable.Core
open Feliz
open Swate.Components.Primitive.Blankslate.Types

module private BlankslateHelper =

    let resolveTextClasses (textSize: BlankslateTextSize) =
        match textSize with
        | BlankslateTextSize.Small ->
            "swt:text-sm swt:font-semibold", "swt:text-xs swt:text-base-content/70", "swt:size-4"
        | BlankslateTextSize.Medium ->
            "swt:text-base swt:font-semibold", "swt:text-sm swt:text-base-content/70", "swt:size-5"
        | BlankslateTextSize.Large ->
            "swt:text-lg swt:font-semibold", "swt:text-base swt:text-base-content/70", "swt:size-6"

open BlankslateHelper

[<Erase; Mangle(false)>]
type Blankslate =

    [<ReactComponent>]
    static member private ActionButton(action: BlankslateAction) =
        let buttonClassName =
            match action.Kind with
            | BlankslateActionKind.Primary -> "swt:btn swt:btn-primary swt:w-full swt:justify-start swt:gap-2"
            | BlankslateActionKind.Secondary -> "swt:btn swt:btn-outline swt:w-full swt:justify-start swt:gap-2"

        Html.button [
            prop.className buttonClassName
            prop.disabled action.Disabled
            prop.onClick (fun _ -> action.OnClick())
            prop.children [
                match action.IconClassName with
                | Some iconClassName -> Html.span [ prop.className $"swt:iconify {iconClassName} swt:size-4" ]
                | None -> Html.none
                Html.span action.Label
            ]
        ]

    [<ReactComponent(true)>]
    static member Blankslate
        (
            title: string,
            ?description: string,
            ?iconClassName: string,
            ?textSize: BlankslateTextSize,
            ?actions: BlankslateAction list,
            ?leadingElement: ReactElement,
            ?trailingElement: ReactElement,
            ?fullHeight: bool,
            ?testId: string,
            ?className: string
        ) : ReactElement =
        let textSize = defaultArg textSize BlankslateTextSize.Medium

        let titleClassName, descriptionClassName, iconSizeClassName =
            resolveTextClasses textSize

        let fullHeight = defaultArg fullHeight true
        let actions = defaultArg actions []

        Html.div [
            if testId.IsSome then
                prop.testId testId.Value
            prop.className [
                "swt:flex swt:flex-col swt:justify-center swt:p-4"
                if fullHeight then
                    "swt:h-full"
                match className with
                | Some customClassName -> customClassName
                | None -> ()
            ]
            prop.children [
                Html.div [
                    prop.className
                        "swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-100 swt:p-4 swt:flex swt:flex-col swt:items-center swt:text-center swt:gap-4"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-start swt:gap-2"
                            prop.children [
                                match iconClassName with
                                | Some iconClass ->
                                    Html.span [
                                        prop.className $"swt:iconify {iconClass} {iconSizeClassName} swt:mt-0.5"
                                    ]
                                | None -> Html.none
                                Html.h2 [ prop.className titleClassName; prop.text title ]
                            ]
                        ]
                        match description with
                        | Some contentText ->
                            Html.p [
                                prop.className $"swt:mt-2 {descriptionClassName}"
                                prop.text contentText
                            ]
                        | None -> Html.none
                        match leadingElement with
                        | Some content -> Html.div [ prop.className "swt:mt-4"; prop.children [ content ] ]
                        | None -> Html.none
                        if not actions.IsEmpty then
                            Html.div [
                                prop.className "swt:mt-4 swt:flex swt:flex-col swt:gap-2"
                                prop.children (actions |> List.map Blankslate.ActionButton)
                            ]
                        match trailingElement with
                        | Some content -> Html.div [ prop.className "swt:mt-4"; prop.children [ content ] ]
                        | None -> Html.none
                    ]
                ]
            ]
        ]