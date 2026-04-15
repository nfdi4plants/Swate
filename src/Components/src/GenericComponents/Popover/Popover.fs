namespace Swate.Components

open Fable.Core
open Feliz
open Swate.Components.PopoverContext

module private PopoverHelper =

    let requireContext () =
        match usePopoverCtx () with
        | Some ctx -> ctx
        | None -> failwith "Popover render components must be used inside Popover.Popover."

    let dataState isOpen = if isOpen then "open" else "closed"

    let resolveProps (props: obj option) = props |> Option.defaultValue null

    let defaultMiddleware () = [|
        FloatingUI.Middleware.offset 10
        FloatingUI.Middleware.flip ()
        FloatingUI.Middleware.shift {| padding = 8 |}
    |]

[<Erase; Mangle(false)>]
type Popover =

    [<ReactComponent>]
    static member Popover
        (
            children: ReactElement,
            ?isOpen: bool,
            ?onOpenChange: bool -> unit,
            ?defaultOpen: bool,
            ?placement: FloatingUI.Placement,
            ?middleware: FloatingUI.IMiddleware[],
            ?modal: bool,
            ?debug: string,
            ?clickProps: FloatingUI.UseClickProps,
            ?dismissProps: FloatingUI.UseDismissProps,
            ?portalId: string,
            ?preserveTabOrder: bool,
            ?initialFocus: obj,
            ?returnFocus: obj,
            ?visuallyHiddenDismiss: obj,
            ?closeOnFocusOut: bool
        ) =
        let controlledOpen = isOpen

        let uncontrolledOpen, setUncontrolledOpen =
            React.useState (defaultArg defaultOpen false)

        let isOpen = defaultArg controlledOpen uncontrolledOpen

        let setIsOpen next =
            onOpenChange |> Option.iter (fun fn -> fn next)

            if controlledOpen.IsNone then
                setUncontrolledOpen next

        let floating =
            FloatingUI.useFloating (
                ``open`` = isOpen,
                onOpenChange = setIsOpen,
                placement = defaultArg placement FloatingUI.Placement.Bottom,
                middleware = defaultArg middleware (PopoverHelper.defaultMiddleware ()),
                whileElementsMounted = FloatingUI.autoUpdate
            )

        let click =
            match clickProps with
            | Some props -> FloatingUI.useClick (floating.context, props)
            | None -> FloatingUI.useClick floating.context

        let dismiss =
            match dismissProps with
            | Some props -> FloatingUI.useDismiss (floating.context, props)
            | None -> FloatingUI.useDismiss floating.context

        let role =
            FloatingUI.useRole (floating.context, FloatingUI.UseRoleProps(role = FloatingUI.RoleAttribute.Dialog))

        let interactions = FloatingUI.useInteractions [| click; dismiss; role |]

        let labelId, setLabelId = React.useStateWithUpdater<string option> None

        let descriptionId, setDescriptionId = React.useStateWithUpdater<string option> None

        let providerValue: PopoverContext = {
            isOpen = isOpen
            setIsOpen = setIsOpen
            floating = floating
            interactions = interactions
            modal = defaultArg modal false
            labelId = labelId
            setLabelId = setLabelId
            descriptionId = descriptionId
            setDescriptionId = setDescriptionId
            debug = debug
            portalId = portalId
            preserveTabOrder = defaultArg preserveTabOrder true
            initialFocus = initialFocus
            returnFocus = returnFocus
            visuallyHiddenDismiss = visuallyHiddenDismiss
            closeOnFocusOut = closeOnFocusOut
        }

        PopoverCtx.Provider(Some providerValue, children)

    [<ReactComponent>]
    static member Trigger
        (children: ReactElement, ?className: string, ?interactionProps: obj, ?props: IReactProperty list, ?debug: string) =
        let ctx = PopoverHelper.requireContext ()
        let resolvedDebug = debug |> Option.orElse ctx.debug

        Html.button [
            prop.type'.button
            prop.ref (unbox ctx.floating.refs.setReference)
            prop.custom ("data-state", PopoverHelper.dataState ctx.isOpen)
            if resolvedDebug.IsSome then
                prop.testId ("popover_trigger_" + resolvedDebug.Value)
            prop.className [
                "swt:btn"
                if className.IsSome then
                    className.Value
            ]
            yield!
                prop.spread
                <| ctx.interactions.getReferenceProps (PopoverHelper.resolveProps interactionProps)
            if props.IsSome then
                yield! props.Value
            prop.children children
        ]

    [<ReactComponent>]
    static member TriggerRender
        (
            render:
                {|
                    isOpen: bool
                    setReference: obj
                    referenceProps: obj
                |}
                    -> ReactElement,
            ?interactionProps: obj
        ) =
        let ctx = PopoverHelper.requireContext ()

        render {|
            isOpen = ctx.isOpen
            setReference = unbox ctx.floating.refs.setReference
            referenceProps = ctx.interactions.getReferenceProps (PopoverHelper.resolveProps interactionProps)
        |}

    [<ReactComponent>]
    static member Content
        (
            children: ReactElement,
            ?className: string,
            ?interactionProps: obj,
            ?props: IReactProperty list,
            ?debug: string,
            ?ariaLabel: string
        ) =
        let ctx = PopoverHelper.requireContext ()
        let resolvedDebug = debug |> Option.orElse ctx.debug

        if ctx.isOpen then
            FloatingUI.FloatingPortal(
                ?id = ctx.portalId,
                preserveTabOrder = ctx.preserveTabOrder,
                children =
                    FloatingUI.FloatingFocusManager(
                        context = ctx.floating.context,
                        modal = ctx.modal,
                        ?initialFocus = ctx.initialFocus,
                        ?returnFocus = ctx.returnFocus,
                        ?visuallyHiddenDismiss = ctx.visuallyHiddenDismiss,
                        ?closeOnFocusOut = ctx.closeOnFocusOut,
                        children =
                            Html.div [
                                prop.ref (unbox ctx.floating.refs.setFloating)
                                prop.custom ("style", ctx.floating.floatingStyles)
                                prop.custom ("data-state", PopoverHelper.dataState ctx.isOpen)
                                match ctx.labelId with
                                | Some lid -> prop.ariaLabelledBy lid
                                | None ->
                                    if ariaLabel.IsSome then
                                        prop.ariaLabel ariaLabel.Value
                                if ctx.descriptionId.IsSome then
                                    prop.ariaDescribedBy ctx.descriptionId.Value
                                if resolvedDebug.IsSome then
                                    prop.testId ("popover_content_" + resolvedDebug.Value)
                                prop.className [
                                    "swt:z-[9999] swt:min-w-56 swt:max-w-[min(28rem,calc(100vw-2rem))]"
                                    "swt:rounded-md swt:border swt:border-base-300 swt:bg-base-100"
                                    "swt:p-4 swt:shadow-md swt:outline-hidden"
                                    if className.IsSome then
                                        className.Value
                                ]
                                yield!
                                    prop.spread
                                    <| ctx.interactions.getFloatingProps (PopoverHelper.resolveProps interactionProps)
                                if props.IsSome then
                                    yield! props.Value
                                prop.children children
                            ]
                    )
            )
        else
            Html.none

    [<ReactComponent>]
    static member Heading(children: ReactElement, ?className: string, ?props: IReactProperty list, ?id: string) =
        let generatedId = FloatingUI.useId ()
        let headingId = defaultArg id generatedId
        let ctx = PopoverHelper.requireContext ()

        React.useEffect (
            (fun () ->
                ctx.setLabelId (fun cur ->
                    match cur with
                    | None -> Some headingId
                    | s -> s
                )

                FsReact.createDisposable (fun () ->
                    ctx.setLabelId (fun cur ->
                        match cur with
                        | Some x when x = headingId -> None
                        | s -> s
                    )
                )
            ),
            [| headingId :> obj; ctx.setLabelId :> obj |]
        )

        Html.h2 [
            prop.id headingId
            prop.className [
                "swt:text-base swt:font-semibold swt:leading-tight"
                if className.IsSome then
                    className.Value
            ]
            if props.IsSome then
                yield! props.Value
            prop.children children
        ]

    [<ReactComponent>]
    static member Description(children: ReactElement, ?className: string, ?props: IReactProperty list, ?id: string) =
        let generatedId = FloatingUI.useId ()
        let descriptionId = defaultArg id generatedId
        let ctx = PopoverHelper.requireContext ()

        React.useEffect (
            (fun () ->
                ctx.setDescriptionId (fun cur ->
                    match cur with
                    | None -> Some descriptionId
                    | s -> s
                )

                FsReact.createDisposable (fun () ->
                    ctx.setDescriptionId (fun cur ->
                        match cur with
                        | Some x when x = descriptionId -> None
                        | s -> s
                    )
                )
            ),
            [| descriptionId :> obj; ctx.setDescriptionId :> obj |]
        )

        Html.p [
            prop.id descriptionId
            prop.className [
                "swt:text-sm swt:opacity-70"
                if className.IsSome then
                    className.Value
            ]
            if props.IsSome then
                yield! props.Value
            prop.children children
        ]

    [<ReactComponent>]
    static member Close(children: ReactElement, ?className: string, ?props: IReactProperty list) =
        let ctx = PopoverHelper.requireContext ()

        Html.button [
            prop.type'.button
            prop.className [
                "swt:btn swt:btn-sm"
                if className.IsSome then
                    className.Value
            ]
            prop.onClick (fun _ -> ctx.setIsOpen false)
            if props.IsSome then
                yield! props.Value
            prop.children children
        ]