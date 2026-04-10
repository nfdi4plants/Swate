# Guidelines for isolated React components (`src/Components`)

Components in `src/Components` are expected to be reusable and application-agnostic.

# File Rules

- **Component folder**: Each component should be placed in a folder named after the component use context.
    - e.g. `src/Components/src/TermSearch` contains `TermSearch.tsx`, `TermSearchConfigProvider.fs` and `TermSearchConfigSetter.fs`
    - e.g. `src/Components/src/Authentication` contains `Authentication.fs`, `AccountManager.fs`, `Helper.fs` and `Types.fs`
- **PascalCase file names**: `MyComponent.tsx` for the component "MyComponent" and `MyComponent.stories.tsx` for its Storybook tests.
- **File namespace**: The file namespace should match the folder structure. For example:

    -  `src/Components/src/Authentication/Authentication.fs` should have:

        ```fsharp
        namespace Swate.Components.Authentication

        [<Erase; Mangle(false)>]
        type Authentication =
            // ...
        ```

    - `src/Components/src/Authentication/Types.fs` should have:

        ```fsharp
        module Swate.Components.Authentication.Types

        // ...
        ```
- **Colocated stories**: Storybook files must be colocated with the component and named `<Component>.stories.tsx`.

# Syntax Rules

- **PascalCase**: MUST use PascalCase for component and object names.
- **CamelCase**: MUST use camelCase for props and functions.
- **`[<ReactComponent>]`**: If a component returns a `ReactElement`, it MUST be decorated with `[<ReactComponent>]`.
- **Components as static members**: Any components SHOULD be defined as static members of a class. They MUST use tupled args. This allows us to use optional params with `?optionalParam` syntax, as well as named params with `namedParam = namedParamValue` syntax. These Classes MUST have the `[<Erase; Mangle(false)>]` attribute to improve interop with native TypeScript.

    ```fsharp
    [<Erase; Mangle(false)>]
    type MyComponent =
        [<ReactComponent>]
        static member MyComponent () : ReactElement =
            // ...
    ```
- **Subcomponents**: Larger components MUST be split into smaller logical subcomponents. These subcomponents MAY be defined as static members of the main component class, or as separate component-files in the same folder.
- **Helper functions**: Helper functions MUST NOT be defined as static members of the component class. They MAY be defined in a separate file, or as private module `module <FileName>Helper` within the component file.

# Design Principles

- **No app-state coupling**: Components MUST be designed to be reusable and not coupled to any specific application state or workflow. They should receive all necessary data and handlers via props.
- **No app-side effects**: do not call app-specific services directly; pass handlers/callbacks via props.

#