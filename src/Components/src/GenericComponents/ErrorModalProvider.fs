namespace Swate.Components

open Feliz
open Fable.Core

type private ErrorModalMsg =
    | Enqueue of ErrorModalEntry
    | EnqueueMany of ErrorModalEntry list
    | DismissById of string
    | DismissBatchItem of string * string
    | DismissAll

[<Erase; Mangle(false)>]
type ErrorModalProvider =

    static member private reducer (queue: ErrorModalEntry list) (msg: ErrorModalMsg) =
        match msg with
        | Enqueue entry -> queue @ [ entry ]
        | EnqueueMany entries -> queue @ entries
        | DismissById id -> queue |> List.filter (fun entry -> entry.Id <> id)
        | DismissBatchItem(batchId, itemId) ->
            queue
            |> List.choose (fun entry ->
                match entry with
                | ErrorModalEntry.Batch batch when batch.Id = batchId ->
                    let nextErrors = batch.Errors |> List.filter (fun request -> request.Id <> itemId)

                    if nextErrors.IsEmpty then
                        None
                    else
                        ErrorModalEntry.Batch { batch with Errors = nextErrors } |> Some
                | _ -> Some entry
            )
        | DismissAll -> []

    static member private actionButton (action: ErrorModalAction) =
        let className =
            match action.Style with
            | ErrorModalActionStyle.Primary -> "swt:btn-primary"
            | ErrorModalActionStyle.Error -> "swt:btn-error"
            | ErrorModalActionStyle.Neutral -> "swt:btn-neutral"

        Html.button [
            prop.className $"swt:btn {className}"
            prop.onClick (fun _ -> action.OnClick ())
            prop.children [
                if action.IconClassName.IsSome then
                    Html.i [
                        prop.className [ "swt:iconify"; action.IconClassName.Value ]
                    ]
                Html.span action.Label
            ]
        ]

    static member private requestMessage (message: string) =
        Html.div [
            prop.className "swt:whitespace-pre-wrap"
            prop.children (
                message.Split('\n')
                |> Array.collect (fun line -> [| Html.text line; Html.br [] |])
            )
        ]

    static member private requestDetails (details: string option) =
        match details with
        | None -> Html.none
        | Some details ->
            Html.details [
                prop.className "swt:collapse swt:collapse-arrow swt:border swt:border-base-300 swt:bg-base-200"
                prop.children [
                    Html.summary [
                        prop.className "swt:collapse-title swt:min-h-0 swt:py-3 swt:font-medium"
                        prop.text "Technical details"
                    ]
                    Html.div [
                        prop.className "swt:collapse-content"
                        prop.children [
                            Html.pre [
                                prop.className "swt:whitespace-pre-wrap swt:text-sm"
                                prop.text details
                            ]
                        ]
                    ]
                ]
            ]

    static member private requestBody (request: ErrorModalRequest) =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3"
            prop.children [
                ErrorModalProvider.requestMessage request.Message
                ErrorModalProvider.requestDetails request.Details
            ]
        ]

    static member private pendingEntryLabel (entry: ErrorModalEntry) =
        match entry with
        | ErrorModalEntry.Single request -> request.Title
        | ErrorModalEntry.Batch batch -> $"{batch.Title} ({batch.Errors.Length} errors)"
        | ErrorModalEntry.Cancelable request -> $"{request.Title} (action required)"

    static member private pendingEntriesBox (remainingEntries: ErrorModalEntry list) =
        if remainingEntries.IsEmpty then
            Html.none
        else
            Html.div [
                prop.className "swt:rounded-box swt:border swt:border-error/20 swt:bg-base-200 swt:p-3 swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:items-center swt:gap-2"
                        prop.children [
                            Html.i [
                                prop.className "swt:iconify swt:fluent--alert-24-regular swt:size-4"
                            ]
                            Html.span [
                                prop.className "swt:text-sm swt:font-semibold"
                                prop.text $"There are {remainingEntries.Length} more error modal(s) waiting."
                            ]
                        ]
                    ]
                    Html.ul [
                        prop.className "swt:list-disc swt:pl-5 swt:text-sm swt:opacity-80"
                        prop.children [
                            for pendingEntry in remainingEntries do
                                Html.li [
                                    prop.key pendingEntry.Id
                                    prop.text (ErrorModalProvider.pendingEntryLabel pendingEntry)
                                ]
                        ]
                    ]
                ]
            ]

    static member private dismissSingleRequest (dismissById: string -> unit) (request: ErrorModalRequest) =
        request.OnDismiss |> Option.iter (fun callback -> callback ())
        dismissById request.Id

    static member private dismissCancelableRequest (dismissById: string -> unit) (request: CancelableErrorModalRequest) =
        request.OnDismiss |> Option.iter (fun callback -> callback ())
        dismissById request.Id

    static member private cancelCancelableRequest (dismissById: string -> unit) (request: CancelableErrorModalRequest) =
        request.OnCancel |> Option.iter (fun callback -> callback ())
        dismissById request.Id

    static member private dismissBatch (dismissById: string -> unit) (batch: ErrorModalBatch) =
        batch.Errors
        |> List.iter (fun request -> request.OnDismiss |> Option.iter (fun callback -> callback ()))

        batch.OnDismiss |> Option.iter (fun callback -> callback ())
        dismissById batch.Id

    static member private dismissBatchItemRequest
        (
            dismissBatchItem: string -> string -> unit,
            batch: ErrorModalBatch,
            request: ErrorModalRequest
        ) =
        let isLastItem = batch.Errors.Length <= 1

        request.OnDismiss |> Option.iter (fun callback -> callback ())

        if isLastItem then
            batch.OnDismiss |> Option.iter (fun callback -> callback ())

        dismissBatchItem batch.Id request.Id

    static member private requestCard
        (
            dismissBatchItem: string -> string -> unit,
            batch: ErrorModalBatch,
            request: ErrorModalRequest
        ) =
        Html.div [
            prop.key request.Id
            prop.className "swt:rounded-box swt:border swt:border-error/20 swt:bg-base-100 swt:p-4 swt:flex swt:flex-col swt:gap-3"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-start swt:gap-3"
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--error-circle-24-regular swt:size-5 swt:text-error"
                        ]
                        Html.div [
                            prop.className "swt:flex-1 swt:flex swt:flex-col swt:gap-1"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:text-base swt:font-semibold"
                                    prop.text request.Title
                                ]
                                ErrorModalProvider.requestBody request
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:justify-end swt:gap-2"
                    prop.children [
                        for action in request.Actions do
                            ErrorModalProvider.actionButton action
                        Html.button [
                            prop.className "swt:btn swt:btn-primary"
                            prop.text request.DismissLabel
                            prop.onClick (fun _ ->
                                ErrorModalProvider.dismissBatchItemRequest(dismissBatchItem, batch, request))
                        ]
                    ]
                ]
            ]
        ]

    static member private resolveEntryForDismissAll (entry: ErrorModalEntry) =
        match entry with
        | ErrorModalEntry.Single request ->
            request.OnDismiss |> Option.iter (fun callback -> callback ())
        | ErrorModalEntry.Batch batch ->
            batch.Errors
            |> List.iter (fun request -> request.OnDismiss |> Option.iter (fun callback -> callback ()))

            batch.OnDismiss |> Option.iter (fun callback -> callback ())
        | ErrorModalEntry.Cancelable request ->
            request.OnCancel |> Option.iter (fun callback -> callback ())

    [<ReactComponent>]
    static member ErrorModalHost() =
        let errorModal = Contexts.ErrorModal.useErrorModal ()
        let currentEntry = errorModal.current

        let remainingEntries =
            match errorModal.queue with
            | _ :: rest -> rest
            | [] -> []

        match currentEntry with
        | None -> Html.none
        | Some(ErrorModalEntry.Single request) ->
            BaseModal.Modal(
                isOpen = true,
                setIsOpen =
                    (fun isOpen ->
                        if not isOpen then
                            ErrorModalProvider.dismissSingleRequest errorModal.dismissById request
                    ),
                header =
                    Html.div [
                        prop.className "swt:flex swt:items-center swt:gap-2"
                        prop.children [
                            Html.i [
                                prop.className "swt:iconify swt:fluent--error-circle-24-filled swt:size-6"
                            ]
                            Html.span request.Title
                        ]
                    ],
                children =
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-3"
                        prop.children [
                            ErrorModalProvider.requestBody request
                            ErrorModalProvider.pendingEntriesBox remainingEntries
                        ]
                    ],
                footer =
                    Html.div [
                        prop.className "swt:flex swt:w-full swt:flex-wrap swt:gap-2"
                        prop.children [
                            Html.div [
                                prop.className "swt:ml-auto swt:flex swt:flex-wrap swt:justify-end swt:gap-2"
                                prop.children [
                                    for action in request.Actions do
                                        ErrorModalProvider.actionButton action
                                    if errorModal.queue.Length > 1 then
                                        Html.button [
                                            prop.className "swt:btn swt:btn-neutral"
                                            prop.text "Dismiss All Errors"
                                            prop.onClick (fun _ -> errorModal.dismissAll ())
                                        ]
                                    Html.button [
                                        prop.className "swt:btn swt:btn-primary"
                                        prop.text request.DismissLabel
                                        prop.onClick (fun _ ->
                                            ErrorModalProvider.dismissSingleRequest errorModal.dismissById request)
                                    ]
                                ]
                            ]
                        ]
                    ],
                className = "swt:max-w-2xl"
            )
        | Some(ErrorModalEntry.Batch batch) ->
            BaseModal.Modal(
                isOpen = true,
                setIsOpen =
                    (fun isOpen ->
                        if not isOpen then
                            ErrorModalProvider.dismissBatch errorModal.dismissById batch
                    ),
                header =
                    Html.div [
                        prop.className "swt:flex swt:items-center swt:gap-2"
                        prop.children [
                            Html.i [
                                prop.className "swt:iconify swt:fluent--error-circle-24-filled swt:size-6"
                            ]
                            Html.span batch.Title
                            Html.span [
                                prop.className "swt:badge swt:badge-error swt:text-error-content"
                                prop.text $"{batch.Errors.Length} errors"
                            ]
                        ]
                    ],
                children =
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-3"
                        prop.children [
                            match batch.Summary with
                            | Some summary ->
                                Html.p [
                                    prop.className "swt:text-sm swt:opacity-80"
                                    prop.text summary
                                ]
                            | None -> Html.none
                            Html.div [
                                prop.className "swt:flex swt:flex-col swt:gap-3"
                                prop.children [
                                    for request in batch.Errors do
                                        ErrorModalProvider.requestCard(errorModal.dismissBatchItem, batch, request)
                                ]
                            ]
                            ErrorModalProvider.pendingEntriesBox remainingEntries
                        ]
                    ],
                footer =
                    Html.div [
                        prop.className "swt:flex swt:w-full swt:flex-wrap swt:justify-end swt:gap-2"
                        prop.children [
                            if errorModal.queue.Length > 1 then
                                Html.button [
                                    prop.className "swt:btn swt:btn-neutral"
                                    prop.text "Dismiss All Errors"
                                    prop.onClick (fun _ -> errorModal.dismissAll ())
                                ]
                            Html.button [
                                prop.className "swt:btn swt:btn-primary"
                                prop.text batch.DismissLabel
                                prop.onClick (fun _ ->
                                    ErrorModalProvider.dismissBatch errorModal.dismissById batch)
                            ]
                        ]
                    ],
                className = "swt:max-w-4xl"
            )
        | Some(ErrorModalEntry.Cancelable request) ->
            let footerExtraButtons =
                if errorModal.queue.Length > 1 then
                    Html.button [
                        prop.className "swt:btn swt:btn-neutral"
                        prop.text "Dismiss All Errors"
                        prop.onClick (fun _ -> errorModal.dismissAll ())
                    ]
                else
                    Html.none

            CancelableErrorModal.Modal(
                isOpen = true,
                setIsOpen =
                    (fun isOpen ->
                        if not isOpen then
                            ErrorModalProvider.cancelCancelableRequest errorModal.dismissById request
                    ),
                request = request,
                onDismiss = (fun () -> ErrorModalProvider.dismissCancelableRequest errorModal.dismissById request),
                onCancel = (fun () -> ErrorModalProvider.cancelCancelableRequest errorModal.dismissById request),
                appendix = ErrorModalProvider.pendingEntriesBox remainingEntries,
                footerExtraButtons = footerExtraButtons
            )

    [<ReactComponent(true)>]
    static member ErrorModalProvider(children: ReactElement) =
        let queue, dispatch = React.useReducer (ErrorModalProvider.reducer, [])
        let currentEntry = queue |> List.tryHead

        let dismissById id = dispatch (DismissById id)
        let dismissBatchItem batchId itemId = dispatch (DismissBatchItem(batchId, itemId))

        let dismissAll () =
            queue |> List.iter ErrorModalProvider.resolveEntryForDismissAll
            dispatch DismissAll

        let dismissCurrent () =
            match currentEntry with
            | Some(ErrorModalEntry.Single request) ->
                ErrorModalProvider.dismissSingleRequest dismissById request
            | Some(ErrorModalEntry.Batch batch) ->
                ErrorModalProvider.dismissBatch dismissById batch
            | Some(ErrorModalEntry.Cancelable request) ->
                ErrorModalProvider.dismissCancelableRequest dismissById request
            | None -> ()

        let cancelCurrent () =
            match currentEntry with
            | Some(ErrorModalEntry.Cancelable request) ->
                ErrorModalProvider.cancelCancelableRequest dismissById request
            | _ -> dismissCurrent ()

        let contextValue: ErrorModalContext =
            React.useMemo (
                (fun _ -> {
                    current = currentEntry
                    queue = queue
                    enqueue = fun request -> dispatch (Enqueue(ErrorModalEntry.Single request))
                    enqueueMany =
                        fun requests ->
                            requests
                            |> List.map ErrorModalEntry.Single
                            |> EnqueueMany
                            |> dispatch
                    enqueueBatch =
                        fun batch ->
                            if batch.Errors.IsEmpty then
                                ()
                            else
                                dispatch (Enqueue(ErrorModalEntry.Batch batch))
                    enqueueCancelable =
                        fun request -> dispatch (Enqueue(ErrorModalEntry.Cancelable request))
                    report =
                        fun message ->
                            dispatch (Enqueue(ErrorModalEntry.Single(ErrorModalRequest.create(message))))
                    dismissCurrent = dismissCurrent
                    dismissById = dismissById
                    dismissBatchItem = dismissBatchItem
                    dismissAll = dismissAll
                    cancelCurrent = cancelCurrent
                }),
                [|
                    box currentEntry
                    box queue
                |]
            )

        Contexts.ErrorModal.ErrorModalCtx.Provider(
            contextValue,
            React.Fragment [
                children
                ErrorModalProvider.ErrorModalHost()
            ]
        )

    [<ReactComponent>]
    static member private EntryContent
        (
            showSingle: bool,
            showQueued: bool,
            showCancelable: bool,
            showBatch: bool
        ) =
        let errorModal = Contexts.ErrorModal.useErrorModal ()

        let enqueueSingle () =
            errorModal.enqueue (
                ErrorModalRequest.create(
                    "The renderer could not finish the requested operation.",
                    title = "Sample runtime error",
                    details = "This is a sample error used in Storybook."
                )
            )

        let enqueueMultiple () =
            errorModal.enqueueMany [
                ErrorModalRequest.create("The first queued error.", title = "Queued error 1")
                ErrorModalRequest.create("The second queued error.", title = "Queued error 2")
            ]

        let enqueueCancelable () =
            errorModal.enqueueCancelable (
                CancelableErrorModalRequest.create(
                    "A running task can be canceled before it completes.",
                    title = "Cancelable error",
                    onCancel = (fun () -> Browser.Dom.console.log ("Canceled sample error"))
                )
            )

        let enqueueBatch () =
            errorModal.enqueueBatch (
                ErrorModalBatch.create(
                    [
                        ErrorModalRequest.create(
                            "The first validation error is visible together with the others.",
                            title = "Visible error 1",
                            details = "Shared detail block for the first item.",
                            dismissLabel = "Dismiss error 1"
                        )
                        ErrorModalRequest.create(
                            "The second validation error can be dismissed independently.",
                            title = "Visible error 2",
                            dismissLabel = "Dismiss error 2"
                        )
                        ErrorModalRequest.create(
                            "The third validation error is also informational.",
                            title = "Visible error 3",
                            dismissLabel = "Dismiss error 3"
                        )
                    ],
                    title = "Multiple errors at once",
                    summary = "All of these errors belong to the same operation and are shown together."
                )
            )

        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:gap-2"
            prop.children [
                if showSingle then
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.text "Show Error"
                        prop.onClick (fun _ -> enqueueSingle ())
                    ]
                if showQueued then
                    Html.button [
                        prop.className "swt:btn swt:btn-neutral"
                        prop.text "Queue Errors"
                        prop.onClick (fun _ -> enqueueMultiple ())
                    ]
                if showCancelable then
                    Html.button [
                        prop.className "swt:btn"
                        prop.text "Show Cancelable Error"
                        prop.onClick (fun _ -> enqueueCancelable ())
                    ]
                if showBatch then
                    Html.button [
                        prop.className "swt:btn swt:btn-error"
                        prop.text "Show Multiple Errors"
                        prop.onClick (fun _ -> enqueueBatch ())
                    ]
            ]
        ]

    [<ReactComponent>]
    static member SingleEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.EntryContent(true, false, false, false))

    [<ReactComponent>]
    static member QueuedEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.EntryContent(false, true, false, false))

    [<ReactComponent>]
    static member CancelableEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.EntryContent(false, false, true, false))

    [<ReactComponent>]
    static member BatchEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.EntryContent(false, false, false, true))
