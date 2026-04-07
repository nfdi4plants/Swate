# Intention

Make API-Calls and state handling more robust by migrating to UseElmish hook. Should use calls to GitLabAPI with `Cmd.ofPromise.either` to ensure all exceptions are caught and mapped into the model, avoiding unhandled promise rejections and ensuring the UI can react to all outcomes of the async operations. Elmish model should be private to the component.

Success criteria for this plan:
- [ ] ExplorePanel state is managed by local Elmish model (no useEffect-driven load side effects for repo loading).
- [ ] Async API calls are started from Elmish commands and all exceptions are mapped into model messages.
- [ ] UI can represent loading, success, and error outcomes through model state.
- [ ] Elmish model and update flow stay private to the component module.

# Summary of findings

Verified Feliz.UseElmish syntax from the 3 resources and mapped it to your current ExplorePanel.

What is verified
1. [RESOLVED] The hook signature is `React.useElmish(init, update, dependencies)`, returning `model, dispatch`.
2. [RESOLVED] `init` returns `model, cmd` and `update` returns `model, cmd`.
3. [RESOLVED] Dependencies control re-initialization exactly like `React.useEffect`.
4. [RESOLVED] It is valid to combine useElmish with other hooks, but primary state flow should live in Elmish messages.
5. [RESOLVED] For async work, Elmish commands are the right place; `Cmd.OfPromise.either` is the robust pattern for success plus exception mapping. `Cmd.OfPromise.perform` is acceptable when only success mapping is required.

Local findings in your code
1. [RESOLVED] `ExplorePanel` currently still uses many `React.useState` values and a `useEffect`-driven load pipeline in `DataHubBrowser.fs`.
2. [RESOLVED] The current line `let model, dispatch = React.useElmish ()` is placeholder/invalid and needs `init/update/deps` in `DataHubBrowser.fs`.
3. [RESOLVED] A starter `Msg` with `LoadReposRequest/LoadReposReponse` exists in `DataHubBrowser.fs`, but is not yet wired to `ExplorePanel`.
4. [RESOLVED] Required request/result contracts already exist in `Types.fs`.

# Plan to migrate ExplorePanel to local Elmish model

1. [PENDING] Define a dedicated ExplorePanel Elmish module in `DataHubBrowser.fs` near the current `DatahubBrowserModel`.
Model should include at least all ExploreLoadRequest fields by storing either:
- Request: ExploreLoadRequest
or
- Equivalent individual fields (Target, SearchTerm, Page, PerPage, SortField, SortDirection, SelectedGroupId, IsAuthenticated, User)
and also UI result state: Repos, Pagination, Groups, GroupsLoaded, GroupsLoadError, IsLoading, Error, DraftSearchTerm.
Intention verification: Aligned. This is required to make state handling robust and private to component scope.

2. [PENDING] Replace `useEffect` loading with message-driven commands.
Use messages like:
- Initialize
- SetTab of ExploreTab
- SetSearchTerm of string
- SubmitSearch
- SetSortField of ExploreSortField
- ToggleSortDirection
- SetSelectedGroupId of int option
- SetPage of int
- ExternalReload
- LoadReposRequest of ExploreLoadRequest
- LoadReposResponse of Result<ExploreLoadResult, string>
Intention verification: Aligned. Removes ad-hoc side effects and centralizes transitions in update.

3. [PENDING] Centralize request construction in one helper.
Create buildRequest : Model -> ExploreLoadRequest so all load-triggering messages reuse one source of truth and always include auth/user/reload-sensitive data.
Intention verification: Aligned. Reduces inconsistent request state and improves reliability.

4. [PENDING] Implement `init` to bootstrap first load.
init should build initial model from props user and default filters, then return model plus Cmd.ofMsg (LoadReposRequest initialRequest).
Intention verification: Aligned. Ensures predictable initial state and first API call via Elmish flow.

5. [PENDING] Implement `update` with robust async handling.
On LoadReposRequest:
- set IsLoading true, clear Error
- run command with Cmd.OfPromise.either (or perform if you keep Result-only flow)
On LoadReposResponse (Ok):
- set Repos/Pagination/Groups/GroupsLoaded/GroupsLoadError
- set IsLoading false
- apply selected-group fallback logic
On LoadReposResponse (Error):
- set Error, clear repos/pagination, set IsLoading false
Intention verification: Strongly aligned. This directly satisfies exception-safe API handling and UI outcome handling.

6. [PENDING] Encode tab/filter behavior as pure state transitions.
For SetTab, SetSortField, ToggleSortDirection, SetSelectedGroupId, SubmitSearch:
- update model
- reset page to 1 where needed
- dispatch LoadReposRequest with rebuilt request
This removes duplicated setPage/load logic currently spread in handlers.
Intention verification: Aligned. Increases robustness by making behavior deterministic and testable.

7. [PENDING] Handle dependencies correctly in `useElmish`.
Use something like dependencies containing user and reloadTrigger so component reinitializes when auth context or external reload changes.
This replaces current useEffect dependency list in DataHubBrowser.fs.
Intention verification: Aligned. Prevents stale state when external auth/reload inputs change.

8. [PENDING] Keep render mostly unchanged, but read from model only.
Replace all local state reads with model fields and all setX calls with dispatch Msg.
Tabs/Filter/Pagination calls remain structurally the same, just wired through dispatch.
Intention verification: Aligned. Preserves UI while moving behavior to robust Elmish update flow.

9. [PENDING] Add regression tests for update logic.
At minimum:
- SetTab resets page and triggers load
- SubmitSearch uses draft term and resets page
- LoadReposResponse Ok updates repos/pagination/groups
- LoadReposResponse Error sets error and loading false
- YourOrganisations with no selected group and non-empty groups picks first group behavior
Intention verification: Aligned. Tests are necessary to prove robustness improvements and prevent regressions.

Recommended message and command shape for your case
1. [RESOLVED] Keep your current `LoadReposRequest/LoadReposResponse` pattern exactly.
2. [RESOLVED] For maximum robustness, prefer `Cmd.OfPromise.either` around `loadRepos request` and map exceptions into `LoadReposResponse (Error ex.Message)`.
3. [RESOLVED] If you want `perform` style, use it only when you intentionally accept exception bubbling and only map the returned `Result` into response msg.

# Current status against intention

- [PARTIALLY RESOLVED] Planning and syntax verification are complete.
- [PENDING] Migration implementation in `DataHubBrowser.fs`.
- [PENDING] Regression tests for the new local Elmish update flow.

If you want, I can implement this migration directly in DataHubBrowser.fs in one pass next, including the first test file skeleton for the update function.