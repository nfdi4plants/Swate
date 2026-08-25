module Swate.Components.Hooks.UseResettableState


open Feliz
open Fable.Core

// SOURCE: https://react.dev/learn/you-might-not-need-an-effect#resetting-all-state-when-a-prop-changes

// Storing information from previous renders like this can be hard to understand, but it’s better than updating the same state in an Effect. In the above example, setSelection is called directly during a render. React will re-render the List immediately after it exits with a return statement. React has not rendered the List children or updated the DOM yet, so this lets the List children skip rendering the stale selection value.

// When you update a component during rendering, React throws away the returned JSX and immediately retries rendering. To avoid very slow cascading retries, React only lets you update the same component’s state during a render. If you update another component’s state during a render, you’ll see an error. A condition like items !== prevItems is necessary to avoid loops. You may adjust state like this, but any other side effects (like changing the DOM or setting timeouts) should stay in event handlers or Effects to keep components pure.

// Although this pattern is more efficient than an Effect, most components shouldn’t need it either. No matter how you do it, adjusting state based on props or other state makes your data flow more difficult to understand and debug. Always check whether you can reset all state with a key or calculate everything during rendering instead. For example, instead of storing (and resetting) the selected item, you can store the selected item ID:

// Note @Freymaurer: I implemented this for the Select component. It uses index based selection.

type React with

    [<Hook>]
    static member useResettableState<'T, 'K when 'K: equality>(initialValue: 'T, key: 'K) =
        let state, setState = React.useState(initialValue)
        let previousKey, setPreviousKey = React.useState(key)

        if key <> previousKey then
            setPreviousKey key
            setState initialValue

        state, setState

    [<Hook>]
    static member useResettableState(initialValue: 'T, dependency: 'K, compareFn: 'K -> 'K -> bool) =
        let state, setState = React.useState(initialValue)
        let previousDependency, setPreviousDependency = React.useState(dependency)

        if not (compareFn dependency previousDependency) then
            setPreviousDependency dependency
            setState initialValue

        state, setState
