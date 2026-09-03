namespace Renderer.Components.Composite.ArcOpening

open Fable.Core
open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Renderer.Context.ArcOpeningContext
open Swate.Components.Composite.ArcOpening
open Swate.Components.Primitive.ErrorModal.Context

[<Erase; Mangle(false)>]
type Provider =

    [<ReactComponent>]
    static member Provider(children: ReactElement) =
        let isOpeningArc, setIsOpeningArcState = React.useState false
        let isOpeningArcRef = React.useRef false
        let errorModal = useErrorModalCtx ()
        let appState = Renderer.Context.AppStateContext.useAppStateCtx ()

        let setIsOpeningArc value = setIsOpeningArcState value

        let requestGate: Helper.RequestGate = {
            tryBegin =
                fun () ->
                    if isOpeningArcRef.current then
                        false
                    else
                        isOpeningArcRef.current <- true
                        true
            finish = fun () -> isOpeningArcRef.current <- false
        }

        let onOpenArcError =
            createErrorModalCallback errorModal.enqueue "Could not open ARC" appState

        let openArc () =
            Helper.openWithProgress
                requestGate
                Api.ipcArcVaultApi.pickDirectory
                (openArcByPath onOpenArcError)
                onOpenArcError
                setIsOpeningArc
            |> Promise.start

        let openArcByPathWithSharedProgress arcPath =
            Helper.openPathWithProgress requestGate arcPath (openArcByPath onOpenArcError) setIsOpeningArc
            |> Promise.start

        let controller = {
            isOpeningArc = isOpeningArc
            openArc = openArc
            openArcByPath = openArcByPathWithSharedProgress
        }

        ArcOpeningCtx.Provider(controller, React.Fragment [ children; Modals.OpeningArc(isOpeningArc) ])
