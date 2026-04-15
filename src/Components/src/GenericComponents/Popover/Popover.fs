namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Popover.Context

module private PopoverHelper =

    [<Import("Children", "react")>]
    let private reactChildren: obj = jsNative

    [<Import("Fragment", "react")>]
    let private reactFragment: obj = jsNative

    [<Import("cloneElement", "react")>]
    let private cloneElement (element: obj) (props: obj) : ReactElement = jsNative

    [<Emit("Heading")>]
    let headingComponent: obj = jsNative

    [<Emit("Description")>]
    let descriptionComponent: obj = jsNative

    [<Emit("$0.toArray($1)")>]
    let private childrenToArray (childrenApi: obj) (children: obj) : obj[] = jsNative

    [<Emit("$0.type === $1")>]
    let private isElementType (element: obj) (componentType: obj) : bool = jsNative

    [<Emit("$0.props.children")>]
    let private getElementChildren (element: obj) : obj = jsNative

    type ProcessedChildren = {
        children: ReactElement
        hasLabel: bool
        hasDescription: bool
    }

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

    let private asChildren (children: ReactElement list) =
        match children with
        | [] -> Html.none
        | [ child ] -> child
        | many -> React.Fragment many

    let decorateSemantics headingComponent descriptionComponent labelId descriptionId (children: ReactElement) =
        let rec processNode hasLabel hasDescription (node: obj) =
            if isElementType node headingComponent then
                let child =
                    if hasLabel then
                        unbox<ReactElement> node
                    else
                        cloneElement node {| id = labelId |}

                child, true, hasDescription
            elif isElementType node descriptionComponent then
                let child =
                    if hasDescription then
                        unbox<ReactElement> node
                    else
                        cloneElement node {| id = descriptionId |}

                child, hasLabel, true
            elif isElementType node reactFragment then
                let processed = processChildren hasLabel hasDescription (getElementChildren node)
                cloneElement node {| children = processed.children |}, processed.hasLabel, processed.hasDescription
            else
                unbox<ReactElement> node, hasLabel, hasDescription

        and processChildren hasLabel hasDescription (children: obj) =
            let mutable seenLabel = hasLabel
            let mutable seenDescription = hasDescription
            let processedChildren = ResizeArray<ReactElement>()

            for child in childrenToArray reactChildren children do
                let nextChild, nextHasLabel, nextHasDescription =
                    processNode seenLabel seenDescription child

                processedChildren.Add nextChild
                seenLabel <- nextHasLabel
                seenDescription <- nextHasDescription

            {
                children = processedChildren |> Seq.toList |> asChildren
                hasLabel = seenLabel
                hasDescription = seenDescription
            }

        processChildren false false children

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

        let providerValue: PopoverContext = {
            isOpen = isOpen
            setIsOpen = setIsOpen
            floating = floating
            interactions = interactions
            modal = defaultArg modal false
            labelId = FloatingUI.useId ()
            descriptionId = FloatingUI.useId ()
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

        let semantics =
            PopoverHelper.decorateSemantics
                PopoverHelper.headingComponent
                PopoverHelper.descriptionComponent
                ctx.labelId
                ctx.descriptionId
                children

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
                                if semantics.hasLabel then
                                    prop.ariaLabelledBy ctx.labelId
                                elif ariaLabel.IsSome then
                                    prop.ariaLabel ariaLabel.Value
                                if semantics.hasDescription then
                                    prop.ariaDescribedBy ctx.descriptionId
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
                                prop.children semantics.children
                            ]
                    )
            )
        else
            Html.none

    [<ReactComponent>]
    static member Heading(children: ReactElement, ?className: string, ?props: IReactProperty list, ?id: string) =
        let _ = PopoverHelper.requireContext ()

        Html.h2 [
            if id.IsSome then
                prop.id id.Value
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
        let _ = PopoverHelper.requireContext ()

        Html.p [
            if id.IsSome then
                prop.id id.Value
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