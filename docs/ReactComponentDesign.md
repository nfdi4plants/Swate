# Guidelines for isolated React components (`src/Components`)

Components in `src/Components` are expected to be reusable and application-agnostic.

# File Rules

- **Component folder**: Each component should be placed in a folder named after the component use context.
    - e.g. `src/Components/src/TermSearch` contains `TermSearch.tsx`, `TermSearchConfigProvider.fs` and `TermSearchConfigSetter.fs`
    - e.g. `src/Components/src/Authentication` contains `Authentication.fs`, `AccountManager.fs`, `Helper.fs` and `Types.fs`
    - e.g. higher level nested components MAY be placed in subfolders and add the subfolder name to the namespace, e.g. `src/Components/src/Notes/NoteSearch/src/Components/src/Notes/NoteSearch/NoteSearchComponent.fs` with namespace `namespace Swate.Components.Notes.NoteSearch`.
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
- **Types**: `private` types that are only used within a single component file MUST be defined in the same file. Public types that are shared across multiple files MUST be defined in a separate `Types.fs` file.
- **Helper functions**: Helper functions MUST NOT be defined as static members of the component class. They MAY be defined in a separate file, or as **private** module `module <FileName>Helper` within the component file.
- **React context**: If a component needs to share state or configuration with a React context. It MUST define the base context in a separate file `**/<ComponentName>/Context.fs` or `**/<ComponentName>/Contexts/<ContextName>.fs` or `**/<ComponentName>/<ContextName>.fs` if there are multiple contexts associated with the component, with the following properties.
    - File namespace MUST follow folder structure and be named `Swate.Components.<Path>.<ContextName>`.
    - MAY contain the relevant public types.
    - MAY contain helper functions inside a MAYBE public module. This module should be named `<ContextName>Helper`.
    - It MUST NOT contain any React components.
    - It MUST contain the `React.createContext` implementation in PascalCase with the suffix `Ctx`. For example, `ExampleCtx`.
    - It MUST contain a hook to easily bind to the context. This hook MUST be named `use<ContextName>Ctx`. For example, `useExampleCtx`.
    - Example for an context "Example" for a component "ExampleComponent":

        ```fsharp
        module Swate.Components.ExampleComponent.Context

        // ..

        type PublicExampleType = {
            // ...
        }

        module ContextHelper =
            // ...

        let ExampleCtx = React.createContext<PublicExampleType> (PublicExampleType.init ())

        [<Hook>]
        let useExampleCtx () = React.useContext ExampleCtx
        ```