module Renderer.MainSyncedState

open Fable.Core
open Feliz

type MainSyncedStateConfig<'T> = {
    initial: 'T
    load: unit -> JS.Promise<'T>
    subscribe: ('T -> unit) -> unit -> unit
    onError: exn -> unit
    dependencies: obj[]
}

type MainSyncedState<'T> = {
    state: 'T
    isLoading: bool
    refresh: unit -> unit
}

[<Hook>]
let useMainSyncedState<'T> (config: MainSyncedStateConfig<'T>) : MainSyncedState<'T> =
    let state, setState = React.useState config.initial
    let isLoading, setIsLoading = React.useState true
    let activeTokenRef = React.useRef 0
    let liveUpdateVersionRef = React.useRef 0
    let requestVersionRef = React.useRef 0

    let loadSnapshot =
        React.useCallback (
            (fun () ->
                let activeToken = activeTokenRef.current
                let liveUpdateVersionAtStart = liveUpdateVersionRef.current
                let requestVersion = requestVersionRef.current + 1
                requestVersionRef.current <- requestVersion
                setIsLoading true

                promise {
                    try
                        let! snapshot = config.load ()
                        let requestIsCurrent = requestVersionRef.current = requestVersion
                        let componentIsActive = activeTokenRef.current = activeToken
                        let noLiveUpdateArrived = liveUpdateVersionRef.current = liveUpdateVersionAtStart

                        if componentIsActive && requestIsCurrent && noLiveUpdateArrived then
                            setState snapshot
                            setIsLoading false
                        elif componentIsActive && requestIsCurrent then
                            setIsLoading false
                    with e ->
                        let requestIsCurrent = requestVersionRef.current = requestVersion
                        let componentIsActive = activeTokenRef.current = activeToken
                        let noLiveUpdateArrived = liveUpdateVersionRef.current = liveUpdateVersionAtStart

                        if componentIsActive && requestIsCurrent && noLiveUpdateArrived then
                            setIsLoading false
                            config.onError e
                        elif componentIsActive && requestIsCurrent then
                            setIsLoading false
                }
                |> Promise.start),
            config.dependencies
        )

    React.useEffect (
        (fun () ->
            activeTokenRef.current <- activeTokenRef.current + 1
            let activeToken = activeTokenRef.current

            let dispose =
                config.subscribe (fun nextState ->
                    if activeTokenRef.current = activeToken then
                        liveUpdateVersionRef.current <- liveUpdateVersionRef.current + 1
                        setState nextState
                        setIsLoading false
                )

            loadSnapshot ()

            fun () ->
                activeTokenRef.current <- activeTokenRef.current + 1
                dispose ()),
        config.dependencies
    )

    {
        state = state
        isLoading = isLoading
        refresh = loadSnapshot
    }
