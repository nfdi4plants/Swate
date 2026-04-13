namespace Swate.Components.ErrorModal

open Feliz
open Fable.Core
open Swate.Components
open Swate.Components.ErrorModal


[<Erase; Mangle(false)>]
type ErrorModalProvider =

    static member private PendingEntryLabel (entry: ErrorModalEntry) =
        match entry with
        | ErrorModalEntry.Single request -> request.Title
        | ErrorModalEntry.Batch batch -> $"{batch.Title} ({batch.Errors.Length} errors)"

    [<ReactComponent>]
    static member private PendingEntriesBox (remainingEntries: ErrorModalEntry list) =
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
                                    prop.text (ErrorModalProvider.PendingEntryLabel pendingEntry)
                                ]
                        ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member private BulkDismissButton (onClick: unit -> unit) =
        Html.button [
            prop.className "swt:btn swt:btn-neutral"
            prop.text "Dismiss Related Errors"
            prop.onClick (fun _ -> onClick ())
        ]

    [<ReactComponent>]
    static member private RequestCard
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
                                ErrorModal.BodyBlock request
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:justify-end swt:gap-2"
                    prop.children [
                        for action in request.Actions do
                            ErrorModal.ActionButton action
                        Html.button [
                            prop.className "swt:btn swt:btn-primary"
                            prop.text request.DismissLabel
                            prop.onClick (fun _ ->
                                Helper.dismissBatchItemRequest(dismissBatchItem, batch, request))
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member ErrorModalHost() =
        let errorModal = ErrorModal.Context.useErrorModal ()
        let currentEntry = errorModal.current
        let currentScopeEntries =
            match currentEntry with
            | Some entry -> Helper.entriesInScope entry.ScopeId errorModal.queue
            | None -> []
        let hasAdditionalScopeEntries = currentScopeEntries.Length > 1

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
                            Helper.dismissSingleRequest errorModal.dismissById request
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
                            ErrorModal.BodyBlock request
                            ErrorModalProvider.PendingEntriesBox remainingEntries
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
                                        ErrorModal.ActionButton action
                                    if hasAdditionalScopeEntries then
                                        ErrorModalProvider.BulkDismissButton errorModal.dismissAll
                                    Html.button [
                                        prop.className "swt:btn swt:btn-primary"
                                        prop.text request.DismissLabel
                                        prop.onClick (fun _ ->
                                            Helper.dismissSingleRequest errorModal.dismissById request)
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
                            Helper.dismissBatch errorModal.dismissById batch
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
                                        ErrorModalProvider.RequestCard(errorModal.dismissBatchItem, batch, request)
                                ]
                            ]
                            ErrorModalProvider.PendingEntriesBox remainingEntries
                        ]
                    ],
                footer =
                    Html.div [
                        prop.className "swt:flex swt:w-full swt:flex-wrap swt:justify-end swt:gap-2"
                        prop.children [
                            if hasAdditionalScopeEntries then
                                ErrorModalProvider.BulkDismissButton errorModal.dismissAll
                            Html.button [
                                prop.className "swt:btn swt:btn-primary"
                                prop.text batch.DismissLabel
                                prop.onClick (fun _ ->
                                    Helper.dismissBatch errorModal.dismissById batch)
                            ]
                        ]
                    ],
                className = "swt:max-w-4xl"
            )

    [<ReactComponent(true)>]
    static member ErrorModalProvider(children: ReactElement) =
        let queue, dispatch = React.useReducer (Helper.reducer, [])
        let currentEntry = queue |> List.tryHead

        let dismissById id = dispatch (DismissById id)
        let dismissBatchItem batchId itemId = dispatch (DismissBatchItem(batchId, itemId))

        let dismissAll () =
            let targets =
                match currentEntry with
                | Some entry -> Helper.entriesInScope entry.ScopeId queue
                | None -> []

            let targetIds = targets |> List.map _.Id |> Set.ofList

            if not targetIds.IsEmpty then
                targets |> List.iter Helper.resolveEntryForScopedDismiss
                dispatch (DismissManyByIds targetIds)

        let dismissCurrent () =
            match currentEntry with
            | Some(ErrorModalEntry.Single request) ->
                Helper.dismissSingleRequest dismissById request
            | Some(ErrorModalEntry.Batch batch) ->
                Helper.dismissBatch dismissById batch
            | None -> ()

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
                    report =
                        fun message ->
                            dispatch (Enqueue(ErrorModalEntry.Single(ErrorModalRequest.create(message))))
                    dismissCurrent = dismissCurrent
                    dismissById = dismissById
                    dismissBatchItem = dismissBatchItem
                    dismissAll = dismissAll
                }),
                [|
                    box currentEntry
                    box queue
                |]
            )

        ErrorModal.Context.ErrorModalCtx.Provider(
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
            showBatch: bool
        ) =
        let errorModal = ErrorModal.Context.useErrorModal ()

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
                if showBatch then
                    Html.button [
                        prop.className "swt:btn swt:btn-error"
                        prop.text "Show Multiple Errors"
                        prop.onClick (fun _ -> enqueueBatch ())
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private ScopedEntryContent() =
        let errorModal = ErrorModal.Context.useErrorModal ()

        let enqueueScopedQueue () =
            errorModal.enqueue (
                ErrorModalRequest.create(
                    "The visible ARC A error should dismiss together with other ARC A entries only.",
                    title = "ARC A error",
                    scopeId = "arc-a"
                )
            )

            errorModal.enqueue (
                ErrorModalRequest.create(
                    "This queued ARC A error should dismiss together with the visible ARC A entry.",
                    title = "ARC A follow-up error",
                    scopeId = "arc-a"
                )
            )

            errorModal.enqueue (
                ErrorModalRequest.create(
                    "This ARC B error should remain queued after ARC A related errors are dismissed.",
                    title = "ARC B error",
                    scopeId = "arc-b"
                )
            )

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-primary"
                    prop.text "Queue Scoped Errors"
                    prop.onClick (fun _ -> enqueueScopedQueue ())
                ]
            ]
        ]

    [<ReactComponent>]
    static member SingleEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.EntryContent(true, false, false))

    [<ReactComponent>]
    static member QueuedEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.EntryContent(false, true, false))

    [<ReactComponent>]
    static member BatchEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.EntryContent(false, false, true))

    [<ReactComponent>]
    static member ScopedQueueEntry() =
        ErrorModalProvider.ErrorModalProvider(ErrorModalProvider.ScopedEntryContent())
