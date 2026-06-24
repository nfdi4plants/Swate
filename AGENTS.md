## ReactComponent design rules

- **Purpose**: These guidelines are for components in `src/Components`, which are meant to be reusable and application-agnostic. They should not contain app-specific logic or state coupling.
- Read the full guidelines in [docs/ReactComponentDesign.md](docs/ReactComponentDesign.md) for detailed rules on file structure, syntax, and design principles for components in `src/Components`.

## Icon Convention rules

- Use `swt:iconify` with fully qualified icon classes (prefer `swt:fluent--...`) for new icons.

## Test Convention rules

- You MUST NOT do source file testing to check if certain code is present. This is a brittle testing strategy that will break if the code is refactored, even if the behavior is unchanged. Instead, test the behavior directly by calling the relevant functions and checking their outputs or side effects.

## Fable Convention rules

- You MUST implement reusable bindings for JavaScript native libraries/functions. Dynamic Access using `?` can be brittle and SHOULD be avoided in favor of strongly typed bindings.
- Not all F# types translate to native JavaScript types. This can be important in certain performance-sensitive scenarios as well as outward facing APIs, which are meant to be usable from JavaScript apps. (The transpiled Swate.Components project is also published to npm.)
    - Anonymous record types and classes with the `[<POJO>]` attribute translate to native JavaScript objects.
    - Arrays/ResizeArrays translate to native JavaScript arrays.
    - for promised use Fable.Promise's `promise {}` computation expression. It can be converted easily from and to F#s `async {}` computation expression.
