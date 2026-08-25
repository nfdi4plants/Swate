module Swate.Components.Hooks.UseKeyedState


open Feliz
open Fable.Core
open Swate.Components


type React with

    // SOURCE:
    // - https://react.dev/learn/you-might-not-need-an-effect#resetting-all-state-when-a-prop-changes
    // - https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes

    /// Keep local state that must reset when an associated "key" changes. For example, resetting
    /// the current page when a table dataset is replaced, or resetting the selected row when
    /// switching datasets. React's "adjust state during render" pattern (setState inside a
    /// conditional) achieves the same, but returns stale values for one render pass and caused
    /// render-order issues in combination with child effects.
    ///
    /// Instead, store the key together with the value and DERIVE the value during render:
    /// if the stored key no longer matches the current key, fall back to the initial value.
    /// The correct value is returned in the very first render pass after a key change, without
    /// any setState during render (StrictMode and React Compiler friendly).
    ///
    /// Trade-offs:
    /// - The stale value stays in the state cell under its old key. If the key later changes
    ///   back, the previous value for that key is returned instead of the initial value.
    /// - The key is compared with structural equality on every render. Prefer small, primitive
    ///   keys (ids, names, tuples) over large structures.
    /// - setValue is recreated whenever the key changes (useCallback). A stale setValue from a
    ///   previous render writes the old key and is neutralized by the key check.
    [<Hook>]

    static member useKeyedState<'value, 'key when 'key: equality>(initialValue: 'value, key: 'key) =
        let state, setState = React.useState (KeyedState.init(initialValue, key))

        let value =
            if state.key = key then
                state.state
            else
                initialValue

        let setValue (value: 'value) =
            setState (KeyedState.init(value, key))

        let setValueStable = React.useCallback (setValue, [| key |])

        value, setValueStable
