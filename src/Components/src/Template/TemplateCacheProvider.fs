namespace Swate.Components.Template

open System
open ARCtrl
open Elmish
open Fable.Core
open Feliz
open Feliz.UseElmish
open Swate.Components
open Swate.Components.Template.Types

module TemplateHelper = Swate.Components.Template.Helper
module TemplateCacheCtx = Swate.Components.Template.TemplateCacheContext

module private TemplateCacheProviderModel =

    type Msg =
        | LoadTemplatesRequest of forceRefresh: bool
        | LoadTemplatesResponse of requestId: Guid * result: Result<Template[], string>

    type State = {
        CacheState: TemplateCacheState
        LoadState: TemplateLoadState
        LatestFetchId: Guid option
        IsRefreshing: bool
        RefreshError: string option
    }

    let private sortTemplates (templates: Template[]) =
        templates |> Array.sortBy (fun template -> template.Name)

    let private hasVisibleTemplates (loadState: TemplateLoadState) =
        match loadState with
        | TemplateLoadState.Loaded templates -> templates.Length > 0
        | _ -> false

    let init (cacheState: TemplateCacheState) =
        let loadState =
            match TemplateHelper.tryReadTemplatesFromCache cacheState with
            | Ok(Some templates) -> templates |> sortTemplates |> TemplateLoadState.Loaded
            | _ -> TemplateLoadState.Loading

        {
            CacheState = cacheState
            LoadState = loadState
            LatestFetchId = None
            IsRefreshing = false
            RefreshError = None
        },
        Cmd.ofMsg (LoadTemplatesRequest false)

    let update (loadTemplates: unit -> Async<Result<Template[], string>>) msg state =
        match msg with
        | LoadTemplatesRequest forceRefresh ->
            let cachedTemplatesResult =
                TemplateHelper.tryReadTemplatesFromCache state.CacheState

            let shouldFetchFresh =
                forceRefresh
                || Result.isError cachedTemplatesResult
                || TemplateHelper.shouldFetchFresh false state.CacheState DateTime.UtcNow

            if shouldFetchFresh then
                let requestId = Guid.NewGuid()

                let nextLoadState =
                    if hasVisibleTemplates state.LoadState then
                        state.LoadState
                    else
                        TemplateLoadState.Loading

                let nextState = {
                    state with
                        LatestFetchId = Some requestId
                        LoadState = nextLoadState
                        IsRefreshing = true
                        RefreshError = None
                }

                let cmd =
                    Cmd.OfAsync.either
                        loadTemplates
                        ()
                        (fun result -> LoadTemplatesResponse(requestId, result))
                        (fun error -> LoadTemplatesResponse(requestId, Error error.Message))

                nextState, cmd
            else
                match cachedTemplatesResult with
                | Ok(Some templates) ->
                    {
                        state with
                            LoadState = templates |> sortTemplates |> TemplateLoadState.Loaded
                            IsRefreshing = false
                            RefreshError = None
                    },
                    Cmd.none
                | Ok None ->
                    {
                        state with
                            LoadState = TemplateLoadState.Loading
                            IsRefreshing = false
                            RefreshError = None
                    },
                    Cmd.none
                | Error message ->
                    {
                        state with
                            CacheState = TemplateCacheState.Empty
                            LoadState = TemplateLoadState.LoadError message
                            IsRefreshing = false
                            RefreshError = None
                    },
                    Cmd.none
        | LoadTemplatesResponse(requestId, result) when state.LatestFetchId <> Some requestId -> state, Cmd.none
        | LoadTemplatesResponse(_, result) ->
            match result with
            | Ok templates ->
                let sortedTemplates = templates |> sortTemplates

                {
                    state with
                        CacheState = TemplateHelper.toCacheState sortedTemplates DateTime.UtcNow
                        LoadState = TemplateLoadState.Loaded sortedTemplates
                        LatestFetchId = None
                        IsRefreshing = false
                        RefreshError = None
                },
                Cmd.none
            | Error message ->
                if hasVisibleTemplates state.LoadState then
                    {
                        state with
                            LatestFetchId = None
                            IsRefreshing = false
                            RefreshError = Some message
                    },
                    Cmd.none
                else
                    {
                        state with
                            LoadState = TemplateLoadState.LoadError message
                            LatestFetchId = None
                            IsRefreshing = false
                            RefreshError = None
                    },
                    Cmd.none

[<Erase; Mangle(false)>]
type TemplateCacheProvider =

    [<ReactComponent>]
    static member TemplateCacheProvider
        (loadTemplates: unit -> Async<Result<Template[], string>>, children: ReactElement)
        =

        let cacheState, setCacheState =
            React.useLocalStorage (TemplateHelper.CacheStorageKey, TemplateCacheState.Empty)

        let model, dispatch =
            React.useElmish (
                (fun () -> TemplateCacheProviderModel.init cacheState),
                TemplateCacheProviderModel.update loadTemplates,
                [||]
            )

        React.useEffect (
            (fun () ->
                if cacheState <> model.CacheState then
                    setCacheState model.CacheState
            ),
            [| box cacheState; box model.CacheState |]
        )

        let contextValue: TemplateCacheCtx.TemplateCacheContext = {
            LoadState = model.LoadState
            IsRefreshing = model.IsRefreshing
            RefreshError = model.RefreshError
            RefreshTemplates = fun () -> dispatch (TemplateCacheProviderModel.LoadTemplatesRequest true)
        }

        TemplateCacheCtx.TemplateCacheCtx.Provider(contextValue, children)