namespace Components

open Swate.Components
open Fable.Core.JsInterop
open ARCtrl

type TermSearchImplementation =

    static member Main
        (
            term: Term option,
            setTerm: Term option -> unit,
            model: Model.Model,
            ?parentId: string,
            ?onFocus,
            ?classNames,
            ?autoFocus,
            ?fullwidth,
            ?portalTermDropdown
        ) =

        Swate.Components.TermSearch.TermSearch(
            setTerm,
            term,
            ?parentId = parentId,
            advancedSearch = !^true,
            ?onFocus = onFocus,
            ?autoFocus = autoFocus,
            ?classNames = classNames,
            disableDefaultSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
            disableDefaultAllChildrenSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
            disableDefaultParentSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
            termSearchQueries = model.PersistentStorageState.TIBQueries.TermSearch,
            parentSearchQueries = model.PersistentStorageState.TIBQueries.ParentSearch,
            allChildrenSearchQueries = model.PersistentStorageState.TIBQueries.AllChildrenSearch,
            showDetails = true,
            portalModals = Browser.Dom.document.body,
            ?fullwidth = fullwidth,
            ?portalTermDropdown = portalTermDropdown
        )