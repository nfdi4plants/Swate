namespace Elmish.HMR

open Fable.Core.JsInterop
open Browser
open Elmish

[<RequireQualifiedAccess>]
module Program =

    type Msg<'msg> =
        | UserMsg of 'msg
        | Stop

    module Internal =

        type Platform =
            | Browser
            | ReactNative

        let platform =
            match window?navigator?product with
            | "ReactNative" -> ReactNative
            | _ -> Browser

        let tryRestoreState (hmrState : 'model ref) (data : obj) =
            match platform with
            | ReactNative ->
                let savedHmrState = window?react_native_elmish_hmr_state
                if not (isNull (box savedHmrState)) then
                    hmrState.Value <- savedHmrState

            | Browser ->
                if not (isNull data) && not (isNull data?hmrState) then
                    hmrState.Value <- data?hmrState

        let saveState (data : obj) (hmrState : 'model) =
            match platform with
            | ReactNative ->
                window?react_native_elmish_hmr_state <- hmrState
            | Browser ->
                data?hmrState <- hmrState


    let inline private updateElmish_HMR_Count () =
        window?Elmish_HMR_Count <-
            if isNull window?Elmish_HMR_Count then
                0
            else
                window?Elmish_HMR_Count + 1

    /// Start the dispatch loop with `'arg` for the init() function.
    let inline runWith (arg: 'arg) (program: Program<'arg, 'model, 'msg, 'view>) =
#if !DEBUG
        Program.runWith arg program
#else
        let hmrState : 'model ref = ref (unbox null)

        match Bundler.current with
        | Some current ->
            updateElmish_HMR_Count ()

            let hmrDataObject =
                match current with
                | Bundler.Vite ->
                    HMR.Vite.hot.accept()
                    HMR.Vite.hot.data

                | Bundler.WebpackESM ->
                    HMR.Webpack.hot.accept()
                    HMR.Webpack.hot.data

                | Bundler.WebpackCJS_and_Parcel ->
                    HMR.Parcel.hot.accept()
                    HMR.Parcel.hot.data

            Internal.tryRestoreState hmrState hmrDataObject

        | None ->
            ()

        let mapUpdate userUpdate (msg : Msg<'msg>) (model : 'model) =
            let newModel,cmd =
                match msg with
                | UserMsg userMsg ->
                    userUpdate userMsg model

                | Stop ->
                    model, Cmd.none

            hmrState.Value <- newModel

            newModel
            , Cmd.map UserMsg cmd

        let createModel (model, cmd) =
            model, cmd

        let mapInit userInit args =
            if isNull (box hmrState.Value) then
                let (userModel, userCmd) = userInit args

                userModel
                , Cmd.map UserMsg userCmd
            else
                hmrState.Value, Cmd.none

        let mapSetState userSetState (userModel : 'model) dispatch =
            userSetState userModel (UserMsg >> dispatch)

        let hmrSubscription =
            let handler dispatch =
                match Bundler.current with
                | Some current ->
                    match current with
                    | Bundler.Vite ->
                        HMR.Vite.hot.dispose(fun data ->
                            Internal.saveState data hmrState.Value

                            dispatch Stop
                        )

                    | Bundler.WebpackESM ->
                        HMR.Webpack.hot.dispose(fun data ->
                            Internal.saveState data hmrState.Value

                            dispatch Stop
                        )

                    | Bundler.WebpackCJS_and_Parcel ->
                        HMR.Parcel.hot.dispose(fun data ->
                            Internal.saveState data hmrState.Value

                            dispatch Stop
                        )

                | None ->
                    ()
                // nothing to cleanup
                {new System.IDisposable with member _.Dispose () = ()}

            [["Hmr"], handler ]

        let mapSubscribe subscribe model =
            Sub.batch [
                subscribe model |> Sub.map "HmrUser" UserMsg
                hmrSubscription
            ]

        let mapView userView model dispatch =
            userView model (UserMsg >> dispatch)

        let mapTermination (predicate, terminate) =
            let mapPredicate =
                function
                | UserMsg msg -> predicate msg
                | Stop -> true

            mapPredicate, terminate

        program
        |> Program.map mapInit mapUpdate mapView mapSetState mapSubscribe mapTermination
        |> Program.runWith arg
#endif

    /// Start the dispatch loop with `unit` for the init() function.
    let inline run (program: Program<unit, 'model, 'msg, 'view>) =
#if !DEBUG
        Program.run program
#else
        runWith () program
#endif

    (*
        Shadow: Fable.Elmish.React
    *)

    let inline withReactBatched placeholderId (program: Program<_,_,_,_>) =
#if !DEBUG
        Elmish.React.Program.withReactBatched placeholderId program
#else
        Elmish.React.Program.Internal.withReactBatchedUsing lazyView2With placeholderId program
#endif

    let inline withReactSynchronous placeholderId (program: Program<_,_,_,_>) =
#if !DEBUG
        Elmish.React.Program.withReactSynchronous placeholderId program
#else
        Elmish.React.Program.Internal.withReactSynchronousUsing lazyView2With placeholderId program
#endif

    let inline withReactHydrate placeholderId (program: Program<_,_,_,_>) =
#if !DEBUG
        Elmish.React.Program.withReactHydrate placeholderId program
#else
        Elmish.React.Program.Internal.withReactHydrateUsing lazyView2With placeholderId program
#endif

    [<System.Obsolete("Use withReactBatched")>]
    let inline withReact placeholderId (program: Program<_,_,_,_>) =
#if !DEBUG
        Elmish.React.Program.withReactBatched placeholderId program
#else
        Elmish.React.Program.Internal.withReactBatchedUsing lazyView2With placeholderId program
#endif

    [<System.Obsolete("Use withReactSynchronous")>]
    let inline withReactUnoptimized placeholderId (program: Program<_,_,_,_>) =
#if !DEBUG
        Elmish.React.Program.withReactSynchronous placeholderId program
#else
        Elmish.React.Program.Internal.withReactSynchronousUsing lazyView2With placeholderId program
#endif
